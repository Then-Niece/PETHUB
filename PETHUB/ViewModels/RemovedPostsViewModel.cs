using PETHUB.Models;

namespace PETHUB.ViewModels
{
    public class RemovedPostsViewModel
    {
        // Identifies which type of removed post is being displayed
        // on the Details page. Expected values are "listing" or "lostfound".
        public string PostType { get; set; } = string.Empty;


        // Contains the removed Marketplace listing when the Details page
        // is displaying a Marketplace post.
        public Listing? Listing { get; set; }


        // Contains the removed Lost & Found post when the Details page
        // is displaying a Lost & Found post.
        public LostFound? LostFound { get; set; }


        // Contains the specific reason entered by the Administrator
        // when the post was confirmed as violating PETHUB rules.
        public string? AdminActionReason { get; set; }


        // Contains the Member's appeal for the specific removed post
        // currently being displayed on the Details page.
        //
        // This remains null when the Member has not submitted an appeal.
        // The Appeal points to the existing Listing or Lost & Found post;
        // it does not create another copy of the post.
        public Appeal? Appeal { get; set; }


        // Contains the Member's removed Marketplace listings together
        // with the Administrator's reason for removing each listing.
        //
        // The tuple keeps the existing Listing entity and its corresponding
        // moderation reason together for the Removed Posts Index page.
        public List<(Listing Listing, string? AdminActionReason)> RemovedListings { get; set; }
            = new();


        // Contains the Member's removed Lost & Found posts together
        // with the Administrator's reason for removing each post.
        //
        // The tuple keeps the existing Lost & Found entity and its
        // corresponding moderation reason together for the Index page.
        public List<(LostFound Report, string? AdminActionReason)> RemovedLostFound { get; set; }
            = new();
    }
}