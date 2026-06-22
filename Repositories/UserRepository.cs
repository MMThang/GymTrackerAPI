using Dapper;
using GymTracker.Data;
using GymTracker.Entities;
using GymTracker.Interfaces;
using GymTracker.Responses;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GymTracker.Repositories
{
    public class UserRepository : IUser
    {
        private readonly AppDBContext _context;
        private readonly IConfiguration _configuration;
        private readonly IRefreshToken _refreshTokenRepository;
        public UserRepository(AppDBContext context, IConfiguration config, IRefreshToken refreshTokenRepository)
        {
            _context = context;
            _configuration = config;
            _refreshTokenRepository = refreshTokenRepository;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("WebApiDatabase"));
        }

        public async Task<bool> register(string username, string password, string confirmPassword)
        {
            if (username.Length < 6)
            {
                throw new ArgumentOutOfRangeException(nameof(username), "Username minimum 6 characters");
            }
            if (password.Length < 6 || confirmPassword.Length < 6)
            {
                throw new ArgumentOutOfRangeException(nameof(password), "Password minimum 6 characters");
            }

            await using var connection = GetConnection();
            var existingUser = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"Username\" = @Username", new { Username = username });
            if (existingUser == null)
            {
                if (password == confirmPassword)
                {
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
                    await _context.Users.AddAsync(new User
                    {
                        Username = username,
                        Password = hashedPassword
                    });
                    await _context.SaveChangesAsync();
                    return true;
                }
                else
                {
                    throw new ArgumentException("Password don't match");
                }
            }
            else
            {
                throw new InvalidOperationException("User already existed");
            }
        }
        public async Task<UserLoginResponse> login(string username, string password)
        {
            await using var connection = GetConnection();
            var existingUser = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"Username\" = @Username", new { Username = username });

            if (existingUser == null)
            {
                throw new UnauthorizedAccessException("Username does not exist");
            }
            else
            {
                if (BCrypt.Net.BCrypt.Verify(password, existingUser.Password))
                {
                    return new UserLoginResponse
                    {
                        AccessToken = _refreshTokenRepository.GenerateAccessToken(existingUser),
                        RefreshToken = await _refreshTokenRepository.GenerateRefreshToken(existingUser.UserId)
                    };
                }
                else
                {
                    throw new UnauthorizedAccessException("Incorrect password");
                }
            }
        }
    }
}
