using System.ComponentModel.DataAnnotations;
using edu_tracking.Domain;

namespace edu_tracking.Models.Admin;

/// <summary>Create and edit form for a student. <see cref="Id"/> is null when creating.</summary>
public class StudentFormViewModel
{
    public Guid? Id { get; set; }

    public bool IsEdit => Id.HasValue;

    [Required]
    [StringLength(200)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    /// <summary>The login name. Fixed after creation so sign-in never changes under a student.</summary>
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

    [Required]
    [Display(Name = "Grade level")]
    public GradeLevel GradeLevel { get; set; } = Domain.GradeLevel.Grade1;

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [StringLength(200)]
    public string? School { get; set; }

    [StringLength(1000)]
    [Display(Name = "Internal notes")]
    public string? Notes { get; set; }

    [Display(Name = "Guardians")]
    public List<Guid> ParentIds { get; set; } = [];

    /// <summary>Leave blank on create to have one generated.</summary>
    [StringLength(100, MinimumLength = 8)]
    [Display(Name = "Temporary password")]
    public string? TemporaryPassword { get; set; }

    /// <summary>Repopulated by the controller so the guardian list can be rendered.</summary>
    public IReadOnlyList<ParentOption> AvailableParents { get; set; } = [];
}

public record ParentOption(Guid Id, string Name);
