using edu_tracking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace edu_tracking.Data.Configurations;

public class ClassGroupConfiguration : IEntityTypeConfiguration<ClassGroup>
{
    public void Configure(EntityTypeBuilder<ClassGroup> builder)
    {
        builder.ToTable("ClassGroups", t =>
        {
            t.HasCheckConstraint(
                "CK_ClassGroup_MaxStudents",
                $"[MaxStudents] >= 1 AND [MaxStudents] <= {ClassGroup.MaxAllowedStudents}");

            // Together with the conditional UPDATE used to claim a place, this makes
            // overbooking impossible rather than merely unlikely.
            t.HasCheckConstraint(
                "CK_ClassGroup_Members",
                "[MembersCount] >= 0 AND [MembersCount] <= [MaxStudents]");

            t.HasCheckConstraint("CK_ClassGroup_Dates", "[EndDate] >= [StartDate]");
        });

        builder.Property(g => g.Title).HasMaxLength(200);
        builder.Property(g => g.RowVersion).IsRowVersion();

        builder.HasOne(g => g.Teacher)
            .WithMany(t => t.ClassGroups)
            .HasForeignKey(g => g.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Subject)
            .WithMany(s => s.ClassGroups)
            .HasForeignKey(g => g.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => new { g.TeacherId, g.StartDate });

        // Drives the student-facing "open classes for my grade and subject" search.
        builder.HasIndex(g => new { g.SubjectId, g.GradeLevel, g.IsOpenForEnrollment });
    }
}

public class ClassGroupScheduleConfiguration : IEntityTypeConfiguration<ClassGroupSchedule>
{
    public void Configure(EntityTypeBuilder<ClassGroupSchedule> builder)
    {
        builder.ToTable("ClassGroupSchedules", t =>
            t.HasCheckConstraint("CK_ClassGroupSchedule_TimeRange", "[EndTime] > [StartTime]"));

        builder.HasOne(s => s.ClassGroup)
            .WithMany(g => g.Schedule)
            .HasForeignKey(s => s.ClassGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.ClassGroupId, s.DayOfWeek });
    }
}

public class ClassGroupMemberConfiguration : IEntityTypeConfiguration<ClassGroupMember>
{
    public void Configure(EntityTypeBuilder<ClassGroupMember> builder)
    {
        builder.HasKey(m => new { m.ClassGroupId, m.StudentId });

        builder.HasOne(m => m.ClassGroup)
            .WithMany(g => g.Members)
            .HasForeignKey(m => m.ClassGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Student)
            .WithMany()
            .HasForeignKey(m => m.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.StudentId, m.Status });
    }
}
