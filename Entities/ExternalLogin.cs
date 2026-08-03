namespace GymTracker.Entities
{
    public class ExternalLogin
    {
        public int Id { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        public AuthProvider Provider { get; set; }

        public string ProviderUserId { get; set; } = null!;
    }
}