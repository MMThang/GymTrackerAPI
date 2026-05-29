using GymTracker.DTOs.SetDTOs;

namespace GymTracker.DTOs.ExerciseDTOs
{
    public class ExerciseDTO
    {
        public Guid? exerciseId { get; set; }
        public string exerciseName { get; set; }
        public List<SetDTO> sets { get; set; }
    }
}
