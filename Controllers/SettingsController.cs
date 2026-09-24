using Microsoft.AspNetCore.Mvc;

namespace loop.Controllers
{
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
