namespace edu_tracking.Domain;

/// <summary>
/// A recurring weekly working window. Times are the teacher's LOCAL wall-clock times:
/// "Sunday 5pm" must stay 5pm after a daylight-saving shift, so the conversion to UTC
/// happens when slots are materialised, using the teacher's <c>TimeZoneId</c>.
/// </summary>
public class TeacherWeeklyAvailability : IAuditable
{
    public int Id { get; set; }

    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>Length of each slot the generator carves out of this window.</summary>
    public int SlotMinutes { get; set; } = 60;

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<TeacherSlot> GeneratedSlots { get; set; } = [];
}
