using Microsoft.AspNetCore.Identity;

namespace edu_tracking.Domain.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }

    public ApplicationRole(string name) : base(name) { }

    public string? Description { get; set; }
}
