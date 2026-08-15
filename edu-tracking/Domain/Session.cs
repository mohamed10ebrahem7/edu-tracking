namespace edu_tracking.Domain;

/// <summary>
/// A lesson occupying exactly one <see cref="TeacherSlot"/>. Holds one student for a
/// one-to-one lesson and several for a group class, via <see cref="SessionParticipant"/>.
/// </summary>
public class Session : IAuditable
{
    public long Id { get; set; }

    public long SlotId { get; set; }
    public TeacherSlot Slot { get; set; } = null!;

    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public SessionType Type { get; set; } = SessionType.OneToOne;
    public string? Title { get; set; }

    public int Capacity { get; set; } = 1;

    /// <summary>
    /// Denormalised count of confirmed participants. Only ever changed by a conditional
    /// UPDATE so two students cannot claim the same last seat.
    /// </summary>
    public int SeatsTaken { get; set; }

    public decimal DefaultPrice { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
    public string? MeetingUrl { get; set; }
    public string? CancellationReason { get; set; }

    /// <summary>Teacher has finished setting the session up and it may appear in search.</summary>
    public bool IsPublished { get; set; }
    public bool AllowSelfBooking { get; set; } = true;

    /// <summary>Null means open to any grade.</summary>
    public GradeLevel? TargetGradeLevel { get; set; }

    /// <summary>
    /// Copied from the slot so public search can be served by one covering index
    /// without joining. Only the reschedule operation may write these.
    /// </summary>
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<SessionParticipant> Participants { get; set; } = [];
    public SessionReport? Report { get; set; }
}
