using PETHUB.Models;
using System;
using System.Collections.Generic;

namespace PETHUB.ViewModels
{
    public class PetFeedFeedViewModel
    {
        public int PetFeedId { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public DateTime DateCreated { get; set; }

        public PetFeedType Type { get; set; }

        // Images
        public ICollection<PetFeedImage>? Images { get; set; }
        //member of the comments collection
        public ICollection<PetFeedComment> Comments { get; set; }

        // Count of comments so that we can display it in the view without having to load all comments
        public int CommentCount { get; set; }


        // Paw system
        public int PawCount { get; set; }

        public bool IsPawed { get; set; }

        //this is for the highlight system, if the post is highlighted, it will be displayed in a different way
        public bool IsHighlighted { get; set; }
    }
}