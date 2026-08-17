using edu_tracking.Data;
using edu_tracking.Domain;
using edu_tracking.Domain.Identity;
using edu_tracking.Models;
using edu_tracking.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace edu_tracking.Services;

/// <summary>
/// The Users &gt; Teachers screens. A teacher is an <see cref="ApplicationUser"/> in the
/// Teacher role plus a <see cref="Teacher"/> profile; account work is delegated to
/// <see cref="AccountAdminService"/>.
/// </summary>
public class TeacherAdminService(ApplicationDbContext db, AccountAdminService accounts)
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
        var (weekStart, weekEnd) = AccountAdminService.CurrentWeekUtc();

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
                    ? AccountStatus.Inactive
                    : t.User.MustChangePassword ? AccountStatus.Pending : AccountStatus.Active,
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

    public Task<AccountResult> CreateAsync(TeacherFormViewModel form)
    {
        var password = string.IsNullOrWhiteSpace(form.TemporaryPassword)
            ? AccountAdminService.GenerateTemporaryPassword()
            : form.TemporaryPassword;

        return accounts.InTransactionAsync(async () =>
        {
            var account = new NewAccount(form.FullName, form.UserName, form.Email, form.Phone);
            var (user, error) = await accounts.CreateUserAsync(account, AppRoles.Teacher, password);

            if (user is null)
            {
                return AccountResult.Fail(error!);
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
            return AccountResult.Ok(password);
        });
    }

    public async Task<AccountResult> UpdateAsync(TeacherFormViewModel form)
    {
        var teacher = await db.Teachers
            .Include(t => t.User)
            .Include(t => t.Subjects)
            .FirstOrDefaultAsync(t => t.UserId == form.Id);

        if (teacher is null)
        {
            return AccountResult.Fail("That teacher no longer exists.");
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

        var emailError = await accounts.UpdateEmailAsync(teacher.User, form.Email);
        if (emailError is not null)
        {
            return AccountResult.Fail(emailError);
        }

        SyncSubjects(teacher, form.SubjectIds);

        await db.SaveChangesAsync();
        return AccountResult.Ok();
    }

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

        return filter.Status switch
        {
            AccountStatus.Active => query.Where(t => t.User.IsActive && !t.User.MustChangePassword),
            AccountStatus.Pending => query.Where(t => t.User.IsActive && t.User.MustChangePassword),
            AccountStatus.Inactive => query.Where(t => !t.User.IsActive),
            _ => query
        };
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
}
