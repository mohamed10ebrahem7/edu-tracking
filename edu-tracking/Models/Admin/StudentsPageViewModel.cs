using edu_tracking.Domain;
using edu_tracking.Infrastructure;

namespace edu_tracking.Models.Admin;

/// <summary>Everything the Users &gt; Students list screen renders.</summary>
public class StudentsPageViewModel
{
    public string CurrentUserName { get; init; } = "Admin";

    public IReadOnlyList<StatCard> Stats { get; init; } = [];
    public IReadOnlyList<StudentRow> Students { get; init; } = [];

    public StudentFilter Filter { get; init; } = new();
    public PageInfo Page { get; init; } = new();

    public static IReadOnlyList<GradeOption> AllGrades { get; } =
        [.. Enum.GetValues<GradeLevel>().Select(g => new GradeOption((int)g, Ui.GradeLabel(g)))];
}

public record GradeOption(int Value, string Label);

public class StudentRow
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Email { get; init; }
    public GradeLevel GradeLevel { get; init; }
    public string? School { get; init; }
    public IReadOnlyList<string> Guardians { get; init; } = [];
    public int SessionsThisWeek { get; init; }
    public int AttendanceMarked { get; init; }
    public int AttendanceAttended { get; init; }
    public string Status { get; init; } = AccountStatus.Active;
    public bool IsActive { get; init; }
    public DateTime JoinedUtc { get; init; }

    public string Grade => Ui.GradeLabel(GradeLevel);

    /// <summary>Null until at least one session has had its attendance marked.</summary>
    public int? AttendanceRate => AttendanceMarked == 0
        ? null
        : (int)Math.Round(AttendanceAttended * 100d / AttendanceMarked);
}

public class StudentFilter : ListFilter
{
    public GradeLevel? GradeLevel { get; init; }

    protected override void AddExtraRouteValues(Dictionary<string, string> values)
    {
        if (GradeLevel is GradeLevel grade)
        {
            values["gradeLevel"] = ((int)grade).ToString();
        }
    }
}
