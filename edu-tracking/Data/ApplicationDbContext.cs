using edu_tracking.Domain;
using edu_tracking.Domain.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace edu_tracking.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<ParentStudent> ParentStudents => Set<ParentStudent>();

    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();

    public DbSet<TeacherWeeklyAvailability> TeacherAvailabilities => Set<TeacherWeeklyAvailability>();
    public DbSet<TeacherTimeOff> TeacherTimeOffs => Set<TeacherTimeOff>();
    public DbSet<TeacherSlot> TeacherSlots => Set<TeacherSlot>();

    public DbSet<ClassGroup> ClassGroups => Set<ClassGroup>();
    public DbSet<ClassGroupSchedule> ClassGroupSchedules => Set<ClassGroupSchedule>();
    public DbSet<ClassGroupMember> ClassGroupMembers => Set<ClassGroupMember>();

    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionParticipant> SessionParticipants => Set<SessionParticipant>();
    public DbSet<SessionReport> SessionReports => Set<SessionReport>();
    public DbSet<BookingRequest> BookingRequests => Set<BookingRequest>();

    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        StampAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <remarks>
    /// Bypassed by <c>ExecuteUpdate</c>/<c>ExecuteDelete</c>, which do not go through
    /// the change tracker. Set the timestamps explicitly in those calls.
    /// </remarks>
    private void StampAuditFields()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = now;
                    break;
            }
        }
    }
}
