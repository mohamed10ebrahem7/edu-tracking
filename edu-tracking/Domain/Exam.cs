namespace edu_tracking.Domain;

public class Exam : IAuditable
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public Guid CreatedByTeacherId { get; set; }
    public Teacher CreatedByTeacher { get; set; } = null!;

    public GradeLevel GradeLevel { get; set; }

    /// <summary>
    /// Offline exams are marked by hand; online MCQ exams fill the same attempt rows
    /// automatically once questions are added in a later phase.
    /// </summary>
    public ExamMode Mode { get; set; } = ExamMode.Offline;

    public decimal TotalMarks { get; set; }
    public decimal PassMarks { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsPublished { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ExamAttempt> Attempts { get; set; } = [];
}
