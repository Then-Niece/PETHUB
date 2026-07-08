
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PETHUB.Models;
using PETHUB.Data;

public class LostFoundsController : Controller
{
    private readonly ApplicationDbContext _context;

    public LostFoundsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: LOSTFOUNDS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.LostFounds.ToListAsync());
    }

    // GET: LOSTFOUNDS/Details/5
    public async Task<IActionResult> Details(int? lostfoundid)
    {
        if (lostfoundid == null)
        {
            return NotFound();
        }

        var lostfound = await _context.LostFounds
            .FirstOrDefaultAsync(m => m.LostFoundId == lostfoundid);
        if (lostfound == null)
        {
            return NotFound();
        }

        return View(lostfound);
    }

    // GET: LOSTFOUNDS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: LOSTFOUNDS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("LostFoundId,Title,Description,Type,DateReported,Location,Images")] LostFound lostfound)
    {
        if (ModelState.IsValid)
        {
            _context.Add(lostfound);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(lostfound);
    }

    // GET: LOSTFOUNDS/Edit/5
    public async Task<IActionResult> Edit(int? lostfoundid)
    {
        if (lostfoundid == null)
        {
            return NotFound();
        }

        var lostfound = await _context.LostFounds.FindAsync(lostfoundid);
        if (lostfound == null)
        {
            return NotFound();
        }
        return View(lostfound);
    }

    // POST: LOSTFOUNDS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? lostfoundid, [Bind("LostFoundId,Title,Description,Type,DateReported,Location,Images")] LostFound lostfound)
    {
        if (lostfoundid != lostfound.LostFoundId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(lostfound);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LostFoundExists(lostfound.LostFoundId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(lostfound);
    }

    // GET: LOSTFOUNDS/Delete/5
    public async Task<IActionResult> Delete(int? lostfoundid)
    {
        if (lostfoundid == null)
        {
            return NotFound();
        }

        var lostfound = await _context.LostFounds
            .FirstOrDefaultAsync(m => m.LostFoundId == lostfoundid);
        if (lostfound == null)
        {
            return NotFound();
        }

        return View(lostfound);
    }

    // POST: LOSTFOUNDS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? lostfoundid)
    {
        var lostfound = await _context.LostFounds.FindAsync(lostfoundid);
        if (lostfound != null)
        {
            _context.LostFounds.Remove(lostfound);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool LostFoundExists(int? lostfoundid)
    {
        return _context.LostFounds.Any(e => e.LostFoundId == lostfoundid);
    }
}
