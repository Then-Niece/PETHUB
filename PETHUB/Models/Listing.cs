using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PETHUB.Models
{
    public class Listing
    {
        [Key]
        public int ListingId { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Description { get; set; }

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        public DateTime DatePosted { get; set; }

        // Late added property
        public string? Location { get; set; }

        // Optional user link
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        // Navigation property: one listing can have many images
        public ICollection<ListingImage>? Images { get; set; }
    }
}
