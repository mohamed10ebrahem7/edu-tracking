namespace edu_tracking.Domain;

/// <summary>
/// One student's involvement in one session: the confirmed booking, their attendance,
/// and the teacher's feedback about them specifically. A row here means an approved
/// booking; pending ones live in <see cref="BookingRequest"/>.
/// </summary>
public class SessionParticipant
{
    public long Id { get; set; }

    public long SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public ParticipantStatus Status { get; set; } = ParticipantStatus.Confirmed;

    /// <summary>The student themselves, a parent, or an admin.</summary>
    public Guid BookedByUserId { get; set; }
    public DateTime BookedAtUtc { get; set; }
    public decimal Price { get; set; }
    public string? CancellationReason { get; set; }

    public AttendanceStatus Attendance { get; set; } = AttendanceStatus.NotMarked;
    public int? MinutesAttended { get; set; }
    public Guid? MarkedByUserId { get; set; }
    public DateTime? MarkedAtUtc { get; set; }

    /// <summary>1..5, per student, because a group session grades each student separately.</summary>
    public byte? PerformanceRating { get; set; }
    public string? TeacherComment { get; set; }
    public bool IsVisibleToParent { get; set; } = true;
}
