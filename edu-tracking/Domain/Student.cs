using edu_tracking.Domain.Identity;

namespace edu_tracking.Domain;

/// <summary>
/// Student profile. Shares its primary key with the Identity user, so every student
/// has exactly one login and <c>StudentId == UserId</c> everywhere in the model.
/// </summary>
public class Student
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public GradeLevel GradeLevel { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? School { get; set; }
    public string? Notes { get; set; }

    public ICollection<ParentStudent> Parents { get; set; } = [];
    public ICollection<SessionParticipant> Sessions { get; set; } = [];
    public ICollection<BookingRequest> BookingRequests { get; set; } = [];
    public ICollection<ExamAttempt> ExamAttempts { get; set; } = [];
}
