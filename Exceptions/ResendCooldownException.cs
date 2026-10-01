namespace GymTracker.Exceptions
{
    public class ResendCooldownException : Exception
    {
        public DateTime ResendCooldownTimeStamp { get; }

        public ResendCooldownException(DateTime resendCooldownTimeStamp, string message)
            : base(message)
        {
            ResendCooldownTimeStamp = resendCooldownTimeStamp;
        }
    }
}