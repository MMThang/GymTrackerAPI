using System.Collections.ObjectModel;

namespace GymTracker.Entities
{
    public class WorkoutSession
    {
        public Guid WorkoutSessionId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string WorkoutSessionName { get; set; } = null!;
        public string? Notes { get; set; }
        public DateOnly CreateDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        public ICollection<Exercise> Exercises { get; set; } = new Collection<Exercise>();
    }
}
