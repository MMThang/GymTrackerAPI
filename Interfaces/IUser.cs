using GymTracker.Responses;

namespace GymTracker.Interfaces
{
    public interface IUser
    {
        Task<SendingOTPEmailResponse> sendingOTPEmail(string email, string password, string confirmPassword);
        Task<SendingOTPEmailResponse> resendVerificationCodeAsync(string email);
        Task VerifyEmailAsync(string email, string otp);
        Task<UserLoginResponse> login(string email, string password);
    }
}
