using GymTracker.DTOs.SetDTOs;

namespace GymTracker.DTOs.ExerciseDTOs
{
    public class ExerciseDetailDTO
    {
        public Guid ExerciseId { get; set; }
        public string ExerciseName { get; set; }
        public List<SetDetailDTO> Sets { get; set; } = new List<SetDetailDTO>();
    }
}
