using System.ComponentModel.DataAnnotations;

namespace edu_tracking.Models.Admin;

/// <summary>Create and edit form for a parent. <see cref="Id"/> is null when creating.</summary>
public class ParentFormViewModel
{
    public Guid? Id { get; set; }

    public bool IsEdit => Id.HasValue;

    [Required]
    [StringLength(200)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    /// <summary>The login name. Fixed after creation so sign-in never changes under a parent.</summary>
    [Required]
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z0-9._@+\-]+$", ErrorMessage = "Use letters, digits or . _ - @ + only.")]
    [Display(Name = "Username")]
    public string UserName { get; set; } = "";

    /// <summary>Required: a parent is the contact of record for their children.</summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }

    [Display(Name = "Children")]
    public List<Guid> StudentIds { get; set; } = [];

    /// <summary>Applies to every child linked to this parent.</summary>
    [Required]
    [StringLength(50)]
    public string Relationship { get; set; } = ParentRelationship.Guardian;

    [Display(Name = "Primary contact for these children")]
    public bool IsPrimaryContact { get; set; } = true;

    /// <summary>Leave blank on create to have one generated.</summary>
    [StringLength(100, MinimumLength = 8)]
    [Display(Name = "Temporary password")]
    public string? TemporaryPassword { get; set; }

    /// <summary>Repopulated by the controller so the child list can be rendered.</summary>
    public IReadOnlyList<StudentOption> AvailableStudents { get; set; } = [];
}

public record StudentOption(Guid Id, string Name, string Grade);
