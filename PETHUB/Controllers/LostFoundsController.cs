using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;
using PETHUB.Services;

public class LostFoundsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notificationService;
    private readonly AuditLogService _auditLogService;
    public LostFoundsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService, AuditLogService auditLogService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
    }

    // GET: LostFounds
    // Supports approval status, Lost/Found report type, and pet type filters.
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(
        string? status,
        string? lostFoundType,
        string? petType)
    {
        // Start with all Lost & Found reports and load the related
        // user and image data required by the existing approval view.
        var lostfounds = _context.LostFounds
            .Include(l => l.User)
            .Include(l => l.Images)
            .AsQueryable();

        // Apply the existing approval-status filter.
        // Lost & Found uses its own ApprovalStatus enum.
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ApprovalStatus>(status, out var selectedStatus))
        {
            // EF Core translates this comparison into a database WHERE condition.
            lostfounds = lostfounds.Where(l => l.Status == selectedStatus);
        }

        // Apply the Lost/Found report-type filter.
        // LostFoundType separates Lost reports from Found reports.
        if (!string.IsNullOrWhiteSpace(lostFoundType) &&
            Enum.TryParse<LostFoundType>(
                lostFoundType,
                out var selectedReportType))
        {
            // Filter the query using the LostFound.Type property.
            lostfounds = lostfounds.Where(l => l.Type == selectedReportType);
        }

        // Apply the Dog/Cat filter.
        // Lost & Found uses its own PetType enum.
        if (!string.IsNullOrWhiteSpace(petType) &&
            Enum.TryParse<PetType>(
                petType,
                out var selectedPetType))
        {
            // Filter the query using the LostFound.PetType property.
            lostfounds = lostfounds.Where(l => l.PetType == selectedPetType);
        }

        // Execute the query after all selected filters have been applied.
        return View(await lostfounds.ToListAsync());
    }

    // GET: LostFounds/Details/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Details(int? id)
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

    // GET: LostFounds/Approve
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var report = await _context.LostFounds
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.LostFoundId == id);

        if (report == null)
        {
            return NotFound();
        }

        //Approve report
        report.Status = ApprovalStatus.Approved;

        await _context.SaveChangesAsync();

        // Retrieves the Admin who successfully approved the Lost & Found report.
        // UserManager gets the ApplicationUser associated with the current login.
        var currentUser = await _userManager.GetUserAsync(User);

        // Records the approval only after the report status has been
        // successfully saved as Approved in the database.
        if (currentUser != null)
        {
            await _auditLogService.LogAsync(
                currentUser,
                "Approved Post");
        }

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
        if (report == null)
        {
            return NotFound();
        }

        report.Status = ApprovalStatus.Rejected;
        await _context.SaveChangesAsync();


        string notificationTitle;
        string notificationMessage;

        if (report.Type == LostFoundType.Lost)
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
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);

        var lostfound = await _context.LostFounds
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l =>
                l.LostFoundId == id &&
                l.UserId == userId);

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
        {
            return NotFound();
        }

        return View(lostfound);
    }

    // POST: LostFounds/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Edit(
    int id,
    LostFound lostFound,
    List<IFormFile> Images,
    List<int> DeletedImageIds)
    {
        // Verify that the ID in the URL matches the ID submitted by the form.
        // This prevents the form from accidentally updating a different report.
        if (id != lostFound.LostFoundId)
        {
            return NotFound();
        }

        // Validate the submitted Lost & Found data before accessing the database.
        // If validation fails, return the submitted model so the user can correct it.
        if (!ModelState.IsValid)
        {
            return View(lostFound);
        }

        // Get the Identity ID of the currently authenticated Member.
        // This is used to ensure that Members can only edit their own reports.
        var userId = _userManager.GetUserId(User);

        // Retrieve the existing report from the database.
        // Images are included because the Edit action can add new images.
        var existing = await _context.LostFounds
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l =>
                l.LostFoundId == id &&
                l.UserId == userId);

        // If the report does not exist or does not belong to the current Member,
        // do not allow the update.
        if (existing == null)
        {
            return NotFound();
        }

        // Only the owner can save changes.
        // Admins bypass this check, although this action currently only allows Members.
        if (!User.IsInRole("Admin") && existing.UserId != userId)
        {
            return Forbid();
        }

        // Members cannot edit an already approved report.
        // Removed, Pending, and Rejected reports remain editable.
        if (!User.IsInRole("Admin") &&
            existing.Status == ApprovalStatus.Approved)
        {
            return Forbid();
        }

        // Remember whether the report was Removed before editing.
        // This allows us to distinguish a Removed report being resubmitted
        // from a normal edit of a Pending or Rejected report.
        bool wasRemoved = existing.Status == ApprovalStatus.Removed;


        // =========================================================
        // UPDATE REPORT INFORMATION
        // =========================================================

        // Copy the editable values from the submitted model into the
        // existing database entity.
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

        // Preserve the existing behavior of updating the report date whenever
        // the report is edited.
        existing.DateReported = DateTime.Now;



        // DELETE MARKED EXISTING IMAGES

        if (DeletedImageIds != null && DeletedImageIds.Any())
        {
            foreach (var imageId in DeletedImageIds)
            {
                var image = existing.Images
                    .FirstOrDefault(i =>
                        i.LostFoundImageId == imageId);

                if (image == null)
                {
                    continue;
                }

                var filePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    image.ImagePath.TrimStart('/')
                );

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.LostFoundImages.Remove(image);
            }
        }




        // =========================================================
        // REMOVED → PENDING
        // =========================================================

        // A Removed report must return to Pending when its owner edits and
        // resubmits it. This sends the corrected report back through the
        // normal Admin approval process.
        if (wasRemoved)
        {
            existing.Status = ApprovalStatus.Pending;
        }


        // =========================================================
        // ADD NEW IMAGES
        // =========================================================

        // Only process the image upload when the Member actually selected
        // one or more non-empty files.
        if (Images != null && Images.Any(i => i.Length > 0))
        {
            // Save the uploaded files using the existing ImageHelper.
            // The helper creates LostFoundImage entities using the report ID
            // and stores them under the existing "lostfound" image location.
            var savedImages = await ImageHelper.SaveImagesAsync(
                Images,
                existing.LostFoundId,
                (imgId, path) => new LostFoundImage
                {
                    LostFoundId = imgId,
                    ImagePath = path
                },
                "lostfound"
            );

            // Add the newly created image entities to Entity Framework's
            // change tracker so they are inserted when SaveChangesAsync runs.
            _context.AddRange(savedImages);
        }


        // =========================================================
        // SAVE CHANGES
        // =========================================================

        // Persist the edited report, its new status if it was Removed,
        // and any newly uploaded images.
        await _context.SaveChangesAsync();

        // Retrieve the Member who successfully edited the Lost & Found post.
        // UserManager gets the ApplicationUser associated with the current login.
        var currentUser = await _userManager.GetUserAsync(User);

        // Record the edit only after the report and its image changes
        // have been successfully saved to the database.
        if (currentUser != null)
        {
            await _auditLogService.LogAsync(
                currentUser,
                "Edited Post");
        }

        // =========================================================
        // ADMIN RESUBMISSION NOTIFICATION
        // =========================================================

        // Only notify Admins when a previously Removed report has been
        // resubmitted. Normal Pending or Rejected edits do not trigger
        // this notification.
        if (wasRemoved)
        {
            // Retrieve all users assigned to the Admin role.
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            // Determine the notification text based on whether the report
            // is a Lost or Found report.
            string notificationTitle;
            string notificationMessage;

            if (existing.Type == LostFoundType.Lost)
            {
                notificationTitle = "Lost Report Resubmitted";
                notificationMessage =
                    "A previously removed Lost Report has been edited and resubmitted for approval.";
            }
            else
            {
                notificationTitle = "Found Report Resubmitted";
                notificationMessage =
                    "A previously removed Found Report has been edited and resubmitted for approval.";
            }

            // Notify every Admin that the corrected report is waiting
            // for another moderation review.
            foreach (var admin in admins)
            {
                await _notificationService.CreateNotificationAsync(
                    admin.Id,
                    NotificationType.NewLostFoundSubmission,
                    notificationTitle,
                    notificationMessage,
                    existing.Images.FirstOrDefault()?.ImagePath,
                    "/LostFounds/Details/" + existing.LostFoundId,
                    lostFoundId: existing.LostFoundId
                );
            }
        }


        // Return the Member to the existing Lost & Found Details page
        // after successfully saving the edited report.
        return RedirectToAction(
            "LostFoundDetails",
            "MyPosts",
            new { id = existing.LostFoundId });
    }

    // GET: LostFounds/Delete/5
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);

        var lostfound = await _context.LostFounds
            .Include(l => l.User)
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l =>
                l.LostFoundId == id &&
                l.UserId == userId);

        if (lostfound == null)
        {
            return NotFound();
        }

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
        {
            return NotFound();
        }

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

        // Retrieve the Member who successfully deleted the Lost & Found post.
        var currentUser = await _userManager.GetUserAsync(User);

        // Record the deletion only after the report has been successfully
        // removed from the database.
        if (currentUser != null)
        {
            await _auditLogService.LogAsync(
                currentUser,
                "Deleted Post");
        }

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
        {
            return NotFound();
        }

        var lostFoundId = await ImageHelper.RemoveImageAsync(
            _context,
            _context.LostFoundImages,
            id,
            img => img.ImagePath,
            img => img.LostFoundId
        // no need to pass ownership check here, already done above
        );

        if (lostFoundId == null)
        {
            return NotFound();
        }

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
        {
            return View(lostFound);
        }

        lostFound.DateReported = DateTime.Now;
        lostFound.Status = ApprovalStatus.Pending;

        if (User.Identity?.IsAuthenticated != true && ClientIdImage != null)
        {
            lostFound.ClientIdImagePath = await ClientIdUploadHelper.SaveClientIdAsync(ClientIdImage);
        }

        _context.Add(lostFound);
        await _context.SaveChangesAsync();

        // Only authenticated users have an Identity account that can be
        // associated with an audit record. Anonymous Lost & Found submissions
        // therefore do not create a Member audit log.
        var currentUser = await _userManager.GetUserAsync(User);

        // Record the successful creation of the Lost & Found post.
        // This happens only after the report has been saved successfully.
        if (currentUser != null)
        {
            await _auditLogService.LogAsync(
                currentUser,
                "Created Post");
        }

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


    // GET: LostFounds/Browse.
    // lostFoundType filters Lost/Found while petType filters Dog/Cat.
    [AllowAnonymous]
    public async Task<IActionResult> Browse(
        string? lostFoundType,
        string? petType)
    {
        // Get the current user's ID so members do not see their own reports.
        var userid = _userManager.GetUserId(User);

        // Start with the existing public Lost & Found rules.
        // Only approved and active reports are displayed.
        var lostfounds = _context.LostFounds
            .Where(l =>
                l.Status == ApprovalStatus.Approved &&
                l.RStatus == ReportStatus.Active &&
                l.UserId != userid)
            .Include(l => l.User)
            .Include(l => l.Images)
            .AsQueryable();

        // Apply the Lost/Found filter when a specific report type was selected.
        // Enum.TryParse converts "Lost" or "Found" into LostFoundType.
        if (!string.IsNullOrWhiteSpace(lostFoundType) &&
            Enum.TryParse<LostFoundType>(
                lostFoundType,
                out var selectedReportType))
        {
            // EF Core filters the query to the selected Lost/Found type.
            lostfounds = lostfounds.Where(l => l.Type == selectedReportType);
        }

        // Apply the Dog/Cat filter when a specific pet type was selected.
        // LostFound uses its own PetType enum, separate from Marketplace's ListPetType.
        if (!string.IsNullOrWhiteSpace(petType) &&
            Enum.TryParse<PetType>(petType, out var selectedPetType))
        {
            // Only reports matching the selected pet type are returned.
            lostfounds = lostfounds.Where(l => l.PetType == selectedPetType);
        }

        // Execute the final query after all selected filters have been applied.
        return View(await lostfounds.ToListAsync());
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
