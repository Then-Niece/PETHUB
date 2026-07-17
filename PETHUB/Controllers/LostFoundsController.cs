using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;
using System.Security.Claims;

public class LostFoundsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    public LostFoundsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    { _context = context; _userManager = userManager; }

    // The old admin endpoint stays safe and leads to the shared moderation queue.
    [Authorize(Roles = "Admin")]
    public IActionResult Index() => RedirectToAction("Approvals", "Listings");

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var report = await _context.LostFounds.Include(l => l.User).Include(l => l.Images)
            .FirstOrDefaultAsync(l => l.LostFoundId == id);
        return report == null ? NotFound() : View(report);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var report = await _context.LostFounds.FindAsync(id);
        if (report == null) return NotFound();
        report.Status = ApprovalStatus.Approved;
        await _context.SaveChangesAsync();
        return RedirectToAction("Approvals", "Listings");
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(int id)
    {
        var report = await _context.LostFounds.FindAsync(id);
        if (report == null) return NotFound();
        report.Status = ApprovalStatus.Rejected;
        await _context.SaveChangesAsync();
        return RedirectToAction("Approvals", "Listings");
    }

    [Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var report = await _context.LostFounds.Include(l => l.Images).FirstOrDefaultAsync(l => l.LostFoundId == id);
        if (report == null) return NotFound();
        return CanManage(report) ? View(report) : Forbid();
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> Edit(int id, LostFound lostFound, List<IFormFile> Images)
    {
        if (id != lostFound.LostFoundId) return NotFound();
        var existing = await _context.LostFounds.Include(l => l.Images).FirstOrDefaultAsync(l => l.LostFoundId == id);
        if (existing == null) return NotFound();
        if (!CanManage(existing)) return Forbid(); // Clients never reach this action.
        if (!ModelState.IsValid) return View(existing);

        existing.Title = lostFound.Title; existing.Description = lostFound.Description;
        existing.Type = lostFound.Type; existing.Breed = lostFound.Breed;
        existing.PetType = lostFound.PetType; existing.Sex = lostFound.Sex;
        existing.LostDate = lostFound.LostDate; existing.Location = lostFound.Location;
        existing.DateReported = DateTime.Now;
        if (Images?.Count > 0)
            _context.AddRange(await ImageUploadHelper.SaveImagesAsync(Images, existing.LostFoundId,
                (reportId, path) => new LostFoundImage { LostFoundId = reportId, ImagePath = path }, "lostfound"));
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Browse));
    }

    [Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var report = await _context.LostFounds.Include(l => l.Images).FirstOrDefaultAsync(l => l.LostFoundId == id);
        if (report == null) return NotFound();
        return CanManage(report) ? View(report) : Forbid();
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var report = await _context.LostFounds.Include(l => l.Images).FirstOrDefaultAsync(l => l.LostFoundId == id);
        if (report == null) return NotFound();
        if (!CanManage(report)) return Forbid();
        foreach (var image in report.Images ?? Enumerable.Empty<LostFoundImage>())
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImagePath.TrimStart('/'));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            _context.LostFoundImages.Remove(image);
        }
        _context.LostFounds.Remove(report);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Browse));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> RemoveImage(int id)
    {
        var image = await _context.LostFoundImages.Include(i => i.LostFound).FirstOrDefaultAsync(i => i.LostFoundImageId == id);
        if (image == null) return NotFound();
        if (image.LostFound == null || !CanManage(image.LostFound)) return Forbid();
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImagePath.TrimStart('/'));
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        _context.LostFoundImages.Remove(image); await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = image.LostFoundId });
    }

    [AllowAnonymous]
    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
    public async Task<IActionResult> Create(LostFound lostFound, List<IFormFile> Images)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            // Registered members own their reports; client contact details are not overwritten.
            lostFound.UserId = _userManager.GetUserId(User);
        }
        else if (string.IsNullOrWhiteSpace(lostFound.ClientName) || string.IsNullOrWhiteSpace(lostFound.ClientContact))
            ModelState.AddModelError("", "Name and contact are required for unregistered clients.");
        if (!ModelState.IsValid) return View(lostFound);
        lostFound.DateReported = DateTime.Now; lostFound.Status = ApprovalStatus.Pending;
        _context.Add(lostFound); await _context.SaveChangesAsync();
        if (Images?.Count > 0)
        { _context.AddRange(await ImageUploadHelper.SaveImagesAsync(Images, lostFound.LostFoundId,
            (reportId, path) => new LostFoundImage { LostFoundId = reportId, ImagePath = path }, "lostfound")); await _context.SaveChangesAsync(); }
        return RedirectToAction(nameof(SubmissionPending));
    }

    public IActionResult SubmissionPending() => View();

    [AllowAnonymous]
    public async Task<IActionResult> Browse(string? petTypeFilter)
    {
        var reports = await _context.LostFounds.Where(l => l.Status == ApprovalStatus.Approved)
            .Include(l => l.User).Include(l => l.Images).ToListAsync();
        if (Enum.TryParse<PetType>(petTypeFilter, true, out var petType)) reports = reports.Where(l => l.PetType == petType).ToList();
        ViewBag.PetTypeFilter = petTypeFilter;
        return View(reports);
    }

    [AllowAnonymous]
    public async Task<IActionResult> BrowseDetails(int? id)
    {
        if (id == null) return NotFound();
        var report = await _context.LostFounds.Include(l => l.User).Include(l => l.Images)
            .FirstOrDefaultAsync(l => l.LostFoundId == id && l.Status == ApprovalStatus.Approved);
        return report == null ? NotFound() : View(report);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Member")]
    public async Task<IActionResult> Resolve(int id)
    {
        var report = await _context.LostFounds.FindAsync(id);
        if (report == null) return NotFound();
        if (!CanManage(report)) return Forbid();
        report.ResolutionStatus = ReportResolutionStatus.Resolved;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Browse));
    }

    // Admins may manage all records; members only records owned by their account.
    private bool CanManage(LostFound report) => User.IsInRole("Admin") || report.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier);
}
