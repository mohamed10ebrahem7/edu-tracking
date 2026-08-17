using edu_tracking.Data;
using edu_tracking.Domain.Identity;
using edu_tracking.Models;
using edu_tracking.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace edu_tracking.Services;

/// <summary>
/// The Users &gt; Admins screens. Unlike teachers and students an admin has no profile
/// table, so this works straight on the accounts in the Admin role. It also guards
/// against an admin locking everyone, or themselves, out of the panel.
/// </summary>
public class AdminUserService(ApplicationDbContext db, AccountAdminService accounts)
{
    public async Task<AdminsPageViewModel> GetPageAsync(AdminFilter filter, Guid currentUserId, string currentUserName)
    {
        var query = ApplyFilters(AdminUsers(), filter);

        var totalItems = await query.CountAsync();
        var currentPage = Math.Max(1, filter.Page);

        var admins = await query
            .OrderBy(u => u.FullName)
            .Skip((currentPage - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(u => new AdminRow
            {
                Id = u.Id,
                Name = u.FullName,
                UserName = u.UserName!,
                Email = u.Email,
                Phone = u.PhoneNumber,
                Status = !u.IsActive
                    ? AccountStatus.Inactive
                    : u.MustChangePassword ? AccountStatus.Pending : AccountStatus.Active,
                IsActive = u.IsActive,
                IsCurrentUser = u.Id == currentUserId,
                JoinedUtc = u.CreatedAtUtc
            })
            .ToListAsync();

        return new AdminsPageViewModel
        {
            CurrentUserName = currentUserName,
            Admins = admins,
            Stats = await GetStatsAsync(),
            ActiveAdmins = await AdminUsers().CountAsync(u => u.IsActive),
            Filter = filter,
            Page = new PageInfo { Current = currentPage, Size = filter.PageSize, TotalItems = totalItems }
        };
    }

    public Task<AdminFormViewModel?> GetForEditAsync(Guid id) =>
        AdminUsers()
            .Where(u => u.Id == id)
            .Select(u => new AdminFormViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                UserName = u.UserName!,
                Email = u.Email,
                Phone = u.PhoneNumber
            })
            .FirstOrDefaultAsync();

    public Task<AccountResult> CreateAsync(AdminFormViewModel form)
    {
        var password = string.IsNullOrWhiteSpace(form.TemporaryPassword)
            ? AccountAdminService.GenerateTemporaryPassword()
            : form.TemporaryPassword;

        // No profile row to add, but the transaction keeps the account and its role
        // assignment from landing separately.
        return accounts.InTransactionAsync(async () =>
        {
            var account = new NewAccount(form.FullName, form.UserName, form.Email, form.Phone);
            var (user, error) = await accounts.CreateUserAsync(account, AppRoles.Admin, password);

            return user is null ? AccountResult.Fail(error!) : AccountResult.Ok(password);
        });
    }

    public async Task<AccountResult> UpdateAsync(AdminFormViewModel form)
    {
        var user = await AdminUsers().FirstOrDefaultAsync(u => u.Id == form.Id);
        if (user is null)
        {
            return AccountResult.Fail("That admin no longer exists.");
        }

        user.FullName = form.FullName.Trim();
        user.PhoneNumber = form.Phone;

        var emailError = await accounts.UpdateEmailAsync(user, form.Email);
        if (emailError is not null)
        {
            return AccountResult.Fail(emailError);
        }

        await db.SaveChangesAsync();
        return AccountResult.Ok();
    }

    /// <summary>Refuses any change that would leave nobody able to sign in to the panel.</summary>
    public async Task<AccountResult> SetActiveAsync(Guid id, bool isActive, Guid currentUserId)
    {
        if (!isActive)
        {
            if (id == currentUserId)
            {
                return AccountResult.Fail("You cannot deactivate your own account.");
            }

            var othersRemain = await AdminUsers().AnyAsync(u => u.IsActive && u.Id != id);
            if (!othersRemain)
            {
                return AccountResult.Fail("At least one admin must stay active.");
            }
        }

        return await accounts.SetActiveAsync(id, isActive);
    }

    /// <summary>
    /// Resetting your own password would hand you a temporary one and end your session,
    /// so it is only allowed for other admins.
    /// </summary>
    public Task<AccountResult> ResetPasswordAsync(Guid id, Guid currentUserId) =>
        id == currentUserId
            ? Task.FromResult(AccountResult.Fail("You cannot reset your own password here."))
            : accounts.ResetPasswordAsync(id);

    // ---------- helpers ----------

    /// <summary>Accounts in the Admin role. There is no Admins table to query.</summary>
    private IQueryable<ApplicationUser> AdminUsers() =>
        from user in db.Users
        join userRole in db.UserRoles on user.Id equals userRole.UserId
        join role in db.Roles on userRole.RoleId equals role.Id
        where role.Name == AppRoles.Admin
        select user;

    private static IQueryable<ApplicationUser> ApplyFilters(IQueryable<ApplicationUser> query, AdminFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(u =>
                u.FullName.Contains(search) ||
                u.Email!.Contains(search) ||
                u.UserName!.Contains(search));
        }

        return filter.Status switch
        {
            AccountStatus.Active => query.Where(u => u.IsActive && !u.MustChangePassword),
            AccountStatus.Pending => query.Where(u => u.IsActive && u.MustChangePassword),
            AccountStatus.Inactive => query.Where(u => !u.IsActive),
            _ => query
        };
    }

    private async Task<IReadOnlyList<StatCard>> GetStatsAsync()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var total = await AdminUsers().CountAsync();
        var active = await AdminUsers().CountAsync(u => u.IsActive);
        var pending = await AdminUsers().CountAsync(u => u.IsActive && u.MustChangePassword);
        var addedThisMonth = await AdminUsers().CountAsync(u => u.CreatedAtUtc >= monthStart);

        return
        [
            new StatCard("Total Admins", total.ToString(), null, "bi-shield-lock", "purple"),
            new StatCard("Active", active.ToString(), null, "bi-check2-circle", "green"),
            new StatCard("Awaiting First Sign-in", pending.ToString(), null, "bi-hourglass-split", "orange"),
            new StatCard("Added This Month", addedThisMonth.ToString(), null, "bi-person-plus", "blue")
        ];
    }
}
