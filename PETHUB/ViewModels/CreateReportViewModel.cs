using PETHUB.Models;
using System.ComponentModel.DataAnnotations;

namespace PETHUB.ViewModels
{
    public class CreateReportViewModel : IValidatableObject
    {
        // Identifies whether the member is reporting a Marketplace listing
        // or a Lost & Found post.
        [Required]
        public ReportedContentType ContentType { get; set; }

        // Stores the ID of the Listing or LostFound record being reported.
        // The controller will use ContentType to determine which table to query.
        [Required]
        public int ContentId { get; set; }

        // Stores the predefined reason selected by the member.
        [Required]
        public UserReportReason Reason { get; set; }

        // Stores the member's custom explanation when "Other" is selected.
        // This remains nullable because it is only required for the Other option.
        public string? OtherReason { get; set; }

        // Stores additional information explaining why the member is reporting
        // the selected Marketplace listing or Lost & Found post.
        public string? Description { get; set; }

        // Performs server-side ViewModel validation for the conditional
        // "Other" reason requirement. IValidatableObject allows validation
        // to depend on the value of another property.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // When the member selects Other, a custom reason must be provided.
            // IsNullOrWhiteSpace treats null, empty strings, and whitespace-only
            // input as missing values.
            if (Reason == UserReportReason.Other &&
                string.IsNullOrWhiteSpace(OtherReason))
            {
                yield return new ValidationResult(
                    "Please specify your reason when selecting Other.",
                    new[] { nameof(OtherReason) });
            }
        }
    }
}