using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Patients;

public sealed class CreatePatientViewModel : IValidatableObject
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    [Display(Name = "Name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 2)]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._-]{1,63}$", ErrorMessage = "Use letters, numbers, dots, hyphens, or underscores, with no spaces.")]
    [Display(Name = "Patient ID")]
    public string ExternalPatientId { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [StringLength(2000)]
    [Display(Name = "Notes")]
    public string NotesSummary { get; set; } = string.Empty;

    [BindNever]
    public string ImportNotice { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth is null)
            yield break;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (DateOfBirth.Value > today)
            yield return new ValidationResult("Date of birth cannot be in the future.", [nameof(DateOfBirth)]);
        if (DateOfBirth.Value.Year < 1900)
            yield return new ValidationResult("Date of birth is too far in the past.", [nameof(DateOfBirth)]);
    }
}
