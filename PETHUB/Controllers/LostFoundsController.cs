using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
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
    public async Task<IActionResult> Index()
    {
        var lostfounds = await _context.LostFounds
            .Include(l => l.Images)
            .ToListAsync();
        return View(lostfounds);
    }

    // GET: LostFounds/Details/5
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
    public async Task<IActionResult> Reject(int id)
    {
        var report = await _context.LostFounds.FindAsync(id);
        if (report == null) return NotFound();

        report.Status = ApprovalStatus.Rejected;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: LostFounds/Edit/5
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

            if (Images != null && Images.Count > 0)
            {
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                foreach (var file in Images)
                {
                    var uniqueFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadDir, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await file.CopyToAsync(stream);

                    var lostFoundImage = new LostFoundImage
                    {
                        LostFoundId = existing.LostFoundId,
                        ImagePath = "/images/" + uniqueFileName
                    };
                    _context.Add(lostFoundImage);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(lostFound);
    }

    // GET: LostFounds/Delete/5
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

    private bool LostFoundExists(int id)
    {
        return _context.LostFounds.Any(e => e.LostFoundId == id);
    }
}
