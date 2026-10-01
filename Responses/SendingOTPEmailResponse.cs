namespace GymTracker.Responses
{
    public class SendingOTPEmailResponse
    {
        // UTC moment when the next verification code may be sent.
        public required DateTime ResendCooldownTimeStamp { get; set; }
    }
}