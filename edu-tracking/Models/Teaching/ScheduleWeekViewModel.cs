using edu_tracking.Domain;
using edu_tracking.Infrastructure;

namespace edu_tracking.Models.Teaching;

/// <summary>
/// What a cell in the weekly schedule means. Free time is computed, never stored, so a
/// week with no classes still shows the teacher's working hours as bookable.
/// </summary>
public enum CellKind
{
    /// <summary>Outside working hours, inside time off, or a block the teacher closed.</summary>
    Unavailable,

    /// <summary>A gap between two working windows on the same day.</summary>
    Break,

    /// <summary>Inside working hours with no class on it.</summary>
    Free,

    /// <summary>A class is scheduled here but nobody has joined yet.</summary>
    Scheduled,

    /// <summary>A class with at least one member.</summary>
    Booked
}

/// <summary>A row of the grid: one time band shared by all seven days.</summary>
public record ScheduleBand(TimeOnly Start, TimeOnly End)
{
    public string Label => $"{Start:HH\\:mm} - {End:HH\\:mm}";
}

public record ScheduleDay(DateOnly Date, bool IsToday)
{
    public string Name => Date.ToString("ddd");
    public string Number => Date.Day.ToString();
}

public record ScheduleCell
{
    public CellKind State { get; init; }

    /// <summary>How many bands the cell covers, for a class longer than one band.</summary>
    public int RowSpan { get; init; } = 1;

    public DateOnly Date { get; init; }
    public TimeOnly Start { get; init; }
    public TimeOnly End { get; init; }

    public int? GroupId { get; init; }
    public long? SlotId { get; init; }
    public string? Subject { get; init; }
    public GradeLevel? Grade { get; init; }
    public ClassKind? Kind { get; init; }
    public int Members { get; init; }
    public int Capacity { get; init; }
    public int PendingRequests { get; init; }
    public string? Note { get; init; }

    public string Seats => $"{Members}/{Capacity}";
    public string? Detail => Grade is null ? null : $"{Ui.GradeLabel(Grade.Value)} - {Kind}";
    public string TimeLabel => $"{Start:HH\\:mm} - {End:HH\\:mm}";
    public bool HasClass => GroupId is not null;
}

/// <summary>
/// One grid row. A null cell is covered by a longer class in a band above it, and is
/// skipped so the rowspan lines up.
/// </summary>
public record ScheduleRow(ScheduleBand Band, IReadOnlyList<ScheduleCell?> Cells);

public class ScheduleWeekViewModel
{
    public string CurrentUserName { get; init; } = "Teacher";

    /// <summary>The teacher's subjects, shown under their name in the topbar.</summary>
    public string? Subjects { get; init; }

    public DateOnly WeekStart { get; init; }
    public DateOnly WeekEnd { get; init; }
    public DateOnly PreviousWeekStart { get; init; }
    public DateOnly NextWeekStart { get; init; }
    public DateOnly ThisWeekStart { get; init; }
    public bool IsCurrentWeek { get; init; }
    public string ZoneName { get; init; } = "UTC";

    public IReadOnlyList<StatCard> Stats { get; init; } = [];
    public IReadOnlyList<ScheduleDay> Days { get; init; } = [];
    public IReadOnlyList<ScheduleRow> Rows { get; init; } = [];

    /// <summary>False means the grid has nothing to draw until working hours are set.</summary>
    public bool HasWorkingHours { get; init; }

    public string WeekLabel => WeekStart.Month == WeekEnd.Month
        ? $"{WeekStart:MMM d} - {WeekEnd:d, yyyy}"
        : $"{WeekStart:MMM d} - {WeekEnd:MMM d, yyyy}";
}
