using GymTracker.DTOs.ExerciseDTOs;

namespace GymTracker.DTOs.WorkoutSessionDTOs
{
    public class UpdateWorkoutSessionDTO
    {
        public Guid workoutSessionId { get; set; }
        public string workoutSessionName { get; set; }
        public string? note { get; set; }
        public List<ExerciseDTO> exercises { get; set; }
    }
}
