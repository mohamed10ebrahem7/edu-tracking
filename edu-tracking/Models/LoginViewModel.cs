using System.ComponentModel.DataAnnotations;

namespace edu_tracking.Models;

public class LoginViewModel
{
    [Required]
    [Display(Name = "Username")]
    public string UserName { get; set; } = null!;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
