namespace edu_tracking.Domain;

/// <summary>
/// A student asking a teacher for a seat. Targets the slot rather than the session,
/// because every bookable thing — an empty one-to-one opening and a published group
/// class alike — occupies exactly one slot.
/// <para>
/// Pending requests deliberately do NOT hold a seat: several students may compete for
/// the same slot and the teacher chooses. Capacity is claimed at approval time, and a
/// request expires when its slot's start time passes.
/// </para>
/// </summary>
public class BookingRequest
{
    public long Id { get; set; }

    public long SlotId { get; set; }
    public TeacherSlot Slot { get; set; } = null!;

    /// <summary>Denormalised from the slot to drive the teacher's inbox query.</summary>
    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    /// <summary>The student themselves, or a parent acting for them.</summary>
    public Guid RequestedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public string? StudentMessage { get; set; }

    public BookingRequestStatus Status { get; set; } = BookingRequestStatus.Pending;
    public Guid? DecidedByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }

    /// <summary>Shown to the student verbatim, so keep private teacher notes out of it.</summary>
    public string? DeclineMessageToStudent { get; set; }
}
