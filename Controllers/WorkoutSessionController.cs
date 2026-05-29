using GymTracker.DTOs.WorkoutSessionDTOs;
using GymTracker.Extensions;
using GymTracker.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkoutSessionController : ControllerBase
    {
        private readonly IWorkoutSession _workoutSessionService;
        public WorkoutSessionController(IWorkoutSession workoutSessionService)
        {
            _workoutSessionService = workoutSessionService;
        }

        [Authorize]
        [HttpPost("workout-session")]
        public async Task<IActionResult> createWorkoutSession([FromBody] CreateWorkoutSessionDTO createWorkoutSessionDTO)
        {
            try
            {
                // Validate that the userId in the DTO matches the authenticated user
                User.ValidateUserAccess(createWorkoutSessionDTO.userId);

                var response = await _workoutSessionService.createWorkoutSession(createWorkoutSessionDTO);
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

        [Authorize]
        [HttpGet("workout-sessions/{userId}")]
        public async Task<IActionResult> getWorkoutSessionList(Guid userId)
        {
            try
            {
                // Validate that the requested userId matches the authenticated user
                User.ValidateUserAccess(userId);

                var workoutSessions = await _workoutSessionService.getWorkoutSessionsList(userId);
                return Ok(workoutSessions);
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

        [Authorize]
        [HttpGet("workout-session/{workoutSessionId}")]
        public async Task<IActionResult> getWorkoutSession(Guid workoutSessionId)
        {
            try
            {
                // Validate that the workout session belongs to the authenticated user
                var workoutUserId = await _workoutSessionService.getWorkoutSessionUserId(workoutSessionId);
                if (workoutUserId == null)
                {
                    return NotFound("Workout session not found");
                }

                User.ValidateUserAccess(workoutUserId.Value);

                var workoutSession = await _workoutSessionService.getWorkoutSession(workoutSessionId);
                return Ok(workoutSession);
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

        [Authorize]
        [HttpGet("workout-calendar/{userId}/{month}/{year}")]
        public async Task<IActionResult> getWorkoutSessionsByMonthYear(Guid userId, int month, int year)
        {
            try
            {
                // Validate that the requested userId matches the authenticated user
                User.ValidateUserAccess(userId);

                var calendarData = await _workoutSessionService.getWorkoutSessionsByMonthYear(userId, month, year);
                return Ok(calendarData);
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

        [Authorize]
        [HttpPut("workout-session")]
        public async Task<IActionResult> updateWorkoutSession([FromBody] UpdateWorkoutSessionDTO updateDTO)
        {
            try
            {

                // Validate that the workout session belongs to the authenticated user
                var workoutUserId = await _workoutSessionService.getWorkoutSessionUserId(updateDTO.workoutSessionId);
                if (workoutUserId == null)
                {
                    return NotFound("Workout session not found");
                }

                User.ValidateUserAccess(workoutUserId.Value);

                var response = await _workoutSessionService.updateWorkoutSession(updateDTO);
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

        [Authorize]
        [HttpDelete("workout-session/{workoutSessionId}")]
        public async Task<IActionResult> deleteWorkoutSession(Guid workoutSessionId)
        {
            try
            {
                // Validate that the workout session belongs to the authenticated user
                var workoutUserId = await _workoutSessionService.getWorkoutSessionUserId(workoutSessionId);
                if (workoutUserId == null)
                {
                    return NotFound("Workout session not found");
                }

                User.ValidateUserAccess(workoutUserId.Value);

                var response = await _workoutSessionService.deleteWorkoutSession(workoutSessionId);
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