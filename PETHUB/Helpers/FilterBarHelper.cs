using PETHUB.ViewModels;

namespace PETHUB.Helpers
{
    // Provides reusable filter definitions for different PETHUB pages.
    // Each page can combine one or more of these definitions into a FilterBarViewModel.
    public static class FilterBarHelper
    {
        // Creates the reusable approval-status filter.
        public static FilterDefinition Status(string? selectedValue = null)
        {
            return new FilterDefinition
            {
                // This becomes the "status" query-string parameter.
                ParameterName = "status",

                // Text displayed above the dropdown.
                Label = "Status",

                // Preserve the current value supplied by the controller.
                SelectedValue = selectedValue,

                Options =
                {
                    // Empty value means no status filter.
                    new FilterOption
                    {
                        Label = "All",
                        Value = ""
                    },

                    // Shows posts waiting for approval.
                    new FilterOption
                    {
                        Label = "Pending",
                        Value = "Pending"
                    },

                    // Shows approved posts.
                    new FilterOption
                    {
                        Label = "Approved",
                        Value = "Approved"
                    },

                    // Shows rejected posts.
                    new FilterOption
                    {
                        Label = "Rejected",
                        Value = "Rejected"
                    }
                }
            };
        }

        // Creates the reusable Marketplace/Lost & Found post-source filter.
        public static FilterDefinition PostType(string? selectedValue = null)
        {
            return new FilterDefinition
            {
                // This becomes the "type" query-string parameter.
                ParameterName = "type",

                // Text displayed above the dropdown.
                Label = "Post Type",

                // Preserve the current value supplied by the controller.
                SelectedValue = selectedValue,

                Options =
                {
                    // Empty value displays both post sources.
                    new FilterOption
                    {
                        Label = "All",
                        Value = ""
                    },

                    // Displays Marketplace listings only.
                    new FilterOption
                    {
                        Label = "Marketplace",
                        Value = "Marketplace"
                    },

                    // Displays Lost & Found reports only.
                    new FilterOption
                    {
                        Label = "Lost & Found",
                        Value = "LostFound"
                    }
                }
            };
        }

        // Creates the reusable Lost & Found report-type filter.
        // This separates Lost reports from Found reports.
        public static FilterDefinition LostFoundType(string? selectedValue = null)
        {
            return new FilterDefinition
            {
                // This becomes the "lostFoundType" query-string parameter.
                ParameterName = "lostFoundType",

                // Label displayed above the dropdown.
                Label = "Report Type",

                // Empty value means both Lost and Found reports.
                DefaultValue = "",

                // Preserve the currently selected value after the page reloads.
                SelectedValue = selectedValue,

                Options =
                {
                    // Shows both Lost and Found reports.
                    new FilterOption
                    {
                        Label = "All",
                        Value = ""
                    },

                    // Shows Lost reports only.
                    new FilterOption
                    {
                        Label = "Lost",
                        Value = "Lost"
                    },

                    // Shows Found reports only.
                    new FilterOption
                    {
                        Label = "Found",
                        Value = "Found"
                    }
                }
            };
        }

        // Creates the reusable Marketplace listing-type filter.
        // This separates adoption listings from sale listings.
        public static FilterDefinition ListingType(string? selectedValue = null)
        {
            return new FilterDefinition
            {
                // This becomes the "listingType" query-string parameter.
                ParameterName = "listingType",

                // Label displayed above the dropdown.
                Label = "Listing Type",

                // Empty value means both adoption and sale listings.
                DefaultValue = "",

                // Preserve the currently selected value after the page reloads.
                SelectedValue = selectedValue,

                Options =
                {
                    // Shows both types of Marketplace listings.
                    new FilterOption
                    {
                        Label = "All",
                        Value = ""
                    },

                    // Matches ListType.For_Adoption in the Listing model.
                    new FilterOption
                    {
                        Label = "For Adoption",
                        Value = "For_Adoption"
                    },

                    // Matches ListType.For_Sale in the Listing model.
                    new FilterOption
                    {
                        Label = "For Sale",
                        Value = "For_Sale"
                    }
                }
            };
        }

        // Creates the reusable pet-type filter.
        // This allows pages to display all pets, dogs only, or cats only.
        public static FilterDefinition PetType(string? selectedValue = null)
        {
            return new FilterDefinition
            {
                // This becomes the "petType" query-string parameter.
                ParameterName = "petType",

                // Label displayed above the dropdown.
                Label = "Pet Type",

                // All is the default because no pet-type filtering is applied.
                DefaultValue = "",

                // Preserve the currently selected value after the page reloads.
                SelectedValue = selectedValue,

                Options =
                {
                    // Displays both cats and dogs.
                    new FilterOption
                    {
                        Label = "All",
                        Value = ""
                    },

                    // Displays dogs only.
                    // The value matches the existing ListPetType.Dog enum value.
                    new FilterOption
                    {
                        Label = "Dogs",
                        Value = "Dog"
                    },

                    // Displays cats only.
                    // The value matches the existing ListPetType.Cat enum value.
                    new FilterOption
                    {
                        Label = "Cats",
                        Value = "Cat"
                    }
                }
            };
        }
        // Combines one or more reusable filter definitions into one filter bar.
        // params allows the caller to provide one, two, or many filters.
        public static FilterBarViewModel Create(
            params FilterDefinition[] filters)
        {
            return new FilterBarViewModel
            {
                // Convert the supplied filter definitions into the collection
                // consumed by the reusable _FilterBar partial.
                Filters = filters.ToList()
            };
        }
    }
}