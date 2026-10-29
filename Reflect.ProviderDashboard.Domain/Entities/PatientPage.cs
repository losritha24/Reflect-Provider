namespace Reflect.ProviderDashboard.Domain.Entities;

public record PatientPage(IReadOnlyList<Patient> Items, int TotalCount, int Page, int PageSize);
