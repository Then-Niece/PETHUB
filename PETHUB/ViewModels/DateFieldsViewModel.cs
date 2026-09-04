namespace PETHUB.ViewModels
{
    public class DateFieldsViewModel
    {
        // Name of the actual DateTime property.
        // Examples:
        // Birthdate
        // LostDate
        public string FieldName { get; set; } = string.Empty;


        // Label displayed above the fields.
        public string Label { get; set; } = "Date";


        // Existing value when editing.
        public DateTime? Value { get; set; }


        // Can this field accept dates after today?
        public bool AllowFuture { get; set; } = true;


        // Optional minimum age.
        // Example:
        // Birthdate = 18
        // LostDate = null
        public int? MinimumAge { get; set; }


        // Lowest year shown in the dropdown.
        public int MinimumYear { get; set; } = 1900;
    }
}