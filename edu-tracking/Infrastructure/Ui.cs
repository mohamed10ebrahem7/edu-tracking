namespace edu_tracking.Infrastructure;

/// <summary>
/// Small presentation helpers shared by the admin views. Tone keys map to the
/// <c>tone-*</c> classes in admin.css.
/// </summary>
public static class Ui
{
    public static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return "?";
        }

        return parts.Length >= 2
            ? $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}"
            : parts[0][..Math.Min(2, parts[0].Length)].ToUpper();
    }

    public static string RoleTone(string role) => role switch
    {
        "Student" => "purple",
        "Teacher" => "green",
        "Parent" => "orange",
        "Admin" => "blue",
        _ => "gray"
    };

    public static string StatusTone(string status) => status switch
    {
        "Active" => "green",
        "Pending" => "orange",
        "Suspended" => "red",
        _ => "gray"
    };
}
