using GymTracker.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GymTracker.Controllers
{
    [Route("api/Auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IGoogleAuth _googleAuthRepository;
        private readonly IConfiguration _configuration;

        public AuthController(IGoogleAuth googleAuthRepository, IConfiguration configuration)
        {
            _googleAuthRepository = googleAuthRepository;
            _configuration = configuration;
        }

        [HttpGet("google")]
        public IActionResult GoogleLogin()
        {
            var response = _googleAuthRepository.GetAuthorizationUrl();

            Response.Cookies.Append(
                "GoogleOAuthState",
                response.State,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    MaxAge = TimeSpan.FromMinutes(10)
                });

            return Redirect(response.AuthorizationUrl);

        }

        [HttpGet("google/callback")]
        public async Task<IActionResult> GoogleCallback(
            [FromQuery] string code,
            [FromQuery] string state
        )
        {
            // 1. Get the state stored before redirecting to Google
            var storedState = Request.Cookies["GoogleOAuthState"];

            // 2. Validate state
            if (string.IsNullOrEmpty(storedState) ||
                storedState != state)
            {
                return BadRequest(
                    "Invalid OAuth state."
                );
            }

            // 3. Delete state cookie
            Response.Cookies.Delete(
                "GoogleOAuthState"
            );

            // 4. Exchange code and validate Google identity
            var googleUser = await _googleAuthRepository.HandleCallbackAsync(code);

            // 5. Application authentication layer
            var loginResponse = await _googleAuthRepository.LoginWithGoogleAsync(googleUser);


            // 5. Redirect to Next.js
            Response.Cookies.Append(
                "refreshToken",
                loginResponse.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = Request.IsHttps ? SameSiteMode.Strict : SameSiteMode.Lax,
                });
            Response.Cookies.Append(
                "session",
                loginResponse.AccessToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                });
            var frontendUrl = _configuration.GetValue<string>("FrontendUrl");
            return Redirect(
                $"{frontendUrl}/calendar"
            );
        }
    }
}