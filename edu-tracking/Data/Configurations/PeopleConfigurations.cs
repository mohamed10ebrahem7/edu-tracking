using edu_tracking.Domain;
using edu_tracking.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace edu_tracking.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.TimeZoneId).IsRequired().HasMaxLength(100);

        builder.HasIndex(u => u.FullName);
    }
}

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.UserId);

        builder.HasOne(s => s.User)
            .WithOne(u => u.Student)
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.School).HasMaxLength(200);
        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.HasIndex(s => s.GradeLevel);
    }
}

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.HasKey(t => t.UserId);

        builder.HasOne(t => t.User)
            .WithOne(u => u.Teacher)
            .HasForeignKey<Teacher>(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.Bio).HasMaxLength(2000);
        builder.Property(t => t.HourlyRate).HasPrecision(18, 2);
    }
}

public class ParentConfiguration : IEntityTypeConfiguration<Parent>
{
    public void Configure(EntityTypeBuilder<Parent> builder)
    {
        builder.HasKey(p => p.UserId);

        builder.HasOne(p => p.User)
            .WithOne(u => u.Parent)
            .HasForeignKey<Parent>(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ParentStudentConfiguration : IEntityTypeConfiguration<ParentStudent>
{
    public void Configure(EntityTypeBuilder<ParentStudent> builder)
    {
        builder.HasKey(ps => new { ps.ParentId, ps.StudentId });

        builder.Property(ps => ps.Relationship).IsRequired().HasMaxLength(50);

        builder.HasOne(ps => ps.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(ps => ps.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ps => ps.Student)
            .WithMany(s => s.Parents)
            .HasForeignKey(ps => ps.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ps => ps.StudentId);
    }
}
