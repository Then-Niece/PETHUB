using PETHUB.Models;

namespace PETHUB.ViewModels
{
    public class AdminReportDetailsViewModel
    {
        // The specific report selected from the Admin Reports Index.
        // This remains the main report being reviewed by the Admin.
        public UserReport Report { get; set; } = null!;


        // Contains every report associated with the same Marketplace
        // listing or Lost & Found post as the selected report.
        // This allows the Admin to review multiple reports for one post.
        public List<UserReport> RelatedReports { get; set; } = new();


        // Contains the Member's latest appeal for the reported post.
        // This is null when the owner has not submitted an appeal.
        // The Appeal references the existing Listing or Lost & Found post
        // instead of creating another copy of the post.
        public Appeal? Appeal { get; set; }
    }
}