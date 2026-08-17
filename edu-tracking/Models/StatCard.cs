namespace edu_tracking.Models;

/// <summary>
/// A headline figure at the top of a panel screen, rendered by
/// <c>Views/Shared/Panel/_StatCards.cshtml</c>.
/// </summary>
/// <param name="Tone">A colour key: purple, green, blue, orange, red or gray.</param>
/// <param name="Trend">Optional; omitted when there is no comparison figure to show.</param>
public record StatCard(string Label, string Value, string? Trend, string Icon, string Tone);
