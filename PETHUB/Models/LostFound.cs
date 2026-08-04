using System.ComponentModel.DataAnnotations;

namespace PETHUB.Models
{
    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public enum ReportStatus
    {
        Active,
        Resolved
    }
    public enum LostFoundType
    {
        Lost,
        Found
    }

    public enum PetType
    {
        Dog,
        Cat
    }

    public enum PetSex
    {
        Unknown,
        Male,
        Female
    }

    public class LostFound
    {
        public int LostFoundId { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public LostFoundType Type { get; set; } // "Lost" or "Found"

        [DataType(DataType.Date)]
        public DateTime DateReported { get; set; }

        [Required(ErrorMessage = "Province is required.")]
        public string Province { get; set; }

        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; }

        [Required(ErrorMessage = "Barangay is required.")]
        public string Barangay { get; set; }

        // Optional
        [StringLength(200, ErrorMessage = "Street address cannot exceed 200 characters.")]
        public string? StreetAddress { get; set; }

        // New fields
        [Required]
        public string? Breed { get; set; } // optional

        [Required]
        public PetType PetType { get; set; } // required dropdown: "Dog" or "Cat"
        
   
        public PetSex? Sex { get; set; } // optional dropdown: "Male" or "Female"

        [DataType(DataType.Date)]
        public DateTime? LostDate { get; set; } // optional

        // For registered members
        public string? UserId { get; set; } // FK to ApplicationUser
        public ApplicationUser? User { get; set; }
        // For unregistered clients

        public string? ClientName { get; set; }

        [StringLength(11, MinimumLength = 11, ErrorMessage = "Contact number must be exactly 11 digits.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Contact number must contain only numbers.")]
        public string? ClientContact { get; set; }

        public ICollection<LostFoundImage>? Images { get; set; }

        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending; // default to Pending

        //New Property for Client's Id
        
        public string? ClientIdImagePath { get; set; }
        public ReportStatus RStatus { get; set; } = ReportStatus.Active;
    }
}
