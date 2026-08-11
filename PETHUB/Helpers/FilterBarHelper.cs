using PETHUB.ViewModels;

namespace PETHUB.Helpers
{
    // Provides reusable filter configurations for different PETHUB pages.
    // Common filter definitions are kept here so individual Razor views
    // do not have to recreate the same filter options.
    public static class FilterBarHelper
    {
        // Creates the standard approval-status filter.
        // Used by Marketplace Approval, Lost & Found Approval, and later MyPosts.
        public static FilterBarViewModel Status(string parameterName = "status")
        {
            return new FilterBarViewModel
            {
                // The controller reads this value from the query string.
                ParameterName = parameterName,

                Options =
                {
                    // Empty value means no status filter.
                    new FilterOption
                    {
                        Label = "All",
                        Value = "",
                        CssClass = "btn-secondary"
                    },

                    // Shows posts waiting for approval.
                    new FilterOption
                    {
                        Label = "Pending",
                        Value = "Pending",
                        CssClass = "btn-warning"
                    },

                    // Shows approved posts.
                    new FilterOption
                    {
                        Label = "Approved",
                        Value = "Approved",
                        CssClass = "btn-success"
                    },

                    // Shows rejected posts.
                    new FilterOption
                    {
                        Label = "Rejected",
                        Value = "Rejected",
                        CssClass = "btn-danger"
                    }
                }
            };
        }

        // Creates the reusable PetFeed type filter.
        // This can be used anywhere that needs to separate Announcements
        // from Pet Tips.
        public static FilterBarViewModel PetFeedType(string parameterName = "type")
        {
            return new FilterBarViewModel
            {
                // The PetFeed controller will read this value from the query string.
                ParameterName = parameterName,

                Options =
                {
                    // Empty value means both Announcements and Pet Tips are shown.
                    new FilterOption
                    {
                        Label = "All",
                        Value = "",
                        CssClass = "btn-secondary"
                    },

                    // Matches PetFeedType.Announcement in the existing model.
                    new FilterOption
                    {
                        Label = "Announcements",
                        Value = "Announcement",
                        CssClass = "btn-primary"
                    },

                    // Matches PetFeedType.PetTip in the existing model.
                    new FilterOption
                    {
                        Label = "Pet Tips",
                        Value = "PetTip",
                        CssClass = "btn-primary"
                    }
                }
            };
        }
    }
}