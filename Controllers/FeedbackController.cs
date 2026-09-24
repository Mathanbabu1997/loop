using Microsoft.AspNetCore.Mvc;

namespace LOOP.Controllers
{
    public class FeedbackController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        public IActionResult Import()
        {
            return View();
        }
    }
}