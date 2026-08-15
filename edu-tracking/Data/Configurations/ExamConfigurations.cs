using edu_tracking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace edu_tracking.Data.Configurations;

public class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.ToTable("Exams", t =>
            t.HasCheckConstraint("CK_Exam_Marks", "[PassMarks] >= 0 AND [PassMarks] <= [TotalMarks]"));

        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.TotalMarks).HasPrecision(6, 2);
        builder.Property(e => e.PassMarks).HasPrecision(6, 2);

        builder.HasOne(e => e.Subject)
            .WithMany(s => s.Exams)
            .HasForeignKey(e => e.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByTeacher)
            .WithMany(t => t.Exams)
            .HasForeignKey(e => e.CreatedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.SubjectId, e.GradeLevel });
    }
}

public class ExamAttemptConfiguration : IEntityTypeConfiguration<ExamAttempt>
{
    public void Configure(EntityTypeBuilder<ExamAttempt> builder)
    {
        builder.Property(a => a.Score).HasPrecision(6, 2);
        builder.Property(a => a.Grade).HasMaxLength(5);
        builder.Property(a => a.TeacherFeedback).HasMaxLength(2000);

        builder.HasOne(a => a.Exam)
            .WithMany(e => e.Attempts)
            .HasForeignKey(a => a.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Student)
            .WithMany(s => s.ExamAttempts)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // One attempt per student per exam for now; drop this when retakes are supported.
        builder.HasIndex(a => new { a.ExamId, a.StudentId }).IsUnique();
    }
}
