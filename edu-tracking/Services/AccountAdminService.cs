using System.Security.Cryptography;
using edu_tracking.Data;
using edu_tracking.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace edu_tracking.Services;

public record AccountResult(bool Succeeded, string? Error = null, string? TemporaryPassword = null)
{
    public static AccountResult Ok(string? temporaryPassword = null) => new(true, null, temporaryPassword);
    public static AccountResult Fail(string error) => new(false, error);
}

public record NewAccount(string FullName, string UserName, string? Email, string? Phone);

/// <summary>
/// The account half of every "manage users" screen. Each role also has a profile row
/// (Teacher, Student, ...), which its own service adds inside <see cref="InTransactionAsync"/>
/// so the login and the profile are always saved together.
/// </summary>
public class AccountAdminService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
{
    /// <summary>
    /// Runs <paramref name="action"/> in a transaction, committing only when it succeeds.
    /// The context is configured with EnableRetryOnFailure, which requires a manual
    /// transaction to sit inside the execution strategy so the whole unit can be retried.
    /// </summary>
    public Task<AccountResult> InTransactionAsync(Func<Task<AccountResult>> action)
    {
        var strategy = db.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            var result = await action();
            if (result.Succeeded)
            {
                await transaction.CommitAsync();
            }

            return result;
        });
    }

    /// <summary>Creates the login and puts it in <paramref name="role"/>.</summary>
    public async Task<(ApplicationUser? User, string? Error)> CreateUserAsync(NewAccount account, string role, string password)
    {
        var user = new ApplicationUser
        {
            UserName = account.UserName.Trim(),
            Email = Clean(account.Email),
            EmailConfirmed = true,
            PhoneNumber = account.Phone,
            FullName = account.FullName.Trim(),
            IsActive = true,
            MustChangePassword = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return (null, Describe(created));
        }

        var roleAdded = await userManager.AddToRoleAsync(user, role);
        return roleAdded.Succeeded ? (user, null) : (null, Describe(roleAdded));
    }

    /// <summary>Goes through UserManager so the normalised email stays in sync.</summary>
    public async Task<string?> UpdateEmailAsync(ApplicationUser user, string? email)
    {
        var newEmail = Clean(email);
        if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var changed = await userManager.SetEmailAsync(user, newEmail);
        return changed.Succeeded ? null : Describe(changed);
    }

    public async Task<AccountResult> SetActiveAsync(Guid id, bool isActive)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return AccountResult.Fail("That user no longer exists.");
        }

        user.IsActive = isActive;
        await db.SaveChangesAsync();

        // Invalidates any cookie the user is still holding.
        await userManager.UpdateSecurityStampAsync(user);

        return AccountResult.Ok();
    }

    public async Task<AccountResult> ResetPasswordAsync(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return AccountResult.Fail("That user no longer exists.");
        }

        var password = GenerateTemporaryPassword();
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(user, token, password);

        if (!reset.Succeeded)
        {
            return AccountResult.Fail(Describe(reset));
        }

        user.MustChangePassword = true;
        await db.SaveChangesAsync();

        return AccountResult.Ok(password);
    }

    public Task<string?> GetNameAsync(Guid id) =>
        db.Users.Where(u => u.Id == id).Select(u => u.FullName).FirstOrDefaultAsync();

    public static string GenerateTemporaryPassword()
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

    /// <summary>Sunday-to-Saturday window used by the "this week" columns.</summary>
    public static (DateTime Start, DateTime End) CurrentWeekUtc()
    {
        var today = DateTime.UtcNow.Date;
        var start = today.AddDays(-(int)today.DayOfWeek);
        return (start, start.AddDays(7));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Describe(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Description));
}
