using Microsoft.AspNetCore.Mvc;

namespace RealexTraceConditionProveOfConcept.Controllers
{
    public class HomeController : Controller
    {
        public HomeController()
        {
        }

        public IActionResult Index()
        {
            return RedirectToAction("Index", "RealexRaceConditionDebug");
        }
    }
}
