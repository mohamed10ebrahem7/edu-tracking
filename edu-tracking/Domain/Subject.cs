namespace edu_tracking.Domain;

public class Subject : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<TeacherSubject> Teachers { get; set; } = [];
    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<Exam> Exams { get; set; } = [];
}
