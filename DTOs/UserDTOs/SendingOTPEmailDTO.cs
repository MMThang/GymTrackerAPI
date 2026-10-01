namespace GymTracker.DTOs.UserDTOs
{
    public class SendingOTPEmailDTO
    {
        public string email { get; set; }
        public string password { get; set; }
        public string confirmPassword { get; set; }
    }
}
