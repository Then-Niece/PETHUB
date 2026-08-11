using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.ViewModels;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;


    public NotificationsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    
    public async Task<IActionResult> GetNotifications()
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .Select(n => new NotificationViewModel
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                ImagePath = n.ImagePath,
                RedirectUrl = n.RedirectUrl,
                IsRead = n.IsRead,
                IsSeen = n.IsSeen,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        //  Mark notification as seen when they are loaded
        var unseenNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsSeen)
            .ToListAsync();

        foreach (var notification in unseenNotifications)
        {
            notification.IsSeen = true;
        }

        if (unseenNotifications.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        return PartialView("_NotificationDropdown", notifications);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationSeen(int id)
    {
        var userId = _userManager.GetUserId(User);

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.NotificationId == id &&
                n.UserId == userId);

        if (notification == null)
            return NotFound();

        notification.IsSeen = true;

        await _context.SaveChangesAsync();

        return Ok();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.NotificationId == id &&
                n.UserId == userId);

        if (notification == null)
            return NotFound();

        notification.IsRead = true;
        notification.IsSeen = true;

        await _context.SaveChangesAsync();

        return Ok();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.NotificationId == id &&
                n.UserId == userId);

        if (notification == null)
            return NotFound();

        _context.Notifications.Remove(notification);

        await _context.SaveChangesAsync();

        return Ok();
    }


    [HttpGet]
    public async Task<IActionResult> GetUnreadNotificationCount()
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var count = await _context.Notifications
            .CountAsync(n =>
                n.UserId == userId &&
                !n.IsRead);

        return Json(count);
    }
}