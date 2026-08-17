using System.ComponentModel.DataAnnotations;

namespace edu_tracking.Models.Teaching;

/// <summary>
/// Working hours and time off. Nothing here creates a meeting: the hours decide where a
/// class may be placed and which free cells the schedule paints.
/// </summary>
public class AvailabilityPageViewModel
{
    public string CurrentUserName { get; init; } = "Teacher";
    public string? Subjects { get; init; }
    public string ZoneName { get; init; } = "UTC";

    public IReadOnlyList<WorkingHoursRow> WorkingHours { get; init; } = [];
    public IReadOnlyList<TimeOffRow> TimeOff { get; init; } = [];

    public WorkingHoursInput NewWindow { get; init; } = new();
    public TimeOffInput NewTimeOff { get; init; } = new();
}

public record WorkingHoursRow(
    int Id,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotMinutes,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    int ClassCount)
{
    public string TimeLabel => $"{StartTime:HH\\:mm} - {EndTime:HH\\:mm}";
}

public record TimeOffRow(int Id, DateOnly From, DateOnly To, string? Reason);

public class WorkingHoursInput
{
    [Display(Name = "Day")]
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Sunday;

    [Display(Name = "From")]
    [DataType(DataType.Time)]
    public TimeOnly StartTime { get; set; } = new(9, 0);

    [Display(Name = "To")]
    [DataType(DataType.Time)]
    public TimeOnly EndTime { get; set; } = new(17, 0);

    [Display(Name = "Slot length (minutes)")]
    [Range(15, 240)]
    public int SlotMinutes { get; set; } = 60;
}

public class TimeOffInput
{
    [Display(Name = "First day off")]
    [DataType(DataType.Date)]
    public DateOnly From { get; set; }

    [Display(Name = "Last day off")]
    [DataType(DataType.Date)]
    public DateOnly To { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}
