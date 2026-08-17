namespace edu_tracking.Domain;

/// <summary>
/// One weekday meeting in a group's week. Times are the teacher's LOCAL wall-clock times,
/// the same convention as <see cref="TeacherWeeklyAvailability"/>: "Sunday 5pm" stays 5pm
/// across a daylight-saving shift, so the conversion to UTC happens per date when meetings
/// are generated.
/// </summary>
public class ClassGroupSchedule
{
    public int Id { get; set; }

    public int ClassGroupId { get; set; }
    public ClassGroup ClassGroup { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public ICollection<TeacherSlot> GeneratedSlots { get; set; } = [];

    public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;
}
