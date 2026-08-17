using edu_tracking.Domain;
using edu_tracking.Infrastructure;

namespace edu_tracking.Models.Teaching;

/// <summary>One class: who is in it, who is asking to be, and every meeting it produced.</summary>
public class GroupDetailViewModel
{
    public string CurrentUserName { get; init; } = "Teacher";
    public string? Subjects { get; init; }

    public int Id { get; init; }
    public string Subject { get; init; } = "";
    public string? Title { get; init; }
    public GradeLevel GradeLevel { get; init; }
    public ClassKind Kind { get; init; }
    public int Members { get; init; }
    public int MaxStudents { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public bool RepeatsWeekly { get; init; }
    public bool IsOpenForEnrollment { get; init; }

    public IReadOnlyList<string> Schedule { get; init; } = [];
    public IReadOnlyList<GroupMemberRow> Roster { get; init; } = [];
    public IReadOnlyList<JoinRequestRow> PendingRequests { get; init; } = [];
    public IReadOnlyList<OccurrenceRow> Occurrences { get; init; } = [];

    public string Name => string.IsNullOrWhiteSpace(Title) ? Subject : Title;
    public string Detail => $"{Ui.GradeLabel(GradeLevel)} - {Kind}";
    public string Seats => $"{Members}/{MaxStudents}";
    public bool IsFull => Members >= MaxStudents;
}

public record GroupMemberRow(Guid StudentId, string Name, GradeLevel Grade, MemberStatus Status, DateTime JoinedAtUtc)
{
    public bool IsActive => Status == MemberStatus.Active;
}

public record JoinRequestRow(long Id, string StudentName, GradeLevel Grade, DateTime RequestedAtUtc, string? Message);

public record OccurrenceRow(long SlotId, DateOnly Date, TimeOnly Start, TimeOnly End, SlotStatus Status, bool HasAttendance)
{
    public string TimeLabel => $"{Start:HH\\:mm} - {End:HH\\:mm}";
    public bool CanCancel => Status != SlotStatus.Unavailable && !HasAttendance;
}
