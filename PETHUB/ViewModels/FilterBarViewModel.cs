namespace PETHUB.ViewModels
{
    // Stores the configuration for one reusable filter bar.
    // The actual filter options are supplied by FilterBarHelper.
    public class FilterBarViewModel
    {
        // Name of the query-string parameter, such as "status" or "type".
        public string ParameterName { get; set; } = string.Empty;

        // Options that the shared Razor partial will render.
        public List<FilterOption> Options { get; set; } = new();
    }

    // Represents one button that can be displayed by the filter bar.
    public class FilterOption
    {
        // Text displayed to the user.
        public string Label { get; set; } = string.Empty;

        // Value placed into the query string when selected.
        public string Value { get; set; } = string.Empty;

        // CSS class controlling the button's appearance.
        public string CssClass { get; set; } = "btn-secondary";
    }
}