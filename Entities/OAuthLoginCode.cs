namespace GymTracker.Entities
{
    public class OAuthLoginCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Hash of the raw one-time code (never store the raw code).
        public required string CodeHash { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}