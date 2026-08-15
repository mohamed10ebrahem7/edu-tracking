using System.Diagnostics;
using edu_tracking.Infrastructure;
using edu_tracking.Models;
using Microsoft.AspNetCore.Mvc;

namespace edu_tracking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account");
            }

            var panel = RolePanel.ControllerFor(User);

            // "Home" means the account has no role yet, so redirecting would loop.
            return panel == "Home"
                ? View()
                : RedirectToAction("Index", panel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
