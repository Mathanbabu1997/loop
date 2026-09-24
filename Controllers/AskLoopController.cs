using Microsoft.AspNetCore.Mvc;

namespace loop.Controllers
{
    public class AskLoopController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
