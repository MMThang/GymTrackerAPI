namespace GymTracker.DTOs.WorkoutSessionDTOs
{
    public class WorkoutSessionCalendarDTO
    {
        public DateOnly Date { get; set; }
        public bool HasWorkoutSession { get; set; }
        public Guid? WorkoutSessionId { get; set; }
        public bool HasNote { get; set; }
    }
}
