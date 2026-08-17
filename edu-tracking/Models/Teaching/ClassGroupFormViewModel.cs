using System.ComponentModel.DataAnnotations;
using edu_tracking.Domain;
using edu_tracking.Infrastructure;

namespace edu_tracking.Models.Teaching;

/// <summary>
/// Create and edit a class. The seven day rows are always posted; only the ticked ones
/// become meetings, which keeps the form a plain post with no JavaScript behind it.
/// </summary>
public class ClassGroupFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Subject")]
    [Range(1, int.MaxValue, ErrorMessage = "Pick a subject.")]
    public int SubjectId { get; set; }

    [Display(Name = "Grade")]
    public GradeLevel GradeLevel { get; set; } = GradeLevel.Grade7;

    [Display(Name = "Type")]
    public ClassKind Kind { get; set; } = ClassKind.Lecture;

    [StringLength(200)]
    [Display(Name = "Title (optional)")]
    public string? Title { get; set; }

    [Display(Name = "Maximum students")]
    [Range(1, ClassGroup.MaxAllowedStudents)]
    public int MaxStudents { get; set; } = 1;

    [Display(Name = "First week starts")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; }

    [Display(Name = "Last week ends")]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; }

    [Display(Name = "Repeat every week")]
    public bool RepeatsWeekly { get; set; } = true;

    [Display(Name = "Open for students to join")]
    public bool IsOpenForEnrollment { get; set; } = true;

    public List<ClassDayInput> Days { get; set; } = [];

    // ---------- filled in for display, never trusted from the post ----------

    public string CurrentUserName { get; set; } = "Teacher";
    public string? Subjects { get; set; }

    public IReadOnlyList<SubjectChoice> AvailableSubjects { get; set; } = [];

    /// <summary>Working hours per day, so the teacher can see what a class must fit inside.</summary>
    public IReadOnlyList<string> WorkingHours { get; set; } = [];

    /// <summary>Members freeze the schedule: only the title, size and enrolment can move.</summary>
    public bool HasMembers { get; set; }

    public bool IsNew => Id == 0;
    public string Heading => IsNew ? "Create a class" : "Edit class";

    /// <summary>Seven rows Monday to Sunday, matching the schedule's columns.</summary>
    public static List<ClassDayInput> EmptyWeek() =>
        [.. Enumerable.Range(0, 7).Select(i => new ClassDayInput { DayOfWeek = (DayOfWeek)(((i + 1) % 7)) })];
}

public class ClassDayInput
{
    public bool Selected { get; set; }
    public DayOfWeek DayOfWeek { get; set; }

    [DataType(DataType.Time)]
    public TimeOnly? StartTime { get; set; }

    [DataType(DataType.Time)]
    public TimeOnly? EndTime { get; set; }

    public string Name => DayOfWeek.ToString();
}

public record SubjectChoice(int Id, string Name);

public record GradeChoice(int Value, string Label)
{
    public static readonly IReadOnlyList<GradeChoice> All =
        [.. Enum.GetValues<GradeLevel>().Select(g => new GradeChoice((int)g, Ui.GradeLabel(g)))];
}
