using edu_tracking.Data;
using edu_tracking.Domain;
using edu_tracking.Domain.Identity;
using edu_tracking.Models;
using edu_tracking.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace edu_tracking.Services;

/// <summary>
/// The Users &gt; Students screens. A student is an <see cref="ApplicationUser"/> in the
/// Student role plus a <see cref="Student"/> profile, optionally linked to guardians
/// through <see cref="ParentStudent"/>.
/// </summary>
public class StudentAdminService(ApplicationDbContext db, AccountAdminService accounts)
{
    /// <summary>Parent accounts that can be attached as guardians.</summary>
    public async Task<IReadOnlyList<ParentOption>> GetParentOptionsAsync() =>
        await db.Parents
            .Where(p => p.User.IsActive)
            .OrderBy(p => p.User.FullName)
            .Select(p => new ParentOption(p.UserId, p.User.FullName))
            .ToListAsync();

    public async Task<StudentsPageViewModel> GetPageAsync(StudentFilter filter, string currentUserName)
    {
        var query = ApplyFilters(db.Students.AsQueryable(), filter);

        var totalItems = await query.CountAsync();
        var currentPage = Math.Max(1, filter.Page);
        var (weekStart, weekEnd) = AccountAdminService.CurrentWeekUtc();

        var students = await query
            .OrderBy(s => s.User.FullName)
            .Skip((currentPage - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(s => new StudentRow
            {
                Id = s.UserId,
                Name = s.User.FullName,
                Email = s.User.Email,
                GradeLevel = s.GradeLevel,
                School = s.School,
                Guardians = s.Parents.Select(ps => ps.Parent.User.FullName).ToList(),
                SessionsThisWeek = db.SessionParticipants.Count(p =>
                    p.StudentId == s.UserId &&
                    p.Status == ParticipantStatus.Confirmed &&
                    p.Session.StartUtc >= weekStart &&
                    p.Session.StartUtc < weekEnd),
                AttendanceMarked = db.SessionParticipants.Count(p =>
                    p.StudentId == s.UserId && p.Attendance != AttendanceStatus.NotMarked),
                AttendanceAttended = db.SessionParticipants.Count(p =>
                    p.StudentId == s.UserId &&
                    (p.Attendance == AttendanceStatus.Present || p.Attendance == AttendanceStatus.Late)),
                Status = !s.User.IsActive
                    ? AccountStatus.Inactive
                    : s.User.MustChangePassword ? AccountStatus.Pending : AccountStatus.Active,
                IsActive = s.User.IsActive,
                JoinedUtc = s.User.CreatedAtUtc
            })
            .ToListAsync();

        return new StudentsPageViewModel
        {
            CurrentUserName = currentUserName,
            Students = students,
            Stats = await GetStatsAsync(weekStart, weekEnd),
            Filter = filter,
            Page = new PageInfo { Current = currentPage, Size = filter.PageSize, TotalItems = totalItems }
        };
    }

    public async Task<StudentFormViewModel?> GetForEditAsync(Guid id)
    {
        var student = await db.Students
            .Where(s => s.UserId == id)
            .Select(s => new StudentFormViewModel
            {
                Id = s.UserId,
                FullName = s.User.FullName,
                UserName = s.User.UserName!,
                Email = s.User.Email,
                Phone = s.User.PhoneNumber,
                GradeLevel = s.GradeLevel,
                DateOfBirth = s.DateOfBirth,
                School = s.School,
                Notes = s.Notes,
                ParentIds = s.Parents.Select(ps => ps.ParentId).ToList()
            })
            .FirstOrDefaultAsync();

        if (student is not null)
        {
            student.AvailableParents = await GetParentOptionsAsync();
        }

        return student;
    }

    public Task<AccountResult> CreateAsync(StudentFormViewModel form)
    {
        var password = string.IsNullOrWhiteSpace(form.TemporaryPassword)
            ? AccountAdminService.GenerateTemporaryPassword()
            : form.TemporaryPassword;

        return accounts.InTransactionAsync(async () =>
        {
            var account = new NewAccount(form.FullName, form.UserName, form.Email, form.Phone);
            var (user, error) = await accounts.CreateUserAsync(account, AppRoles.Student, password);

            if (user is null)
            {
                return AccountResult.Fail(error!);
            }

            db.Students.Add(new Student
            {
                UserId = user.Id,
                GradeLevel = form.GradeLevel,
                DateOfBirth = form.DateOfBirth,
                School = form.School,
                Notes = form.Notes
            });

            AddGuardians(user.Id, form.ParentIds, hasPrimaryContact: false);

            await db.SaveChangesAsync();
            return AccountResult.Ok(password);
        });
    }

    public async Task<AccountResult> UpdateAsync(StudentFormViewModel form)
    {
        var student = await db.Students
            .Include(s => s.User)
            .Include(s => s.Parents)
            .FirstOrDefaultAsync(s => s.UserId == form.Id);

        if (student is null)
        {
            return AccountResult.Fail("That student no longer exists.");
        }

        student.User.FullName = form.FullName.Trim();
        student.User.PhoneNumber = form.Phone;
        student.GradeLevel = form.GradeLevel;
        student.DateOfBirth = form.DateOfBirth;
        student.School = form.School;
        student.Notes = form.Notes;

        var emailError = await accounts.UpdateEmailAsync(student.User, form.Email);
        if (emailError is not null)
        {
            return AccountResult.Fail(emailError);
        }

        SyncGuardians(student, form.ParentIds);

        await db.SaveChangesAsync();
        return AccountResult.Ok();
    }

    // ---------- helpers ----------

    private static IQueryable<Student> ApplyFilters(IQueryable<Student> query, StudentFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(s =>
                s.User.FullName.Contains(search) ||
                s.User.Email!.Contains(search) ||
                s.User.UserName!.Contains(search) ||
                s.School!.Contains(search));
        }

        if (filter.GradeLevel is GradeLevel grade)
        {
            query = query.Where(s => s.GradeLevel == grade);
        }

        return filter.Status switch
        {
            AccountStatus.Active => query.Where(s => s.User.IsActive && !s.User.MustChangePassword),
            AccountStatus.Pending => query.Where(s => s.User.IsActive && s.User.MustChangePassword),
            AccountStatus.Inactive => query.Where(s => !s.User.IsActive),
            _ => query
        };
    }

    private async Task<IReadOnlyList<StatCard>> GetStatsAsync(DateTime weekStart, DateTime weekEnd)
    {
        var total = await db.Students.CountAsync();
        var active = await db.Students.CountAsync(s => s.User.IsActive);

        var sessionsThisWeek = await db.SessionParticipants.CountAsync(p =>
            p.Status == ParticipantStatus.Confirmed &&
            p.Session.StartUtc >= weekStart &&
            p.Session.StartUtc < weekEnd);

        var marked = await db.SessionParticipants.CountAsync(p => p.Attendance != AttendanceStatus.NotMarked);
        var attended = await db.SessionParticipants.CountAsync(p =>
            p.Attendance == AttendanceStatus.Present || p.Attendance == AttendanceStatus.Late);

        var attendance = marked == 0 ? "—" : $"{Math.Round(attended * 100d / marked)}%";

        return
        [
            new StatCard("Total Students", total.ToString(), null, "bi-mortarboard", "purple"),
            new StatCard("Active", active.ToString(), null, "bi-check2-circle", "green"),
            new StatCard("Sessions This Week", sessionsThisWeek.ToString(), null, "bi-calendar-week", "blue"),
            new StatCard("Attendance Rate", attendance, null, "bi-clipboard-check", "orange")
        ];
    }

    private void AddGuardians(Guid studentId, IEnumerable<Guid> parentIds, bool hasPrimaryContact)
    {
        foreach (var parentId in parentIds.Distinct())
        {
            db.ParentStudents.Add(new ParentStudent
            {
                StudentId = studentId,
                ParentId = parentId,
                Relationship = "Guardian",
                // First guardian attached becomes the contact of record.
                IsPrimaryContact = !hasPrimaryContact
            });

            hasPrimaryContact = true;
        }
    }

    private void SyncGuardians(Student student, List<Guid> parentIds)
    {
        foreach (var link in student.Parents.Where(ps => !parentIds.Contains(ps.ParentId)).ToList())
        {
            db.ParentStudents.Remove(link);
        }

        var kept = student.Parents.Where(ps => parentIds.Contains(ps.ParentId)).ToList();
        AddGuardians(
            student.UserId,
            parentIds.Where(id => kept.All(ps => ps.ParentId != id)),
            hasPrimaryContact: kept.Any(ps => ps.IsPrimaryContact));
    }
}
