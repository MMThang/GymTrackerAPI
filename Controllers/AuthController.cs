using GymTracker.DTOs.UserDTOs;
using GymTracker.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GymTracker.Controllers
{
    [Route("api/Auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IGoogleAuth _googleAuthRepository;
        private readonly IOAuthLoginCode _oauthLoginCodeRepository;

        private readonly IConfiguration _configuration;

        public AuthController(
            IGoogleAuth googleAuthRepository,
            IOAuthLoginCode oauthLoginCodeRepository,
            IConfiguration configuration)
        {
            _googleAuthRepository = googleAuthRepository;
            _oauthLoginCodeRepository = oauthLoginCodeRepository;

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

                // 2. Exchange Google's code and validate Google identity
                var googleUser = await _googleAuthRepository.HandleCallbackAsync(code, nonce, codeVerifier);

                // 3. Find or create the application user
                var user = await _googleAuthRepository.FindOrCreateGoogleUserAsync(googleUser);

                // 4. Generate a short-lived, single-use one-time login code
                var loginCode = await _oauthLoginCodeRepository.CreateLoginCode(user.UserId);

                // 5. Redirect to the frontend with ONLY the one-time code.
                //    Never put access or refresh tokens in the URL.
                var frontendUrl = _configuration.GetValue<string>("FrontendUrl");
                var allowedOrigins = _configuration.GetSection("AllowedOrigins").Get<string[]>()!;
                if (!allowedOrigins.Contains(frontendUrl))
                {
                    throw new InvalidOperationException("FrontendUrl must match an entry in AllowedOrigins.");
                }
                return Redirect(
                    $"{frontendUrl}/calendar?code={loginCode}"
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

        [HttpPost("oauth/exchange")]
        public async Task<IActionResult> Exchange(
            [FromBody] OAuthExchangeRequestDTO request)
        {
            try
            {
                // Atomically validate and consume the one-time code.
                var response = await _oauthLoginCodeRepository.ExchangeLoginCode(request.Code);

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
