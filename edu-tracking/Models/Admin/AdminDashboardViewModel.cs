namespace edu_tracking.Models.Admin;

/// <summary>
/// Everything the admin dashboard renders. Built today from <c>AdminDashboardData</c>;
/// swap that for database queries and the views need no changes.
/// </summary>
public class AdminDashboardViewModel
{
    public string CurrentUserName { get; init; } = "Admin";

    public IReadOnlyList<StatCard> Stats { get; init; } = [];
    public UserDistribution Distribution { get; init; } = new();
    public IReadOnlyList<ActivityItem> RecentActivities { get; init; } = [];
    public IReadOnlyList<OverviewItem> SystemOverview { get; init; } = [];
    public IReadOnlyList<RecentUser> RecentUsers { get; init; } = [];
    public IReadOnlyList<QuickAction> QuickActions { get; init; } = [];
}

/// <summary>Tone is a colour key: purple, green, blue, orange, red or gray.</summary>
/// <param name="Trend">Optional; omitted when there is no comparison figure to show.</param>
public record StatCard(string Label, string Value, string? Trend, string Icon, string Tone);

public record DistributionSlice(string Label, int Count, string Tone);

public record ActivityItem(string Title, string TimeAgo, string Icon, string Tone);

public record OverviewItem(string Title, string Detail, string Badge, string Tone, string Icon);

public record RecentUser(string Name, string Role, string Email, string Status, string JoinedDate);

public record QuickAction(string Label, string Icon, string Tone);

public class UserDistribution
{
    public IReadOnlyList<DistributionSlice> Slices { get; init; } = [];

    /// <summary>
    /// Headline figure shown in the middle of the ring. It can legitimately differ from
    /// the sum of the slices (a user may hold no role), so percentages are measured
    /// against it while the arcs stay proportional to the slices.
    /// </summary>
    public int? Total { get; init; }

    public int DisplayTotal => Total ?? SliceSum;

    public int SliceSum => Slices.Sum(s => s.Count);

    public double PercentageOf(DistributionSlice slice) =>
        DisplayTotal == 0 ? 0 : Math.Round(slice.Count * 100.0 / DisplayTotal, 1);

    /// <summary>Share of the ring this slice occupies, so the ring always closes.</summary>
    public double ArcFractionOf(DistributionSlice slice) =>
        SliceSum == 0 ? 0 : slice.Count / (double)SliceSum;
}
