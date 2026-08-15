namespace edu_tracking.Domain;

/// <summary>
/// A student may have more than one guardian, and a parent more than one child.
/// </summary>
public class ParentStudent
{
    public Guid ParentId { get; set; }
    public Parent Parent { get; set; } = null!;

    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public string Relationship { get; set; } = "Guardian";
    public bool IsPrimaryContact { get; set; }
}
