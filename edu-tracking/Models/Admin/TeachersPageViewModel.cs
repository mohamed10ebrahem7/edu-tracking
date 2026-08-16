namespace edu_tracking.Models.Admin;

/// <summary>Everything the Users &gt; Teachers list screen renders.</summary>
public class TeachersPageViewModel
{
    public string CurrentUserName { get; init; } = "Admin";

    public IReadOnlyList<StatCard> Stats { get; init; } = [];
    public IReadOnlyList<TeacherRow> Teachers { get; init; } = [];
    public IReadOnlyList<SubjectOption> Subjects { get; init; } = [];

    public TeacherFilter Filter { get; init; } = new();
    public PageInfo Page { get; init; } = new();
}

public record SubjectOption(int Id, string Name);

public class TeacherRow
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public IReadOnlyList<string> Subjects { get; init; } = [];
    public decimal HourlyRate { get; init; }
    public int Students { get; init; }
    public int SessionsThisWeek { get; init; }
    public string Status { get; init; } = AccountStatus.Active;
    public bool AcceptingBookings { get; init; }
    public bool IsActive { get; init; }
    public DateTime JoinedUtc { get; init; }
}

public class TeacherFilter : ListFilter
{
    public int? SubjectId { get; init; }

    protected override void AddExtraRouteValues(Dictionary<string, string> values)
    {
        if (SubjectId is int subjectId)
        {
            values["subjectId"] = subjectId.ToString();
        }
    }
}
