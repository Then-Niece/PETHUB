using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;

public class LostFoundsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public LostFoundsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: LostFounds
    [Authorize(Roles="Admin")]
    public async Task<IActionResult> Index()
    {
        var lostfounds = await _context.LostFounds
            .Include(l => l.Images)
            .ToListAsync();
        return View(lostfounds);
    }

    // GET: LostFounds/Details/5
    [Authorize(Roles ="Admin")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var lostfound = await _context.LostFounds
            .Include(l => l.Images)
            .FirstOrDefaultAsync(m => m.LostFoundId == id);

        if (lostfound == null) return NotFound();

        return View(lostfound);
    }

    // GET: LostFounds/Approve
    [HttpPost]
    [Authorize(Roles ="Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var report = await _context.LostFounds.FindAsync(id);
        if (report == null) return NotFound();

        report.Status = ApprovalStatus.Approved;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: LostFounds/Reject
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(int id)
    {
        var report = await _context.LostFounds.FindAsync(id);
        if (report == null) return NotFound();

        report.Status = ApprovalStatus.Rejected;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: LostFounds/Edit/5
    [Authorize(Roles = "Admin,Member")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var lostfound = await _context.LostFounds
            .Include(l => l.Images)
            .FirstOrDefaultAsync(m => m.LostFoundId == id);

        if (lostfound == null) return NotFound();

        return View(lostfound);
    }

    // POST: LostFounds/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Member")]
    public async Task<IActionResult> Edit(int id, LostFound lostFound, List<IFormFile> Images)
    {
        if (id != lostFound.LostFoundId) return NotFound();

        if (ModelState.IsValid)
        {
            var existing = await _context.LostFounds
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.LostFoundId == id);

            if (existing == null) return NotFound();

            existing.Title = lostFound.Title;
            existing.Description = lostFound.Description;
            existing.Type = lostFound.Type;
            existing.Breed = lostFound.Breed;
            existing.PetType = lostFound.PetType;
            existing.Sex = lostFound.Sex;
            existing.LostDate = lostFound.LostDate;
            existing.ClientName = lostFound.ClientName;
            existing.ClientContact = lostFound.ClientContact;
            existing.Location = lostFound.Location;
            existing.DateReported = DateTime.Now;

            // na-apply diri ang imageuploadhelper
            if (Images != null && Images.Count > 0)
            {
                var savedImages = await ImageUploadHelper.SaveImagesAsync(
                    Images,
                    lostFound.LostFoundId,
                    (id, path) => new LostFoundImage { LostFoundId = id, ImagePath = path },
                    "lostfound"
                );

                _context.AddRange(savedImages);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        return View(lostFound);
    }

    // GET: LostFounds/Delete/5
    [Authorize(Roles = "Admin,Member")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var lostfound = await _context.LostFounds
            .Include(l => l.Images)
            .FirstOrDefaultAsync(m => m.LostFoundId == id);

        if (lostfound == null) return NotFound();

        return View(lostfound);
    }

    // POST: LostFounds/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Member")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var lostFound = await _context.LostFounds
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l => l.LostFoundId == id);

        if (lostFound != null)
        {
            if (lostFound.Images != null && lostFound.Images.Any())
            {
                foreach (var img in lostFound.Images)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);

                    _context.LostFoundImages.Remove(img);
                }
            }

            _context.LostFounds.Remove(lostFound);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Member")]
    public async Task<IActionResult> RemoveImage(int id)
    {
        var image = await _context.LostFoundImages.FindAsync(id);
        if (image != null)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImagePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _context.LostFoundImages.Remove(image);
            await _context.SaveChangesAsync();

            return RedirectToAction("Edit", new { id = image.LostFoundId });
        }

        return NotFound();
    }



    // GET: LostFounds/Create
    [AllowAnonymous]
    public IActionResult Create() => View();

    // POST: LostFounds/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Create(LostFound lostFound, List<IFormFile> Images)
    {
        if (User.Identity.IsAuthenticated)
        {
            // Placeholder for future member linking
            // lostFound.MemberId = _userManager.GetUserId(User);
        }
        else
        {
            // Require client info
            if (string.IsNullOrEmpty(lostFound.ClientName) || string.IsNullOrEmpty(lostFound.ClientContact))
            {
                ModelState.AddModelError("", "Name and contact are required for unregistered clients.");
            }
        }

        if (ModelState.IsValid)
        {
            lostFound.DateReported = DateTime.Now;
            lostFound.Status = ApprovalStatus.Pending;
            _context.Add(lostFound);
            await _context.SaveChangesAsync();

            if (Images != null && Images.Count > 0)
            {
                var savedImages = await ImageUploadHelper.SaveImagesAsync(
                    Images,
                    lostFound.LostFoundId,
                    (id, path) => new LostFoundImage { LostFoundId = id, ImagePath = path },
                    "lostfound"
                );

                _context.AddRange(savedImages);
                await _context.SaveChangesAsync();
            }


            // Show confirmation modal
            return RedirectToAction(nameof(SubmissionPending));
        }
        return View(lostFound);
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
        var lostfounds = await _context.LostFounds
            .Where(l => l.Status == ApprovalStatus.Approved)
            .Include(l => l.Images)
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
