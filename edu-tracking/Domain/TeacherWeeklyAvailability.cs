namespace edu_tracking.Domain;

/// <summary>
/// A recurring weekly working window. Nothing is generated from it: it declares when the
/// teacher works, which is used to validate that a class sits inside working hours and to
/// paint the free time left over on the schedule.
/// <para>
/// Times are the teacher's LOCAL wall-clock times: "Sunday 5pm" must stay 5pm after a
/// daylight-saving shift, so the conversion to UTC happens per date, using the teacher's
/// <c>TimeZoneId</c>.
/// </para>
/// </summary>
public class TeacherWeeklyAvailability : IAuditable
{
    public int Id { get; set; }

    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>Granularity the schedule uses when slicing leftover time into free cells.</summary>
    public int SlotMinutes { get; set; } = 60;

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
