using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Web.Infrastructure;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Messages;

public static class MessageTemplates
{
    public static readonly IReadOnlyDictionary<string, string> All = new Dictionary<string, string>
    {
        ["checkin"] = "Checking in. How has your week been? Open Reflect when you have a few minutes.",
        ["calm"] = "When you can, try a Creative Calm module such as Breathing Mandala or Containment.",
        ["journal"] = "Consider a short Reflection Journals entry about this week before our next visit.",
        ["appointment"] = "A reminder that we are due to check in. Please contact the clinic to confirm a time."
    };

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        ["checkin"] = "Check-in",
        ["calm"] = "Try a calm module",
        ["journal"] = "Journal prompt",
        ["appointment"] = "Appointment reminder"
    };
}

public sealed class MessagesViewModel
{
    public IReadOnlyList<Patient> Patients { get; set; } = [];
    public IReadOnlyList<OutboxMessage> Outbox { get; set; } = [];
    public Guid? PatientId { get; set; }
    public string Notice { get; set; }
    public string Error { get; set; }
}
