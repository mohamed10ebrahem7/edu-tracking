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

    public static readonly string[] StatusOptions = [TeacherStatus.Active, TeacherStatus.Pending, TeacherStatus.Inactive];
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
    public string Status { get; init; } = TeacherStatus.Active;
    public bool AcceptingBookings { get; init; }
    public bool IsActive { get; init; }
    public DateTime JoinedUtc { get; init; }
}

/// <summary>
/// Derived from the account rather than stored: a teacher who still has a temporary
/// password has not signed in yet, which is worth showing separately from a disabled one.
/// </summary>
public static class TeacherStatus
{
    public const string Active = "Active";
    public const string Pending = "Pending";
    public const string Inactive = "Inactive";
}

public class TeacherFilter
{
    public string? Search { get; init; }
    public int? SubjectId { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;

    /// <summary>Keeps the active filters on pagination links.</summary>
    public Dictionary<string, string> ToRouteValues(int page)
    {
        var values = new Dictionary<string, string> { ["page"] = page.ToString() };

        if (!string.IsNullOrWhiteSpace(Search)) values["search"] = Search;
        if (SubjectId is int subjectId) values["subjectId"] = subjectId.ToString();
        if (!string.IsNullOrWhiteSpace(Status)) values["status"] = Status;

        return values;
    }
}

public class PageInfo
{
    public int Current { get; init; } = 1;
    public int Size { get; init; } = 10;
    public int TotalItems { get; init; }

    public int TotalPages => Size == 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalItems / (double)Size));
    public int FirstShown => TotalItems == 0 ? 0 : ((Current - 1) * Size) + 1;
    public int LastShown => Math.Min(Current * Size, TotalItems);
}
