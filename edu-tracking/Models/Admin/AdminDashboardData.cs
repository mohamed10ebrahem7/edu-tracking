namespace edu_tracking.Models.Admin;

/// <summary>
/// Placeholder content for the dashboard design. Replace each member with a query
/// against <c>ApplicationDbContext</c> when the real screens are wired up; the shape
/// of what the views consume stays the same.
/// </summary>
public static class AdminDashboardData
{
    public static AdminDashboardViewModel Build(string currentUserName) => new()
    {
        CurrentUserName = currentUserName,
        Stats = Stats,
        Distribution = Distribution,
        RecentActivities = RecentActivities,
        SystemOverview = SystemOverview,
        RecentUsers = RecentUsers,
        QuickActions = QuickActions
    };

    private static readonly StatCard[] Stats =
    [
        new("Total Users", "532", "12% from last month", "bi-people", "purple"),
        new("Students", "320", "8% from last month", "bi-mortarboard", "green"),
        new("Teachers", "68", "5% from last month", "bi-person-badge", "blue"),
        new("Parents", "144", "9% from last month", "bi-people-fill", "orange"),
        new("Sessions (This Week)", "128", "15% from last week", "bi-calendar-week", "indigo")
    ];

    private static readonly UserDistribution Distribution = new()
    {
        Total = 532,
        Slices =
        [
            new("Students", 320, "purple"),
            new("Teachers", 68, "green"),
            new("Parents", 144, "orange"),
            new("Admins", 12, "blue")
        ]
    };

    private static readonly ActivityItem[] RecentActivities =
    [
        new("New student John Doe was added", "2 min ago", "bi-person-plus", "purple"),
        new("Teacher Sarah Johnson created a new session", "15 min ago", "bi-calendar-plus", "green"),
        new("Parent Mark Williams updated profile", "1 hour ago", "bi-person-gear", "blue"),
        new("Exam \"Math - Chapter 3\" was published", "2 hours ago", "bi-bar-chart", "orange"),
        new("Admin James Brown created a new teacher", "3 hours ago", "bi-shield-check", "indigo")
    ];

    private static readonly OverviewItem[] SystemOverview =
    [
        new("Active Sessions", "24", "Live", "green", "bi-broadcast"),
        new("Exams This Week", "7", "Upcoming", "blue", "bi-file-earmark-text"),
        new("Pending Reports", "15", "Pending", "orange", "bi-clipboard-data"),
        new("System Status", "All Systems Operational", "Healthy", "green", "bi-hdd-stack")
    ];

    private static readonly RecentUser[] RecentUsers =
    [
        new("John Doe", "Student", "john.doe@email.com", "Active", "May 20, 2024"),
        new("Sarah Johnson", "Teacher", "sarah.j@email.com", "Active", "May 19, 2024"),
        new("Mark Williams", "Parent", "mark.w@email.com", "Active", "May 18, 2024"),
        new("James Brown", "Admin", "james.b@email.com", "Active", "May 17, 2024")
    ];

    private static readonly QuickAction[] QuickActions =
    [
        new("Add New Student", "bi-person-plus", "purple"),
        new("Add New Teacher", "bi-person-badge", "green"),
        new("Add New Parent", "bi-people", "blue"),
        new("Create New Session", "bi-calendar-plus", "orange"),
        new("Create New Exam", "bi-file-earmark-plus", "indigo")
    ];
}
