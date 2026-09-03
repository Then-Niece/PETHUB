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


    public LostFoundsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        NotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }


    // =========================================================
    // ADMIN - LOST & FOUND INDEX
    // =========================================================

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(
        string? status,
        string? lostFoundType,
        string? petType)
    {
        var lostFounds =
            _context.LostFounds
                .Include(l => l.User)
                .Include(l => l.Images)
                .AsQueryable();


        // ---------------------------------------------------------
        // APPROVAL STATUS FILTER
        // ---------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ApprovalStatus>(
                status,
                out var selectedStatus))
        {
            lostFounds =
                lostFounds.Where(
                    l => l.Status == selectedStatus
                );
        }


        // ---------------------------------------------------------
        // LOST / FOUND FILTER
        // ---------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(lostFoundType) &&
            Enum.TryParse<LostFoundType>(
                lostFoundType,
                out var selectedReportType))
        {
            lostFounds =
                lostFounds.Where(
                    l => l.Type == selectedReportType
                );
        }


        // ---------------------------------------------------------
        // PET TYPE FILTER
        // ---------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(petType) &&
            Enum.TryParse<PetType>(
                petType,
                out var selectedPetType))
        {
            lostFounds =
                lostFounds.Where(
                    l => l.PetType == selectedPetType
                );
        }


        return View(
            await lostFounds.ToListAsync()
        );
    }


    // =========================================================
    // ADMIN - DETAILS
    // =========================================================

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }


        var lostFound =
            await _context.LostFounds
                .Include(l => l.User)
                .Include(l => l.Images)
                .FirstOrDefaultAsync(
                    l => l.LostFoundId == id
                );


        if (lostFound == null)
        {
            return NotFound();
        }


        return View(lostFound);
    }


    // =========================================================
    // ADMIN - APPROVE
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var report =
            await _context.LostFounds
                .Include(r => r.Images)
                .FirstOrDefaultAsync(
                    r => r.LostFoundId == id
                );


        if (report == null)
        {
            return NotFound();
        }


        // Prevent duplicate approval notifications.
        if (report.Status == ApprovalStatus.Approved)
        {
            TempData["InfoMessage"] =
                "This Lost & Found report is already approved.";

            return RedirectToAction(nameof(Index));
        }


        report.Status =
            ApprovalStatus.Approved;


        await _context.SaveChangesAsync();


        // =====================================================
        // NOTIFY REGISTERED REPORT OWNER
        // =====================================================

        if (!string.IsNullOrEmpty(report.UserId))
        {
            string notificationTitle;
            string notificationMessage;


            if (report.Type == LostFoundType.Lost)
            {
                notificationTitle =
                    "Lost Report Approved";

                notificationMessage =
                    "Your Lost Report has been approved and is now visible in Lost & Found.";
            }
            else
            {
                notificationTitle =
                    "Found Report Approved";

                notificationMessage =
                    "Your Found Report has been approved and is now visible in Lost & Found.";
            }


            await _notificationService
                .CreateNotificationAsync(
                    report.UserId,
                    NotificationType.LostFoundApproved,
                    notificationTitle,
                    notificationMessage,
                    report.Images
                        .FirstOrDefault()
                        ?.ImagePath,
                    "/LostFounds/BrowseDetails/" +
                    report.LostFoundId,
                    lostFoundId:
                        report.LostFoundId
                );
        }


        // =====================================================
        // NOTIFY NEARBY MEMBERS
        // =====================================================

        await _notificationService
            .NotifyNearbyMembersAsync(report);


        TempData["SuccessMessage"] =
            "Lost & Found report approved successfully.";


        return RedirectToAction(nameof(Index));
    }


    // =========================================================
    // ADMIN - REJECT
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(int id)
    {
        var report =
            await _context.LostFounds
                .Include(r => r.Images)
                .FirstOrDefaultAsync(
                    r => r.LostFoundId == id
                );


        if (report == null)
        {
            return NotFound();
        }


        // Prevent duplicate rejection notifications.
        if (report.Status == ApprovalStatus.Rejected)
        {
            TempData["InfoMessage"] =
                "This Lost & Found report is already rejected.";

            return RedirectToAction(nameof(Index));
        }


        report.Status =
            ApprovalStatus.Rejected;


        await _context.SaveChangesAsync();


        // =====================================================
        // NOTIFY REGISTERED REPORT OWNER
        // =====================================================

        /*
         * Guest-created reports do not have a UserId,
         * so only registered Members can receive an
         * in-app account notification.
         */
        if (!string.IsNullOrEmpty(report.UserId))
        {
            string notificationTitle;
            string notificationMessage;


            if (report.Type == LostFoundType.Lost)
            {
                notificationTitle =
                    "Lost Report Rejected";

                notificationMessage =
                    "Your Lost Report has been rejected because it does not meet our community standards.";
            }
            else
            {
                notificationTitle =
                    "Found Report Rejected";

                notificationMessage =
                    "Your Found Report has been rejected because it does not meet our community standards.";
            }


            await _notificationService
                .CreateNotificationAsync(
                    report.UserId,
                    NotificationType.LostFoundRejected,
                    notificationTitle,
                    notificationMessage,
                    report.Images
                        .FirstOrDefault()
                        ?.ImagePath,
                    "/LostFounds/BrowseDetails/" +
                    report.LostFoundId,
                    lostFoundId:
                        report.LostFoundId
                );
        }


        TempData["SuccessMessage"] =
            "Lost & Found report rejected successfully.";


        return RedirectToAction(nameof(Index));
    }


    // =========================================================
    // MEMBER - EDIT GET
    // =========================================================

    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }


        var userId =
            _userManager.GetUserId(User);


        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }


        var lostFound =
            await _context.LostFounds
                .Include(l => l.Images)
                .FirstOrDefaultAsync(
                    l =>
                        l.LostFoundId == id &&
                        l.UserId == userId
                );


        // The report does not exist or
        // does not belong to this Member.
        if (lostFound == null)
        {
            return NotFound();
        }


        // Approved reports cannot be edited
        // by Members.
        if (lostFound.Status ==
            ApprovalStatus.Approved)
        {
            return Forbid();
        }


        return View(lostFound);
    }


    // =========================================================
    // MEMBER - EDIT POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Edit(
        int id,
        LostFound lostFound,
        List<IFormFile> Images,
        List<int> DeletedImageIds)
    {
        if (id != lostFound.LostFoundId)
        {
            return NotFound();
        }


        var userId =
            _userManager.GetUserId(User);


        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }


        // =====================================================
        // LOAD EXISTING REPORT
        // =====================================================

        var existing =
            await _context.LostFounds
                .Include(l => l.Images)
                .FirstOrDefaultAsync(
                    l =>
                        l.LostFoundId == id &&
                        l.UserId == userId
                );


        if (existing == null)
        {
            return NotFound();
        }


        // Members cannot edit approved reports.
        if (existing.Status ==
            ApprovalStatus.Approved)
        {
            return Forbid();
        }


        // =====================================================
        // MODEL VALIDATION
        // =====================================================

        if (!ModelState.IsValid)
        {
            /*
             * Restore important database values
             * needed by the Edit view.
             */
            lostFound.Images =
                existing.Images;

            lostFound.UserId =
                existing.UserId;

            lostFound.Status =
                existing.Status;


            return View(lostFound);
        }


        var wasRemoved =
            existing.Status ==
            ApprovalStatus.Removed;


        // =====================================================
        // UPDATE REPORT INFORMATION
        // =====================================================

        existing.Title =
            lostFound.Title;

        existing.Description =
            lostFound.Description;

        existing.Type =
            lostFound.Type;

        existing.Breed =
            lostFound.Breed;

        existing.PetType =
            lostFound.PetType;

        existing.Sex =
            lostFound.Sex;

        existing.LostDate =
            lostFound.LostDate;

        existing.ClientName =
            lostFound.ClientName;

        existing.ClientContact =
            lostFound.ClientContact;

        existing.Province =
            lostFound.Province;

        existing.City =
            lostFound.City;

        existing.Barangay =
            lostFound.Barangay;

        existing.StreetAddress =
            lostFound.StreetAddress;


        existing.DateReported =
            DateTime.Now;


        // =====================================================
        // DELETE SELECTED EXISTING IMAGES
        // =====================================================

        if (DeletedImageIds != null &&
            DeletedImageIds.Any())
        {
            foreach (var imageId
                     in DeletedImageIds)
            {
                var image =
                    existing.Images
                        .FirstOrDefault(
                            i =>
                                i.LostFoundImageId ==
                                imageId
                        );


                if (image == null)
                {
                    continue;
                }


                var filePath =
                    Path.Combine(
                        Directory
                            .GetCurrentDirectory(),
                        "wwwroot",
                        image.ImagePath
                            .TrimStart('/')
                    );


                if (System.IO.File
                    .Exists(filePath))
                {
                    System.IO.File
                        .Delete(filePath);
                }


                _context
                    .LostFoundImages
                    .Remove(image);
            }
        }


        // =====================================================
        // REMOVED -> PENDING
        // =====================================================

        if (wasRemoved)
        {
            existing.Status =
                ApprovalStatus.Pending;
        }


        // =====================================================
        // ADD NEW IMAGES
        // =====================================================

        if (Images != null &&
            Images.Any(
                image =>
                    image.Length > 0))
        {
            var savedImages =
                await ImageHelper
                    .SaveImagesAsync(
                        Images,
                        existing.LostFoundId,

                        (lostFoundId, path) =>
                            new LostFoundImage
                            {
                                LostFoundId =
                                    lostFoundId,

                                ImagePath =
                                    path
                            },

                        "lostfound"
                    );


            _context.AddRange(
                savedImages
            );
        }


        // =====================================================
        // SAVE CHANGES
        // =====================================================

        await _context
            .SaveChangesAsync();


        // =====================================================
        // NOTIFY ADMINS WHEN REMOVED REPORT IS RESUBMITTED
        // =====================================================

        if (wasRemoved)
        {
            var admins =
                await _userManager
                    .GetUsersInRoleAsync(
                        "Admin"
                    );


            string notificationTitle;
            string notificationMessage;


            if (existing.Type ==
                LostFoundType.Lost)
            {
                notificationTitle =
                    "Lost Report Resubmitted";

                notificationMessage =
                    "A previously removed Lost Report has been edited and resubmitted for approval.";
            }
            else
            {
                notificationTitle =
                    "Found Report Resubmitted";

                notificationMessage =
                    "A previously removed Found Report has been edited and resubmitted for approval.";
            }


            foreach (var admin
                     in admins)
            {
                await _notificationService
                    .CreateNotificationAsync(
                        admin.Id,
                        NotificationType
                            .NewLostFoundSubmission,
                        notificationTitle,
                        notificationMessage,
                        existing.Images
                            .FirstOrDefault()
                            ?.ImagePath,
                        "/LostFounds/Details/" +
                        existing.LostFoundId,
                        lostFoundId:
                            existing.LostFoundId
                    );
            }


            TempData["SuccessMessage"] =
                "Your Lost & Found report has been updated and resubmitted for approval.";
        }
        else
        {
            TempData["SuccessMessage"] =
                "Lost & Found report updated successfully.";
        }


        return RedirectToAction(
            "LostFoundDetails",
            "MyPosts",
            new
            {
                id = existing.LostFoundId
            }
        );
    }

    // =========================================================
    // MEMBER - REMOVE INDIVIDUAL IMAGE
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> RemoveImage(
        int id)
    {
        var userId =
            _userManager.GetUserId(User);


        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }


        var image =
            await _context
                .LostFoundImages
                .Include(i => i.LostFound)
                .FirstOrDefaultAsync(
                    i =>
                        i.LostFoundImageId == id
                );


        if (image == null ||
            image.LostFound == null ||
            image.LostFound.UserId != userId)
        {
            return NotFound();
        }


        var lostFoundId =
            await ImageHelper
                .RemoveImageAsync(
                    _context,
                    _context.LostFoundImages,
                    id,
                    img => img.ImagePath,
                    img => img.LostFoundId
                );


        if (lostFoundId == null)
        {
            return NotFound();
        }


        return RedirectToAction(
            nameof(Edit),
            new
            {
                id = lostFoundId
            }
        );
    }


    // =========================================================
    // CREATE - GET
    // =========================================================

    [AllowAnonymous]
    public IActionResult Create()
    {
        return View();
    }


    // =========================================================
    // CREATE - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Create(
        LostFound lostFound,
        List<IFormFile> Images,
        IFormFile? ClientIdImage)
    {
        // =====================================================
        // REGISTERED MEMBER
        // =====================================================

        if (User.Identity?.IsAuthenticated == true)
        {
            lostFound.UserId =
                _userManager.GetUserId(User);
        }


        // =====================================================
        // GUEST SUBMISSION
        // =====================================================

        else
        {
            if (string.IsNullOrWhiteSpace(
                lostFound.ClientName))
            {
                ModelState.AddModelError(
                    nameof(
                        LostFound.ClientName),
                    "Name is required."
                );
            }


            if (string.IsNullOrWhiteSpace(
                lostFound.ClientContact))
            {
                ModelState.AddModelError(
                    nameof(
                        LostFound.ClientContact),
                    "Contact number is required."
                );
            }


            if (ClientIdImage == null)
            {
                ModelState.AddModelError(
                    "ClientIdImage",
                    "A valid ID is required."
                );
            }
        }


        if (!ModelState.IsValid)
        {
            return View(lostFound);
        }


        // =====================================================
        // INITIAL REPORT VALUES
        // =====================================================

        lostFound.DateReported =
            DateTime.Now;

        lostFound.Status =
            ApprovalStatus.Pending;


        // =====================================================
        // GUEST ID IMAGE
        // =====================================================

        if (User.Identity?.IsAuthenticated != true &&
            ClientIdImage != null)
        {
            lostFound.ClientIdImagePath =
                await ClientIdUploadHelper
                    .SaveClientIdAsync(
                        ClientIdImage
                    );
        }


        // =====================================================
        // SAVE REPORT FIRST
        // =====================================================

        _context.Add(lostFound);


        await _context
            .SaveChangesAsync();


        string? imagePath = null;


        // =====================================================
        // SAVE REPORT IMAGES
        // =====================================================

        if (Images != null &&
            Images.Count > 0)
        {
            try
            {
                var savedImages =
                    await ImageHelper
                        .SaveImagesAsync(
                            Images,
                            lostFound.LostFoundId,

                            (lostFoundId, path) =>
                                new LostFoundImage
                                {
                                    LostFoundId =
                                        lostFoundId,

                                    ImagePath =
                                        path
                                },

                            "lostfound"
                        );


                _context.AddRange(
                    savedImages
                );


                await _context
                    .SaveChangesAsync();


                imagePath =
                    savedImages
                        .FirstOrDefault()
                        ?.ImagePath;
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "Some images could not be uploaded."
                );


                return View(lostFound);
            }
        }


        // =====================================================
        // NOTIFY ADMINS
        // =====================================================

        var admins =
            await _userManager
                .GetUsersInRoleAsync(
                    "Admin"
                );


        string notificationTitle;
        string notificationMessage;


        if (lostFound.Type ==
            LostFoundType.Lost)
        {
            notificationTitle =
                "Lost Report Approval";

            notificationMessage =
                "A new Lost Report is waiting for Approval.";
        }
        else
        {
            notificationTitle =
                "Found Report Approval";

            notificationMessage =
                "A new Found Report is waiting for Approval.";
        }


        foreach (var admin
                 in admins)
        {
            await _notificationService
                .CreateNotificationAsync(
                    admin.Id,
                    NotificationType
                        .NewLostFoundSubmission,
                    notificationTitle,
                    notificationMessage,
                    imagePath,
                    "/LostFounds/Details/" +
                    lostFound.LostFoundId,
                    lostFoundId:
                        lostFound.LostFoundId
                );
        }


        return RedirectToAction(
            nameof(SubmissionPending)
        );
    }


    // =========================================================
    // SUBMISSION PENDING
    // =========================================================

    [AllowAnonymous]
    public IActionResult SubmissionPending()
    {
        return View();
    }


    // =========================================================
    // PUBLIC - BROWSE
    // =========================================================

    [AllowAnonymous]
    public async Task<IActionResult> Browse(
        string? lostFoundType,
        string? petType)
    {
        var userId =
            _userManager.GetUserId(User);


        /*
         * Public Lost & Found rules:
         *
         * 1. Report must be approved.
         * 2. Report itself must still be active.
         * 3. Logged-in Members do not see their own
         *    reports in Browse.
         * 4. Guest-created reports remain visible.
         * 5. Reports belonging to Inactive Members
         *    are hidden.
         */
        var lostFounds =
            _context.LostFounds

                .Include(l => l.User)
                .Include(l => l.Images)

                .Where(l =>

                    l.Status ==
                    ApprovalStatus.Approved

                    &&

                    l.RStatus ==
                    ReportStatus.Active

                    &&

                    (
                        userId == null ||
                        l.UserId != userId
                    )

                    &&

                    (
                        // Guest-created report.
                        l.UserId == null

                        ||

                        // Registered owner must
                        // still be active.
                        (
                            l.User != null &&
                            l.User.Status ==
                            UserStatus.Active
                        )
                    )
                )

                .AsQueryable();


        // ---------------------------------------------------------
        // LOST / FOUND FILTER
        // ---------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                lostFoundType) &&
            Enum.TryParse<LostFoundType>(
                lostFoundType,
                out var selectedReportType))
        {
            lostFounds =
                lostFounds.Where(
                    l =>
                        l.Type ==
                        selectedReportType
                );
        }


        // ---------------------------------------------------------
        // PET TYPE FILTER
        // ---------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                petType) &&
            Enum.TryParse<PetType>(
                petType,
                out var selectedPetType))
        {
            lostFounds =
                lostFounds.Where(
                    l =>
                        l.PetType ==
                        selectedPetType
                );
        }


        return View(
            await lostFounds.ToListAsync()
        );
    }


    // =========================================================
    // PUBLIC - BROWSE DETAILS
    // =========================================================

    [AllowAnonymous]
    public async Task<IActionResult> BrowseDetails(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }


        // =====================================================
        // LOAD REPORT
        // =====================================================

        var lostFound =
            await _context.LostFounds
                .Include(l => l.User)
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l =>
                    l.LostFoundId == id
                );


        if (lostFound == null)
        {
            return NotFound();
        }


        // =====================================================
        // OWNER REDIRECT
        // =====================================================
        //
        // Guest-created reports have no UserId, so they are
        // unaffected by this check.
        //
        // If a logged-in Member owns this report, send them to
        // the owner-specific My Posts details page.
        // =====================================================

        if (
            User.Identity?.IsAuthenticated == true &&
            lostFound.UserId != null
        )
        {
            var currentUserId =
                _userManager.GetUserId(User);

            if (lostFound.UserId == currentUserId)
            {
                return RedirectToAction(
                    "LostFoundDetails",
                    "MyPosts",
                    new
                    {
                        id = lostFound.LostFoundId
                    }
                );
            }
        }


        // =====================================================
        // PUBLIC AVAILABILITY CHECK
        // =====================================================

        if (
            lostFound.Status != ApprovalStatus.Approved ||
            lostFound.RStatus != ReportStatus.Active
        )
        {
            return NotFound();
        }


        // =====================================================
        // REGISTERED OWNER STATUS CHECK
        // =====================================================
        //
        // Guest-created reports are allowed.
        //
        // Reports owned by a registered Member are publicly
        // visible only when that Member is still Active.
        // =====================================================

        if (
            lostFound.UserId != null &&
            (
                lostFound.User == null ||
                lostFound.User.Status != UserStatus.Active
            )
        )
        {
            return NotFound();
        }


        return View(lostFound);
    }
}