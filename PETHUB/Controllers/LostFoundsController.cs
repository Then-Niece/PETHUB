using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;
using PETHUB.Services;
using PETHUB.ViewModels;

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


    // =========================================================
    // ADMIN - LOST & FOUND INDEX
    // =========================================================

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(
        string? status,
        string? lostFoundType,
        string? petType,
        int page = 1)
    {
        // =========================================================
        // PAGINATION SETTINGS
        // =========================================================

        const int pageSize = 10;

        // Prevent invalid page numbers.
        if (page < 1)
        {
            page = 1;
        }


        // =========================================================
        // EXISTING LOST & FOUND QUERY
        // =========================================================

        // Start with all Lost & Found reports and load the related
        // user and image data required by the existing approval view.
        var lostfounds = _context.LostFounds
            .Include(l => l.User)
            .Include(l => l.Images)
            .AsQueryable();


        // =========================================================
        // EXISTING APPROVAL-STATUS FILTER
        // =========================================================

        // Apply the existing approval-status filter.
        // Lost & Found uses its own ApprovalStatus enum.
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


        // =========================================================
        // EXISTING LOST / FOUND FILTER
        // =========================================================

        // Apply the Lost/Found report-type filter.
        // LostFoundType separates Lost reports from Found reports.
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


        // =========================================================
        // EXISTING PET TYPE FILTER
        // =========================================================

        // Apply the Dog/Cat filter.
        // Lost & Found uses its own PetType enum.
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


        // =========================================================
        // PAGINATION
        // =========================================================

        // Count the reports AFTER all selected filters have been applied.
        var totalItems = await lostfounds.CountAsync();

        // Calculate the total number of pages.
        var totalPages = (int)Math.Ceiling(
            totalItems / (double)pageSize
        );

        // Prevent the requested page from going beyond
        // the available number of pages.
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }


        // =========================================================
        // GET CURRENT PAGE
        // =========================================================

        // Retrieve only the reports needed for the current page.
        // Lost & Found displays 10 reports per page.
        var pagedLostFounds = await lostfounds
            .OrderByDescending(l => l.DateReported)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();


        // =========================================================
        // CREATE PAGED RESULT
        // =========================================================

        var result = new PaginationViewModel<LostFound>
        {
            Items = pagedLostFounds,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };


        // =========================================================
        // RETURN VIEW
        // =========================================================

        return View(result);
    }
    // GET: LostFounds/Details/5
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




        // =========================================================
        // REMOVED → PENDING
        // =========================================================

        // A Removed report must return to Pending when its owner edits and
        // resubmits it. This sends the corrected report back through the
        // normal Admin approval process.
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
       string? petType,
       int page = 1)
    {
        // =========================================================
        // PAGINATION SETTINGS
        // =========================================================

        const int pageSize = 12;

        // Prevent invalid page numbers.
        if (page < 1)
        {
            page = 1;
        }


        // =========================================================
        // GET CURRENT USER
        // =========================================================

        // Get the current user's ID so members do not see their own reports.
        // For guests, GetUserId returns null and all public reports remain available.
        var userid = _userManager.GetUserId(User);


        // =========================================================
        // EXISTING LOST & FOUND QUERY
        // =========================================================

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


        // =========================================================
        // EXISTING LOST / FOUND FILTER
        // =========================================================

        // Apply the Lost/Found filter when a specific report type was selected.
        // Enum.TryParse converts "Lost" or "Found" into LostFoundType.
        if (!string.IsNullOrWhiteSpace(lostFoundType) &&
            Enum.TryParse<LostFoundType>(
                lostFoundType,
                out var selectedReportType))
        {
            // EF Core filters the query to the selected Lost/Found type.
            lostfounds = lostfounds.Where(
                l => l.Type == selectedReportType);
        }


        // =========================================================
        // EXISTING PET TYPE FILTER
        // =========================================================

        // Apply the Dog/Cat filter when a specific pet type was selected.
        // Lost & Found uses its own PetType enum.
        if (!string.IsNullOrWhiteSpace(petType) &&
            Enum.TryParse<PetType>(
                petType,
                out var selectedPetType))
        {
            // EF Core filters the query to the selected pet type.
            lostfounds = lostfounds.Where(
                l => l.PetType == selectedPetType);
        }


        // =========================================================
        // PAGINATION
        // =========================================================

        // Count the results AFTER all selected filters have been applied.
        var totalItems = await lostfounds.CountAsync();


        // Calculate the total number of pages.
        var totalPages = (int)Math.Ceiling(
            totalItems / (double)pageSize);


        // Prevent the requested page from going beyond
        // the available number of pages.
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }


        // =========================================================
        // GET CURRENT PAGE
        // =========================================================

        // Retrieve only the reports needed for the current page.
        // Lost & Found displays 12 reports per page.
        var pagedLostFounds = await lostfounds
            .OrderByDescending(l => l.DateReported)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();


        // =========================================================
        // CREATE PAGED RESULT
        // =========================================================

        var result = new PaginationViewModel<LostFound>
        {
            Items = pagedLostFounds,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };


        // =========================================================
        // RETURN TO LOST & FOUND VIEW
        // =========================================================

        return View(result);
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