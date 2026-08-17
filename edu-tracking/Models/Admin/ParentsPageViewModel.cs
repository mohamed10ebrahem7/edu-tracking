namespace edu_tracking.Models.Admin;

/// <summary>Everything the Users &gt; Parents list screen renders.</summary>
public class ParentsPageViewModel
{
    public string CurrentUserName { get; init; } = "Admin";

    public IReadOnlyList<StatCard> Stats { get; init; } = [];
    public IReadOnlyList<ParentRow> Parents { get; init; } = [];

    public ParentFilter Filter { get; init; } = new();
    public PageInfo Page { get; init; } = new();
}

public class ParentRow
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public IReadOnlyList<ChildLink> Children { get; init; } = [];
    public string Status { get; init; } = AccountStatus.Active;
    public bool IsActive { get; init; }
    public DateTime JoinedUtc { get; init; }

    /// <summary>Normally one value; a parent could in principle differ per child.</summary>
    public string Relationships => Children.Count == 0
        ? "—"
        : string.Join(", ", Children.Select(c => c.Relationship).Distinct());
}

public record ChildLink(string Name, string Relationship, bool IsPrimaryContact);

public class ParentFilter : ListFilter
{
    /// <summary>Surfaces parents who still need a child attached.</summary>
    public bool? WithoutChildren { get; init; }

    protected override void AddExtraRouteValues(Dictionary<string, string> values)
    {
        if (WithoutChildren == true)
        {
            values["withoutChildren"] = "true";
        }
    }
}

public static class ParentRelationship
{
    public const string Father = "Father";
    public const string Mother = "Mother";
    public const string Guardian = "Guardian";

    public static readonly string[] All = [Father, Mother, Guardian, "Other"];
}
