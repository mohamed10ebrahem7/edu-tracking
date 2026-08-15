using edu_tracking.Domain;
using edu_tracking.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace edu_tracking.Data;

public static class DbInitializer
{
    /// <summary>
    /// Seeds the four roles, a first admin, and the starter subject list. Safe to run on
    /// every start; skips quietly when migrations have not been applied yet.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        try
        {
            if ((await db.Database.GetPendingMigrationsAsync()).Any())
            {
                logger.LogWarning("Skipping seed: database has pending migrations. Run 'dotnet ef database update'.");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Skipping seed: could not reach the database.");
            return;
        }

        await SeedRolesAsync(services);
        await SeedAdminAsync(services, logger);
        await SeedSubjectsAsync(db);
    }

    private static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }
        }
    }

    private static async Task SeedAdminAsync(IServiceProvider services, ILogger logger)
    {
        var config = services.GetRequiredService<IConfiguration>().GetSection("SeedAdmin");
        var userName = config["UserName"];
        var password = config["Password"];

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("No SeedAdmin configured; skipping admin seed.");
            return;
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByNameAsync(userName) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = userName,
            Email = config["Email"],
            EmailConfirmed = true,
            FullName = config["FullName"] ?? "System Administrator",
            CreatedAtUtc = DateTime.UtcNow,
            MustChangePassword = true
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to seed admin: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        logger.LogInformation("Seeded admin user '{UserName}'.", userName);
    }

    private static async Task SeedSubjectsAsync(ApplicationDbContext db)
    {
        if (await db.Subjects.AnyAsync())
        {
            return;
        }

        db.Subjects.AddRange(
            new Subject { Name = "Mathematics", Code = "MATH" },
            new Subject { Name = "Arabic", Code = "AR" },
            new Subject { Name = "English", Code = "EN" },
            new Subject { Name = "Science", Code = "SCI" },
            new Subject { Name = "Physics", Code = "PHY" },
            new Subject { Name = "Chemistry", Code = "CHEM" },
            new Subject { Name = "Biology", Code = "BIO" });

        await db.SaveChangesAsync();
    }
}
