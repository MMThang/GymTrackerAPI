using GymTracker.Entities;
using GymTracker.Responses;

namespace GymTracker.Interfaces
{
    public interface IGoogleAuth
    {
        GoogleAuthorizationResponse GetAuthorizationUrl();
        Task<GoogleUserInfo> HandleCallbackAsync(string code, string? nonce, string codeVerifier);
        Task<User> FindOrCreateGoogleUserAsync(GoogleUserInfo googleUser);
    }
}