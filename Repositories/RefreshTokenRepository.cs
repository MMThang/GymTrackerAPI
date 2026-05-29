using Dapper;
using GymTracker.Data;
using GymTracker.Entities;
using GymTracker.Interfaces;
using GymTracker.Responses;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GymTracker.Repositories
{
    public class RefreshTokenRepository : IRefreshToken
    {
        private readonly AppDBContext _context;
        private readonly IConfiguration _configuration;
        public RefreshTokenRepository(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _configuration = config;
        }
        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("WebApiDatabase"));
        }

        public string GenerateAccessToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sid, user.UserId.ToString())
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetValue<string>("AppSettings:Token")!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
            var tokenDescriptor = new JwtSecurityToken(
                issuer: _configuration.GetValue<string>("AppSettings:Issuer"),
                audience: _configuration.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: creds
            );
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(tokenDescriptor);
        }

        public async Task<string> GenerateRefreshToken(Guid userId)
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
                var token = Convert.ToHexString(randomBytes);
                var hashedToken = HashToken(token);
                var expiryDate = DateTime.UtcNow.AddDays(7);

                using var connection = GetConnection();
                await connection.ExecuteAsync(
                    "INSERT INTO \"RefreshTokens\" (\"UserId\", \"HashedToken\", \"ExpiryDate\", \"IsRevoked\") VALUES (@UserId, @HashedToken, @ExpiryDate, FALSE)",
                    new { UserId = userId, HashedToken = hashedToken, ExpiryDate = expiryDate });

                return token;
            }
        }
        private string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hash);
        }

        private async Task RevokeAllUserTokens(Guid userId, IDbConnection connection)
        {
            await connection.ExecuteAsync(
                "UPDATE \"RefreshTokens\"SET \"IsRevoked\" = TRUE,\"RevokedAt\" = @Now WHERE \"UserId\" = @UserId AND \"IsRevoked\" = FALSE",
                new { UserId = userId, Now = DateTime.UtcNow });
        }

        public async Task<UserLoginResponse> ValidateRefreshToken(string refreshToken)
        {
            await using var connection = GetConnection();

            var tokenHash = HashToken(refreshToken);

            // 1. Get token
            var token = await connection.QueryFirstOrDefaultAsync<RefreshToken>(
                "SELECT * FROM \"RefreshTokens\" WHERE \"HashedToken\" = @HashedToken",
                new { HashedToken = tokenHash });

            if (token == null)
                throw new UnauthorizedAccessException("Invalid refresh token");

            // 2. Check expiry
            if (token.ExpiryDate < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Token expired");

            // 3. 🔥 REUSE DETECTION
            if (token.IsRevoked)
            {
                // possible token theft
                await RevokeAllUserTokens(token.UserId, connection);

                throw new UnauthorizedAccessException("Token reuse detected. All sessions revoked.");
            }

            // 4. Revoke the old token before generating a new one
            await connection.ExecuteAsync(
                "UPDATE \"RefreshTokens\" SET \"IsRevoked\" = TRUE, \"RevokedAt\" = @Now WHERE \"Id\" = @RefreshTokenId",
                new { RefreshTokenId = token.Id, Now = DateTime.UtcNow });

            // 5. Generate new tokens (ROTATION)
            var newRefreshToken = await GenerateRefreshToken(token.UserId);

            // 6. Get user
            var user = await connection.QuerySingleAsync<User>(
                "SELECT * FROM \"Users\" WHERE \"UserId\" = @UserId",
                new { token.UserId });

            return new UserLoginResponse { AccessToken = GenerateAccessToken(user), RefreshToken = newRefreshToken };
        }
    }
}
