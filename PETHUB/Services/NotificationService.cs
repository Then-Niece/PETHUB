using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;

namespace PETHUB.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotificationAsync(
            string userId,
            NotificationType type,
            string title,
            string message,
            string? imagePath = null,
            string? redirectUrl = null,
            int? listingId = null,
            int? lostFoundId = null,
            int? petFeedId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                ImagePath = imagePath,
                RedirectUrl = redirectUrl,
                ListingId = listingId,
                LostFoundId = lostFoundId,
                PetFeedId = petFeedId,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
        }


        public async Task NotifyNearbyMembersAsync(LostFound report)
        {
            if (string.IsNullOrWhiteSpace(report.City))
            {
                return;
            }

            var members = await _context.Users
                .Where(u =>
                    u.City == report.City &&
                    u.Id != report.UserId)
                .ToListAsync();

            foreach (var member in members)
            {
                if (report.Type == LostFoundType.Lost)
                {
                    await CreateNotificationAsync(
                        member.Id,
                        NotificationType.LostPetNearby,
                        "Lost Pet Nearby",
                        $"A lost pet has been reported in {report.City}. Tap to view the report and help reunite the pet with its owner.",
                        report.Images.FirstOrDefault()?.ImagePath,
                        "/LostFounds/BrowseDetails/" + report.LostFoundId,
                        lostFoundId: report.LostFoundId
                    );
                }
                else
                {
                    await CreateNotificationAsync(
                        member.Id,
                        NotificationType.FoundPetNearby,
                        "Found Pet Nearby",
                        $"A found pet has been reported in {report.City}. Tap to view the report and help identify its owner.",
                        report.Images.FirstOrDefault()?.ImagePath,
                        "/LostFounds/BrowseDetails/" + report.LostFoundId,
                        lostFoundId: report.LostFoundId
                    );
                }
            }
        }

        // Creates or updates the single aggregate report notification for each Admin.
        // Instead of creating one notification per report, every Admin has only one
        // NewUserReport notification whose message represents the current pending count.
        public async Task UpdateAdminReportNotificationAsync(
            IEnumerable<ApplicationUser> admins)
        {
            // Count only reports that are still waiting for Admin action.
            // Dismissed and Resolved reports are excluded because they are already finalized.
            var pendingReportCount = await _context.UserReports
                .CountAsync(r => r.Status == UserReportStatus.Pending);

            // Process each Admin using the Admin list supplied by the controller.
            foreach (var admin in admins)
            {
                // Find this Admin's existing aggregate report notification.
                // There should be only one NewUserReport notification for each Admin.
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n =>
                        n.UserId == admin.Id &&
                        n.Type == NotificationType.NewUserReport);

                // If there are no pending reports, the aggregate notification should not exist.
                if (pendingReportCount == 0)
                {
                    // Remove the notification if one was previously created.
                    if (notification != null)
                    {
                        _context.Notifications.Remove(notification);
                    }

                    // Move to the next Admin instead of creating an empty notification.
                    continue;
                }

                // Use singular wording when exactly one pending report exists.
                // Otherwise, display the total number of pending reports.
                var message = pendingReportCount == 1
                    ? "A new report has been submitted and is waiting for review."
                    : $"{pendingReportCount} new reports have been submitted and are waiting for review.";

                if (notification == null)
                {
                    // Create the first aggregate notification for this Admin.
                    notification = new Notification
                    {
                        UserId = admin.Id,

                        // This identifies the notification as the special aggregate
                        // notification for the Admin's report queue.
                        Type = NotificationType.NewUserReport,

                        Title = "New Reports",
                        Message = message,

                        // The notification opens the dedicated Admin Reports page
                        // instead of pointing to one specific reported post.
                        RedirectUrl = "/Reports",

                        // A newly created/updated report notification should appear unread.
                        IsRead = false,
                        IsSeen = false,

                        // Record when the aggregate notification was created/updated.
                        CreatedAt = DateTime.Now
                    };

                    // Add the new notification to EF Core's change tracker.
                    _context.Notifications.Add(notification);
                }
                else
                {
                    // Update the existing notification instead of creating another one.
                    notification.Title = "New Reports";
                    notification.Message = message;
                    notification.RedirectUrl = "/Reports";

                    // Treat the updated aggregate notification as new/unread.
                    notification.IsRead = false;
                    notification.IsSeen = false;

                    // Refresh the timestamp so the updated notification moves to the
                    // current position in the existing notification list.
                    notification.CreatedAt = DateTime.Now;
                }
            }

            // Save all Admin notification changes together.
            // EF Core inserts, updates, or deletes the tracked notification records.
            await _context.SaveChangesAsync();
        }

        // Removes the aggregate Admin report notification when no Pending reports remain.
        // If at least one Pending report still exists, the existing notification is left
        // unchanged, which preserves the user's required notification update behavior.
        public async Task RemoveAdminReportNotificationIfNoPendingAsync(
            IEnumerable<ApplicationUser> admins)
        {
            // Count reports that still require Admin review.
            // Only Pending reports keep the aggregate Admin notification alive.
            var pendingReportCount = await _context.UserReports
                .CountAsync(r => r.Status == UserReportStatus.Pending);

            // If reports are still pending, do not modify the Admin notification.
            if (pendingReportCount > 0)
            {
                return;
            }

            // Process every Admin supplied by the controller.
            foreach (var admin in admins)
            {
                // Locate this Admin's single aggregate report notification.
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n =>
                        n.UserId == admin.Id &&
                        n.Type == NotificationType.NewUserReport);

                // Delete the notification when no Pending reports remain.
                if (notification != null)
                {
                    _context.Notifications.Remove(notification);
                }
            }

            // Commit all notification deletions to the database.
            await _context.SaveChangesAsync();
        }

        //Add more here if needed in the future
    }
}