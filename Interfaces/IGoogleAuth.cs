using GymTracker.Responses;

namespace GymTracker.Interfaces
{
    public interface IGoogleAuth
    {
        GoogleAuthorizationResponse GetAuthorizationUrl();
        Task<GoogleUserInfo> HandleCallbackAsync(string code);
        Task<UserLoginResponse> LoginWithGoogleAsync(GoogleUserInfo googleUser);
    }
}