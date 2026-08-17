using edu_tracking.Data;
using edu_tracking.Domain;
using edu_tracking.Infrastructure;
using edu_tracking.Models.Teaching;
using Microsoft.EntityFrameworkCore;

namespace edu_tracking.Services;

/// <summary>
/// Working hours and time off. Neither creates a meeting: the hours decide where a class
/// may be placed and which free cells the schedule paints, and time off keeps generation
/// off certain dates.
/// </summary>
public class TeacherAvailabilityService(ApplicationDbContext db)
{
    public async Task<AvailabilityPageViewModel> GetPageAsync(Guid teacherId, string currentUserName)
    {
        var profile = await db.Teachers
            .Where(t => t.UserId == teacherId)
            .Select(t => new
            {
                t.User.TimeZoneId,
                Subjects = t.Subjects.OrderBy(s => s.Subject.Name).Select(s => s.Subject.Name).ToList()
            })
            .FirstOrDefaultAsync();

        var clock = new WallClock(profile?.TimeZoneId);
        var today = clock.Today();

        var windows = await db.TeacherAvailabilities
            .AsNoTracking()
            .Where(a => a.TeacherId == teacherId && a.IsActive)
            .OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime)
            .Select(a => new WorkingHoursRow(
                a.Id,
                a.DayOfWeek,
                a.StartTime,
                a.EndTime,
                a.SlotMinutes,
                a.EffectiveFrom,
                a.EffectiveTo,
                db.ClassGroupSchedules.Count(s => s.ClassGroup.TeacherId == teacherId
                    && s.DayOfWeek == a.DayOfWeek
                    && s.StartTime >= a.StartTime
                    && s.EndTime <= a.EndTime)))
            .ToListAsync();

        var timeOff = await db.TeacherTimeOffs
            .AsNoTracking()
            .Where(o => o.TeacherId == teacherId)
            .OrderBy(o => o.StartUtc)
            .Select(o => new { o.Id, o.StartUtc, o.EndUtc, o.Reason })
            .ToListAsync();

        return new AvailabilityPageViewModel
        {
            CurrentUserName = currentUserName,
            Subjects = profile is null || profile.Subjects.Count == 0 ? null : string.Join(", ", profile.Subjects),
            ZoneName = clock.ZoneName,
            WorkingHours = windows,
            TimeOff =
            [
                .. timeOff.Select(o => new TimeOffRow(
                    o.Id,
                    clock.ToLocal(o.StartUtc).Date,
                    // Stored end is exclusive midnight, so the last day off is the day before.
                    clock.ToLocal(o.EndUtc.AddMinutes(-1)).Date,
                    o.Reason))
            ],
            NewTimeOff = new TimeOffInput { From = today, To = today }
        };
    }

    public async Task<GroupResult> AddWorkingHoursAsync(Guid teacherId, WorkingHoursInput input)
    {
        if (input.EndTime <= input.StartTime)
        {
            return GroupResult.Fail("The end time has to be after the start time.");
        }

        var clock = await ClockAsync(teacherId);

        var clashes = await db.TeacherAvailabilities
            .AnyAsync(a => a.TeacherId == teacherId
                && a.IsActive
                && a.DayOfWeek == input.DayOfWeek
                && a.StartTime < input.EndTime
                && a.EndTime > input.StartTime);

        if (clashes)
        {
            return GroupResult.Fail($"You already have working hours covering part of that {input.DayOfWeek}.");
        }

        db.TeacherAvailabilities.Add(new TeacherWeeklyAvailability
        {
            TeacherId = teacherId,
            DayOfWeek = input.DayOfWeek,
            StartTime = input.StartTime,
            EndTime = input.EndTime,
            SlotMinutes = input.SlotMinutes,
            EffectiveFrom = clock.Today(),
            IsActive = true
        });

        await db.SaveChangesAsync();

        return GroupResult.Ok(notice: $"{input.DayOfWeek} {input.StartTime:HH\\:mm}-{input.EndTime:HH\\:mm} added.");
    }

    /// <summary>
    /// Refused while a class still sits inside the window: the classes would be left
    /// outside the teacher's own working hours with no way to explain why.
    /// </summary>
    public async Task<GroupResult> RemoveWorkingHoursAsync(Guid teacherId, int id)
    {
        var window = await db.TeacherAvailabilities
            .FirstOrDefaultAsync(a => a.Id == id && a.TeacherId == teacherId);

        if (window is null)
        {
            return GroupResult.Fail("Those working hours no longer exist.");
        }

        var used = await db.ClassGroupSchedules.AnyAsync(s => s.ClassGroup.TeacherId == teacherId
            && s.DayOfWeek == window.DayOfWeek
            && s.StartTime >= window.StartTime
            && s.EndTime <= window.EndTime);

        if (used)
        {
            return GroupResult.Fail("A class is scheduled inside these hours. Move or delete the class first.");
        }

        db.TeacherAvailabilities.Remove(window);
        await db.SaveChangesAsync();

        return GroupResult.Ok(notice: "Working hours removed.");
    }

    public async Task<GroupResult> AddTimeOffAsync(Guid teacherId, TimeOffInput input)
    {
        if (input.To < input.From)
        {
            return GroupResult.Fail("The last day off cannot be before the first.");
        }

        var clock = await ClockAsync(teacherId);

        if (input.To < clock.Today())
        {
            return GroupResult.Fail("Those dates have already passed. Time off only affects classes created from now on.");
        }

        var startUtc = clock.StartOfDayUtc(input.From);
        var endUtc = clock.StartOfDayUtc(input.To.AddDays(1));

        var booked = await db.TeacherSlots
            .Where(s => s.TeacherId == teacherId && s.StartUtc < endUtc && s.EndUtc > startUtc)
            .Where(s => s.Session != null && s.Session.ClassGroup.MembersCount > 0)
            .CountAsync();

        if (booked > 0)
        {
            return GroupResult.Fail($"{booked} meetings in that range already have students. Cancel them first.");
        }

        db.TeacherTimeOffs.Add(new TeacherTimeOff
        {
            TeacherId = teacherId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Reason = string.IsNullOrWhiteSpace(input.Reason) ? null : input.Reason.Trim()
        });

        await db.SaveChangesAsync();

        return GroupResult.Ok(notice: "Time off added. Existing meetings in that range stay put until you cancel them.");
    }

    public async Task<GroupResult> RemoveTimeOffAsync(Guid teacherId, int id)
    {
        var off = await db.TeacherTimeOffs.FirstOrDefaultAsync(o => o.Id == id && o.TeacherId == teacherId);
        if (off is null)
        {
            return GroupResult.Fail("That time off no longer exists.");
        }

        db.TeacherTimeOffs.Remove(off);
        await db.SaveChangesAsync();

        return GroupResult.Ok(notice: "Time off removed.");
    }

    private async Task<WallClock> ClockAsync(Guid teacherId) =>
        new(await db.Teachers.Where(t => t.UserId == teacherId).Select(t => t.User.TimeZoneId).FirstOrDefaultAsync());
}
