using System.Security.Claims;
using edu_tracking.Domain.Identity;
using edu_tracking.Models.Teaching;
using edu_tracking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace edu_tracking.Controllers;

/// <summary>
/// The teacher panel. Every query and every post is scoped to the signed-in teacher's id,
/// so one teacher can never reach another's class or block.
/// </summary>
[Authorize(Roles = AppRoles.Teacher)]
public class TeacherController(
    TeacherScheduleService schedule,
    ClassGroupService groups,
    TeacherAvailabilityService availability) : Controller
{
    public IActionResult Index() => RedirectToAction(nameof(Schedule));

    // ---------- the weekly schedule ----------

    [HttpGet]
    public async Task<IActionResult> Schedule(DateOnly? week)
    {
        return View(await schedule.GetWeekAsync(TeacherId, week, CurrentUserName));
    }

    // ---------- classes ----------

    [HttpGet]
    public async Task<IActionResult> CreateGroup(DateOnly? date, TimeOnly? start, TimeOnly? end)
    {
        return View("GroupForm", await groups.NewFormAsync(TeacherId, date, start, end, CurrentUserName));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(ClassGroupFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(form);
        }

        var result = await groups.CreateAsync(TeacherId, form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return await RedisplayAsync(form);
        }

        TempData["Success"] = result.Notice;
        return RedirectToAction(nameof(Group), new { id = result.GroupId });
    }

    [HttpGet]
    public async Task<IActionResult> EditGroup(int id)
    {
        var form = await groups.GetForEditAsync(TeacherId, id, CurrentUserName);
        if (form is null)
        {
            return NotFound();
        }

        return View("GroupForm", form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditGroup(ClassGroupFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(form);
        }

        var result = await groups.UpdateAsync(TeacherId, form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return await RedisplayAsync(form);
        }

        TempData["Success"] = result.Notice;
        return RedirectToAction(nameof(Group), new { id = form.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Group(int id)
    {
        var detail = await groups.GetDetailAsync(TeacherId, id, CurrentUserName);
        if (detail is null)
        {
            return NotFound();
        }

        return View(detail);
    }

    // ---------- join requests and members ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(long id, int groupId)
    {
        Report(await groups.ApproveRequestAsync(TeacherId, id, TeacherId));
        return RedirectToAction(nameof(Group), new { id = groupId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineRequest(long id, int groupId, string? message)
    {
        Report(await groups.DeclineRequestAsync(TeacherId, id, message, TeacherId));
        return RedirectToAction(nameof(Group), new { id = groupId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int id, Guid studentId)
    {
        Report(await groups.RemoveMemberAsync(TeacherId, id, studentId, TeacherId));
        return RedirectToAction(nameof(Group), new { id });
    }

    /// <summary>Closes one meeting of a class, which turns that block grey on the schedule.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseOccurrence(long id, byte[]? rowVersion, DateOnly? week, int? groupId)
    {
        Report(await groups.CloseOccurrenceAsync(TeacherId, id, rowVersion));

        return groupId is { } group and > 0
            ? RedirectToAction(nameof(Group), new { id = group })
            : RedirectToAction(nameof(Schedule), new { week });
    }

    // ---------- working hours and time off ----------

    [HttpGet]
    public async Task<IActionResult> Availability()
    {
        return View(await availability.GetPageAsync(TeacherId, CurrentUserName));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddWorkingHours(WorkingHoursInput input)
    {
        Report(await availability.AddWorkingHoursAsync(TeacherId, input));
        return RedirectToAction(nameof(Availability));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveWorkingHours(int id)
    {
        Report(await availability.RemoveWorkingHoursAsync(TeacherId, id));
        return RedirectToAction(nameof(Availability));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTimeOff(TimeOffInput input)
    {
        Report(await availability.AddTimeOffAsync(TeacherId, input));
        return RedirectToAction(nameof(Availability));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveTimeOff(int id)
    {
        Report(await availability.RemoveTimeOffAsync(TeacherId, id));
        return RedirectToAction(nameof(Availability));
    }

    // ---------- shared ----------

    private async Task<IActionResult> RedisplayAsync(ClassGroupFormViewModel form)
    {
        return View("GroupForm", await groups.FillChoicesAsync(TeacherId, form, CurrentUserName));
    }

    private void Report(GroupResult result)
    {
        if (result.Succeeded)
        {
            TempData["Success"] = result.Notice;
        }
        else
        {
            TempData["Error"] = result.Error;
        }
    }

    private string CurrentUserName => User.Identity?.Name ?? "Teacher";

    /// <summary>A teacher's profile shares its key with the account, so this is both.</summary>
    private Guid TeacherId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : Guid.Empty;
}
