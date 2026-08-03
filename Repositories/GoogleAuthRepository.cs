using System.Security.Cryptography;
using System.Text;
using Dapper;
using Google.Apis.Auth;
using GymTracker.Entities;
using GymTracker.Interfaces;
using GymTracker.Responses;
using Microsoft.Extensions.Options;
using Npgsql;

namespace GymTracker.Repositories
{
    public class GoogleAuthRepository : IGoogleAuth
    {
        private readonly GoogleOAuthSettings _googleOAuthSettings;
        private readonly IRefreshToken _refreshTokenRepository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GoogleAuthRepository(
            IOptions<GoogleOAuthSettings> googleOAuthOptions,
            IRefreshToken refreshTokenRepository,
            HttpClient httpClient,
            IConfiguration configuration
        )
        {
            _googleOAuthSettings = googleOAuthOptions.Value;
            _refreshTokenRepository = refreshTokenRepository;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("WebApiDatabase"));
        }

        public GoogleAuthorizationResponse GetAuthorizationUrl()
        {
            // Generate a cryptographically secure random state
            var state = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32)
            );

            var nonce = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32)
            );

            var codeVerifier = GenerateCodeVerifier();

            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            var queryParams = new Dictionary<string, string>
            {
                ["client_id"] = _googleOAuthSettings.ClientId,
                ["redirect_uri"] = _googleOAuthSettings.RedirectUri,
                ["response_type"] = "code",
                ["scope"] = "openid email profile",
                ["state"] = state,
                ["nonce"] = nonce,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256",
                ["prompt"] = "select_account"
            };

            var queryString = string.Join(
                "&",
                queryParams.Select(x =>
                    $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"
                )
            );

            var authorizationUrl =
                $"https://accounts.google.com/o/oauth2/v2/auth?{queryString}";

            return new GoogleAuthorizationResponse
            {
                AuthorizationUrl = authorizationUrl,
                State = state,
                Nonce = nonce,
                CodeVerifier = codeVerifier
            };
        }

        public async Task<GoogleUserInfo> HandleCallbackAsync(string code, string? nonce, string codeVerifier)
        {
            // 1. Exchange authorization code for Google tokens
            var tokenRequest = new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _googleOAuthSettings.ClientId,
                ["client_secret"] = _googleOAuthSettings.ClientSecret,
                ["redirect_uri"] = _googleOAuthSettings.RedirectUri,
                ["code_verifier"] = codeVerifier,
                ["grant_type"] = "authorization_code"
            };

            using var tokenContent = new FormUrlEncodedContent(tokenRequest);

            var tokenResponse = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                tokenContent
            );

            if (!tokenResponse.IsSuccessStatusCode)
            {
                throw new UnauthorizedAccessException(
                    "Failed to exchange Google authorization code."
                );
            }

            var googleTokens =
                await tokenResponse.Content
                    .ReadFromJsonAsync<GoogleTokenResponse>();

            if (googleTokens == null ||
                string.IsNullOrEmpty(googleTokens.IdToken))
            {
                throw new UnauthorizedAccessException(
                    "Google did not return a valid ID token."
                );
            }

            // 2. Validate Google's ID token
            GoogleJsonWebSignature.Payload payload;

            try
            {
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_googleOAuthSettings.ClientId]
                };

                payload = await GoogleJsonWebSignature.ValidateAsync(
                    googleTokens.IdToken,
                    validationSettings
                );
                if (string.IsNullOrEmpty(nonce))
                {
                    throw new UnauthorizedAccessException(
                        "Google OAuth nonce is missing."
                    );
                }
                if (payload.Nonce != nonce)
                {
                    throw new UnauthorizedAccessException(
                        "Invalid Google OAuth nonce."
                    );
                }
            }
            catch (InvalidJwtException)
            {
                throw new UnauthorizedAccessException(
                    "Invalid Google ID token."
                );
            }

            // 3. Make sure Google's email is verified
            if (!payload.EmailVerified)
            {
                throw new UnauthorizedAccessException(
                    "Google email address is not verified."
                );
            }

            // 4. Return only the identity information
            return new GoogleUserInfo
            {
                GoogleId = payload.Subject,
                Email = payload.Email,
                Name = payload.Name,
                EmailVerified = payload.EmailVerified
            };
        }

        public async Task<UserLoginResponse> LoginWithGoogleAsync(
            GoogleUserInfo googleUser)
        {
            await using var connection = GetConnection();

            // 1. Find ExternalLogin with User data via JOIN
            var externalLogin = (await connection.QueryAsync<ExternalLogin, User, ExternalLogin>(
                "SELECT el.*, u.* FROM \"ExternalLogins\" el INNER JOIN \"Users\" u ON u.\"UserId\" = el.\"UserId\" WHERE el.\"Provider\" = @Provider AND el.\"ProviderUserId\" = @ProviderUserId",
                (el, u) =>
                {
                    el.User = u;
                    return el;
                },
                new
                {
                    Provider = AuthProvider.Google,
                    ProviderUserId = googleUser.GoogleId
                },
                splitOn: "UserId"
            )).FirstOrDefault();

            if (externalLogin != null)
            {
                // Google account already linked
                return await GenerateLoginResponse(
                    externalLogin.User
                );
            }

            // 2. Find existing account by email
            var existingUser = await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM \"Users\" WHERE \"Email\" = @Email",
                new { Email = googleUser.Email }
            );

            if (existingUser != null)
            {
                // Start account linking flow
                // Don't generate JWT yet
                // Don't automatically link yet

                throw new InvalidOperationException(
                    "Account linking required."
                );
            }

            // 3. Brand-new Google account
            var newUserId = Guid.NewGuid();

            var newUser = new User
            {
                UserId = newUserId,
                Username = googleUser.Name,
                Email = googleUser.Email,
                Password = null,
                EmailVerified = googleUser.EmailVerified
            };

            await connection.ExecuteAsync(
                "INSERT INTO \"Users\" (\"UserId\", \"Username\", \"Email\", \"Password\", \"EmailVerified\", \"RegisterDate\") VALUES (@UserId, @Username, @Email, @Password, @EmailVerified, @RegisterDate)",
                new
                {
                    newUser.UserId,
                    newUser.Username,
                    newUser.Email,
                    Password = (string?)null,
                    newUser.EmailVerified,
                    newUser.RegisterDate
                }
            );

            await connection.ExecuteAsync(
                "INSERT INTO \"ExternalLogins\" (\"UserId\", \"Provider\", \"ProviderUserId\") VALUES (@UserId, @Provider, @ProviderUserId)",
                new
                {
                    UserId = newUserId,
                    Provider = AuthProvider.Google,
                    ProviderUserId = googleUser.GoogleId
                }
            );

            // 4. Now we know which GymTracker user this is
            return await GenerateLoginResponse(newUser);
        }

        private async Task<UserLoginResponse> GenerateLoginResponse(User user)
        {
            var accessToken = _refreshTokenRepository.GenerateAccessToken(user);
            var refreshToken = await _refreshTokenRepository.GenerateRefreshToken(user.UserId);

            return new UserLoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        private static string GenerateCodeVerifier()
        {
            var bytes =
                RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string GenerateCodeChallenge(
            string codeVerifier)
        {
            using var sha256 = SHA256.Create();

            var bytes = Encoding.ASCII.GetBytes(codeVerifier);

            var hash = sha256.ComputeHash(bytes);

            return Convert.ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        // private static string GenerateUsernameFromEmail(string email)
        // {
        //     // Take the part before @ and ensure it's unique
        //     var baseUsername = email.Split('@')[0];

        //     // If the username is too short, pad it
        //     if (baseUsername.Length < 6)
        //     {
        //         baseUsername = baseUsername + new string('_', 6 - baseUsername.Length);
        //     }

        //     return baseUsername;
        // }
    }
}