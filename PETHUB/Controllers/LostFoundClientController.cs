
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Models;
using PETHUB.Data;

namespace PETHUB.Controllers
{
    public class LostFoundClientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LostFoundClientController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LostFounds/Create
        public IActionResult Create() => View();

        // POST: LostFounds/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                            LostFoundId = lostFound.LostFoundId,
                            ImagePath = "/images/" + uniqueFileName
                        };
                        _context.Add(lostFoundImage);
                    }
                    await _context.SaveChangesAsync();
                }

                // Show confirmation modal
                return RedirectToAction(nameof(SubmissionPending));
            }
            return View(lostFound);
        }

        // GET: LostFounds/SubmissionPending
        public IActionResult SubmissionPending()
        {
            return View();
        }

        // GET: LOSTFOUNDS
        public async Task<IActionResult> Index()
        {
            var lostfounds = await _context.LostFounds
                .Where(l => l.Status == ApprovalStatus.Approved)
                .Include(l => l.Images)
                .ToListAsync();

            return View(lostfounds);
        }

        // GET: LOSTFOUNDS/Details/5
        public async Task<IActionResult> Details(int? id)
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
    }
}
