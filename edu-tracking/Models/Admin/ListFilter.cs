namespace edu_tracking.Models.Admin;

/// <summary>Search, status and paging shared by the user-management lists.</summary>
public abstract class ListFilter
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;

    /// <summary>Keeps the active filters on pagination links.</summary>
    public Dictionary<string, string> ToRouteValues(int page)
    {
        var values = new Dictionary<string, string> { ["page"] = page.ToString() };

        if (!string.IsNullOrWhiteSpace(Search)) values["search"] = Search;
        if (!string.IsNullOrWhiteSpace(Status)) values["status"] = Status;

        AddExtraRouteValues(values);

        return values;
    }

    /// <summary>Lets a list add its own filter, such as subject or grade.</summary>
    protected virtual void AddExtraRouteValues(Dictionary<string, string> values)
    {
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

/// <summary>
/// Derived from the account rather than stored: an active user who still holds a
/// temporary password has not set their own yet, which is worth showing separately
/// from a disabled account.
/// </summary>
public static class AccountStatus
{
    public const string Active = "Active";
    public const string Pending = "Pending";
    public const string Inactive = "Inactive";

    public static readonly string[] All = [Active, Pending, Inactive];
}
