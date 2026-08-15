using System.Security.Claims;
using edu_tracking.Domain.Identity;

namespace edu_tracking.Infrastructure;

/// <summary>
/// Maps a user's role to the controller that hosts their panel. Admin wins when a user
/// somehow holds several roles.
/// </summary>
public static class RolePanel
{
    public static string ControllerFor(IEnumerable<string> roles)
    {
        var set = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (set.Contains(AppRoles.Admin)) return "Admin";
        if (set.Contains(AppRoles.Teacher)) return "Teacher";
        if (set.Contains(AppRoles.Parent)) return "Parent";
        if (set.Contains(AppRoles.Student)) return "Student";

        return "Home";
    }

    public static string ControllerFor(ClaimsPrincipal user) =>
        ControllerFor(AppRoles.All.Where(user.IsInRole));
}
