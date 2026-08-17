using edu_tracking.Data;
using edu_tracking.Domain;
using edu_tracking.Domain.Identity;
using edu_tracking.Infrastructure;
using edu_tracking.Models;
using edu_tracking.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace edu_tracking.Services;

/// <summary>
/// The Users &gt; Parents screens. A parent is an <see cref="ApplicationUser"/> in the Parent
/// role plus a <see cref="Parent"/> profile, linked to students through <see cref="ParentStudent"/>.
/// Every child keeps exactly one primary contact, which this service maintains whenever
/// links change.
/// </summary>
public class ParentAdminService(ApplicationDbContext db, AccountAdminService accounts)
{
    /// <summary>Students that can be attached as children.</summary>
    public async Task<IReadOnlyList<StudentOption>> GetStudentOptionsAsync()
    {
        var students = await db.Students
            .Where(s => s.User.IsActive)
            .OrderBy(s => s.User.FullName)
            .Select(s => new { s.UserId, s.User.FullName, s.GradeLevel })
            .ToListAsync();

        return [.. students.Select(s => new StudentOption(s.UserId, s.FullName, Ui.GradeLabel(s.GradeLevel)))];
    }

    public async Task<ParentsPageViewModel> GetPageAsync(ParentFilter filter, string currentUserName)
    {
        var query = ApplyFilters(db.Parents.AsQueryable(), filter);

        var totalItems = await query.CountAsync();
        var currentPage = Math.Max(1, filter.Page);

        var parents = await query
            .OrderBy(p => p.User.FullName)
            .Skip((currentPage - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(p => new ParentRow
            {
                Id = p.UserId,
                Name = p.User.FullName,
                Email = p.User.Email,
                Phone = p.User.PhoneNumber,
                Children = p.Children
                    .Select(c => new ChildLink(c.Student.User.FullName, c.Relationship, c.IsPrimaryContact))
                    .ToList(),
                Status = !p.User.IsActive
                    ? AccountStatus.Inactive
                    : p.User.MustChangePassword ? AccountStatus.Pending : AccountStatus.Active,
                IsActive = p.User.IsActive,
                JoinedUtc = p.User.CreatedAtUtc
            })
            .ToListAsync();

        return new ParentsPageViewModel
        {
            CurrentUserName = currentUserName,
            Parents = parents,
            Stats = await GetStatsAsync(),
            Filter = filter,
            Page = new PageInfo { Current = currentPage, Size = filter.PageSize, TotalItems = totalItems }
        };
    }

    public async Task<ParentFormViewModel?> GetForEditAsync(Guid id)
    {
        var parent = await db.Parents
            .Where(p => p.UserId == id)
            .Select(p => new ParentFormViewModel
            {
                Id = p.UserId,
                FullName = p.User.FullName,
                UserName = p.User.UserName!,
                Email = p.User.Email,
                Phone = p.User.PhoneNumber,
                StudentIds = p.Children.Select(c => c.StudentId).ToList(),
                Relationship = p.Children.Select(c => c.Relationship).FirstOrDefault()!,
                IsPrimaryContact = p.Children.Any(c => c.IsPrimaryContact)
            })
            .FirstOrDefaultAsync();

        if (parent is not null)
        {
            if (string.IsNullOrEmpty(parent.Relationship))
            {
                parent.Relationship = ParentRelationship.Guardian;
            }

            parent.AvailableStudents = await GetStudentOptionsAsync();
        }

        return parent;
    }

    public Task<AccountResult> CreateAsync(ParentFormViewModel form)
    {
        var password = string.IsNullOrWhiteSpace(form.TemporaryPassword)
            ? AccountAdminService.GenerateTemporaryPassword()
            : form.TemporaryPassword;

        return accounts.InTransactionAsync(async () =>
        {
            var account = new NewAccount(form.FullName, form.UserName, form.Email, form.Phone);
            var (user, error) = await accounts.CreateUserAsync(account, AppRoles.Parent, password);

            if (user is null)
            {
                return AccountResult.Fail(error!);
            }

            db.Parents.Add(new Parent { UserId = user.Id });

            foreach (var studentId in form.StudentIds.Distinct())
            {
                db.ParentStudents.Add(new ParentStudent
                {
                    ParentId = user.Id,
                    StudentId = studentId,
                    Relationship = form.Relationship,
                    IsPrimaryContact = form.IsPrimaryContact
                });
            }

            await db.SaveChangesAsync();
            await SetPrimaryContactsAsync(form.StudentIds, user.Id, form.IsPrimaryContact);

            return AccountResult.Ok(password);
        });
    }

    public async Task<AccountResult> UpdateAsync(ParentFormViewModel form)
    {
        var parent = await db.Parents
            .Include(p => p.User)
            .Include(p => p.Children)
            .FirstOrDefaultAsync(p => p.UserId == form.Id);

        if (parent is null)
        {
            return AccountResult.Fail("That parent no longer exists.");
        }

        return await accounts.InTransactionAsync(async () =>
        {
            parent.User.FullName = form.FullName.Trim();
            parent.User.PhoneNumber = form.Phone;

            var emailError = await accounts.UpdateEmailAsync(parent.User, form.Email);
            if (emailError is not null)
            {
                return AccountResult.Fail(emailError);
            }

            // Children that lose this parent still need their contact of record rechecked.
            var affected = parent.Children.Select(c => c.StudentId).Union(form.StudentIds).ToList();
            SyncChildren(parent, form);

            await db.SaveChangesAsync();
            await SetPrimaryContactsAsync(affected, parent.UserId, form.IsPrimaryContact);

            return AccountResult.Ok();
        });
    }

    // ---------- helpers ----------

    private void SyncChildren(Parent parent, ParentFormViewModel form)
    {
        foreach (var link in parent.Children.Where(c => !form.StudentIds.Contains(c.StudentId)).ToList())
        {
            db.ParentStudents.Remove(link);
        }

        var kept = parent.Children.Where(c => form.StudentIds.Contains(c.StudentId)).ToList();
        foreach (var link in kept)
        {
            link.Relationship = form.Relationship;
            link.IsPrimaryContact = form.IsPrimaryContact;
        }

        var newStudentIds = form.StudentIds
            .Distinct()
            .Where(id => kept.All(c => c.StudentId != id));

        foreach (var studentId in newStudentIds)
        {
            db.ParentStudents.Add(new ParentStudent
            {
                ParentId = parent.UserId,
                StudentId = studentId,
                Relationship = form.Relationship,
                IsPrimaryContact = form.IsPrimaryContact
            });
        }
    }

    /// <summary>
    /// Leaves each affected child with exactly one primary contact. The parent being edited
    /// takes the role when <paramref name="isPrimary"/> is set; when they decline it, or drop
    /// the child, it passes to another guardian. A child's only guardian always holds it,
    /// because somebody has to be the contact of record.
    /// </summary>
    private async Task SetPrimaryContactsAsync(IEnumerable<Guid> studentIds, Guid parentId, bool isPrimary)
    {
        var ids = studentIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var links = await db.ParentStudents
            .Where(ps => ids.Contains(ps.StudentId))
            .OrderBy(ps => ps.ParentId)
            .ToListAsync();

        foreach (var guardians in links.GroupBy(ps => ps.StudentId))
        {
            var mine = guardians.FirstOrDefault(ps => ps.ParentId == parentId);
            var others = guardians.Where(ps => ps.ParentId != parentId).ToList();

            var contact = isPrimary && mine is not null
                ? mine
                : others.FirstOrDefault(ps => ps.IsPrimaryContact) ?? others.FirstOrDefault() ?? mine;

            foreach (var link in guardians)
            {
                link.IsPrimaryContact = link == contact;
            }
        }

        await db.SaveChangesAsync();
    }

    private static IQueryable<Parent> ApplyFilters(IQueryable<Parent> query, ParentFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(p =>
                p.User.FullName.Contains(search) ||
                p.User.Email!.Contains(search) ||
                p.User.UserName!.Contains(search) ||
                p.Children.Any(c => c.Student.User.FullName.Contains(search)));
        }

        if (filter.WithoutChildren == true)
        {
            query = query.Where(p => !p.Children.Any());
        }

        return filter.Status switch
        {
            AccountStatus.Active => query.Where(p => p.User.IsActive && !p.User.MustChangePassword),
            AccountStatus.Pending => query.Where(p => p.User.IsActive && p.User.MustChangePassword),
            AccountStatus.Inactive => query.Where(p => !p.User.IsActive),
            _ => query
        };
    }

    private async Task<IReadOnlyList<StatCard>> GetStatsAsync()
    {
        var total = await db.Parents.CountAsync();
        var active = await db.Parents.CountAsync(p => p.User.IsActive);
        var pending = await db.Parents.CountAsync(p => p.User.IsActive && p.User.MustChangePassword);
        var withoutChildren = await db.Parents.CountAsync(p => !p.Children.Any());

        return
        [
            new StatCard("Total Parents", total.ToString(), null, "bi-people", "purple"),
            new StatCard("Active", active.ToString(), null, "bi-check2-circle", "green"),
            new StatCard("Awaiting First Sign-in", pending.ToString(), null, "bi-hourglass-split", "orange"),
            new StatCard("No Child Linked", withoutChildren.ToString(), null, "bi-person-dash", "red")
        ];
    }
}
