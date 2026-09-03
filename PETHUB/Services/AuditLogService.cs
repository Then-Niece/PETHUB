using Microsoft.AspNetCore.Identity;
using PETHUB.Models;

namespace PETHUB.Services
{
    public class AuditLogService
    {
        // UserManager is used to work with the existing ASP.NET Identity users
        // and determine the role of the user creating the audit record.
        private readonly UserManager<ApplicationUser> _userManager;

        // ApplicationDbContext provides access to the AuditLogs table through EF Core.
        private readonly Data.ApplicationDbContext _context;

        // Dependency Injection supplies UserManager and ApplicationDbContext
        // automatically when this service is requested by a controller.
        public AuditLogService(
            UserManager<ApplicationUser> userManager,
            Data.ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Creates one general audit log record for the specified user action.
        // The service determines the user's current Identity role and stores
        // the exact event time using UTC.
        public async Task LogAsync(
            ApplicationUser user,
            string action,
            string? description = null)
        {
            // GetRolesAsync is an ASP.NET Identity function that retrieves
            // all roles currently assigned to the user.
            var roles = await _userManager.GetRolesAsync(user);

            // PETHUB currently uses Admin and Member roles.
            // FirstOrDefault returns the first matching role, or null when
            // the user does not currently have a role.
            var role = roles.FirstOrDefault() ?? "Unknown";

            // Create the audit record that will be inserted into the database.
            var log = new AuditLog
            {
                UserId = user.Id,
                Role = role,
                Action = action,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            // Add the new record to EF Core's change tracker.
            _context.AuditLogs.Add(log);

            // SaveChangesAsync commits the new audit record to SQL Server.
            await _context.SaveChangesAsync();
        }
    }
}