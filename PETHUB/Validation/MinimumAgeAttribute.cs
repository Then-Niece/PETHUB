using System.ComponentModel.DataAnnotations;

namespace PETHUB.Validation
{
    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;


        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
        }


        protected override ValidationResult? IsValid(
    object? value,
    ValidationContext validationContext)
        {
            // Let [Required] handle empty values.
            if (value == null)
            {
                return ValidationResult.Success;
            }


            if (value is not DateTime birthdate)
            {
                return new ValidationResult(
                    "Invalid birthdate."
                );
            }


            var today = DateTime.Today;


            // =====================================================
            // FUTURE DATE CHECK
            // =====================================================

            if (birthdate.Date > today)
            {
                return new ValidationResult(
                    "Birthdate cannot be in the future."
                );
            }


            // =====================================================
            // AGE CALCULATION
            // =====================================================

            var age =
                today.Year - birthdate.Year;


            if (birthdate.Date >
                today.AddYears(-age))
            {
                age--;
            }


            // =====================================================
            // MINIMUM AGE CHECK
            // =====================================================

            if (age < _minimumAge)
            {
                return new ValidationResult(
                    ErrorMessage ??
                    $"You must be at least {_minimumAge} years old."
                );
            }


            return ValidationResult.Success;
        }
    }
}