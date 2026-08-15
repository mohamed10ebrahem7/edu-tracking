using System.ComponentModel.DataAnnotations;

namespace edu_tracking.Models.Admin;

/// <summary>Create and edit form for a teacher. <see cref="Id"/> is null when creating.</summary>
public class TeacherFormViewModel
{
    public Guid? Id { get; set; }

    public bool IsEdit => Id.HasValue;

    [Required]
    [StringLength(200)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    /// <summary>The login name. Fixed after creation so sign-in never changes under a teacher.</summary>
    [Required]
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z0-9._@+\-]+$", ErrorMessage = "Use letters, digits or . _ - @ + only.")]
    [Display(Name = "Username")]
    public string UserName { get; set; } = "";

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }

    [Display(Name = "Subjects")]
    public List<int> SubjectIds { get; set; } = [];

    [Range(0, 100000)]
    [Display(Name = "Hourly rate (EGP)")]
    public decimal HourlyRate { get; set; } = 250;

    [Range(15, 480)]
    [Display(Name = "Session length (minutes)")]
    public int DefaultSessionMinutes { get; set; } = 60;

    [Range(0, 720)]
    [Display(Name = "Minimum booking notice (hours)")]
    public int MinBookingNoticeHours { get; set; } = 12;

    [Range(0, 720)]
    [Display(Name = "Cancellation cutoff (hours)")]
    public int CancellationCutoffHours { get; set; } = 24;

    [StringLength(2000)]
    public string? Bio { get; set; }

    [Display(Name = "Accepting bookings")]
    public bool IsAcceptingBookings { get; set; } = true;

    [Display(Name = "Auto-approve booking requests")]
    public bool AutoApproveBookings { get; set; }

    /// <summary>Leave blank on create to have one generated.</summary>
    [StringLength(100, MinimumLength = 8)]
    [Display(Name = "Temporary password")]
    public string? TemporaryPassword { get; set; }

    /// <summary>Repopulated by the controller so the checkbox list can be rendered.</summary>
    public IReadOnlyList<SubjectOption> AvailableSubjects { get; set; } = [];
}
