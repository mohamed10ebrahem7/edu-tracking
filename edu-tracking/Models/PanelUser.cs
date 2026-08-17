namespace edu_tracking.Models;

/// <summary>
/// Who the panel topbar greets. <paramref name="Subtitle"/> is the small line under the
/// name — a teacher's subjects — and stays null on panels with nothing to add there.
/// </summary>
public record PanelUser(string Name, string? Subtitle = null);
