using edu_tracking.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace edu_tracking.Controllers;

[Authorize(Roles = AppRoles.Parent)]
public class ParentController : Controller
{
    public IActionResult Index() => View();
}
