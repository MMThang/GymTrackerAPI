using GymTracker.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GymTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IRefreshToken _tokenRepository;
        public TokenController(IRefreshToken tokenRepository)
        {
            _tokenRepository = tokenRepository;
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> refreshToken([FromHeader(Name = "refreshToken")] string authorizationHeader)
        {
            try
            {
                var response = await _tokenRepository.ValidateRefreshToken(authorizationHeader);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }
    }
}
