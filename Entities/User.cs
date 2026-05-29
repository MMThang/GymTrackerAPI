using System.Collections.ObjectModel;

namespace GymTracker.Entities
{
    public class User
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Username { get; set; }
        public string Password { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateOnly RegisterDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        public ICollection<WorkoutSession> WorkoutSessions { get; set; } = new Collection<WorkoutSession>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new Collection<RefreshToken>();
    }
}
