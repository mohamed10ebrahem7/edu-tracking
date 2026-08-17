using System.ComponentModel.DataAnnotations;

namespace edu_tracking.Models.Admin;

/// <summary>Create and edit form for an admin. <see cref="Id"/> is null when creating.</summary>
public class AdminFormViewModel
{
    public Guid? Id { get; set; }

    public bool IsEdit => Id.HasValue;

    [Required]
    [StringLength(200)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    /// <summary>The login name. Fixed after creation so sign-in never changes under an admin.</summary>
    [Required]
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z0-9._@+\-]+$", ErrorMessage = "Use letters, digits or . _ - @ + only.")]
    [Display(Name = "Username")]
    public string UserName { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }

    /// <summary>Leave blank on create to have one generated.</summary>
    [StringLength(100, MinimumLength = 8)]
    [Display(Name = "Temporary password")]
    public string? TemporaryPassword { get; set; }
}
