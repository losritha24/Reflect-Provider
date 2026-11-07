using System.ComponentModel.DataAnnotations;
using Reflect.ProviderDashboard.Domain.Entities;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Patients;

public sealed class RemovePatientViewModel
{
    [Required]
    [Display(Name = "Patient")]
    public Guid? PatientId { get; set; }

    [Display(Name = "Confirm")]
    public bool Confirm { get; set; }

    public IReadOnlyList<Patient> Patients { get; set; } = Array.Empty<Patient>();
}
