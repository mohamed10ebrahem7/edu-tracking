using edu_tracking.Data;
using edu_tracking.Domain;
using edu_tracking.Infrastructure;
using edu_tracking.Models;
using edu_tracking.Models.Teaching;
using Microsoft.EntityFrameworkCore;

namespace edu_tracking.Services;

/// <summary>
/// Builds the weekly grid. Only meetings are stored; free time is what is left of the
/// teacher's working hours once the meetings are laid over them, so the week is computed
/// on every request rather than materialised into rows.
/// </summary>
public class TeacherScheduleService(ApplicationDbContext db)
{
    public async Task<ScheduleWeekViewModel> GetWeekAsync(Guid teacherId, DateOnly? requestedWeek, string currentUserName)
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
        var weekStart = Monday(requestedWeek ?? today);
        var weekEnd = weekStart.AddDays(6);

        var fromUtc = clock.StartOfDayUtc(weekStart);
        var toUtc = clock.StartOfDayUtc(weekStart.AddDays(7));

        var occurrences = await LoadOccurrencesAsync(teacherId, fromUtc, toUtc);
        var windows = await LoadWorkingHoursAsync(teacherId, weekStart, weekEnd);
        var timeOff = await LoadTimeOffAsync(teacherId, fromUtc, toUtc);

        var days = new List<ScheduleDay>(7);
        var pieces = new List<List<ScheduleCell>>(7);

        for (var offset = 0; offset < 7; offset++)
        {
            var date = weekStart.AddDays(offset);
            days.Add(new ScheduleDay(date, date == today));
            pieces.Add(BuildDay(date, clock, occurrences, windows, timeOff));
        }

        var rows = BuildRows(pieces, windows, weekStart);

        return new ScheduleWeekViewModel
        {
            CurrentUserName = currentUserName,
            Subjects = profile is null || profile.Subjects.Count == 0 ? null : string.Join(", ", profile.Subjects),
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            PreviousWeekStart = weekStart.AddDays(-7),
            NextWeekStart = weekStart.AddDays(7),
            ThisWeekStart = Monday(today),
            IsCurrentWeek = weekStart == Monday(today),
            ZoneName = clock.ZoneName,
            HasWorkingHours = windows.Count > 0,
            Days = days,
            Rows = rows,
            Stats = BuildStats(pieces, days, today)
        };
    }

    // ---------- loading ----------

    private record OccurrenceRow(
        long SlotId,
        DateTime StartUtc,
        DateTime EndUtc,
        SlotStatus Status,
        string? Note,
        int? GroupId,
        string? Subject,
        GradeLevel? Grade,
        ClassKind? Kind,
        int Members,
        int Capacity,
        int Pending);

    private Task<List<OccurrenceRow>> LoadOccurrencesAsync(Guid teacherId, DateTime fromUtc, DateTime toUtc) =>
        db.TeacherSlots
            .AsNoTracking()
            .Where(s => s.TeacherId == teacherId && s.StartUtc < toUtc && s.EndUtc > fromUtc)
            .Select(s => new OccurrenceRow(
                s.Id,
                s.StartUtc,
                s.EndUtc,
                s.Status,
                s.Note,
                s.Session == null ? null : s.Session.ClassGroupId,
                s.Session == null ? null : s.Session.Subject.Name,
                s.Session == null ? null : s.Session.ClassGroup.GradeLevel,
                s.Session == null ? null : s.Session.ClassGroup.Kind,
                s.Session == null ? 0 : s.Session.ClassGroup.MembersCount,
                s.Session == null ? 0 : s.Session.ClassGroup.MaxStudents,
                s.Session == null
                    ? 0
                    : s.Session.ClassGroup.JoinRequests.Count(r => r.Status == BookingRequestStatus.Pending)))
            .ToListAsync();

    private record WorkingWindow(DayOfWeek DayOfWeek, TimeOnly Start, TimeOnly End, int SlotMinutes);

    private async Task<List<WorkingWindow>> LoadWorkingHoursAsync(Guid teacherId, DateOnly from, DateOnly to) =>
        await db.TeacherAvailabilities
            .AsNoTracking()
            .Where(a => a.TeacherId == teacherId
                && a.IsActive
                && a.EffectiveFrom <= to
                && (a.EffectiveTo == null || a.EffectiveTo >= from))
            .OrderBy(a => a.StartTime)
            .Select(a => new WorkingWindow(a.DayOfWeek, a.StartTime, a.EndTime, a.SlotMinutes))
            .ToListAsync();

    private record TimeOffRange(DateTime StartUtc, DateTime EndUtc, string? Reason);

    private async Task<List<TimeOffRange>> LoadTimeOffAsync(Guid teacherId, DateTime fromUtc, DateTime toUtc) =>
        await db.TeacherTimeOffs
            .AsNoTracking()
            .Where(o => o.TeacherId == teacherId && o.StartUtc < toUtc && o.EndUtc > fromUtc)
            .Select(o => new TimeOffRange(o.StartUtc, o.EndUtc, o.Reason))
            .ToListAsync();

    // ---------- one day ----------

    /// <summary>
    /// The occupied and free stretches of one day, in order. Everything else on that day is
    /// filled in later as a break or as unavailable, which depends on the week as a whole.
    /// </summary>
    private static List<ScheduleCell> BuildDay(
        DateOnly date,
        WallClock clock,
        List<OccurrenceRow> occurrences,
        List<WorkingWindow> windows,
        List<TimeOffRange> timeOff)
    {
        var cells = new List<ScheduleCell>();

        foreach (var occurrence in occurrences)
        {
            var (startDate, start) = clock.ToLocal(occurrence.StartUtc);
            if (startDate != date)
            {
                continue;
            }

            var (endDate, endTime) = clock.ToLocal(occurrence.EndUtc);

            // A meeting running past midnight is clipped so it stays inside its column.
            var end = endDate == date ? endTime : TimeOnly.MaxValue;

            cells.Add(new ScheduleCell
            {
                State = occurrence.Status switch
                {
                    SlotStatus.Unavailable => CellKind.Unavailable,
                    _ when occurrence.Members > 0 => CellKind.Booked,
                    _ when occurrence.GroupId is not null => CellKind.Scheduled,
                    _ => CellKind.Unavailable
                },
                Date = date,
                Start = start,
                End = end,
                SlotId = occurrence.SlotId,
                GroupId = occurrence.GroupId,
                Subject = occurrence.Subject,
                Grade = occurrence.Grade,
                Kind = occurrence.Kind,
                Members = occurrence.Members,
                Capacity = occurrence.Capacity,
                PendingRequests = occurrence.Pending,
                Note = occurrence.Note
            });
        }

        foreach (var window in windows.Where(w => w.DayOfWeek == date.DayOfWeek))
        {
            var step = Math.Max(15, window.SlotMinutes);

            for (var start = window.Start; start < window.End;)
            {
                var end = Add(start, step, window.End);

                if (!cells.Any(c => c.Start < end && c.End > start))
                {
                    var off = FindTimeOff(date, start, end, clock, timeOff);

                    cells.Add(new ScheduleCell
                    {
                        State = off is null ? CellKind.Free : CellKind.Unavailable,
                        Date = date,
                        Start = start,
                        End = end,
                        Note = off is null ? null : off.Reason ?? "Time off"
                    });
                }

                start = end;
            }
        }

        return [.. cells.OrderBy(c => c.Start)];
    }

    private static TimeOffRange? FindTimeOff(
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        WallClock clock,
        List<TimeOffRange> timeOff)
    {
        if (timeOff.Count == 0 || !clock.Exists(date, start))
        {
            return null;
        }

        var startUtc = clock.ToUtc(date, start);
        var endUtc = clock.Exists(date, end) ? clock.ToUtc(date, end) : startUtc.AddMinutes(1);

        return timeOff.FirstOrDefault(o => o.StartUtc < endUtc && o.EndUtc > startUtc);
    }

    // ---------- the grid ----------

    /// <summary>
    /// Rows come from every distinct start and end time in the week, so a 90-minute class
    /// keeps its own band instead of forcing the whole grid onto 90-minute rows.
    /// </summary>
    private static List<ScheduleRow> BuildRows(List<List<ScheduleCell>> pieces, List<WorkingWindow> windows, DateOnly weekStart)
    {
        var boundaries = pieces
            .SelectMany(day => day)
            .SelectMany(cell => new[] { cell.Start, cell.End })
            .Distinct()
            .OrderBy(time => time)
            .ToList();

        var bands = new List<ScheduleBand>();
        for (var i = 1; i < boundaries.Count; i++)
        {
            bands.Add(new ScheduleBand(boundaries[i - 1], boundaries[i]));
        }

        var rows = new List<ScheduleRow>(bands.Count);
        var covered = new bool[bands.Count, 7];

        for (var band = 0; band < bands.Count; band++)
        {
            var cells = new ScheduleCell?[7];

            for (var day = 0; day < 7; day++)
            {
                if (covered[band, day])
                {
                    continue;
                }

                var piece = pieces[day].FirstOrDefault(c => c.Start == bands[band].Start);

                if (piece is null)
                {
                    cells[day] = Filler(bands[band], weekStart.AddDays(day), windows);
                    continue;
                }

                var span = bands.Count(b => b.Start >= piece.Start && b.End <= piece.End);

                for (var k = band; k < band + span && k < bands.Count; k++)
                {
                    covered[k, day] = true;
                }

                cells[day] = piece with { RowSpan = Math.Max(1, span) };
            }

            rows.Add(new ScheduleRow(bands[band], cells));
        }

        return rows;
    }

    /// <summary>
    /// A band with nothing on it: a break when it sits between two working windows on that
    /// day, and plainly unavailable when it is outside them altogether.
    /// </summary>
    private static ScheduleCell Filler(ScheduleBand band, DateOnly date, List<WorkingWindow> windows)
    {
        var day = windows.Where(w => w.DayOfWeek == date.DayOfWeek).ToList();

        var inside = day.Count > 0
            && band.Start >= day.Min(w => w.Start)
            && band.End <= day.Max(w => w.End);

        return new ScheduleCell
        {
            State = inside ? CellKind.Break : CellKind.Unavailable,
            Date = date,
            Start = band.Start,
            End = band.End
        };
    }

    // ---------- stat cards ----------

    private static IReadOnlyList<StatCard> BuildStats(List<List<ScheduleCell>> pieces, List<ScheduleDay> days, DateOnly today)
    {
        var all = pieces.SelectMany(day => day).ToList();

        var scheduled = all.Count(c => c.HasClass);
        var free = all.Count(c => c.State == CellKind.Free);
        var total = scheduled + free;

        var todayIndex = days.FindIndex(d => d.Date == today);
        var todayLabel = "—";

        if (todayIndex >= 0)
        {
            var onToday = pieces[todayIndex];
            var todayScheduled = onToday.Count(c => c.HasClass);
            var todayTotal = todayScheduled + onToday.Count(c => c.State == CellKind.Free);
            todayLabel = $"{todayScheduled}/{todayTotal}";
        }

        return
        [
            new StatCard("Total Slots", total.ToString(), "this week", "bi-calendar3", "purple"),
            new StatCard("Booked Slots", scheduled.ToString(), Share(scheduled, total), "bi-calendar-check", "green"),
            new StatCard("Available Slots", free.ToString(), Share(free, total), "bi-calendar-plus", "blue"),
            new StatCard("Today's Slots", todayLabel, "booked of total", "bi-clock-history", "orange")
        ];
    }

    private static string Share(int part, int total) =>
        total == 0 ? "no slots yet" : $"{Math.Round(part * 100.0 / total)}% of the week";

    private static TimeOnly Add(TimeOnly start, int minutes, TimeOnly limit)
    {
        var end = start.AddMinutes(minutes);

        // A window ending at midnight, or a step overshooting it, stops at the window.
        return end <= start || end > limit ? limit : end;
    }

    private static DateOnly Monday(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
}
