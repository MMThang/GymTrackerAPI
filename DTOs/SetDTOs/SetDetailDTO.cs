namespace GymTracker.DTOs.SetDTOs
{
    public class SetDetailDTO
    {
        public Guid SetId { get; set; }
        public int Weight { get; set; }
        public bool IsBodyWeight { get; set; }
        public short Reps { get; set; }
    }
}
