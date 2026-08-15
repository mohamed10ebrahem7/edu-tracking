using System.Security.Cryptography;
using edu_tracking.Data;
using edu_tracking.Domain;
using edu_tracking.Domain.Identity;
using edu_tracking.Models.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace edu_tracking.Services;

public record TeacherSaveResult(bool Succeeded, string? Error = null, string? TemporaryPassword = null)
{
    public static TeacherSaveResult Ok(string? temporaryPassword = null) => new(true, null, temporaryPassword);
    public static TeacherSaveResult Fail(string error) => new(false, error);
}

/// <summary>
/// All database work behind the Users &gt; Teachers screens. A teacher is an
/// <see cref="ApplicationUser"/> in the Teacher role plus a <see cref="Teacher"/> profile,
/// so both are always created and updated together.
/// </summary>
public class TeacherAdminService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
{
    public async Task<IReadOnlyList<SubjectOption>> GetSubjectOptionsAsync() =>
        await db.Subjects
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SubjectOption(s.Id, s.Name))
            .ToListAsync();

    public async Task<TeachersPageViewModel> GetPageAsync(TeacherFilter filter, string currentUserName)
    {
        var query = ApplyFilters(db.Teachers.AsQueryable(), filter);

        var totalItems = await query.CountAsync();
        var currentPage = Math.Max(1, filter.Page);
        var (weekStart, weekEnd) = CurrentWeekUtc();

        var teachers = await query
            .OrderBy(t => t.User.FullName)
            .Skip((currentPage - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new TeacherRow
            {
                Id = t.UserId,
                Name = t.User.FullName,
                Email = t.User.Email,
                Phone = t.User.PhoneNumber,
                Subjects = t.Subjects.Select(ts => ts.Subject.Name).ToList(),
                HourlyRate = t.HourlyRate,
                Students = db.SessionParticipants
                    .Where(p => p.Session.TeacherId == t.UserId && p.Status == ParticipantStatus.Confirmed)
                    .Select(p => p.StudentId)
                    .Distinct()
                    .Count(),
                SessionsThisWeek = db.Sessions
                    .Count(s => s.TeacherId == t.UserId && s.StartUtc >= weekStart && s.StartUtc < weekEnd),
                Status = !t.User.IsActive
                    ? TeacherStatus.Inactive
                    : t.User.MustChangePassword ? TeacherStatus.Pending : TeacherStatus.Active,
                AcceptingBookings = t.IsAcceptingBookings,
                IsActive = t.User.IsActive,
                JoinedUtc = t.User.CreatedAtUtc
            })
            .ToListAsync();

        return new TeachersPageViewModel
        {
            CurrentUserName = currentUserName,
            Teachers = teachers,
            Subjects = await GetSubjectOptionsAsync(),
            Stats = await GetStatsAsync(),
            Filter = filter,
            Page = new PageInfo { Current = currentPage, Size = filter.PageSize, TotalItems = totalItems }
        };
    }

    public async Task<TeacherFormViewModel?> GetForEditAsync(Guid id)
    {
        var teacher = await db.Teachers
            .Where(t => t.UserId == id)
            .Select(t => new TeacherFormViewModel
            {
                Id = t.UserId,
                FullName = t.User.FullName,
                UserName = t.User.UserName!,
                Email = t.User.Email,
                Phone = t.User.PhoneNumber,
                SubjectIds = t.Subjects.Select(ts => ts.SubjectId).ToList(),
                HourlyRate = t.HourlyRate,
                DefaultSessionMinutes = t.DefaultSessionMinutes,
                MinBookingNoticeHours = t.MinBookingNoticeHours,
                CancellationCutoffHours = t.CancellationCutoffHours,
                Bio = t.Bio,
                IsAcceptingBookings = t.IsAcceptingBookings,
                AutoApproveBookings = t.AutoApproveBookings
            })
            .FirstOrDefaultAsync();

        if (teacher is not null)
        {
            teacher.AvailableSubjects = await GetSubjectOptionsAsync();
        }

        return teacher;
    }

    public async Task<TeacherSaveResult> CreateAsync(TeacherFormViewModel form)
    {
        var password = string.IsNullOrWhiteSpace(form.TemporaryPassword)
            ? GenerateTemporaryPassword()
            : form.TemporaryPassword;

        // The account and the profile must appear together or not at all. Because the
        // context is configured with EnableRetryOnFailure, a manual transaction has to
        // run inside the execution strategy so the whole unit is retried as one.
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            var user = new ApplicationUser
            {
                UserName = form.UserName.Trim(),
                Email = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email.Trim(),
                EmailConfirmed = true,
                PhoneNumber = form.Phone,
                FullName = form.FullName.Trim(),
                IsActive = true,
                MustChangePassword = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                return TeacherSaveResult.Fail(Describe(created));
            }

            var roleAdded = await userManager.AddToRoleAsync(user, AppRoles.Teacher);
            if (!roleAdded.Succeeded)
            {
                return TeacherSaveResult.Fail(Describe(roleAdded));
            }

            db.Teachers.Add(new Teacher
            {
                UserId = user.Id,
                Bio = form.Bio,
                HourlyRate = form.HourlyRate,
                DefaultSessionMinutes = form.DefaultSessionMinutes,
                MinBookingNoticeHours = form.MinBookingNoticeHours,
                CancellationCutoffHours = form.CancellationCutoffHours,
                IsAcceptingBookings = form.IsAcceptingBookings,
                AutoApproveBookings = form.AutoApproveBookings
            });

            AddSubjects(user.Id, form.SubjectIds);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return TeacherSaveResult.Ok(password);
        });
    }

    public async Task<TeacherSaveResult> UpdateAsync(TeacherFormViewModel form)
    {
        var teacher = await db.Teachers
            .Include(t => t.User)
            .Include(t => t.Subjects)
            .FirstOrDefaultAsync(t => t.UserId == form.Id);

        if (teacher is null)
        {
            return TeacherSaveResult.Fail("That teacher no longer exists.");
        }

        teacher.User.FullName = form.FullName.Trim();
        teacher.User.PhoneNumber = form.Phone;
        teacher.Bio = form.Bio;
        teacher.HourlyRate = form.HourlyRate;
        teacher.DefaultSessionMinutes = form.DefaultSessionMinutes;
        teacher.MinBookingNoticeHours = form.MinBookingNoticeHours;
        teacher.CancellationCutoffHours = form.CancellationCutoffHours;
        teacher.IsAcceptingBookings = form.IsAcceptingBookings;
        teacher.AutoApproveBookings = form.AutoApproveBookings;

        var newEmail = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email.Trim();
        if (!string.Equals(teacher.User.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            // Goes through UserManager so the normalised email stays in sync.
            var emailChanged = await userManager.SetEmailAsync(teacher.User, newEmail);
            if (!emailChanged.Succeeded)
            {
                return TeacherSaveResult.Fail(Describe(emailChanged));
            }
        }

        SyncSubjects(teacher, form.SubjectIds);

        await db.SaveChangesAsync();
        return TeacherSaveResult.Ok();
    }

    public async Task<TeacherSaveResult> SetActiveAsync(Guid id, bool isActive)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return TeacherSaveResult.Fail("That teacher no longer exists.");
        }

        user.IsActive = isActive;
        await db.SaveChangesAsync();

        // Invalidates any cookie the teacher is still holding.
        await userManager.UpdateSecurityStampAsync(user);

        return TeacherSaveResult.Ok();
    }

    public async Task<TeacherSaveResult> ResetPasswordAsync(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return TeacherSaveResult.Fail("That teacher no longer exists.");
        }

        var password = GenerateTemporaryPassword();
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(user, token, password);

        if (!reset.Succeeded)
        {
            return TeacherSaveResult.Fail(Describe(reset));
        }

        user.MustChangePassword = true;
        await db.SaveChangesAsync();

        return TeacherSaveResult.Ok(password);
    }

    public Task<string?> GetNameAsync(Guid id) =>
        db.Users.Where(u => u.Id == id).Select(u => u.FullName).FirstOrDefaultAsync();

    // ---------- helpers ----------

    private static IQueryable<Teacher> ApplyFilters(IQueryable<Teacher> query, TeacherFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(t =>
                t.User.FullName.Contains(search) ||
                t.User.Email!.Contains(search) ||
                t.User.UserName!.Contains(search));
        }

        if (filter.SubjectId is int subjectId)
        {
            query = query.Where(t => t.Subjects.Any(ts => ts.SubjectId == subjectId));
        }

        query = filter.Status switch
        {
            TeacherStatus.Active => query.Where(t => t.User.IsActive && !t.User.MustChangePassword),
            TeacherStatus.Pending => query.Where(t => t.User.IsActive && t.User.MustChangePassword),
            TeacherStatus.Inactive => query.Where(t => !t.User.IsActive),
            _ => query
        };

        return query;
    }

    private async Task<IReadOnlyList<StatCard>> GetStatsAsync()
    {
        var total = await db.Teachers.CountAsync();
        var active = await db.Teachers.CountAsync(t => t.User.IsActive);
        var accepting = await db.Teachers.CountAsync(t => t.User.IsActive && t.IsAcceptingBookings);
        var averageRate = await db.Teachers.Select(t => (decimal?)t.HourlyRate).AverageAsync() ?? 0m;

        return
        [
            new StatCard("Total Teachers", total.ToString(), null, "bi-person-badge", "purple"),
            new StatCard("Active", active.ToString(), null, "bi-check2-circle", "green"),
            new StatCard("Accepting Bookings", accepting.ToString(), null, "bi-calendar-check", "blue"),
            new StatCard("Avg. Hourly Rate", $"EGP {averageRate:N0}", null, "bi-cash-coin", "orange")
        ];
    }

    private void AddSubjects(Guid teacherId, IEnumerable<int> subjectIds)
    {
        foreach (var subjectId in subjectIds.Distinct())
        {
            db.TeacherSubjects.Add(new TeacherSubject { TeacherId = teacherId, SubjectId = subjectId });
        }
    }

    private void SyncSubjects(Teacher teacher, List<int> subjectIds)
    {
        foreach (var link in teacher.Subjects.Where(ts => !subjectIds.Contains(ts.SubjectId)).ToList())
        {
            db.TeacherSubjects.Remove(link);
        }

        var existing = teacher.Subjects.Select(ts => ts.SubjectId).ToHashSet();
        AddSubjects(teacher.UserId, subjectIds.Where(id => !existing.Contains(id)));
    }

    /// <summary>Sunday-to-Saturday window used by the "sessions this week" column.</summary>
    private static (DateTime Start, DateTime End) CurrentWeekUtc()
    {
        var today = DateTime.UtcNow.Date;
        var start = today.AddDays(-(int)today.DayOfWeek);
        return (start, start.AddDays(7));
    }

    private static string Describe(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Description));

    private static string GenerateTemporaryPassword()
    {
        // Ambiguous characters left out so the password can be read aloud.
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";

        var characters = new List<char> { Pick(upper), Pick(lower), Pick(digits), Pick(digits) };

        while (characters.Count < 10)
        {
            characters.Add(Pick(upper + lower + digits));
        }

        return new string([.. characters.OrderBy(_ => RandomNumberGenerator.GetInt32(1000))]);

        static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
    }
}
