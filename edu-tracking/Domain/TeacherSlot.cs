namespace edu_tracking.Domain;

/// <summary>
/// A concrete block of time on a teacher's calendar: one dated meeting of a class group,
/// or a one-off block the teacher marked unavailable. Free time is not stored here, it is
/// whatever is left of the teacher's working hours once these rows are laid over them.
/// </summary>
public class TeacherSlot : IAuditable
{
    public long Id { get; set; }

    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    public SlotStatus Status { get; set; } = SlotStatus.Available;
    public SlotOrigin Origin { get; set; } = SlotOrigin.Manual;

    /// <summary>The group's weekday row that produced this slot, when generated.</summary>
    public int? ClassGroupScheduleId { get; set; }
    public ClassGroupSchedule? ClassGroupSchedule { get; set; }

    /// <summary>Teacher-private; never expose on student-facing responses.</summary>
    public string? Note { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public Session? Session { get; set; }
}
