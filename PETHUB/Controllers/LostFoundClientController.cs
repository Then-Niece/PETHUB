
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;

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
