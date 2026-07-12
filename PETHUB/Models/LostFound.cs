using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PETHUB.Models
{
    public class LostFound
    {
        public int LostFoundId { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public string Type { get; set; } // "Lost" or "Found"

        [DataType(DataType.Date)]
        public DateTime DateReported { get; set; }

        public string Location { get; set; }

        // New fields
        public string? Breed { get; set; } // optional

        [Required]
        public string PetType { get; set; } // required dropdown: "Dog" or "Cat"

        public string? Sex { get; set; } // optional dropdown: "Male" or "Female"

        [DataType(DataType.Date)]
        public DateTime? LostDate { get; set; } // optional

        // For registered members
        public string? MemberId { get; set; } // FK to ApplicationUser

        // For unregistered clients
        public string? ClientName { get; set; }
        public string? ClientContact { get; set; }

        public ICollection<LostFoundImage>? Images { get; set; }
    }
}
