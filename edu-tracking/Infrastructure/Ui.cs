using edu_tracking.Domain;

namespace edu_tracking.Infrastructure;

/// <summary>
/// Small presentation helpers shared by the panel views. Tone keys map to the
/// <c>tone-*</c> classes in panel.css.
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

    /// <summary>Grade1..Grade12 are numbered 11..22 in the enum, hence the offset.</summary>
    public static string GradeLabel(GradeLevel grade) => grade switch
    {
        GradeLevel.Kg1 => "KG 1",
        GradeLevel.Kg2 => "KG 2",
        _ => $"Grade {(int)grade - 10}"
    };

    public static string StatusTone(string status) => status switch
    {
        "Active" => "green",
        "Pending" => "orange",
        "Suspended" => "red",
        _ => "gray"
    };
}
