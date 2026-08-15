namespace edu_tracking.Domain;

/// <summary>
/// A concrete block of time on a teacher's calendar. This is the single source of truth
/// for the schedule: weekly templates only ever materialise into rows here.
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

    /// <summary>The weekly rule that produced this slot, when generated.</summary>
    public int? SourceAvailabilityId { get; set; }
    public TeacherWeeklyAvailability? SourceAvailability { get; set; }

    /// <summary>Teacher-private; never expose on student-facing responses.</summary>
    public string? Note { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public Session? Session { get; set; }
    public ICollection<BookingRequest> BookingRequests { get; set; } = [];
}
