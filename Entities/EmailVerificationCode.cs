namespace GymTracker.Entities
{
    public class EmailVerificationCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }

        // BCrypt hash of the 6-digit OTP (never store the plaintext OTP).
        public required string OTPHash { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public int Attempts { get; set; }

        public User User { get; set; } = null!;
    }
}