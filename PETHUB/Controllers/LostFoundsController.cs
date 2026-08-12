using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Services;
using PETHUB.Models;

public class LostFoundsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notificationService;

    public LostFoundsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    // GET: LostFounds
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(string status)
    {
        var lostfounds = _context.LostFounds
            .Include(l => l.User)
            .Include(l => l.Images)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<ApprovalStatus>(status, out var selectedStatus))
            {
                lostfounds = lostfounds.Where(l => l.Status == selectedStatus);
            }
        }

        return View(await lostfounds.ToListAsync());
    }

    // GET: LostFounds/Details/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var lostfound = await _context.LostFounds
            .Include(l => l.User)
            .Include(l => l.Images)
            .FirstOrDefaultAsync(m => m.LostFoundId == id);

        if (lostfound == null) return NotFound();

        return View(lostfound);
    }

    // GET: LostFounds/Approve
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var report = await _context.LostFounds
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.LostFoundId == id);

        if (report == null) return NotFound();

        //Approve report
        report.Status = ApprovalStatus.Approved;

        await _context.SaveChangesAsync();

        // Notify the report owner
        if (!string.IsNullOrEmpty(report.UserId))
        {
            string notificationTitle;
            string notificationMessage;

            if (report.Type == LostFoundType.Lost)
            {
                notificationTitle = "Lost Report Approved";
                notificationMessage = "Your Lost Report has been approved and is now visible in Lost & Found.";
            }
            else
            {
                notificationTitle = "Found Report Approved";
                notificationMessage = "Your Found Report has been approved and is now visible in Lost & Found.";
            }

            await _notificationService.CreateNotificationAsync(
                report.UserId,
                NotificationType.LostFoundApproved,
                notificationTitle,
                notificationMessage,
                report.Images.FirstOrDefault()?.ImagePath,
                "/LostFounds/BrowseDetails/" + report.LostFoundId,
                lostFoundId: report.LostFoundId
            );
        }

        // Notify members in the same city
        await _notificationService.NotifyNearbyMembersAsync(report);


        return RedirectToAction(nameof(Index));
    }

    // GET: LostFounds/Reject
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(int id)
    {
        //retrieve the report with its images to ensure we have all necessary data for notifications
        var report = await _context.LostFounds
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.LostFoundId == id);

        // If the report doesn't exist, return a 404 Not Found response
        if (report == null) return NotFound();

        report.Status = ApprovalStatus.Rejected;
        await _context.SaveChangesAsync();


        string notificationTitle;
        string notificationMessage;

        if(report.Type == LostFoundType.Lost)
        {
            notificationTitle = "Lost Report Rejected";
            notificationMessage = "Your Lost Report has been rejected because it does not meet our community standards.";
        }
        else
        {
            notificationTitle = "Found Report Rejected";
            notificationMessage = "Your Found Report has been rejected because it does not meet our community standards.";
        }

        await _notificationService.CreateNotificationAsync(
            report.UserId,
            NotificationType.LostFoundRejected,
            notificationTitle,
            notificationMessage,
            report.Images.FirstOrDefault()?.ImagePath,
            "/LostFounds/BrowseDetails/" + report.LostFoundId,
            lostFoundId: report.LostFoundId
        );


        return RedirectToAction(nameof(Index));
    }

    // GET: LostFounds/Edit/5
    
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var userId = _userManager.GetUserId(User);

        var lostfound = await _context.LostFounds
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l =>
                l.LostFoundId == id &&
                l.UserId == userId);

        if (lostfound == null)
            return NotFound();

        // Only the owner can edit.
        // Admins (if they ever use this action) bypass this check.
        if (!User.IsInRole("Admin") && lostfound.UserId != userId)
        {
            return Forbid();
        }

        // Approved reports cannot be edited by members.
        if (!User.IsInRole("Admin") &&
            lostfound.Status == ApprovalStatus.Approved)
        {
            return Forbid();
        }

        if (lostfound == null)
            return NotFound();


        return View(lostfound);
    }

    // POST: LostFounds/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Edit(int id, LostFound lostFound, List<IFormFile> Images)
    {
        if (id != lostFound.LostFoundId)
            return NotFound();


        if (!ModelState.IsValid)
            return View(lostFound);


        var userId = _userManager.GetUserId(User);


        var existing = await _context.LostFounds
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l => l.LostFoundId == id && l.UserId == userId);
        if (existing == null)
            return NotFound();

        // Only the owner can save changes. Admins bypass this check.
        if (!User.IsInRole("Admin") && existing.UserId != userId)
        {
            return Forbid();
        }

        // Approved reports cannot be edited by members.
        if (!User.IsInRole("Admin") && existing.Status == ApprovalStatus.Approved)
        {
            return Forbid();
        }

            existing.Title = lostFound.Title;
            existing.Description = lostFound.Description;
            existing.Type = lostFound.Type;
            existing.Breed = lostFound.Breed;
            existing.PetType = lostFound.PetType;
            existing.Sex = lostFound.Sex;
            existing.LostDate = lostFound.LostDate;
            existing.ClientName = lostFound.ClientName;
            existing.ClientContact = lostFound.ClientContact;
            existing.Province = lostFound.Province;
            existing.City = lostFound.City;
            existing.Barangay = lostFound.Barangay;
            existing.StreetAddress = lostFound.StreetAddress;
            existing.DateReported = DateTime.Now;


        if (Images != null && Images.Any(i => i.Length > 0))
        {
            var savedImages = await ImageHelper.SaveImagesAsync(
                Images,
                existing.LostFoundId,
                (imgId, path) => new LostFoundImage { LostFoundId = imgId, ImagePath = path },
                "lostfound"
            );

            _context.AddRange(savedImages);
        }

        // Persist updates (and any new images)
        await _context.SaveChangesAsync();

        // Return members to the report they just edited.
        return RedirectToAction("LostFoundDetails", "MyPosts", new { id = existing.LostFoundId });
    }

    // GET: LostFounds/Delete/5
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var userId = _userManager.GetUserId(User);

        var lostfound = await _context.LostFounds
            .Include(l => l.User)
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l =>
                l.LostFoundId == id &&
                l.UserId == userId);

        if (lostfound == null)
            return NotFound();

        return View(lostfound);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = _userManager.GetUserId(User);

        var lostFound = await _context.LostFounds
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l =>
                l.LostFoundId == id &&
                l.UserId == userId);

        if (lostFound == null)
            return NotFound();

        if (!User.IsInRole("Admin") && lostFound.UserId != userId)
        {
            return Forbid();
        }

        if (lostFound.Images != null && lostFound.Images.Any())
        {
            foreach (var img in lostFound.Images)
            {
                var filePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    img.ImagePath.TrimStart('/')
                );

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.LostFoundImages.Remove(img);
            }
        }

        _context.LostFounds.Remove(lostFound);

        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "MyPosts");
    }


    // POST: LostFounds/RemoveImage/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> RemoveImage(int id)
    {
        var userId = _userManager.GetUserId(User);

        // Load image with LostFound included so we can check ownership
        var image = await _context.LostFoundImages
            .Include(i => i.LostFound)
            .FirstOrDefaultAsync(i => i.LostFoundImageId == id);

        if (image == null || image.LostFound.UserId != userId)
            return NotFound();

        var lostFoundId = await ImageHelper.RemoveImageAsync(
            _context,
            _context.LostFoundImages,
            id,
            img => img.ImagePath,
            img => img.LostFoundId
        // no need to pass ownership check here, already done above
        );

        if (lostFoundId == null) return NotFound();
        return RedirectToAction("Edit", new { id = lostFoundId });
    }





    // GET: LostFounds/Create
    [AllowAnonymous]
    public IActionResult Create() => View();

    // POST: LostFounds/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Create(LostFound lostFound, List<IFormFile> Images, IFormFile? ClientIdImage)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            lostFound.UserId = _userManager.GetUserId(User);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(lostFound.ClientName))
            {
                ModelState.AddModelError(nameof(LostFound.ClientName),
                    "Name is required.");
            }

            if (string.IsNullOrWhiteSpace(lostFound.ClientContact))
            {
                ModelState.AddModelError(nameof(LostFound.ClientContact),
                    "Contact number is required.");
            }

            if (ClientIdImage == null)
            {
                ModelState.AddModelError("ClientIdImage",
                    "A valid ID is required.");
            }
        }

        if (!ModelState.IsValid)
            return View(lostFound);
        

        lostFound.DateReported = DateTime.Now;
        lostFound.Status = ApprovalStatus.Pending;

        if (User.Identity?.IsAuthenticated != true && ClientIdImage != null)
        {
            lostFound.ClientIdImagePath = await ClientIdUploadHelper.SaveClientIdAsync(ClientIdImage);
        }

        _context.Add(lostFound);
        await _context.SaveChangesAsync();

        string? imagePath = null;

        if (Images != null && Images.Count > 0)
        {
            try
            {
                var savedImages = await ImageHelper.SaveImagesAsync(
                    Images,
                    lostFound.LostFoundId,
                    (id, path) => new LostFoundImage { LostFoundId = id, ImagePath = path },
                    "lostfound"
                );

                _context.AddRange(savedImages);
                await _context.SaveChangesAsync();

                // Get the path of the first saved image for notification purposes
                imagePath = savedImages
                .FirstOrDefault()
                ?.ImagePath;

            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Some images could not be uploaded.");
                return View(lostFound);
            }
        }

        //gets all of the admins
        var admins = await _userManager.GetUsersInRoleAsync("Admin");

        // Determine notification content based on listing type
        string notificationTitle;
        string notificationMessage;

        if (lostFound.Type == LostFoundType.Lost)
        {
            notificationTitle = "Lost Report Approval";
            notificationMessage = "A new Lost Report is waiting for Approval.";
        }
        else
        {
            notificationTitle = "Found Report Approval";
            notificationMessage = "A new Found Report is waiting for Approval.";
        }

        // Send notification to all admins
        foreach (var admin in admins)
        {
            await _notificationService.CreateNotificationAsync(
                admin.Id,
                NotificationType.NewLostFoundSubmission,
                notificationTitle,
                notificationMessage,
                imagePath,
                "/LostFounds/Details/" + lostFound.LostFoundId,
                lostFoundId: lostFound.LostFoundId
            );
        }

        return RedirectToAction(nameof(SubmissionPending));
    }



    // GET: LostFounds/SubmissionPending
    [AllowAnonymous]
    public IActionResult SubmissionPending()
    {
        return View();
    }


    // GET: LOSTFOUNDS
    [AllowAnonymous]
    public async Task<IActionResult> Browse()
    {
        var userid = _userManager.GetUserId(User);

        var lostfounds = await _context.LostFounds
            .Where(l => l.Status == ApprovalStatus.Approved && l.RStatus == ReportStatus.Active)
            .Include(l => l.User)
            .Include(l => l.Images)
            .Where(l => l.UserId != userid)
            .ToListAsync();

        return View(lostfounds);
    }

    // GET: LOSTFOUNDS/Details/5
    [AllowAnonymous]
    public async Task<IActionResult> BrowseDetails(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var lostfound = await _context.LostFounds
                .Include(l => l.User)
                .Include(l => l.Images)
                .FirstOrDefaultAsync(m => m.LostFoundId == id);


        if (lostfound == null)
        {
            return NotFound();
        }

        return View(lostfound);
    }

    private bool LostFoundExists(int id)
    {
        return _context.LostFounds.Any(e => e.LostFoundId == id);
    }
}
