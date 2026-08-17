namespace edu_tracking.Domain;

public enum GradeLevel
{
    Kg1 = 1,
    Kg2 = 2,
    Grade1 = 11,
    Grade2 = 12,
    Grade3 = 13,
    Grade4 = 14,
    Grade5 = 15,
    Grade6 = 16,
    Grade7 = 17,
    Grade8 = 18,
    Grade9 = 19,
    Grade10 = 20,
    Grade11 = 21,
    Grade12 = 22
}

public enum SlotStatus
{
    Available = 1,
    Unavailable = 2,
    Booked = 3
}

/// <summary>
/// Distinguishes slots produced by the weekly-availability generator from ones a
/// teacher created by hand. The generator may only ever touch <see cref="Generated"/> slots.
/// </summary>
public enum SlotOrigin
{
    Generated = 1,
    Manual = 2
}

public enum SessionType
{
    OneToOne = 1,
    Group = 2
}

/// <summary>What a class group meets for. Shown next to the grade, as "Grade 7 - Lecture".</summary>
public enum ClassKind
{
    Lecture = 1,
    Review = 2,
    Exam = 3
}

public enum MemberStatus
{
    Active = 1,
    Left = 2,
    Removed = 3
}

public enum SessionStatus
{
    Scheduled = 1,
    Completed = 2,
    CancelledByTeacher = 3,
    Cancelled = 4
}

public enum ParticipantStatus
{
    Confirmed = 1,
    Cancelled = 2
}

public enum AttendanceStatus
{
    NotMarked = 0,
    Present = 1,
    Absent = 2,
    Late = 3,
    Excused = 4
}

public enum BookingRequestStatus
{
    Pending = 1,
    Approved = 2,
    Declined = 3,
    WithdrawnByStudent = 4,
    Expired = 5
}

public enum ExamMode
{
    Offline = 1,
    OnlineMcq = 2
}
