using System.ComponentModel.DataAnnotations;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Account;

public sealed class LoginViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Work email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }

    public string ReturnUrl { get; set; } = string.Empty;
}
