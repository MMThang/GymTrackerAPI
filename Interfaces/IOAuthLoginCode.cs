using GymTracker.Entities;
using GymTracker.Responses;

namespace GymTracker.Interfaces
{
    public interface IOAuthLoginCode
    {
        /// <summary>
        /// Generates a cryptographically random, short-lived, single-use
        /// login code for the given user. Only the hash of the code is
        /// persisted. The raw code is returned to be sent to the frontend.
        /// </summary>
        Task<string> CreateLoginCode(Guid userId);

        /// <summary>
        /// Validates and atomically consumes the one-time code, then generates
        /// the application's access and refresh tokens for the owning user.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">
        /// The code is invalid, expired, or has already been used.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// The user associated with the code no longer exists.
        /// </exception>
        Task<UserLoginResponse> ExchangeLoginCode(string code);
    }
}