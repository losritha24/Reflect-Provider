using System.ComponentModel.DataAnnotations;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Account;

public sealed class VerifyViewModel
{
    [Required]
    [StringLength(20, MinimumLength = 6)]
    [Display(Name = "Authentication code")]
    public string Code { get; set; } = string.Empty;

    public bool UseRecoveryCode { get; set; }
}
