using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GymTracker.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(JwtRegisteredClaimNames.Sid);
            if (claim == null || !Guid.TryParse(claim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }

        public static void ValidateUserAccess(this ClaimsPrincipal user, Guid requestedUserId)
        {
            var authenticatedUserId = user.GetUserId();
            if (authenticatedUserId != requestedUserId)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this resource");
            }
        }
    }
}
