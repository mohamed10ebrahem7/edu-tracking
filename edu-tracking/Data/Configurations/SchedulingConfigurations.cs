using edu_tracking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace edu_tracking.Data.Configurations;

public class TeacherWeeklyAvailabilityConfiguration : IEntityTypeConfiguration<TeacherWeeklyAvailability>
{
    public void Configure(EntityTypeBuilder<TeacherWeeklyAvailability> builder)
    {
        builder.ToTable("TeacherWeeklyAvailability", t =>
            t.HasCheckConstraint("CK_TeacherWeeklyAvailability_TimeRange", "[EndTime] > [StartTime]"));

        builder.Property(a => a.SlotMinutes).HasDefaultValue(60);

        builder.HasOne(a => a.Teacher)
            .WithMany(t => t.WeeklyAvailability)
            .HasForeignKey(a => a.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.TeacherId, a.DayOfWeek });
    }
}

public class TeacherTimeOffConfiguration : IEntityTypeConfiguration<TeacherTimeOff>
{
    public void Configure(EntityTypeBuilder<TeacherTimeOff> builder)
    {
        builder.ToTable("TeacherTimeOff", t =>
            t.HasCheckConstraint("CK_TeacherTimeOff_Range", "[EndUtc] > [StartUtc]"));

        builder.Property(o => o.Reason).HasMaxLength(500);

        builder.HasOne(o => o.Teacher)
            .WithMany(t => t.TimeOff)
            .HasForeignKey(o => o.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => new { o.TeacherId, o.StartUtc });
    }
}

public class TeacherSlotConfiguration : IEntityTypeConfiguration<TeacherSlot>
{
    public void Configure(EntityTypeBuilder<TeacherSlot> builder)
    {
        builder.ToTable("TeacherSlots", t =>
            t.HasCheckConstraint("CK_TeacherSlot_Range", "[EndUtc] > [StartUtc]"));

        builder.Property(s => s.Note).HasMaxLength(500);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasOne(s => s.Teacher)
            .WithMany(t => t.Slots)
            .HasForeignKey(s => s.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // A deleted weekly rule must not take surviving (booked) slots with it.
        builder.HasOne(s => s.SourceAvailability)
            .WithMany(a => a.GeneratedSlots)
            .HasForeignKey(s => s.SourceAvailabilityId)
            .OnDelete(DeleteBehavior.SetNull);

        // Makes the slot generator idempotent: re-running it cannot duplicate a slot.
        builder.HasIndex(s => new { s.TeacherId, s.StartUtc }).IsUnique();
        builder.HasIndex(s => new { s.StartUtc, s.Status });
    }
}

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions", t =>
        {
            t.HasCheckConstraint("CK_Session_Capacity", "[Capacity] >= 1");
            t.HasCheckConstraint("CK_Session_Seats", "[SeatsTaken] >= 0 AND [SeatsTaken] <= [Capacity]");
            t.HasCheckConstraint("CK_Session_Range", "[EndUtc] > [StartUtc]");
        });

        builder.Property(s => s.Title).HasMaxLength(200);
        builder.Property(s => s.MeetingUrl).HasMaxLength(500);
        builder.Property(s => s.CancellationReason).HasMaxLength(500);
        builder.Property(s => s.DefaultPrice).HasPrecision(18, 2);
        builder.Property(s => s.Capacity).HasDefaultValue(1);

        // One session per slot; a booked slot cannot be deleted out from under it.
        builder.HasOne(s => s.Slot)
            .WithOne(sl => sl.Session)
            .HasForeignKey<Session>(s => s.SlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Teacher)
            .WithMany(t => t.Sessions)
            .HasForeignKey(s => s.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Subject)
            .WithMany(sub => sub.Sessions)
            .HasForeignKey(s => s.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Drives the student-facing "available sessions for a subject" search.
        builder.HasIndex(s => new { s.SubjectId, s.StartUtc });
        builder.HasIndex(s => new { s.TeacherId, s.StartUtc });
    }
}

public class SessionParticipantConfiguration : IEntityTypeConfiguration<SessionParticipant>
{
    public void Configure(EntityTypeBuilder<SessionParticipant> builder)
    {
        builder.ToTable("SessionParticipants", t =>
            t.HasCheckConstraint(
                "CK_SessionParticipant_Rating",
                "[PerformanceRating] IS NULL OR ([PerformanceRating] >= 1 AND [PerformanceRating] <= 5)"));

        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.TeacherComment).HasMaxLength(2000);
        builder.Property(p => p.CancellationReason).HasMaxLength(500);

        builder.HasOne(p => p.Session)
            .WithMany(s => s.Participants)
            .HasForeignKey(p => p.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Student)
            .WithMany(s => s.Sessions)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.SessionId, p.StudentId }).IsUnique();
        builder.HasIndex(p => new { p.StudentId, p.Status });
    }
}

public class SessionReportConfiguration : IEntityTypeConfiguration<SessionReport>
{
    public void Configure(EntityTypeBuilder<SessionReport> builder)
    {
        builder.Property(r => r.TopicsCovered).IsRequired().HasMaxLength(4000);
        builder.Property(r => r.Homework).HasMaxLength(2000);

        builder.HasOne(r => r.Session)
            .WithOne(s => s.Report)
            .HasForeignKey<SessionReport>(r => r.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BookingRequestConfiguration : IEntityTypeConfiguration<BookingRequest>
{
    public void Configure(EntityTypeBuilder<BookingRequest> builder)
    {
        builder.Property(r => r.StudentMessage).HasMaxLength(1000);
        builder.Property(r => r.DeclineMessageToStudent).HasMaxLength(1000);

        builder.HasOne(r => r.Slot)
            .WithMany(s => s.BookingRequests)
            .HasForeignKey(r => r.SlotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Teacher)
            .WithMany()
            .HasForeignKey(r => r.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Student)
            .WithMany(s => s.BookingRequests)
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Teacher inbox, then "my requests" in the student/parent portal.
        builder.HasIndex(r => new { r.TeacherId, r.Status, r.RequestedAtUtc });
        builder.HasIndex(r => new { r.StudentId, r.Status });

        // One live request per student per slot; declined ones may be re-requested.
        builder.HasIndex(r => new { r.SlotId, r.StudentId })
            .IsUnique()
            .HasFilter($"[Status] = {(int)BookingRequestStatus.Pending}");
    }
}
