using edu_tracking.Domain.Identity;

namespace edu_tracking.Domain;

public class Teacher
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string? Bio { get; set; }
    public decimal HourlyRate { get; set; }
    public int DefaultSessionMinutes { get; set; } = 60;
    public bool IsAcceptingBookings { get; set; } = true;

    /// <summary>
    /// When true, an incoming booking request is approved immediately in the same
    /// transaction, so booking always flows through a single code path.
    /// </summary>
    public bool AutoApproveBookings { get; set; }

    /// <summary>How close to the start time a student may still request a seat.</summary>
    public int MinBookingNoticeHours { get; set; } = 12;

    /// <summary>How close to the start time a student may still cancel free of charge.</summary>
    public int CancellationCutoffHours { get; set; } = 24;

    public ICollection<TeacherSubject> Subjects { get; set; } = [];
    public ICollection<TeacherWeeklyAvailability> WeeklyAvailability { get; set; } = [];
    public ICollection<TeacherTimeOff> TimeOff { get; set; } = [];
    public ICollection<TeacherSlot> Slots { get; set; } = [];
    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<Exam> Exams { get; set; } = [];
}
