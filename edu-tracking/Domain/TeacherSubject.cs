namespace edu_tracking.Domain;

public class TeacherSubject
{
    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    /// <summary>Falls back to <see cref="Teacher.HourlyRate"/> when null.</summary>
    public decimal? HourlyRateOverride { get; set; }
}
