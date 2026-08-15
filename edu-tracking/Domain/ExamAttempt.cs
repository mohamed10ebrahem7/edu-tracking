namespace edu_tracking.Domain;

/// <summary>
/// One student's sitting of an exam, and the mark for it. Teachers type the score in
/// for offline exams; an online MCQ exam will compute it from answers later without
/// changing anything the students and parents read.
/// </summary>
public class ExamAttempt
{
    public long Id { get; set; }

    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;

    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public DateTime? StartedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }

    public decimal? Score { get; set; }
    public string? Grade { get; set; }
    public string? TeacherFeedback { get; set; }

    public Guid? GradedByUserId { get; set; }
    public DateTime? GradedAtUtc { get; set; }
}
