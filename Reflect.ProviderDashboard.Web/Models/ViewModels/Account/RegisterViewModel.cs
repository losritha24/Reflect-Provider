using System.ComponentModel.DataAnnotations;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Account;

public sealed class RegisterViewModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Work email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(12, ErrorMessage = "Use at least 12 characters.")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string ReturnUrl { get; set; } = string.Empty;
}
