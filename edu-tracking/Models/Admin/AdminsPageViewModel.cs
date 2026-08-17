namespace edu_tracking.Models.Admin;

/// <summary>Everything the Users &gt; Admins list screen renders.</summary>
public class AdminsPageViewModel
{
    public string CurrentUserName { get; init; } = "Admin";

    public IReadOnlyList<StatCard> Stats { get; init; } = [];
    public IReadOnlyList<AdminRow> Admins { get; init; } = [];

    public AdminFilter Filter { get; init; } = new();
    public PageInfo Page { get; init; } = new();

    /// <summary>Across all admins, not just the current page, so the last one cannot be locked out.</summary>
    public int ActiveAdmins { get; init; }

    /// <summary>
    /// Mirrors the rules enforced in the service: nobody may switch off their own
    /// account, and the final active admin has to stay.
    /// </summary>
    public bool CanDeactivate(AdminRow admin) => admin.IsActive && !admin.IsCurrentUser && ActiveAdmins > 1;
}

public class AdminRow
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string UserName { get; init; } = "";
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string Status { get; init; } = AccountStatus.Active;
    public bool IsActive { get; init; }
    public bool IsCurrentUser { get; init; }
    public DateTime JoinedUtc { get; init; }
}

public class AdminFilter : ListFilter;
