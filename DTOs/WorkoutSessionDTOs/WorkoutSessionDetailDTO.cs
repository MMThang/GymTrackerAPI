using GymTracker.DTOs.ExerciseDTOs;

namespace GymTracker.DTOs.WorkoutSessionDTOs
{
    public class WorkoutSessionDetailDTO
    {
        public Guid WorkoutSessionId { get; set; }
        public string WorkoutSessionName { get; set; }
        public string? Notes { get; set; }
        public List<ExerciseDetailDTO> Exercises { get; set; } = new List<ExerciseDetailDTO>();
    }
}
