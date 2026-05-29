namespace GymTracker.DTOs.SetDTOs
{
    public class SetDTO
    {
        public Guid? setId { get; set; }
        public short reps { get; set; }
        public int weight { get; set; }
        public bool isBodyWeight { get; set; }
    }
}
