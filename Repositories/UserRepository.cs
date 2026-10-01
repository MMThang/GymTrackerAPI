using Dapper;
using GymTracker.Entities;
using GymTracker.Exceptions;
using GymTracker.Interfaces;
using GymTracker.Responses;
using Npgsql;
using System.Net.Mail;
using System.Security.Cryptography;

namespace GymTracker.Repositories
{
    public class UserRepository : IUser
    {
        private readonly IConfiguration _configuration;
        private readonly IRefreshToken _refreshTokenRepository;
        private readonly IEmailService _emailService;

        private const int MaxOtpAttempts = 5;
        private const int VerificationCodeExpirationMinutes = 5;
        private const int ResendCooldownSeconds = 60;

        public UserRepository(
            IConfiguration config,
            IRefreshToken refreshTokenRepository,
            IEmailService emailService)
        {
            _configuration = config;
            _refreshTokenRepository = refreshTokenRepository;
            _emailService = emailService;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("WebApiDatabase"));
        }

        public async Task<SendingOTPEmailResponse> sendingOTPEmail(string email, string password, string confirmPassword)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            if (!MailAddress.TryCreate(normalizedEmail, out _))
            {
                throw new ArgumentOutOfRangeException(nameof(email), "Invalid email format");
            }
            if (password.Length < 6 || confirmPassword.Length < 6)
            {
                throw new ArgumentOutOfRangeException(nameof(password), "Password minimum 6 characters");
            }
            if (password != confirmPassword)
            {
                throw new ArgumentException("Passwords do not match");
            }

            await using var connection = GetConnection();
            var existingUser = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"Email\" = @Email", new { Email = normalizedEmail });
            if (existingUser != null)
            {
                throw new InvalidOperationException("An email verification code has already been sent to this email");
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            var userId = Guid.NewGuid();

            // Generate a 6-digit OTP and store only its hash, never the plaintext.
            string otp = GenerateOtp();
            var now = DateTime.UtcNow;

            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            // Persist the user + hashed OTP before sending the email, so we
            // never send an OTP that wasn't stored. The transaction keeps the
            // user and its verification code atomic.
            await connection.ExecuteAsync(
                """
                INSERT INTO "Users" ("UserId", "Email", "EmailVerified", "Password", "Phone", "RegisterDate", "Username")
                VALUES (@UserId, @Email, false, @Password, NULL, @RegisterDate, NULL)
                """,
                new
                {
                    UserId = userId,
                    Email = normalizedEmail,
                    Password = hashedPassword,
                    RegisterDate = DateOnly.FromDateTime(now)
                },
                transaction);

            await connection.ExecuteAsync(
                """
                INSERT INTO "EmailVerificationCodes" ("Id", "UserId", "OTPHash", "ExpiresAt", "UsedAt", "CreatedAt", "Attempts")
                VALUES (@Id, @UserId, @OTPHash, @ExpiresAt, NULL, @CreatedAt, 0)
                """,
                new
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    OTPHash = BCrypt.Net.BCrypt.HashPassword(otp),
                    ExpiresAt = now.AddMinutes(VerificationCodeExpirationMinutes),
                    CreatedAt = now,
                    Attempts = 0
                },
                transaction);

            await transaction.CommitAsync();

            await _emailService.SendVerificationCodeAsync(
                normalizedEmail,
                otp
            );

            return new SendingOTPEmailResponse
            {
                ResendCooldownTimeStamp = now.AddSeconds(ResendCooldownSeconds)
            };
        }

        public async Task<SendingOTPEmailResponse> resendVerificationCodeAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            await using var connection = GetConnection();

            var user = await connection.QueryFirstOrDefaultAsync<User>(
                """SELECT * FROM "Users" WHERE "Email" = @Email""",
                new { Email = normalizedEmail });
            if (user == null)
            {
                throw new KeyNotFoundException(
                    "User not found."
                );
            }

            if (user.EmailVerified)
            {
                throw new InvalidOperationException(
                    "Email is already verified."
                );
            }

            // Find the most recently issued, still-active code so the cooldown
            // is measured against the last email that was actually sent.
            var latestCode = await connection.QueryFirstOrDefaultAsync<EmailVerificationCode>(
                """
                SELECT * FROM "EmailVerificationCodes"
                WHERE "UserId" = @UserId AND "UsedAt" IS NULL
                ORDER BY "CreatedAt" DESC
                LIMIT 1
                """,
                new { user.UserId });

            var now = DateTime.UtcNow;

            // Cooldown: a new code may only be issued once the previous one is
            // old enough. Block early resends so the endpoint can't be abused.
            if (latestCode != null)
            {
                var cooldownEnd = latestCode.CreatedAt.AddSeconds(ResendCooldownSeconds);
                if (now < cooldownEnd)
                {
                    throw new ResendCooldownException(
                        cooldownEnd,
                        "Please wait before requesting a new verification code."
                    );
                }
            }

            // Issue a fresh OTP and invalidate the old code(s) atomically, then
            // email it — never send a code that wasn't stored.
            string otp = GenerateOtp();

            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await connection.ExecuteAsync(
                """UPDATE "EmailVerificationCodes" SET "UsedAt" = @Now WHERE "UserId" = @UserId AND "UsedAt" IS NULL""",
                new { user.UserId, Now = now },
                transaction);

            await connection.ExecuteAsync(
                """
                INSERT INTO "EmailVerificationCodes" ("Id", "UserId", "OTPHash", "ExpiresAt", "UsedAt", "CreatedAt", "Attempts")
                VALUES (@Id, @UserId, @OTPHash, @ExpiresAt, NULL, @CreatedAt, 0)
                """,
                new
                {
                    Id = Guid.NewGuid(),
                    UserId = user.UserId,
                    OTPHash = BCrypt.Net.BCrypt.HashPassword(otp),
                    ExpiresAt = now.AddMinutes(VerificationCodeExpirationMinutes),
                    CreatedAt = now
                },
                transaction);

            await transaction.CommitAsync();

            await _emailService.SendVerificationCodeAsync(
                normalizedEmail,
                otp
            );

            return new SendingOTPEmailResponse
            {
                ResendCooldownTimeStamp = now.AddSeconds(ResendCooldownSeconds)
            };
        }

        public async Task VerifyEmailAsync(string email, string otp)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            await using var connection = GetConnection();

            var user = await connection.QueryFirstOrDefaultAsync<User>(
                """SELECT * FROM "Users" WHERE "Email" = @Email""",
                new { Email = normalizedEmail });
            if (user == null)
            {
                throw new KeyNotFoundException(
                    "User not found."
                );
            }

            if (user.EmailVerified)
            {
                throw new InvalidOperationException(
                    "Email is already verified."
                );
            }

            var verificationCode = await connection.QueryFirstOrDefaultAsync<EmailVerificationCode>(
                """
                SELECT * FROM "EmailVerificationCodes"
                WHERE "UserId" = @UserId AND "UsedAt" IS NULL
                ORDER BY "CreatedAt" DESC
                LIMIT 1
                """,
                new { user.UserId });
            if (verificationCode == null)
            {
                throw new InvalidOperationException(
                    "Verification code not found."
                );
            }

            if (verificationCode.ExpiresAt < DateTime.UtcNow)
            {
                throw new InvalidOperationException(
                    "Verification code has expired."
                );
            }

            if (verificationCode.Attempts >= MaxOtpAttempts)
            {
                throw new InvalidOperationException(
                    "Too many failed verification attempts. Please request a new verification code."
                );
            }

            var isValid = VerifyOtp(
                otp,
                verificationCode.OTPHash
            );

            if (!isValid)
            {
                // Track failed guesses so a code can't be brute-forced.
                await connection.ExecuteAsync(
                    """UPDATE "EmailVerificationCodes" SET "Attempts" = "Attempts" + 1 WHERE "Id" = @Id""",
                    new { verificationCode.Id });

                throw new InvalidOperationException(
                    "Invalid verification code."
                );
            }

            // Consume the code and mark the email verified atomically.
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await connection.ExecuteAsync(
                """UPDATE "EmailVerificationCodes" SET "UsedAt" = @Now WHERE "Id" = @Id""",
                new { verificationCode.Id, Now = DateTime.UtcNow },
                transaction);

            await connection.ExecuteAsync(
                """UPDATE "Users" SET "EmailVerified" = true WHERE "UserId" = @UserId""",
                new { user.UserId },
                transaction);

            await transaction.CommitAsync();
        }

        public async Task<UserLoginResponse> login(string email, string password)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            await using var connection = GetConnection();
            var existingUser = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"Email\" = @Email", new { Email = normalizedEmail });

            if (existingUser == null)
            {
                throw new UnauthorizedAccessException("Email does not exist");
            }

            if (string.IsNullOrEmpty(existingUser.Password))
            {
                throw new UnauthorizedAccessException("This email is linked to a Google account. Please sign in with Google.");
            }

            if (!existingUser.EmailVerified)
            {
                throw new UnauthorizedAccessException("Email has not been verified. Please verify your email before logging in.");
            }

            if (!BCrypt.Net.BCrypt.Verify(password, existingUser.Password))
            {
                throw new UnauthorizedAccessException("Incorrect password");
            }

            return new UserLoginResponse
            {
                AccessToken = _refreshTokenRepository.GenerateAccessToken(existingUser),
                RefreshToken = await _refreshTokenRepository.GenerateRefreshToken(existingUser.UserId)
            };

        }

        private static string GenerateOtp()
        {
            int number = RandomNumberGenerator.GetInt32(
                100000,
                1000000
            );

            return number.ToString();
        }

        private static bool VerifyOtp(string otp, string otpHash)
        {
            return BCrypt.Net.BCrypt.Verify(otp, otpHash);
        }
    }
}
