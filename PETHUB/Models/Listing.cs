using System.ComponentModel.DataAnnotations;

namespace PETHUB.Models
{
    public enum ListingStatus
    {
        Pending,
        Adopted,
        Sold
    }

    public enum ListApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public enum ListPetType
    {
        Dog,
        Cat
    }

    public enum ListPetSex
    {
        Unknown,
        Male,
        Female
    }

    public enum ListType
    {
        [Display(Name = "For Adoption")]
        For_Adoption,

        [Display(Name = "For Sale")]
        For_Sale
    }

    public class Listing
    {
        [Key]
        public int ListingId { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Description { get; set; }


        [DataType(DataType.Currency)]
        public int Price { get; set; }

        public DateTime DatePosted { get; set; }

        // Late added property
        [Required(ErrorMessage = "Province is required.")]
        public string Province { get; set; }

        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; }

        [Required(ErrorMessage = "Barangay is required.")]
        public string Barangay { get; set; }

        // Optional
        [StringLength(200, ErrorMessage = "Street address cannot exceed 200 characters.")]
        public string? StreetAddress { get; set; }

        [Required]
        public string? Breed { get; set; }
        // Optional user link
        public string? MemberId { get; set; }
        public ApplicationUser? Member { get; set; }

        [Required]
        public ListingStatus ListStatus { get; set; } = ListingStatus.Pending;
        public ListApprovalStatus Status { get; set; } = ListApprovalStatus.Pending;

        [Required]
        public ListPetType PetType { get; set; }

        [Required]
        public ListPetSex PetSex { get; set; }

        [Required]
        public ListType Type { get; set; }
        // Navigation property: one listing can have many images
        public ICollection<ListingImage>? Images { get; set; }
    }
}
