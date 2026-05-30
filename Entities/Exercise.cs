using System.Collections.ObjectModel;

namespace GymTracker.Entities
{
    public class Exercise
    {
        public Guid ExerciseId { get; set; }
        public Guid WorkoutSessionId { get; set; }
        public WorkoutSession WorkoutSession { get; set; }
        public string ExerciseName { get; set; } = null!;
        public ICollection<Set> Sets { get; set; } = new Collection<Set>();
    }
}
