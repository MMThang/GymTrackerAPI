namespace GymTracker.Responses
{
    public class GoogleAuthorizationResponse
    {
        public string AuthorizationUrl { get; set; } = null!;

        public string State { get; set; } = null!;
    }
}