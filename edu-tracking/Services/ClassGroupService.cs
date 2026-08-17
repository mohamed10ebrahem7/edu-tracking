using edu_tracking.Data;
using edu_tracking.Domain;
using edu_tracking.Infrastructure;
using edu_tracking.Models.Teaching;
using Microsoft.EntityFrameworkCore;

namespace edu_tracking.Services;

public record GroupResult(bool Succeeded, string? Error = null, int GroupId = 0, string? Notice = null)
{
    public static GroupResult Fail(string error) => new(false, error);
    public static GroupResult Ok(int groupId = 0, string? notice = null) => new(true, null, groupId, notice);
}

/// <summary>
/// Everything that writes a class: creating it, re-stamping it, and moving students in and
/// out. A class is the unit a teacher schedules; each date it falls on becomes one
/// <see cref="TeacherSlot"/> plus one <see cref="Session"/>, which is what attendance and
/// reports later hang off.
/// </summary>
public class ClassGroupService(ApplicationDbContext db, AccountAdminService accounts)
{
    /// <summary>A year of weekly meetings is plenty; beyond it the teacher is likely mistaken.</summary>
    private const int MaxDateRangeDays = 400;
    private const int MaxOccurrences = 500;

    public async Task<IReadOnlyList<SubjectChoice>> GetSubjectOptionsAsync(Guid teacherId) =>
        await db.TeacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .OrderBy(ts => ts.Subject.Name)
            .Select(ts => new SubjectChoice(ts.SubjectId, ts.Subject.Name))
            .ToListAsync();

    /// <summary>Prefilled from the free cell the teacher clicked, when they came that way.</summary>
    public async Task<ClassGroupFormViewModel> NewFormAsync(
        Guid teacherId,
        DateOnly? date,
        TimeOnly? start,
        TimeOnly? end,
        string currentUserName)
    {
        var clock = await ClockAsync(teacherId);
        var today = clock.Today();
        var first = date ?? today;

        var form = new ClassGroupFormViewModel
        {
            StartDate = first,
            EndDate = first.AddDays(27),
            Days = ClassGroupFormViewModel.EmptyWeek()
        };

        if (date is not null && start is not null && end is not null)
        {
            var day = form.Days.First(d => d.DayOfWeek == date.Value.DayOfWeek);
            day.Selected = true;
            day.StartTime = start;
            day.EndTime = end;
        }

        return await FillChoicesAsync(teacherId, form, currentUserName);
    }

    public async Task<ClassGroupFormViewModel?> GetForEditAsync(Guid teacherId, int id, string currentUserName)
    {
        var group = await db.ClassGroups
            .AsNoTracking()
            .Where(g => g.Id == id && g.TeacherId == teacherId)
            .Select(g => new
            {
                g.Id,
                g.SubjectId,
                g.GradeLevel,
                g.Kind,
                g.Title,
                g.MaxStudents,
                g.MembersCount,
                g.StartDate,
                g.EndDate,
                g.RepeatsWeekly,
                g.IsOpenForEnrollment,
                Schedule = g.Schedule.Select(s => new { s.DayOfWeek, s.StartTime, s.EndTime }).ToList()
            })
            .FirstOrDefaultAsync();

        if (group is null)
        {
            return null;
        }

        var form = new ClassGroupFormViewModel
        {
            Id = group.Id,
            SubjectId = group.SubjectId,
            GradeLevel = group.GradeLevel,
            Kind = group.Kind,
            Title = group.Title,
            MaxStudents = group.MaxStudents,
            StartDate = group.StartDate,
            EndDate = group.EndDate,
            RepeatsWeekly = group.RepeatsWeekly,
            IsOpenForEnrollment = group.IsOpenForEnrollment,
            HasMembers = group.MembersCount > 0,
            Days = ClassGroupFormViewModel.EmptyWeek()
        };

        foreach (var row in group.Schedule)
        {
            var day = form.Days.First(d => d.DayOfWeek == row.DayOfWeek);
            day.Selected = true;
            day.StartTime = row.StartTime;
            day.EndTime = row.EndTime;
        }

        return await FillChoicesAsync(teacherId, form, currentUserName);
    }

    public async Task<ClassGroupFormViewModel> FillChoicesAsync(
        Guid teacherId,
        ClassGroupFormViewModel form,
        string currentUserName)
    {
        if (form.Days.Count == 0)
        {
            form.Days = ClassGroupFormViewModel.EmptyWeek();
        }

        form.CurrentUserName = currentUserName;
        form.AvailableSubjects = await GetSubjectOptionsAsync(teacherId);

        // The topbar line and the picker are the same list, so one query answers both.
        form.Subjects = form.AvailableSubjects.Count == 0
            ? null
            : string.Join(", ", form.AvailableSubjects.Select(s => s.Name));

        form.WorkingHours = await db.TeacherAvailabilities
            .Where(a => a.TeacherId == teacherId && a.IsActive)
            .OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime)
            .Select(a => $"{a.DayOfWeek} {a.StartTime:HH\\:mm} - {a.EndTime:HH\\:mm}")
            .ToListAsync();

        return form;
    }

    public async Task<GroupResult> CreateAsync(Guid teacherId, ClassGroupFormViewModel form)
    {
        var teacher = await LoadTeacherAsync(teacherId, form.SubjectId);
        if (teacher is null)
        {
            return GroupResult.Fail("Your teacher profile is missing. Ask an admin to check your account.");
        }

        var days = Ticked(form);
        if (Validate(form, days) is { } invalid)
        {
            return GroupResult.Fail(invalid);
        }

        if (!teacher.TeachesSubject)
        {
            return GroupResult.Fail("Pick one of the subjects you are assigned to teach.");
        }

        if (await OutsideWorkingHoursAsync(teacherId, form, days) is { } outside)
        {
            return GroupResult.Fail(outside);
        }

        var plan = await PlanAsync(teacherId, teacher.Clock, form, days, excludeGroupId: null);
        if (plan.Error is not null)
        {
            return GroupResult.Fail(plan.Error);
        }

        return await accounts.InTransactionAsync(async () =>
        {
            var group = new ClassGroup
            {
                TeacherId = teacherId,
                SubjectId = form.SubjectId,
                GradeLevel = form.GradeLevel,
                Kind = form.Kind,
                Title = Clean(form.Title),
                MaxStudents = form.MaxStudents,
                StartDate = form.StartDate,
                EndDate = form.EndDate,
                RepeatsWeekly = form.RepeatsWeekly,
                IsOpenForEnrollment = form.IsOpenForEnrollment
            };

            db.ClassGroups.Add(group);
            await db.SaveChangesAsync();

            var schedule = AddSchedule(group.Id, days);
            await db.SaveChangesAsync();

            Materialise(group, schedule, plan.Occurrences, teacher.HourlyRate);
            await db.SaveChangesAsync();

            return GroupResult.Ok(group.Id, Describe(plan));
        }, r => r.Succeeded);
    }

    /// <summary>
    /// A class nobody has joined can be reshaped freely, so its meetings are thrown away
    /// and generated again. Once a student is in, only the label, the size and whether it
    /// is still open may change: moving a class out from under its members is never right.
    /// </summary>
    public async Task<GroupResult> UpdateAsync(Guid teacherId, ClassGroupFormViewModel form)
    {
        var group = await db.ClassGroups.FirstOrDefaultAsync(g => g.Id == form.Id && g.TeacherId == teacherId);
        if (group is null)
        {
            return GroupResult.Fail("That class no longer exists.");
        }

        var teacher = await LoadTeacherAsync(teacherId, form.SubjectId);
        if (teacher is null)
        {
            return GroupResult.Fail("Your teacher profile is missing. Ask an admin to check your account.");
        }

        var days = Ticked(form);
        if (Validate(form, days) is { } invalid)
        {
            return GroupResult.Fail(invalid);
        }

        if (form.MaxStudents < group.MembersCount)
        {
            return GroupResult.Fail($"{group.MembersCount} students have already joined, so the limit cannot go below that.");
        }

        var attended = await db.SessionParticipants.AnyAsync(p => p.Session.ClassGroupId == group.Id);
        var locked = group.MembersCount > 0 || attended;

        if (locked)
        {
            if (await ScheduleChangedAsync(group, form, days))
            {
                return GroupResult.Fail(
                    "Students have already joined this class, so its subject, grade and times are fixed. " +
                    "Create a new class for a different schedule.");
            }

            ApplyOpenFields(group, form);
            await db.SaveChangesAsync();

            return GroupResult.Ok(group.Id, "Class updated.");
        }

        if (!teacher.TeachesSubject)
        {
            return GroupResult.Fail("Pick one of the subjects you are assigned to teach.");
        }

        if (await OutsideWorkingHoursAsync(teacherId, form, days) is { } outside)
        {
            return GroupResult.Fail(outside);
        }

        var plan = await PlanAsync(teacherId, teacher.Clock, form, days, excludeGroupId: group.Id);
        if (plan.Error is not null)
        {
            return GroupResult.Fail(plan.Error);
        }

        return await accounts.InTransactionAsync(async () =>
        {
            // Sessions first: a slot may not be deleted while one still points at it.
            await db.Sessions.Where(s => s.ClassGroupId == group.Id).ExecuteDeleteAsync();
            await db.TeacherSlots.Where(s => s.ClassGroupSchedule!.ClassGroupId == group.Id).ExecuteDeleteAsync();
            await db.ClassGroupSchedules.Where(s => s.ClassGroupId == group.Id).ExecuteDeleteAsync();

            group.SubjectId = form.SubjectId;
            group.GradeLevel = form.GradeLevel;
            group.Kind = form.Kind;
            group.StartDate = form.StartDate;
            group.EndDate = form.EndDate;
            group.RepeatsWeekly = form.RepeatsWeekly;
            ApplyOpenFields(group, form);

            var schedule = AddSchedule(group.Id, days);
            await db.SaveChangesAsync();

            Materialise(group, schedule, plan.Occurrences, teacher.HourlyRate);
            await db.SaveChangesAsync();

            return GroupResult.Ok(group.Id, Describe(plan));
        }, r => r.Succeeded);
    }

    public async Task<GroupDetailViewModel?> GetDetailAsync(Guid teacherId, int id, string currentUserName)
    {
        var group = await db.ClassGroups
            .AsNoTracking()
            .Where(g => g.Id == id && g.TeacherId == teacherId)
            .Select(g => new
            {
                g.Id,
                Subject = g.Subject.Name,
                g.Title,
                g.GradeLevel,
                g.Kind,
                g.MembersCount,
                g.MaxStudents,
                g.StartDate,
                g.EndDate,
                g.RepeatsWeekly,
                g.IsOpenForEnrollment,
                Schedule = g.Schedule
                    .OrderBy(s => s.DayOfWeek)
                    .Select(s => $"{s.DayOfWeek} {s.StartTime:HH\\:mm} - {s.EndTime:HH\\:mm}")
                    .ToList(),
                Members = g.Members
                    .OrderBy(m => m.Student.User.FullName)
                    .Select(m => new GroupMemberRow(
                        m.StudentId, m.Student.User.FullName, m.Student.GradeLevel, m.Status, m.JoinedAtUtc))
                    .ToList(),
                Requests = g.JoinRequests
                    .Where(r => r.Status == BookingRequestStatus.Pending)
                    .OrderBy(r => r.RequestedAtUtc)
                    .Select(r => new JoinRequestRow(
                        r.Id, r.Student.User.FullName, r.Student.GradeLevel, r.RequestedAtUtc, r.StudentMessage))
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (group is null)
        {
            return null;
        }

        var clock = await ClockAsync(teacherId);

        var occurrences = await db.Sessions
            .AsNoTracking()
            .Where(s => s.ClassGroupId == id)
            .OrderBy(s => s.StartUtc)
            .Select(s => new
            {
                s.SlotId,
                s.StartUtc,
                s.EndUtc,
                s.Slot.Status,
                HasAttendance = s.Participants.Any()
            })
            .ToListAsync();

        return new GroupDetailViewModel
        {
            CurrentUserName = currentUserName,
            Subjects = await SubjectLineAsync(teacherId),
            Id = group.Id,
            Subject = group.Subject,
            Title = group.Title,
            GradeLevel = group.GradeLevel,
            Kind = group.Kind,
            Members = group.MembersCount,
            MaxStudents = group.MaxStudents,
            StartDate = group.StartDate,
            EndDate = group.EndDate,
            RepeatsWeekly = group.RepeatsWeekly,
            IsOpenForEnrollment = group.IsOpenForEnrollment,
            Schedule = group.Schedule,
            Roster = group.Members,
            PendingRequests = group.Requests,
            Occurrences = [.. occurrences.Select(o =>
            {
                var (date, start) = clock.ToLocal(o.StartUtc);
                var (_, end) = clock.ToLocal(o.EndUtc);
                return new OccurrenceRow(o.SlotId, date, start, end, o.Status, o.HasAttendance);
            })]
        };
    }

    // ---------- membership ----------

    public Task<GroupResult> ApproveRequestAsync(Guid teacherId, long requestId, Guid actingUserId) =>
        accounts.InTransactionAsync(async () =>
        {
            var request = await db.BookingRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.TeacherId == teacherId);

            if (request is null)
            {
                return GroupResult.Fail("That request no longer exists.");
            }

            if (request.Status != BookingRequestStatus.Pending)
            {
                return GroupResult.Fail("That request has already been decided.");
            }

            var member = await db.ClassGroupMembers
                .FirstOrDefaultAsync(m => m.ClassGroupId == request.ClassGroupId && m.StudentId == request.StudentId);

            var now = DateTime.UtcNow;

            if (member?.Status != MemberStatus.Active)
            {
                if (!await ClaimPlaceAsync(request.ClassGroupId))
                {
                    return GroupResult.Fail("The class filled up before this request could be approved.");
                }

                if (member is null)
                {
                    db.ClassGroupMembers.Add(new ClassGroupMember
                    {
                        ClassGroupId = request.ClassGroupId,
                        StudentId = request.StudentId,
                        Status = MemberStatus.Active,
                        JoinedAtUtc = now
                    });
                }
                else
                {
                    member.Status = MemberStatus.Active;
                    member.JoinedAtUtc = now;
                    member.EndedAtUtc = null;
                    member.EndedByUserId = null;
                }
            }

            request.Status = BookingRequestStatus.Approved;
            request.DecidedByUserId = actingUserId;
            request.DecidedAtUtc = now;

            await db.SaveChangesAsync();

            return GroupResult.Ok(request.ClassGroupId, "Request approved.");
        }, r => r.Succeeded);

    public async Task<GroupResult> DeclineRequestAsync(Guid teacherId, long requestId, string? message, Guid actingUserId)
    {
        var request = await db.BookingRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.TeacherId == teacherId);

        if (request is null)
        {
            return GroupResult.Fail("That request no longer exists.");
        }

        if (request.Status != BookingRequestStatus.Pending)
        {
            return GroupResult.Fail("That request has already been decided.");
        }

        request.Status = BookingRequestStatus.Declined;
        request.DeclineMessageToStudent = Clean(message);
        request.DecidedByUserId = actingUserId;
        request.DecidedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return GroupResult.Ok(request.ClassGroupId, "Request declined.");
    }

    public Task<GroupResult> RemoveMemberAsync(Guid teacherId, int groupId, Guid studentId, Guid actingUserId) =>
        accounts.InTransactionAsync(async () =>
        {
            var member = await db.ClassGroupMembers
                .FirstOrDefaultAsync(m => m.ClassGroupId == groupId
                    && m.StudentId == studentId
                    && m.ClassGroup.TeacherId == teacherId);

            if (member is null)
            {
                return GroupResult.Fail("That student is not in this class.");
            }

            if (member.Status != MemberStatus.Active)
            {
                return GroupResult.Fail("That student has already left this class.");
            }

            member.Status = MemberStatus.Removed;
            member.EndedAtUtc = DateTime.UtcNow;
            member.EndedByUserId = actingUserId;

            await db.SaveChangesAsync();
            await ReleasePlaceAsync(groupId);

            return GroupResult.Ok(groupId, "Student removed from the class.");
        }, r => r.Succeeded);

    /// <summary>
    /// Closes one meeting: the block turns grey and the class loses that date. Refused once
    /// anyone has joined, because a member is owed every meeting they signed up for.
    /// </summary>
    public async Task<GroupResult> CloseOccurrenceAsync(Guid teacherId, long slotId, byte[]? rowVersion)
    {
        var slot = await db.TeacherSlots
            .Include(s => s.Session)
            .FirstOrDefaultAsync(s => s.Id == slotId && s.TeacherId == teacherId);

        if (slot is null)
        {
            return GroupResult.Fail("That block no longer exists.");
        }

        if (slot.Status == SlotStatus.Unavailable)
        {
            return GroupResult.Fail("That block is already closed.");
        }

        var groupId = 0;

        if (slot.Session is { } session)
        {
            groupId = session.ClassGroupId;

            var members = await db.ClassGroups
                .Where(g => g.Id == groupId)
                .Select(g => g.MembersCount)
                .FirstAsync();

            if (members > 0)
            {
                return GroupResult.Fail("Students have joined this class, so this meeting cannot be closed.");
            }

            if (await db.SessionParticipants.AnyAsync(p => p.SessionId == session.Id))
            {
                return GroupResult.Fail("Attendance has already been recorded for this meeting.");
            }

            db.Sessions.Remove(session);
        }

        slot.Status = SlotStatus.Unavailable;
        slot.Origin = SlotOrigin.Manual;
        slot.ClassGroupScheduleId = null;
        slot.Note = "Closed by teacher";

        if (rowVersion is { Length: > 0 })
        {
            db.Entry(slot).Property(s => s.RowVersion).OriginalValue = rowVersion;
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return GroupResult.Fail("Someone changed this block while the page was open. Reload and try again.");
        }

        return GroupResult.Ok(groupId, "That meeting is now closed.");
    }

    // ---------- generation ----------

    private record DayInput(DayOfWeek Day, TimeOnly Start, TimeOnly End);

    private record PlannedOccurrence(DateOnly Date, TimeOnly LocalStart, DateTime StartUtc, DateTime EndUtc);

    private record OccurrencePlan(
        List<PlannedOccurrence> Occurrences,
        int SkippedTimeOff,
        int SkippedMissingHour,
        string? Error = null);

    private static List<DayInput> Ticked(ClassGroupFormViewModel form) =>
        [.. form.Days
            .Where(d => d.Selected && d.StartTime is not null && d.EndTime is not null)
            .Select(d => new DayInput(d.DayOfWeek, d.StartTime!.Value, d.EndTime!.Value))];

    private static string? Validate(ClassGroupFormViewModel form, List<DayInput> days)
    {
        if (days.Count == 0)
        {
            return "Tick at least one day, and give it a start and an end time.";
        }

        if (days.FirstOrDefault(d => d.End <= d.Start) is { } backwards)
        {
            return $"{backwards.Day} ends before it starts.";
        }

        if (form.EndDate < form.StartDate)
        {
            return "The last week cannot end before the first week starts.";
        }

        if (form.EndDate.DayNumber - form.StartDate.DayNumber > MaxDateRangeDays)
        {
            return "Keep the date range within about a year.";
        }

        return null;
    }

    private async Task<string?> OutsideWorkingHoursAsync(Guid teacherId, ClassGroupFormViewModel form, List<DayInput> days)
    {
        var windows = await db.TeacherAvailabilities
            .Where(a => a.TeacherId == teacherId
                && a.IsActive
                && a.EffectiveFrom <= form.EndDate
                && (a.EffectiveTo == null || a.EffectiveTo >= form.StartDate))
            .Select(a => new { a.DayOfWeek, a.StartTime, a.EndTime })
            .ToListAsync();

        if (windows.Count == 0)
        {
            return "You have no working hours yet. Set them under Availability, then create the class.";
        }

        foreach (var day in days)
        {
            var inside = windows.Any(w => w.DayOfWeek == day.Day && w.StartTime <= day.Start && w.EndTime >= day.End);
            if (!inside)
            {
                return $"{day.Day} {day.Start:HH\\:mm}-{day.End:HH\\:mm} is outside your working hours. " +
                       "Widen them under Availability, or move the class.";
            }
        }

        return null;
    }

    private async Task<OccurrencePlan> PlanAsync(
        Guid teacherId,
        WallClock clock,
        ClassGroupFormViewModel form,
        List<DayInput> days,
        int? excludeGroupId)
    {
        // Without a repeat, only the seven days from the start date produce meetings.
        var last = form.RepeatsWeekly
            ? form.EndDate
            : Min(form.EndDate, form.StartDate.AddDays(6));

        var timeOff = await db.TeacherTimeOffs
            .Where(o => o.TeacherId == teacherId)
            .Select(o => new { o.StartUtc, o.EndUtc })
            .ToListAsync();

        var planned = new List<PlannedOccurrence>();
        var skippedTimeOff = 0;
        var skippedMissingHour = 0;

        for (var date = form.StartDate; date <= last; date = date.AddDays(1))
        {
            foreach (var day in days.Where(d => d.Day == date.DayOfWeek))
            {
                if (!clock.Exists(date, day.Start) || !clock.Exists(date, day.End))
                {
                    skippedMissingHour++;
                    continue;
                }

                var startUtc = clock.ToUtc(date, day.Start);
                var endUtc = clock.ToUtc(date, day.End);

                if (endUtc <= startUtc)
                {
                    skippedMissingHour++;
                    continue;
                }

                if (timeOff.Any(o => o.StartUtc < endUtc && o.EndUtc > startUtc))
                {
                    skippedTimeOff++;
                    continue;
                }

                planned.Add(new PlannedOccurrence(date, day.Start, startUtc, endUtc));
            }
        }

        var plan = new OccurrencePlan(planned, skippedTimeOff, skippedMissingHour);

        if (planned.Count == 0)
        {
            return plan with { Error = "That produces no meetings at all. Check the days and the date range." };
        }

        if (planned.Count > MaxOccurrences)
        {
            return plan with { Error = $"That would create {planned.Count} meetings. Shorten the date range." };
        }

        planned.Sort((a, b) => a.StartUtc.CompareTo(b.StartUtc));

        for (var i = 1; i < planned.Count; i++)
        {
            if (planned[i].StartUtc < planned[i - 1].EndUtc)
            {
                return plan with
                {
                    Error = $"Two of the days you picked overlap on {planned[i].Date:ddd d MMM}."
                };
            }
        }

        if (await FirstClashAsync(teacherId, planned, excludeGroupId) is { } clash)
        {
            return plan with
            {
                Error = $"You already have something booked on {clash.Date:ddd d MMM} at " +
                        $"{clash.LocalStart:HH\\:mm}. Pick another time."
            };
        }

        return plan;
    }

    /// <summary>
    /// The unique index on (TeacherId, StartUtc) catches an identical start; a partial
    /// overlap such as 09:00-10:30 against 10:00-11:00 needs this range check.
    /// </summary>
    private async Task<PlannedOccurrence?> FirstClashAsync(
        Guid teacherId,
        List<PlannedOccurrence> planned,
        int? excludeGroupId)
    {
        var from = planned[0].StartUtc;
        var to = planned.Max(p => p.EndUtc);

        var query = db.TeacherSlots.Where(s => s.TeacherId == teacherId && s.StartUtc < to && s.EndUtc > from);

        if (excludeGroupId is { } groupId)
        {
            query = query.Where(s => s.ClassGroupScheduleId == null
                || s.ClassGroupSchedule!.ClassGroupId != groupId);
        }

        var existing = await query.Select(s => new { s.StartUtc, s.EndUtc }).ToListAsync();

        return planned.FirstOrDefault(p => existing.Any(e => e.StartUtc < p.EndUtc && e.EndUtc > p.StartUtc));
    }

    private List<ClassGroupSchedule> AddSchedule(int groupId, List<DayInput> days)
    {
        var schedule = days
            .Select(d => new ClassGroupSchedule
            {
                ClassGroupId = groupId,
                DayOfWeek = d.Day,
                StartTime = d.Start,
                EndTime = d.End
            })
            .ToList();

        db.ClassGroupSchedules.AddRange(schedule);

        return schedule;
    }

    private void Materialise(
        ClassGroup group,
        List<ClassGroupSchedule> schedule,
        List<PlannedOccurrence> planned,
        decimal hourlyRate)
    {
        var rows = schedule.ToDictionary(s => (s.DayOfWeek, s.StartTime));

        foreach (var occurrence in planned)
        {
            var row = rows[(occurrence.Date.DayOfWeek, occurrence.LocalStart)];
            var hours = (decimal)(occurrence.EndUtc - occurrence.StartUtc).TotalHours;

            var slot = new TeacherSlot
            {
                TeacherId = group.TeacherId,
                StartUtc = occurrence.StartUtc,
                EndUtc = occurrence.EndUtc,
                Status = SlotStatus.Booked,
                Origin = SlotOrigin.Generated,
                ClassGroupScheduleId = row.Id
            };

            db.TeacherSlots.Add(slot);

            db.Sessions.Add(new Session
            {
                Slot = slot,
                ClassGroupId = group.Id,
                TeacherId = group.TeacherId,
                SubjectId = group.SubjectId,
                Type = group.MaxStudents == 1 ? SessionType.OneToOne : SessionType.Group,
                Kind = group.Kind,
                Title = group.Title,
                Capacity = group.MaxStudents,
                TargetGradeLevel = group.GradeLevel,
                DefaultPrice = Math.Round(hourlyRate * hours, 2),
                StartUtc = occurrence.StartUtc,
                EndUtc = occurrence.EndUtc,
                IsPublished = true,
                AllowSelfBooking = group.IsOpenForEnrollment
            });
        }
    }

    private static string Describe(OccurrencePlan plan)
    {
        var parts = new List<string> { $"{plan.Occurrences.Count} meetings scheduled." };

        if (plan.SkippedTimeOff > 0)
        {
            parts.Add($"{plan.SkippedTimeOff} skipped for time off.");
        }

        if (plan.SkippedMissingHour > 0)
        {
            parts.Add($"{plan.SkippedMissingHour} skipped: that hour does not exist on the day the clocks change.");
        }

        return string.Join(" ", parts);
    }

    // ---------- small helpers ----------

    private record TeacherContext(WallClock Clock, decimal HourlyRate, bool TeachesSubject);

    private async Task<TeacherContext?> LoadTeacherAsync(Guid teacherId, int subjectId)
    {
        var row = await db.Teachers
            .Where(t => t.UserId == teacherId)
            .Select(t => new
            {
                t.User.TimeZoneId,
                t.HourlyRate,

                // One row when the teacher is assigned this subject, holding its rate
                // override if it has one, so a single query answers both questions.
                Assignment = t.Subjects
                    .Where(s => s.SubjectId == subjectId)
                    .Select(s => s.HourlyRateOverride)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (row is null)
        {
            return null;
        }

        return new TeacherContext(
            new WallClock(row.TimeZoneId),
            row.Assignment.FirstOrDefault() ?? row.HourlyRate,
            row.Assignment.Count > 0);
    }

    private async Task<WallClock> ClockAsync(Guid teacherId) =>
        new(await db.Teachers.Where(t => t.UserId == teacherId).Select(t => t.User.TimeZoneId).FirstOrDefaultAsync());

    private async Task<string?> SubjectLineAsync(Guid teacherId)
    {
        var names = await db.TeacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .OrderBy(ts => ts.Subject.Name)
            .Select(ts => ts.Subject.Name)
            .ToListAsync();

        return names.Count == 0 ? null : string.Join(", ", names);
    }

    private async Task<bool> ScheduleChangedAsync(ClassGroup group, ClassGroupFormViewModel form, List<DayInput> days)
    {
        if (group.SubjectId != form.SubjectId
            || group.GradeLevel != form.GradeLevel
            || group.Kind != form.Kind
            || group.StartDate != form.StartDate
            || group.EndDate != form.EndDate
            || group.RepeatsWeekly != form.RepeatsWeekly)
        {
            return true;
        }

        var current = await db.ClassGroupSchedules
            .Where(s => s.ClassGroupId == group.Id)
            .Select(s => new { s.DayOfWeek, s.StartTime, s.EndTime })
            .ToListAsync();

        return current.Count != days.Count
            || days.Any(d => !current.Any(c => c.DayOfWeek == d.Day && c.StartTime == d.Start && c.EndTime == d.End));
    }

    private static void ApplyOpenFields(ClassGroup group, ClassGroupFormViewModel form)
    {
        group.Title = Clean(form.Title);
        group.MaxStudents = form.MaxStudents;
        group.IsOpenForEnrollment = form.IsOpenForEnrollment;
    }

    /// <summary>
    /// The only way a place is taken. Zero rows affected means the class filled up first,
    /// which is the whole point of doing it conditionally in the database.
    /// </summary>
    private async Task<bool> ClaimPlaceAsync(int groupId)
    {
        var now = DateTime.UtcNow;

        var affected = await db.ClassGroups
            .Where(g => g.Id == groupId && g.MembersCount < g.MaxStudents)
            .ExecuteUpdateAsync(set => set
                .SetProperty(g => g.MembersCount, g => g.MembersCount + 1)
                .SetProperty(g => g.UpdatedAtUtc, now));

        return affected == 1;
    }

    private Task ReleasePlaceAsync(int groupId)
    {
        var now = DateTime.UtcNow;

        return db.ClassGroups
            .Where(g => g.Id == groupId && g.MembersCount > 0)
            .ExecuteUpdateAsync(set => set
                .SetProperty(g => g.MembersCount, g => g.MembersCount - 1)
                .SetProperty(g => g.UpdatedAtUtc, now));
    }

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
