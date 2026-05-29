using GymTracker.DTOs.UserDTOs;
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

        [HttpPost("register")]
        public async Task<IActionResult> register([FromBody] RegisterDTO registerDTO)
        {
            try
            {
                var response = await _userService.register(registerDTO.username, registerDTO.password, registerDTO.confirmPassword);
                return Ok(response);
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
                var response = await _userService.login(loginDTO.username, loginDTO.password);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}