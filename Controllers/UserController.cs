using GymTracker.DTOs.UserDTOs;
using GymTracker.Exceptions;
using GymTracker.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GymTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUser _userService;
        public UserController(IUser userService)
        {
            _userService = userService;
        }

        [HttpGet("init")]
        public async Task<IActionResult> init()
        {
            try
            {

                return Ok("Hello");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("sending-otp-email")]
        public async Task<IActionResult> sendingOTPEmail([FromBody] SendingOTPEmailDTO sendingOTPEmailDTO)
        {
            try
            {
                var response = await _userService.sendingOTPEmail(sendingOTPEmailDTO.email, sendingOTPEmailDTO.password, sendingOTPEmailDTO.confirmPassword);
                return Ok(response);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return UnprocessableEntity(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return UnprocessableEntity(ex.Message);
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

        [HttpPost("verify-email")]
        public async Task<IActionResult> verifyEmail([FromBody] VerifyEmailDTO verifyEmailDTO)
        {
            try
            {
                await _userService.VerifyEmailAsync(verifyEmailDTO.email, verifyEmailDTO.otp);
                return Ok(new
                {
                    message = "Email verified successfully."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("resend-verification-code")]
        public async Task<IActionResult> resendVerificationCode([FromBody] ResendVerificationCodeDTO resendVerificationCodeDTO)
        {
            try
            {
                var response = await _userService.resendVerificationCodeAsync(resendVerificationCodeDTO.Email);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ResendCooldownException ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    message = ex.Message,
                    resendCooldownTimeStamp = ex.ResendCooldownTimeStamp
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> login([FromBody] LoginDTO loginDTO)
        {
            try
            {
                var response = await _userService.login(loginDTO.email, loginDTO.password);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
