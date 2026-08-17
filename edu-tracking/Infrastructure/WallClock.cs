namespace edu_tracking.Infrastructure;

/// <summary>
/// Wall-clock conversions for one teacher's time zone. Working hours and class schedules
/// are stored as local times, so every date converts on its own: "Sunday 5pm" is a
/// different instant either side of a daylight-saving shift, and on a spring-forward date
/// it may not exist at all.
/// </summary>
public class WallClock
{
    private readonly TimeZoneInfo zone;

    public WallClock(string? timeZoneId) => zone = Resolve(timeZoneId);

    public string ZoneName => zone.Id;

    public DateOnly Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone));

    /// <summary>False on the hour a spring-forward skips.</summary>
    public bool Exists(DateOnly date, TimeOnly time) => !zone.IsInvalidTime(date.ToDateTime(time));

    public DateTime ToUtc(DateOnly date, TimeOnly time) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified), zone);

    /// <summary>Midnight itself can be a skipped hour, so day bounds step forward to a real one.</summary>
    public DateTime StartOfDayUtc(DateOnly date)
    {
        var time = TimeOnly.MinValue;
        while (!Exists(date, time))
        {
            time = time.AddHours(1);
        }

        return ToUtc(date, time);
    }

    public (DateOnly Date, TimeOnly Time) ToLocal(DateTime utc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone);
        return (DateOnly.FromDateTime(local), TimeOnly.FromDateTime(local));
    }

    /// <summary>An unknown or missing id must not take a screen down, so it falls back to UTC.</summary>
    private static TimeZoneInfo Resolve(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
