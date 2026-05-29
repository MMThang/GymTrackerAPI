using GymTracker.Entities;
using GymTracker.Responses;

namespace GymTracker.Interfaces
{
    public interface IRefreshToken
    {
        string GenerateAccessToken(User user);
        Task<string> GenerateRefreshToken(Guid userId);
        Task<UserLoginResponse> ValidateRefreshToken(string refreshToken);
    }
}
