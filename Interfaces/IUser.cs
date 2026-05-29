using GymTracker.Responses;

namespace GymTracker.Interfaces
{
    public interface IUser
    {
        Task<bool> register(string username, string password, string confirmPassword);
        Task<UserLoginResponse> login(string username, string password);
    }
}
