using System.ComponentModel.DataAnnotations;

namespace GymTracker.DTOs.SetDTOs
{
    public class SetDTO : IValidatableObject
    {
        public Guid? setId { get; set; }
        public short reps { get; set; }
        public int? weight { get; set; }
        public bool isBodyWeight { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!isBodyWeight && weight == null)
            {
                yield return new ValidationResult(
                    "Weight must have a value when isBodyWeight is false.",
                    new[] { nameof(weight) }
                );
            }
        }
    }
}
