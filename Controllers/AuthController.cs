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
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    MaxAge = TimeSpan.FromMinutes(10)
                });

            Response.Cookies.Append(
                "GoogleOAuthNonce",
                response.Nonce,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    MaxAge = TimeSpan.FromMinutes(10)
                });

            Response.Cookies.Append(
                "GoogleOAuthCodeVerifier",
                response.CodeVerifier,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
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
            try
            {
                // 1. Get the state, nonce, code_verifier stored before redirecting to Google
                var storedState = Request.Cookies["GoogleOAuthState"];

                if (string.IsNullOrEmpty(storedState) ||
                    storedState != state)
                {
                    throw new UnauthorizedAccessException(
                        "Invalid OAuth state."
                    );
                }

                Response.Cookies.Delete(
                    "GoogleOAuthState"
                );

                var nonce = Request.Cookies["GoogleOAuthNonce"];

                Response.Cookies.Delete(
                    "GoogleOAuthNonce"
                );

                if (string.IsNullOrEmpty(nonce))
                {
                    throw new UnauthorizedAccessException(
                        "Invalid OAuth nonce."
                    );
                }

                var codeVerifier = Request.Cookies["GoogleOAuthCodeVerifier"];

                Response.Cookies.Delete(
                    "GoogleOAuthCodeVerifier"
                );

                if (string.IsNullOrEmpty(codeVerifier))
                {
                    throw new UnauthorizedAccessException(
                        "Invalid OAuth code verifier."
                    );
                }

                // 2. Exchange code and validate Google identity
                var googleUser = await _googleAuthRepository.HandleCallbackAsync(code, nonce, codeVerifier);

                // 3. Application authentication layer
                var loginResponse = await _googleAuthRepository.LoginWithGoogleAsync(googleUser);


                // 4. Redirect to Next.js
                Response.Cookies.Append(
                    "refreshToken",
                    loginResponse.RefreshToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        MaxAge = TimeSpan.FromDays(7)
                    });
                Response.Cookies.Append(
                    "session",
                    loginResponse.AccessToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        MaxAge = TimeSpan.FromMinutes(10)
                    });
                var frontendUrl = _configuration.GetValue<string>("FrontendUrl");
                var allowedOrigins = _configuration.GetSection("AllowedOrigins").Get<string[]>()!;
                if (!allowedOrigins.Contains(frontendUrl))
                {
                    throw new InvalidOperationException("FrontendUrl must match an entry in AllowedOrigins.");
                }
                return Redirect(
                    $"{frontendUrl}/calendar"
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
