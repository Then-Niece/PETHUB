using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using PETHUB.Models;

namespace PETHUB.ViewModels
{
    public class PetFeedViewModel
    {
        public int PetFeedId { get; set; }

        [Required]
        public string Title { get; set; }

        public string Content { get; set; }

        public PetFeedType Type { get; set; }

        // New images uploaded during create/edit
        public List<IFormFile>? Images { get; set; }

        // Existing images when editing
        public ICollection<PetFeedImage>? ExistingImages { get; set; }
    }
}