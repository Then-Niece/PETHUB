using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PETHUB.Models
{
    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected
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

    public enum ReportResolutionStatus
    {
        Open,
        Resolved
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

        [Required]
        public string Location { get; set; }

        // New fields
        [Required]
        public string? Breed { get; set; } // optional

        [Required]
        public PetType PetType { get; set; } // required dropdown: "Dog" or "Cat"
        [Required]

        public PetSex? Sex { get; set; } // optional dropdown: "Male" or "Female"

        [DataType(DataType.Date)]
        public DateTime? LostDate { get; set; } // optional

        // For registered members
        public string? UserId { get; set; } // FK to ApplicationUser
        public ApplicationUser? User { get; set; }
        // For unregistered clients
        public string? ClientName { get; set; }
        public string? ClientContact { get; set; }

        public ICollection<LostFoundImage>? Images { get; set; }

        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending; // default to Pending

        // Public-facing state; separate from the admin-only approval decision.
        public ReportResolutionStatus ResolutionStatus { get; set; } = ReportResolutionStatus.Open;
    }
}
