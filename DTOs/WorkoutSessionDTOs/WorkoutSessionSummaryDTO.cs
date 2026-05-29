namespace GymTracker.DTOs.WorkoutSessionDTOs
{
    public class WorkoutSessionSummaryDTO
    {
        public Guid WorkoutSessionId { get; set; }
        public string WorkoutSessionName { get; set; }
        public int NumberOfExercises { get; set; }
        public int NumberOfSets { get; set; }
        public DateOnly CreateDate { get; set; }
    }
}
