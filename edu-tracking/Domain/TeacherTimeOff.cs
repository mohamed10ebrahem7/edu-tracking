namespace edu_tracking.Domain;

/// <summary>
/// One-off block that suppresses slot generation for a date range (holidays, travel).
/// </summary>
public class TeacherTimeOff
{
    public int Id { get; set; }

    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string? Reason { get; set; }
}
