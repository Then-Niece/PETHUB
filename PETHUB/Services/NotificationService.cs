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
            if (string.IsNullOrWhiteSpace(report.City)) return;

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

    }
}