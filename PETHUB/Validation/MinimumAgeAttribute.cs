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

            var age =
                today.Year - birthdate.Year;


            if (birthdate.Date >
                today.AddYears(-age))
            {
                age--;
            }


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