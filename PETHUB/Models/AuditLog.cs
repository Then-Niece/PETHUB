namespace PETHUB.Models
{
    public class AuditLog
    {
        // Unique ID for this audit log record.
        // EF Core will use this property as the primary key by convention.
        public int Id { get; set; }

        // Stores the Identity User ID of the person who performed the action.
        // This connects the log to the existing ApplicationUser record.
        public string UserId { get; set; } = string.Empty;

        // Stores the user's role at the time the action happened.
        // This allows the log to distinguish Administrator activity from Member activity
        // even if the user's role changes later.
        public string Role { get; set; } = string.Empty;

        // Stores the general action name.
        // Examples: "Logged In", "Logged Out", "Profile Updated".
        public string Action { get; set; } = string.Empty;

        // Optional additional information about the action.
        // This can be used later when an action needs more context without creating
        // another type of audit event.
        public string? Description { get; set; }

        // Stores the exact date and time when the action occurred.
        // UTC is used so the database has one consistent time standard.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}