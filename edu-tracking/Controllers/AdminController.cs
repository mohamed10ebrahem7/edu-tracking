using edu_tracking.Domain;
using edu_tracking.Domain.Identity;
using edu_tracking.Models.Admin;
using edu_tracking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace edu_tracking.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AdminController(
    TeacherAdminService teachers,
    StudentAdminService students,
    AccountAdminService accounts) : Controller
{
    // The dashboard still shows placeholder content from AdminDashboardData.
    public IActionResult Index()
    {
        return View(AdminDashboardData.Build(CurrentUserName));
    }

    // ---------- teachers ----------

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
            return await RedisplayTeacherAsync(form);
        }

        var result = await teachers.CreateAsync(form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return await RedisplayTeacherAsync(form);
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
            return await RedisplayTeacherAsync(form);
        }

        var result = await teachers.UpdateAsync(form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return await RedisplayTeacherAsync(form);
        }

        TempData["Success"] = $"{form.FullName} was updated.";
        return RedirectToAction(nameof(Teachers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetTeacherActive(Guid id, bool isActive)
    {
        var name = await accounts.GetNameAsync(id) ?? "The teacher";
        var result = await accounts.SetActiveAsync(id, isActive);

        Report(result, isActive ? $"{name} was reactivated." : $"{name} was deactivated.");
        return RedirectToAction(nameof(Teachers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetTeacherPassword(Guid id)
    {
        var name = await accounts.GetNameAsync(id) ?? "The teacher";
        var result = await accounts.ResetPasswordAsync(id);

        Report(result, $"New temporary password for {name}: {result.TemporaryPassword}");
        return RedirectToAction(nameof(Teachers));
    }

    // ---------- students ----------

    [HttpGet]
    public async Task<IActionResult> Students(string? search, GradeLevel? gradeLevel, string? status, int page = 1)
    {
        var filter = new StudentFilter
        {
            Search = search,
            GradeLevel = gradeLevel,
            Status = status,
            Page = page
        };

        return View(await students.GetPageAsync(filter, CurrentUserName));
    }

    [HttpGet]
    public async Task<IActionResult> CreateStudent()
    {
        var form = new StudentFormViewModel
        {
            AvailableParents = await students.GetParentOptionsAsync()
        };

        return View("StudentForm", form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStudent(StudentFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayStudentAsync(form);
        }

        var result = await students.CreateAsync(form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return await RedisplayStudentAsync(form);
        }

        TempData["Success"] = $"{form.FullName} was created. Temporary password: {result.TemporaryPassword}";
        return RedirectToAction(nameof(Students));
    }

    [HttpGet]
    public async Task<IActionResult> EditStudent(Guid id)
    {
        var form = await students.GetForEditAsync(id);
        if (form is null)
        {
            return NotFound();
        }

        return View("StudentForm", form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStudent(StudentFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayStudentAsync(form);
        }

        var result = await students.UpdateAsync(form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return await RedisplayStudentAsync(form);
        }

        TempData["Success"] = $"{form.FullName} was updated.";
        return RedirectToAction(nameof(Students));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStudentActive(Guid id, bool isActive)
    {
        var name = await accounts.GetNameAsync(id) ?? "The student";
        var result = await accounts.SetActiveAsync(id, isActive);

        Report(result, isActive ? $"{name} was reactivated." : $"{name} was deactivated.");
        return RedirectToAction(nameof(Students));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetStudentPassword(Guid id)
    {
        var name = await accounts.GetNameAsync(id) ?? "The student";
        var result = await accounts.ResetPasswordAsync(id);

        Report(result, $"New temporary password for {name}: {result.TemporaryPassword}");
        return RedirectToAction(nameof(Students));
    }

    // ---------- shared ----------

    private async Task<IActionResult> RedisplayTeacherAsync(TeacherFormViewModel form)
    {
        form.AvailableSubjects = await teachers.GetSubjectOptionsAsync();
        return View("TeacherForm", form);
    }

    private async Task<IActionResult> RedisplayStudentAsync(StudentFormViewModel form)
    {
        form.AvailableParents = await students.GetParentOptionsAsync();
        return View("StudentForm", form);
    }

    private void Report(AccountResult result, string success)
    {
        if (result.Succeeded)
        {
            TempData["Success"] = success;
        }
        else
        {
            TempData["Error"] = result.Error;
        }
    }

    private string CurrentUserName => User.Identity?.Name ?? "Admin";
}
