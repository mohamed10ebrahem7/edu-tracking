using edu_tracking.Domain.Identity;
using edu_tracking.Models.Admin;
using edu_tracking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace edu_tracking.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AdminController(TeacherAdminService teachers) : Controller
{
    // The dashboard still shows placeholder content from AdminDashboardData.
    public IActionResult Index()
    {
        return View(AdminDashboardData.Build(CurrentUserName));
    }

    [HttpGet]
    public async Task<IActionResult> Teachers(string? search, int? subjectId, string? status, int page = 1)
    {
        var filter = new TeacherFilter
        {
            Search = search,
            SubjectId = subjectId,
            Status = status,
            Page = page
        };

        return View(await teachers.GetPageAsync(filter, CurrentUserName));
    }

    [HttpGet]
    public async Task<IActionResult> CreateTeacher()
    {
        var form = new TeacherFormViewModel
        {
            AvailableSubjects = await teachers.GetSubjectOptionsAsync()
        };

        return View("TeacherForm", form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTeacher(TeacherFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(form);
        }

        var result = await teachers.CreateAsync(form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return await RedisplayAsync(form);
        }

        TempData["Success"] = $"{form.FullName} was created. Temporary password: {result.TemporaryPassword}";
        return RedirectToAction(nameof(Teachers));
    }

    [HttpGet]
    public async Task<IActionResult> EditTeacher(Guid id)
    {
        var form = await teachers.GetForEditAsync(id);
        if (form is null)
        {
            return NotFound();
        }

        return View("TeacherForm", form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTeacher(TeacherFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(form);
        }

        var result = await teachers.UpdateAsync(form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return await RedisplayAsync(form);
        }

        TempData["Success"] = $"{form.FullName} was updated.";
        return RedirectToAction(nameof(Teachers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetTeacherActive(Guid id, bool isActive)
    {
        var result = await teachers.SetActiveAsync(id, isActive);
        var name = await teachers.GetNameAsync(id) ?? "The teacher";

        if (result.Succeeded)
        {
            TempData["Success"] = isActive ? $"{name} was reactivated." : $"{name} was deactivated.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Teachers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetTeacherPassword(Guid id)
    {
        var name = await teachers.GetNameAsync(id) ?? "The teacher";
        var result = await teachers.ResetPasswordAsync(id);

        if (result.Succeeded)
        {
            TempData["Success"] = $"New temporary password for {name}: {result.TemporaryPassword}";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Teachers));
    }

    private async Task<IActionResult> RedisplayAsync(TeacherFormViewModel form)
    {
        form.AvailableSubjects = await teachers.GetSubjectOptionsAsync();
        return View("TeacherForm", form);
    }

    private string CurrentUserName => User.Identity?.Name ?? "Admin";
}
