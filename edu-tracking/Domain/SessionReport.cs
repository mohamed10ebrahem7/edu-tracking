namespace edu_tracking.Domain;

/// <summary>
/// What was taught in a session. Identical for the whole group, so it lives here;
/// per-student marks and comments live on <see cref="SessionParticipant"/>.
/// </summary>
public class SessionReport : IAuditable
{
    public long Id { get; set; }

    public long SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public string TopicsCovered { get; set; } = null!;
    public string? Homework { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
