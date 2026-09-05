using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.Services;
using PETHUB.ViewModels;

namespace PETHUB.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;
        private readonly AuditLogService _auditLogService;

        // Dependency Injection provides the database context and Identity UserManager.
        // ApplicationDbContext handles UserReport, Listing, LostFound, and Appeal operations.
        // UserManager retrieves the currently authenticated member's Identity information.
        public ReportsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService,
            AuditLogService auditLogService)
        {
            // Provides database access for reports, posts, and appeals.
            _context = context;

            // Provides the authenticated user's ID and access to Admin accounts.
            _userManager = userManager;

            // Provides the existing PETHUB notification functionality.
            _notificationService = notificationService;

            // Provides the audit logging functionality.
            _auditLogService = auditLogService;
        }


        // =========================================================
        // CREATE REPORT
        // =========================================================

        // POST: Reports/Create
        // Allows only Members to submit a new report.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Create(CreateReportViewModel model)
        {
            // Gets the ID of the currently authenticated Member.
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Stores the reported Marketplace listing or Lost & Found post.
            Listing? listing = null;
            LostFound? lostFound = null;


            // =========================================================
            // MARKETPLACE REPORT
            // =========================================================

            if (model.ContentType == ReportedContentType.Listing)
            {
                // Retrieves the Marketplace listing being reported.
                listing = await _context.Listings
                    .FirstOrDefaultAsync(l => l.ListingId == model.ContentId);

                if (listing == null)
                {
                    return NotFound();
                }

                // Prevents Members from reporting their own listing.
                if (listing.MemberId == userId)
                {
                    return Forbid();
                }
            }


            // =========================================================
            // LOST & FOUND REPORT
            // =========================================================

            else if (model.ContentType == ReportedContentType.LostFound)
            {
                // Retrieves the Lost & Found post being reported.
                lostFound = await _context.LostFounds
                    .FirstOrDefaultAsync(l => l.LostFoundId == model.ContentId);

                if (lostFound == null)
                {
                    return NotFound();
                }

                // Only registered Member-owned Lost & Found posts
                // can participate in this reporting workflow.
                if (string.IsNullOrEmpty(lostFound.UserId))
                {
                    return Forbid();
                }

                // Prevents Members from reporting their own post.
                if (lostFound.UserId == userId)
                {
                    return Forbid();
                }
            }
            else
            {
                return BadRequest();
            }


            // =========================================================
            // DUPLICATE REPORT CHECK
            // =========================================================

            // Prevents the same Member from having multiple active reports
            // against the same post.
            var existingReport = await _context.UserReports
                .AnyAsync(r =>
                    r.ReporterId == userId &&
                    r.ContentType == model.ContentType &&
                    (
                        (model.ContentType == ReportedContentType.Listing &&
                         r.ListingId == model.ContentId)
                        ||
                        (model.ContentType == ReportedContentType.LostFound &&
                         r.LostFoundId == model.ContentId)
                    ) &&
                    r.Status != UserReportStatus.Dismissed);

            if (existingReport)
            {
                return Conflict("You have already reported this post.");
            }


            // Clears OtherReason when a predefined reason was selected.
            if (model.Reason != UserReportReason.Other)
            {
                model.OtherReason = null;
            }


            // Creates the new report.
            var report = new UserReport
            {
                ReporterId = userId,
                ContentType = model.ContentType,
                Reason = model.Reason,
                OtherReason = model.OtherReason,
                Description = model.Description,
                Status = UserReportStatus.Pending,
                DateCreated = DateTime.UtcNow
            };


            // Stores the foreign key for the reported content.
            if (model.ContentType == ReportedContentType.Listing)
            {
                report.ListingId = model.ContentId;
            }
            else
            {
                report.LostFoundId = model.ContentId;
            }


            // Adds the new report to EF Core.
            _context.UserReports.Add(report);

            // Saves the report before updating the Admin notification.
            await _context.SaveChangesAsync();

            // Retrieve the Member who successfully submitted the report.
            // UserManager gets the ApplicationUser associated with the current login.
            var currentUser = await _userManager.GetUserAsync(User);

            // Record the report only after it has been successfully saved
            // to the database.
            if (currentUser != null)
            {
                await _auditLogService.LogAsync(
                    currentUser,
                    "Reported");
            }

            // Retrieves all Admin accounts.
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            // Updates the aggregate Admin report notification.
            await _notificationService.UpdateAdminReportNotificationAsync(admins);

            // Returns the Member to Home after successful report submission.
            return RedirectToAction("Index", "Home");
        }


        // =========================================================
        // ADMIN REPORT INDEX
        // =========================================================

        // GET: Reports
        // Displays the Administrator's report moderation queue.
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(
            string? reportStatus,
            string? reportType,
            int page = 1)
        {
            // Loads reports together with their Reporter, post owner,
            // and reported content.
            var reports = _context.UserReports
                .Include(r => r.Reporter)
                .Include(r => r.Listing)
                    .ThenInclude(l => l!.Member)
                .Include(r => r.LostFound)
                    .ThenInclude(l => l!.User)
                .AsQueryable();


            // Filters by UserReport status.
            if (!string.IsNullOrWhiteSpace(reportStatus) &&
                Enum.TryParse<UserReportStatus>(
                    reportStatus,
                    true,
                    out var selectedStatus))
            {
                // EF Core translates this into a database WHERE condition.
                reports = reports.Where(r => r.Status == selectedStatus);
            }


            // Filters by reported content type.
            if (!string.IsNullOrWhiteSpace(reportType) &&
                Enum.TryParse<ReportedContentType>(
                    reportType,
                    true,
                    out var selectedType))
            {
                reports = reports.Where(r => r.ContentType == selectedType);
            }


            // Groups reports that belong to the same actual post.
            // Different users can still have separate UserReport records in the database,
            // but the Admin Index will display only one card for that post.
            var groupedReports = reports
                .AsEnumerable()
                .GroupBy(r =>
                    r.ContentType == ReportedContentType.Listing
                        ? $"Listing:{r.ListingId}"
                        : $"LostFound:{r.LostFoundId}")
                // Selects the newest report from each post group.
                // This report becomes the representative record used by the Index card.
                .Select(group => group
                    .OrderByDescending(r => r.DateCreated)
                    .First())
                .ToList();


            // Gets the IDs of the posts represented by the Admin queue.
            // This is done after grouping so each post is checked only once,
            // instead of repeatedly checking the same post for every UserReport.
            var listingIds = groupedReports
                .Where(r =>
                    r.ContentType == ReportedContentType.Listing &&
                    r.ListingId.HasValue)
                .Select(r => r.ListingId!.Value)
                .ToList();

            var lostFoundIds = groupedReports
                .Where(r =>
                    r.ContentType == ReportedContentType.LostFound &&
                    r.LostFoundId.HasValue)
                .Select(r => r.LostFoundId!.Value)
                .ToList();


            // Retrieves Pending Appeals for the Marketplace listings represented
            // in the current Admin queue.
            //
            // This query runs once instead of performing an Appeals query for every
            // individual UserReport. The result is stored in memory so the final
            // sorting and badge detection do not repeatedly access the database.
            var pendingListingAppeals = await _context.Appeals
                .Where(a =>
                    a.Status == AppealStatus.Pending &&
                    a.ListingId.HasValue &&
                    listingIds.Contains(a.ListingId.Value))
                .Select(a => new
                {
                    ListingId = a.ListingId!.Value,
                    a.DateCreated
                })
                .ToListAsync();


            // Retrieves Pending Appeals for the Lost & Found posts represented
            // in the current Admin queue.
            //
            // Like the Marketplace query above, this executes one database query
            // for all relevant Lost & Found posts instead of one query per report.
            var pendingLostFoundAppeals = await _context.Appeals
                .Where(a =>
                    a.Status == AppealStatus.Pending &&
                    a.LostFoundId.HasValue &&
                    lostFoundIds.Contains(a.LostFoundId.Value))
                .Select(a => new
                {
                    LostFoundId = a.LostFoundId!.Value,
                    a.DateCreated
                })
                .ToListAsync();


            // Creates quick in-memory lookups for Pending Appeals.
            //
            // If multiple Pending Appeal records somehow exist for the same post,
            // Max() ensures that the newest appeal date is used for sorting.
            var pendingListingAppealDates = pendingListingAppeals
                .GroupBy(a => a.ListingId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Max(a => a.DateCreated));

            var pendingLostFoundAppealDates = pendingLostFoundAppeals
                .GroupBy(a => a.LostFoundId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Max(a => a.DateCreated));


            // Determines the latest activity date for every representative report.
            //
            // Normal reports use their own DateCreated value.
            // A post with a Pending Appeal uses the newer of:
            // - its newest report date
            // - its newest Pending Appeal date
            //
            // This means a newly submitted appeal can bring an older removed post
            // back to the top of the Admin Reports queue.
            var orderedReports = groupedReports
                .Select(report =>
                {
                    DateTime? appealDate = null;

                    // Checks the Pending Appeal lookup for Marketplace listings.
                    if (report.ContentType == ReportedContentType.Listing &&
                        report.ListingId.HasValue &&
                        pendingListingAppealDates.TryGetValue(
                            report.ListingId.Value,
                            out var listingAppealDate))
                    {
                        appealDate = listingAppealDate;
                    }

                    // Checks the Pending Appeal lookup for Lost & Found posts.
                    if (report.ContentType == ReportedContentType.LostFound &&
                        report.LostFoundId.HasValue &&
                        pendingLostFoundAppealDates.TryGetValue(
                            report.LostFoundId.Value,
                            out var lostFoundAppealDate))
                    {
                        appealDate = lostFoundAppealDate;
                    }

                    // Uses the newest activity associated with this post.
                    // If there is no Pending Appeal, this simply becomes the report date.
                    var latestActivity =
                        appealDate.HasValue && appealDate.Value > report.DateCreated
                            ? appealDate.Value
                            : report.DateCreated;

                    return new
                    {
                        Report = report,
                        HasPendingAppeal = appealDate.HasValue,
                        LatestActivity = latestActivity
                    };
                })
                // Posts currently under appeal are prioritized first.
                .OrderByDescending(x => x.HasPendingAppeal)
                // Within the same priority group, newest activity appears first.
                .ThenByDescending(x => x.LatestActivity)
                // Converts the anonymous objects back into the UserReport collection
                // expected by the existing Admin Reports view.
                .Select(x => x.Report)
                .ToList();


            // Stores the IDs of the representative reports whose posts currently
            // have a Pending Appeal.
            //
            // The Admin Index view can use this collection to display "Under Appeal"
            // without changing the UserReport model or database schema.
            var pendingAppealReportIds = orderedReports
                .Where(report =>
                    (report.ContentType == ReportedContentType.Listing &&
                     report.ListingId.HasValue &&
                     pendingListingAppealDates.ContainsKey(
                         report.ListingId.Value))
                    ||
                    (report.ContentType == ReportedContentType.LostFound &&
                     report.LostFoundId.HasValue &&
                     pendingLostFoundAppealDates.ContainsKey(
                         report.LostFoundId.Value)))
                .Select(report => report.UserReportId)
                .ToHashSet();


            // Makes the Pending Appeal information available to the existing
            // Admin Reports Index Razor view.
            ViewData["PendingAppealReportIds"] =
                pendingAppealReportIds;


            // Builds the existing Admin report filter bar.
            var filters = PETHUB.Helpers.FilterBarHelper.Create(
                PETHUB.Helpers.FilterBarHelper.ReportStatus(
                    reportStatus
                ),
                PETHUB.Helpers.FilterBarHelper.ReportPostType(
                    reportType
                )
            );


            ViewData["ReportFilters"] = filters;


            // =========================================================
            // PAGINATION
            // =========================================================

            const int pageSize = 25;

            if (page < 1)
            {
                page = 1;
            }

            var totalItems = orderedReports.Count;

            var totalPages =
                (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var pagedReports = orderedReports
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();


            var model = new PaginationViewModel<UserReport>
            {
                Items = pagedReports,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };


            // Sends the paginated representative reports to the Index view.
            return View(
                "~/Views/AdminReports/Index.cshtml",
                model
            );
        }


        // =========================================================
        // ADMIN REPORT DETAILS
        // =========================================================

        // GET: Reports/Details/5
        // Displays the selected report, related reports, and the owner's appeal.
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            // A report ID is required.
            if (id == null)
            {
                return NotFound();
            }


            // Loads the selected report and its related content.
            var report = await _context.UserReports
                .Include(r => r.Reporter)
                .Include(r => r.Listing)
                    .ThenInclude(l => l!.Member)
                .Include(r => r.Listing)
                    .ThenInclude(l => l!.Images)
                .Include(r => r.LostFound)
                    .ThenInclude(l => l!.User)
                .Include(r => r.LostFound)
                    .ThenInclude(l => l!.Images)
                .FirstOrDefaultAsync(r => r.UserReportId == id);


            if (report == null)
            {
                return NotFound();
            }


            // =========================================================
            // RELATED REPORTS
            // =========================================================

            // Starts with all UserReports.
            var relatedReportsQuery = _context.UserReports
                .Include(r => r.Reporter)
                .AsQueryable();


            // Finds reports belonging to the same Marketplace listing.
            if (report.ContentType == ReportedContentType.Listing &&
                report.ListingId.HasValue)
            {
                relatedReportsQuery = relatedReportsQuery.Where(r =>
                    r.ContentType == ReportedContentType.Listing &&
                    r.ListingId == report.ListingId);
            }


            // Finds reports belonging to the same Lost & Found post.
            else if (report.ContentType == ReportedContentType.LostFound &&
                     report.LostFoundId.HasValue)
            {
                relatedReportsQuery = relatedReportsQuery.Where(r =>
                    r.ContentType == ReportedContentType.LostFound &&
                    r.LostFoundId == report.LostFoundId);
            }


            // If the report does not have a valid content relationship,
            // only the selected report is returned.
            else
            {
                relatedReportsQuery = relatedReportsQuery.Where(r =>
                    r.UserReportId == report.UserReportId);
            }


            // Newest related reports appear first.
            var relatedReports = await relatedReportsQuery
                .OrderByDescending(r => r.DateCreated)
                .ToListAsync();


            // =========================================================
            // APPEAL
            // =========================================================

            // Holds the latest appeal belonging to the existing reported post.
            Appeal? appeal = null;


            // Retrieves the latest Marketplace appeal.
            if (report.ContentType == ReportedContentType.Listing &&
                report.ListingId.HasValue)
            {
                appeal = await _context.Appeals
                    .Where(a =>
                        a.ListingId == report.ListingId.Value)
                    .OrderByDescending(a => a.AppealId)
                    .FirstOrDefaultAsync();
            }


            // Retrieves the latest Lost & Found appeal.
            else if (report.ContentType == ReportedContentType.LostFound &&
                     report.LostFoundId.HasValue)
            {
                appeal = await _context.Appeals
                    .Where(a =>
                        a.LostFoundId == report.LostFoundId.Value)
                    .OrderByDescending(a => a.AppealId)
                    .FirstOrDefaultAsync();
            }


            // Builds the Admin Details ViewModel.
            var model = new AdminReportDetailsViewModel
            {
                Report = report,
                RelatedReports = relatedReports,
                Appeal = appeal
            };


            // Renders the existing Admin Details page.
            return View(
                "~/Views/AdminReports/Details.cshtml",
                model
            );
        }


        // =========================================================
        // DISMISS REPORT
        // =========================================================

        // POST: Reports/Dismiss
        // Dismisses all reports associated with the same post.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dismiss(int id)
        {
            // Retrieves the selected report.
            var report = await _context.UserReports
                .FirstOrDefaultAsync(r => r.UserReportId == id);


            if (report == null)
            {
                return NotFound();
            }


            // Only Pending reports can be dismissed.
            if (report.Status != UserReportStatus.Pending)
            {
                return BadRequest("Only pending reports can be dismissed.");
            }


            // Retrieves every report associated with the same post.
            var relatedReports = await _context.UserReports
                .Where(r =>
                    r.ContentType == report.ContentType &&
                    (
                        (report.ContentType == ReportedContentType.Listing &&
                         r.ListingId == report.ListingId)
                        ||
                        (report.ContentType == ReportedContentType.LostFound &&
                         r.LostFoundId == report.LostFoundId)
                    ))
                .ToListAsync();


            // Marks all reports for this post as dismissed.
            foreach (var relatedReport in relatedReports)
            {
                relatedReport.Status = UserReportStatus.Dismissed;
            }


            await _context.SaveChangesAsync();

            // Retrieve the Admin who successfully dismissed the reports.
            // UserManager gets the ApplicationUser associated with the current login.
            var currentUser = await _userManager.GetUserAsync(User);

            // Record the dismissal only after all related reports have been
            // successfully marked as dismissed in the database.
            if (currentUser != null)
            {
                await _auditLogService.LogAsync(
                    currentUser,
                    "Dismissed Report");
            }


            // Notifies each Reporter.
            foreach (var relatedReport in relatedReports)
            {
                await _notificationService.CreateNotificationAsync(
                    relatedReport.ReporterId,
                    NotificationType.UserReportRejected,
                    "Report Rejected",
                    "Your report was reviewed by an administrator and no violation was confirmed."
                );
            }


            // Updates the Admin aggregate report notification.
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            await _notificationService.UpdateAdminReportNotificationAsync(admins);


            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // CONFIRM VIOLATION
        // =========================================================

        // POST: Reports/ConfirmViolation
        // Confirms that a reported post violates PETHUB rules.
        //
        // This is intentionally separate from ConfirmAppeal.
        // ConfirmViolation removes a post.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmViolation(
            int id,
            string adminActionReason)
        {
            // Requires an Admin removal reason.
            if (string.IsNullOrWhiteSpace(adminActionReason))
            {
                TempData["ReportError"] =
                    "A reason for removing the post is required.";

                return RedirectToAction(nameof(Details), new { id });
            }


            // Removes unnecessary whitespace.
            adminActionReason = adminActionReason.Trim();


            // Protects the database from an excessively long reason.
            if (adminActionReason.Length > 1000)
            {
                TempData["ReportError"] =
                    "The removal reason cannot exceed 1000 characters.";

                return RedirectToAction(nameof(Details), new { id });
            }


            // Retrieves the selected report and its reported post.
            var report = await _context.UserReports
                .Include(r => r.Listing)
                .Include(r => r.LostFound)
                .FirstOrDefaultAsync(r => r.UserReportId == id);


            if (report == null)
            {
                return NotFound();
            }


            // Only Pending reports can be confirmed.
            if (report.Status != UserReportStatus.Pending)
            {
                return BadRequest("Only pending reports can be confirmed.");
            }


            // Retrieves every report associated with the same post.
            var relatedReports = await _context.UserReports
                .Where(r =>
                    r.ContentType == report.ContentType &&
                    (
                        (report.ContentType == ReportedContentType.Listing &&
                         r.ListingId == report.ListingId)
                        ||
                        (report.ContentType == ReportedContentType.LostFound &&
                         r.LostFoundId == report.LostFoundId)
                    ))
                .ToListAsync();


            // Resolves every report and stores the Admin's removal reason.
            foreach (var relatedReport in relatedReports)
            {
                relatedReport.AdminActionReason = adminActionReason;
                relatedReport.Status = UserReportStatus.Resolved;
            }


            // Removes a Marketplace listing without deleting it.
            if (report.ContentType == ReportedContentType.Listing)
            {
                if (report.Listing == null)
                {
                    return NotFound(
                        "The reported Marketplace listing no longer exists.");
                }

                report.Listing.Status = ListApprovalStatus.Removed;
            }


            // Removes a Lost & Found post without deleting it.
            else if (report.ContentType == ReportedContentType.LostFound)
            {
                if (report.LostFound == null)
                {
                    return NotFound(
                        "The reported Lost & Found post no longer exists.");
                }

                report.LostFound.Status = ApprovalStatus.Removed;
            }


            else
            {
                return BadRequest("Invalid report content type.");
            }


            // Saves the resolved reports and Removed post status.
            await _context.SaveChangesAsync();

            // Retrieves the Admin who successfully confirmed the violation.
            // UserManager gets the ApplicationUser associated with the current login.
            var currentUser = await _userManager.GetUserAsync(User);

            // Records the successful moderation action after the reports and
            // reported post have been saved as resolved/removed.
            if (currentUser != null)
            {
                await _auditLogService.LogAsync(
                    currentUser,
                    "Removed Post");
            }


            // Notifies every Reporter that the report was accepted.
            foreach (var relatedReport in relatedReports)
            {
                await _notificationService.CreateNotificationAsync(
                    relatedReport.ReporterId,
                    NotificationType.UserReportAccepted,
                    "Report Accepted",
                    "Your report was reviewed by an administrator and the reported post was removed."
                );
            }


            // Notifies the Marketplace owner.
            if (report.ContentType == ReportedContentType.Listing &&
                report.Listing != null)
            {
                await _notificationService.CreateNotificationAsync(
                    report.Listing.MemberId,
                    NotificationType.ReportedPostRemoved,
                    "Your Post Was Removed",
                    "Your post was reviewed by an administrator and was removed because it was found to violate PETHUB rules.",
                    redirectUrl: "/RemovedPosts"
                );
            }


            // Notifies the Lost & Found owner.
            else if (report.ContentType == ReportedContentType.LostFound &&
                     report.LostFound != null &&
                     !string.IsNullOrEmpty(report.LostFound.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    report.LostFound.UserId,
                    NotificationType.ReportedPostRemoved,
                    "Your Post Was Removed",
                    "Your post was reviewed by an administrator and was removed because it was found to violate PETHUB rules.",
                    redirectUrl: "/RemovedPosts"
                );
            }


            // Updates the Admin aggregate report notification.
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            await _notificationService.UpdateAdminReportNotificationAsync(admins);


            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // CONFIRM APPEAL
        // =========================================================

        // POST: Reports/ConfirmAppeal
        //
        // Approves ONE specific Pending Appeal and restores ONLY the
        // existing Marketplace listing or Lost & Found post associated
        // with that Appeal.
        //
        // It does NOT modify unrelated Pending posts or reports.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmAppeal(
            int appealId,
            string? adminActionReason)
        {
            // Retrieves exactly the Appeal selected by the Admin.
            // FirstOrDefaultAsync returns only the matching Appeal record.
            var appeal = await _context.Appeals
                .FirstOrDefaultAsync(a => a.AppealId == appealId);


            // Stop if the Appeal no longer exists.
            if (appeal == null)
            {
                return NotFound();
            }


            // Only Pending Appeals can be confirmed.
            //
            // This prevents an already Approved or Rejected Appeal from
            // being processed a second time.
            if (appeal.Status != AppealStatus.Pending)
            {
                return BadRequest(
                    "Only pending appeals can be confirmed.");
            }


            // Normalize the optional Admin response.
            // A null or whitespace-only value is stored as null.
            adminActionReason =
                string.IsNullOrWhiteSpace(adminActionReason)
                    ? null
                    : adminActionReason.Trim();


            // Prevent an unnecessarily large Admin response.
            if (adminActionReason != null &&
                adminActionReason.Length > 2000)
            {
                return BadRequest(
                    "The appeal response cannot exceed 2000 characters.");
            }


            // =========================================================
            // MARKETPLACE APPEAL
            // =========================================================

            if (appeal.ListingId.HasValue)
            {
                // Retrieves ONLY the Listing associated with this Appeal.
                // No other Listing is queried or modified.
                var listing = await _context.Listings
                    .FirstOrDefaultAsync(l =>
                        l.ListingId == appeal.ListingId.Value);


                // The original post must still exist.
                if (listing == null)
                {
                    return NotFound(
                        "The Marketplace listing associated with this appeal no longer exists.");
                }


                // The appeal is specifically for a Removed post.
                //
                // If the post has already been changed by another moderation
                // action, we stop rather than accidentally overwriting that state.
                if (listing.Status != ListApprovalStatus.Removed)
                {
                    return BadRequest(
                        "The Marketplace listing is no longer in the Removed state.");
                }


                // Restore the EXISTING listing.
                //
                // Nothing is duplicated or recreated.
                // The same Listing record simply becomes Approved again.
                listing.Status = ListApprovalStatus.Approved;
            }


            // =========================================================
            // LOST & FOUND APPEAL
            // =========================================================

            else if (appeal.LostFoundId.HasValue)
            {
                // Retrieves ONLY the Lost & Found post associated with this Appeal.
                var lostFound = await _context.LostFounds
                    .FirstOrDefaultAsync(l =>
                        l.LostFoundId == appeal.LostFoundId.Value);


                // The original post must still exist.
                if (lostFound == null)
                {
                    return NotFound(
                        "The Lost & Found post associated with this appeal no longer exists.");
                }


                // The post must still be Removed before it can be restored.
                if (lostFound.Status != ApprovalStatus.Removed)
                {
                    return BadRequest(
                        "The Lost & Found post is no longer in the Removed state.");
                }


                // Restores the EXISTING Lost & Found post.
                //
                // No new Lost & Found post is created.
                lostFound.Status = ApprovalStatus.Approved;
            }


            // =========================================================
            // INVALID APPEAL RELATIONSHIP
            // =========================================================

            else
            {
                // Every Appeal must point to either a Listing or Lost & Found post.
                // If neither foreign key exists, the Appeal is incomplete.
                return BadRequest(
                    "This appeal is not associated with a valid post.");
            }


            // =========================================================
            // APPROVE THE APPEAL
            // =========================================================

            // Changes ONLY this selected Appeal to Approved.
            appeal.Status = AppealStatus.Approved;

            // Stores the optional Admin response.
            appeal.AdminActionReason = adminActionReason;

            // Records when the Admin made the decision.
            appeal.DateResolved = DateTime.Now;


            // Saves the Appeal and the associated post restoration together.
            //
            // The other Pending posts and reports are untouched because
            // they were never loaded or modified by this action.
            await _context.SaveChangesAsync();

            // Retrieves the Admin who successfully confirmed the appeal.
            // UserManager gets the ApplicationUser associated with the current login.
            var currentUser = await _userManager.GetUserAsync(User);

            // Records the successful appeal confirmation only after the Appeal
            // and the associated post restoration have been saved successfully.
            if (currentUser != null)
            {
                await _auditLogService.LogAsync(
                    currentUser,
                    "Confirmed Appeal");
            }


            // Returns to the same Admin Report Details page where the
            // appeal was reviewed.
            //
            // This lets the Admin see the updated Approved Appeal state.
            var reportId = await GetReportIdForAppealAsync(appeal);

            if (reportId.HasValue)
            {
                return RedirectToAction(
                    nameof(Details),
                    new { id = reportId.Value });
            }


            // Fallback in case the original report cannot be located.
            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // REJECT APPEAL
        // =========================================================

        // POST: Reports/RejectAppeal
        //
        // Rejects ONE specific Pending Appeal.
        //
        // The associated post remains Removed.
        // No unrelated post or report is changed.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectAppeal(
            int appealId,
            string? adminActionReason)
        {
            // Retrieves only the selected Appeal.
            var appeal = await _context.Appeals
                .FirstOrDefaultAsync(a => a.AppealId == appealId);


            // Stop if the Appeal does not exist.
            if (appeal == null)
            {
                return NotFound();
            }


            // Only Pending Appeals can be rejected.
            if (appeal.Status != AppealStatus.Pending)
            {
                return BadRequest(
                    "Only pending appeals can be rejected.");
            }


            // Normalize the optional Admin response.
            adminActionReason =
                string.IsNullOrWhiteSpace(adminActionReason)
                    ? null
                    : adminActionReason.Trim();


            // Protects the Appeal's maximum response length.
            if (adminActionReason != null &&
                adminActionReason.Length > 2000)
            {
                return BadRequest(
                    "The appeal response cannot exceed 2000 characters.");
            }


            // Rejects ONLY this Appeal.
            appeal.Status = AppealStatus.Rejected;

            // Stores the Admin's response.
            appeal.AdminActionReason = adminActionReason;

            // Records when the decision was made.
            appeal.DateResolved = DateTime.Now;


            // IMPORTANT:
            // We deliberately do NOT change Listing.Status or LostFound.Status.
            //
            // Therefore:
            //
            // Rejected Appeal
            //       ↓
            // Existing post remains Removed
            //
            // Other Pending posts remain completely untouched.
            await _context.SaveChangesAsync();

            // Retrieves the Admin who successfully rejected the appeal.
            // UserManager gets the ApplicationUser associated with the current login.
            var currentUser = await _userManager.GetUserAsync(User);

            // Records the successful appeal rejection only after the Appeal
            // has been saved as Rejected in the database.
            if (currentUser != null)
            {
                await _auditLogService.LogAsync(
                    currentUser,
                    "Rejected Appeal");
            }


            // Return to the original Admin Report Details page.
            var reportId = await GetReportIdForAppealAsync(appeal);

            if (reportId.HasValue)
            {
                return RedirectToAction(
                    nameof(Details),
                    new { id = reportId.Value });
            }


            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // FIND REPORT FOR APPEAL
        // =========================================================

        // Finds the existing UserReport associated with the appealed post.
        //
        // This helper is used only for redirecting the Admin back to the
        // correct Report Details page after confirming or rejecting an Appeal.
        private async Task<int?> GetReportIdForAppealAsync(Appeal appeal)
        {
            // Marketplace Appeal.
            if (appeal.ListingId.HasValue)
            {
                // Retrieves the newest report associated with this exact listing.
                var report = await _context.UserReports
                    .Where(r =>
                        r.ContentType == ReportedContentType.Listing &&
                        r.ListingId == appeal.ListingId.Value)
                    .OrderByDescending(r => r.UserReportId)
                    .Select(r => (int?)r.UserReportId)
                    .FirstOrDefaultAsync();

                return report;
            }


            // Lost & Found Appeal.
            if (appeal.LostFoundId.HasValue)
            {
                // Retrieves the newest report associated with this exact post.
                var report = await _context.UserReports
                    .Where(r =>
                        r.ContentType == ReportedContentType.LostFound &&
                        r.LostFoundId == appeal.LostFoundId.Value)
                    .OrderByDescending(r => r.UserReportId)
                    .Select(r => (int?)r.UserReportId)
                    .FirstOrDefaultAsync();

                return report;
            }


            // No associated post means no associated report can be found.
            return null;
        }

    }
}