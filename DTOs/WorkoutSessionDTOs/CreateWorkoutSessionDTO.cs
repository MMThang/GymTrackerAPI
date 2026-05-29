using GymTracker.DTOs.ExerciseDTOs;
using GymTracker.Entities;

namespace GymTracker.DTOs.WorkoutSessionDTOs
{
    public class CreateWorkoutSessionDTO
    {
        public string workoutSessionName { get; set; }
        public Guid userId { get; set; }
        public string? note { get; set; }
        public string createDate { get; set; }
        public List<ExerciseDTO> exercises { get; set; }

    }
}
