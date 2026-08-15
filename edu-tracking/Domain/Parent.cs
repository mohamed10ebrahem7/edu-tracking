using edu_tracking.Domain.Identity;

namespace edu_tracking.Domain;

public class Parent
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public ICollection<ParentStudent> Children { get; set; } = [];
}
