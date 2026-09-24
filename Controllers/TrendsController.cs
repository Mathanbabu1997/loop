using Microsoft.AspNetCore.Mvc;

namespace LOOP.Controllers
{
    public class TrendsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}