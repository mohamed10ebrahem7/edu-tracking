using Microsoft.AspNetCore.Identity;

namespace edu_tracking.Domain.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = null!;

    /// <summary>
    /// IANA time zone id used to render UTC schedule times for this user.
    /// </summary>
    public string TimeZoneId { get; set; } = "Africa/Cairo";

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set when an admin creates the account with a temporary password.
    /// </summary>
    public bool MustChangePassword { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Student? Student { get; set; }
    public Teacher? Teacher { get; set; }
    public Parent? Parent { get; set; }
}
