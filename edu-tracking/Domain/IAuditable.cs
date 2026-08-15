namespace edu_tracking.Domain;

/// <summary>
/// Timestamps are stamped centrally in <c>ApplicationDbContext.SaveChangesAsync</c>.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
}
