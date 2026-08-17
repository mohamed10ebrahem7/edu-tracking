namespace edu_tracking.Domain;

/// <summary>
/// A student asking to join a <see cref="ClassGroup"/> for the whole of its run, rather
/// than for one meeting: a one-to-one class is simply a group capped at one student, so
/// there is a single way in.
/// <para>
/// Pending requests deliberately do NOT hold a place: several students may compete for the
/// last place in a group and the teacher chooses. The place is claimed at approval time.
/// </para>
/// </summary>
public class BookingRequest
{
    public long Id { get; set; }

    public int ClassGroupId { get; set; }
    public ClassGroup ClassGroup { get; set; } = null!;

    /// <summary>Denormalised from the group to drive the teacher's inbox query.</summary>
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
