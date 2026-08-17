namespace edu_tracking.Domain;

/// <summary>
/// A student's place in a group. Rows are kept when a student leaves or is removed rather
/// than deleted, so attendance already recorded against past meetings stays explainable.
/// </summary>
public class ClassGroupMember
{
    public int ClassGroupId { get; set; }
    public ClassGroup ClassGroup { get; set; } = null!;

    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public MemberStatus Status { get; set; } = MemberStatus.Active;

    public DateTime JoinedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }

    /// <summary>The student themselves when they leave, or the teacher who removed them.</summary>
    public Guid? EndedByUserId { get; set; }
}
