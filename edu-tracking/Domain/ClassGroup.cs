namespace edu_tracking.Domain;

/// <summary>
/// A class a teacher runs: one subject for one grade, meeting on fixed weekdays between two
/// dates. Students join the group once instead of booking each meeting, so the membership
/// list lives here and every dated meeting becomes a <see cref="Session"/>.
/// </summary>
public class ClassGroup : IAuditable
{
    /// <summary>Ceiling for <see cref="MaxStudents"/>, whatever a form asks for.</summary>
    public const int MaxAllowedStudents = 50;

    public int Id { get; set; }

    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public GradeLevel GradeLevel { get; set; }
    public ClassKind Kind { get; set; } = ClassKind.Lecture;

    /// <summary>Optional label; the schedule falls back to "Grade 7 - Lecture" without it.</summary>
    public string? Title { get; set; }

    public int MaxStudents { get; set; } = 1;

    /// <summary>
    /// Denormalised count of active members. Only ever changed by a conditional UPDATE so
    /// two students cannot claim the same last place.
    /// </summary>
    public int MembersCount { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>When false, only the meetings in the first week are created.</summary>
    public bool RepeatsWeekly { get; set; } = true;

    public bool IsOpenForEnrollment { get; set; } = true;

    public byte[] RowVersion { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ClassGroupSchedule> Schedule { get; set; } = [];
    public ICollection<ClassGroupMember> Members { get; set; } = [];
    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<BookingRequest> JoinRequests { get; set; } = [];

    public bool IsFull => MembersCount >= MaxStudents;
}
