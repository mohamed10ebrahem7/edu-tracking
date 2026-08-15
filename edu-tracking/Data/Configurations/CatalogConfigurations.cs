using edu_tracking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace edu_tracking.Data.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(20);
        builder.Property(s => s.Description).HasMaxLength(500);

        builder.HasIndex(s => s.Name).IsUnique();
        builder.HasIndex(s => s.Code).IsUnique();
    }
}

public class TeacherSubjectConfiguration : IEntityTypeConfiguration<TeacherSubject>
{
    public void Configure(EntityTypeBuilder<TeacherSubject> builder)
    {
        builder.HasKey(ts => new { ts.TeacherId, ts.SubjectId });

        builder.Property(ts => ts.HourlyRateOverride).HasPrecision(18, 2);

        builder.HasOne(ts => ts.Teacher)
            .WithMany(t => t.Subjects)
            .HasForeignKey(ts => ts.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ts => ts.Subject)
            .WithMany(s => s.Teachers)
            .HasForeignKey(ts => ts.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ts => ts.SubjectId);
    }
}
