using edu_tracking.Domain.Identity;
using edu_tracking.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace edu_tracking.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AdminController : Controller
{
    public IActionResult Index()
    {
        // Swap AdminDashboardData for real queries when the data is ready.
        var model = AdminDashboardData.Build(User.Identity?.Name ?? "Admin");
        return View(model);
    }
}
