using PETHUB.Models;

namespace PETHUB.ViewModels
{
    public class PetFeedFeedViewModel
    {
        // ==========================================================
        // EXISTING PETFEED PROPERTIES
        // ==========================================================

        // Stores the PetFeed ID when this item comes from PetFeed.
        // For Marketplace and Lost & Found items, this remains 0.
        public int PetFeedId { get; set; }

        // Common title used by all three feed content types.
        public string Title { get; set; } = string.Empty;

        // Common text/content used by PetFeed, Marketplace, and Lost & Found.
        public string? Content { get; set; }

        // Stores the creation/reporting date of the original record.
        public DateTime DateCreated { get; set; }

        // Existing PetFeed type.
        // This is only populated when ContentType == 0.
        public PetFeedType Type { get; set; }

        // Existing PetFeed image collection.
        // This remains for compatibility with the current PetFeed view.
        public ICollection<PetFeedImage>? Images { get; set; }

        // Existing PetFeed comments.
        // Marketplace and Lost & Found do not use this collection.
        public ICollection<PetFeedComment> Comments { get; set; }
            = new List<PetFeedComment>();

        // Existing PetFeed comment count.
        public int CommentCount { get; set; }

        // Existing PetFeed paw count.
        public int PawCount { get; set; }

        // Existing PetFeed paw state for the logged-in member.
        public bool IsPawed { get; set; }

        // Existing PetFeed highlighting behavior.
        public bool IsHighlighted { get; set; }


        // ==========================================================
        // COMBINED FEED PROPERTIES
        // ==========================================================

        // Identifies whether this feed item came from PetFeed,
        // Marketplace, or Lost & Found.
        public PetFeedContentType ContentType { get; set; }

        // Stores the ID of the original database record.
        //
        // PetFeed       -> PetFeedId
        // Marketplace   -> ListingId
        // Lost & Found  -> LostFoundId
        public int ContentId { get; set; }


        // ==========================================================
        // LOCATION
        // ==========================================================

        // Marketplace and Lost & Found use the member-selected City.
        // PetFeed does not have a City field, so it remains null for
        // administrator-created PetFeed content.
        public string? City { get; set; }

        // Province is included so Marketplace/Lost & Found cards can
        // display the same structured location information later.
        public string? Province { get; set; }


        // ==========================================================
        // MARKETPLACE
        // ==========================================================

        // Identifies whether a Marketplace listing is For Adoption
        // or For Sale.
        public ListType? ListingType { get; set; }

        // Stores the Marketplace pet type.
        public ListPetType? ListingPetType { get; set; }

        // Stores the Marketplace price.
        public int? Price { get; set; }


        // ==========================================================
        // LOST & FOUND
        // ==========================================================

        // Identifies whether the report is Lost or Found.
        public LostFoundType? LostFoundType { get; set; }

        // Stores the Lost & Found pet type.
        public PetType? LostFoundPetType { get; set; }


        // ==========================================================
        // COMBINED IMAGE DATA
        // ==========================================================

        // Contains image paths for the combined feed.
        //
        // PetFeed uses its existing Images collection, while
        // Marketplace and Lost & Found image entities are converted
        // into this common list later by the controller.
        public List<string> ImagePaths { get; set; } = new();


        // ==========================================================
        // NAVIGATION
        // ==========================================================

        // Stores the destination URL for the original content.
        //
        // Marketplace and Lost & Found cards can therefore navigate
        // to their existing Details pages without creating another
        // details system specifically for the combined feed.
        public string DetailsUrl { get; set; } = string.Empty;

        // Stores the ID of the user who created the Marketplace or Lost & Found post.
        // PetFeed administrator posts do not use this value.
        public string? OwnerId { get; set; }

        // Stores the display name of the actual post owner.
        // This allows the combined feed to show the member who created the post
        // instead of incorrectly displaying the administrator.
        public string? OwnerName { get; set; }

        // Stores the owner's profile image path when one is available.
        // This allows Marketplace and Lost & Found cards to use the member's
        // profile information later in Feed.cshtml.
        public string? OwnerProfileImage { get; set; }


        public bool IsSaved { get; set; }
    }

    // Identifies the source of an item in the combined PetFeed.
    public enum PetFeedContentType
    {
        // Administrator-created PetFeed content.
        PetFeed,

        // Member Marketplace listing.
        Marketplace,

        // Member Lost & Found report.
        LostFound
    }
}