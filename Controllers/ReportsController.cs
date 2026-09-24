using Microsoft.AspNetCore.Mvc;

namespace LOOP.Controllers
{
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}