namespace PETHUB.ViewModels
{
    // Represents the complete reusable filter bar.
    // It supports one filter or multiple filters on the same page.
    public class FilterBarViewModel
    {
        // Contains all filter definitions that should be displayed.
        // One definition creates one dropdown; multiple definitions create multiple dropdowns.
        public List<FilterDefinition> Filters { get; set; } = new();
    }

    // Represents one individual filter inside the filter bar.
    public class FilterDefinition
    {
        // Query-string parameter used by this filter.
        // Examples: "status" and "type".
        public string ParameterName { get; set; } = string.Empty;

        // Text displayed above the dropdown.
        // Examples: "Status" and "Post Type".
        public string Label { get; set; } = string.Empty;

        // Value used when no selection has been supplied.
        // For MyPosts, Status defaults to Pending while Post Type defaults to All.
        public string DefaultValue { get; set; } = string.Empty;

        // Current value selected by the user.
        // This value is supplied by the controller/ViewModel after a GET request.
        public string? SelectedValue { get; set; }

        // Available options for this individual filter.
        public List<FilterOption> Options { get; set; } = new();
    }

    // Represents one selectable option inside a filter.
    public class FilterOption
    {
        // Text displayed to the user.
        public string Label { get; set; } = string.Empty;

        // Value sent through the query string.
        public string Value { get; set; } = string.Empty;
    }
}