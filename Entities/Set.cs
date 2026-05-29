namespace GymTracker.Entities
{
    public class Set
    {
        public Guid SetId { get; set; } = Guid.NewGuid();
        public Guid ExerciseId { get; set; }
        public Exercise Exercise { get; set; } = null!;
        public int Weight { get; set; }
        public bool IsBodyWeight { get; set; } =false;
        public short Reps { get; set; }
    }
}
