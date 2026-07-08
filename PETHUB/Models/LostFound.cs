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

        public ICollection<LostFoundImage>? Images { get; set; }
    }
}
