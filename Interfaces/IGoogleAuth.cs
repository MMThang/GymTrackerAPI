using GymTracker.Responses;

namespace GymTracker.Interfaces
{
    public interface IGoogleAuth
    {
        GoogleAuthorizationResponse GetAuthorizationUrl();
        Task<GoogleUserInfo> HandleCallbackAsync(string code, string? nonce, string codeVerifier);
        Task<UserLoginResponse> LoginWithGoogleAsync(GoogleUserInfo googleUser);
    }
}