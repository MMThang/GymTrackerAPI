using System.Security.Cryptography;
using System.Text;
using Dapper;
using GymTracker.Entities;
using GymTracker.Interfaces;
using GymTracker.Responses;
using Microsoft.AspNetCore.WebUtilities;
using Npgsql;

namespace GymTracker.Repositories
{
    public class OAuthLoginCodeRepository : IOAuthLoginCode
    {
        private readonly IConfiguration _configuration;
        private readonly IRefreshToken _tokenRepository;

        public OAuthLoginCodeRepository(IConfiguration configuration, IRefreshToken tokenRepository)
        {
            _configuration = configuration;
            _tokenRepository = tokenRepository;
        }

        private const int ExpiryMinutes = 1;

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("WebApiDatabase"));
        }

        private static string HashCode(string code)
        {
            var hash = SHA256.HashData(
                Encoding.UTF8.GetBytes(code)
            );
            return Convert.ToBase64String(hash);
        }

        public async Task<string> CreateLoginCode(Guid userId)
        {
            // Cryptographically random, single-use code. Guid.NewGuid() is
            // intentionally avoided as the primary security mechanism.
            var bytes = RandomNumberGenerator.GetBytes(32);
            var code = WebEncoders.Base64UrlEncode(bytes);

            var codeHash = HashCode(code);

            await using var connection = GetConnection();
            await connection.ExecuteAsync(
                @"INSERT INTO ""OAuthLoginCodes"" (""Id"", ""CodeHash"", ""UserId"", ""ExpiresAt"", ""UsedAt"", ""CreatedAt"")
                  VALUES (@Id, @CodeHash, @UserId, @ExpiresAt, NULL, @CreatedAt)",
                new
                {
                    Id = Guid.NewGuid(),
                    CodeHash = codeHash,
                    UserId = userId,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(ExpiryMinutes),
                    CreatedAt = DateTime.UtcNow
                }
            );

            return code;
        }

        public async Task<UserLoginResponse> ExchangeLoginCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new UnauthorizedAccessException(
                    "Invalid OAuth login code."
                );
            }

            var codeHash = HashCode(code);

            await using var connection = GetConnection();

            // 1. Find the corresponding database record
            var record = await connection.QueryFirstOrDefaultAsync<OAuthLoginCode>(
                @"SELECT ""Id"", ""CodeHash"", ""UserId"", ""ExpiresAt"", ""UsedAt"", ""CreatedAt""
                  FROM ""OAuthLoginCodes""
                  WHERE ""CodeHash"" = @CodeHash",
                new { CodeHash = codeHash }
            );

            if (record == null)
            {
                throw new UnauthorizedAccessException(
                    "Invalid OAuth login code."
                );
            }

            // 2. Verify it is not expired
            if (record.ExpiresAt <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException(
                    "OAuth login code has expired."
                );
            }

            // 3. Verify it has not already been used
            if (record.UsedAt != null)
            {
                throw new UnauthorizedAccessException(
                    "OAuth login code has already been used."
                );
            }

            // 4. Atomically mark it as used. The UPDATE only affects the row
            //    when it is still valid and unclaimed, so two simultaneous
            //    requests cannot both successfully consume the same code.
            var affectedRows = await connection.ExecuteAsync(
                @"UPDATE ""OAuthLoginCodes""
                  SET ""UsedAt"" = @Now
                  WHERE ""Id"" = @Id
                    AND ""ExpiresAt"" > @Now
                    AND ""UsedAt"" IS NULL",
                new { Id = record.Id, Now = DateTime.UtcNow }
            );

            if (affectedRows == 0)
            {
                // A concurrent request consumed the code first (replay).
                throw new UnauthorizedAccessException(
                    "OAuth login code has already been used."
                );
            }

            // 5. Load the owning user and create the application authentication
            var user = await connection.QuerySingleOrDefaultAsync<User>(
                @"SELECT * FROM ""Users"" WHERE ""UserId"" = @UserId",
                new { record.UserId }
            );

            if (user == null)
            {
                throw new KeyNotFoundException(
                    "User not found"
                );
            }

            return new UserLoginResponse
            {
                AccessToken = _tokenRepository.GenerateAccessToken(user),
                RefreshToken = await _tokenRepository.GenerateRefreshToken(user.UserId)
            };
        }
    }
}