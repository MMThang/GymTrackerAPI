namespace GymTracker.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationCodeAsync(
            string email,
            string code
        );
    }
}