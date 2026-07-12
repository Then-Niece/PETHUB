using Microsoft.AspNetCore.Mvc;

namespace PETHUB.Controllers
{
    public class AdminHomeController : Controller
    {
        public IActionResult AdminHome()
        {
            return View();
        }
    }
}
