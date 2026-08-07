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

    }
}