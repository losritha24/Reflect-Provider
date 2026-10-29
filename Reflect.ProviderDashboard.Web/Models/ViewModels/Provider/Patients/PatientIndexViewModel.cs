using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Interfaces;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Patients;

public class PatientIndexViewModel
{
    public List<PatientRowViewModel> Rows { get; set; } = new();
    public string Search { get; set; }
    public EngagementTrend? Trend { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize => 10;
    public int TotalCount { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public async Task PrepareData(IPatientRepository patients, ITherapySessionRepository sessions, CancellationToken ct)
    {
        var result = await patients.SearchAsync(Search, Trend, Page, PageSize, ct);
        Page = result.Page;
        TotalCount = result.TotalCount;
        Rows.Clear();
        foreach (var patient in result.Items)
        {
            var activity = await sessions.GetByPatientIdAsync(patient.Id, ct);
            Rows.Add(PatientRowViewModel.FromEntity(patient, activity));
        }
    }
}
