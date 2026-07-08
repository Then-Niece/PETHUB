
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


        // GET: LOSTFOUNDS
        public async Task<IActionResult> Index()
        {
            var lostfounds = await _context.LostFounds
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
