using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Services.Chat;

namespace Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderAuthorization.PolicyName)]
public class ChatController : Controller
{
    private readonly IChatResponder _responder;

    public ChatController(IChatResponder responder) => _responder = responder;

    // Antiforgery is enforced globally (AutoValidateAntiforgeryToken); the page JS sends the header.
    // Message text is deliberately never logged or stored.
    [HttpPost]
    [EnableRateLimiting("chat")]
    [RequestSizeLimit(4_096)]
    public async Task<IActionResult> Send([FromBody] ChatRequest request, CancellationToken ct)
    {
        var check = ChatGuardrail.Inspect(request?.Message);
        if (!check.Allowed) return Json(new ChatReply(check.Message, true, []));
        var reply = await _responder.RespondAsync(check.Sanitized, ct);
        return Json(new ChatReply(reply, false, check.Redactions));
    }

    public sealed record ChatRequest(string Message);
    public sealed record ChatReply(string Reply, bool Refused, IReadOnlyList<string> Redacted);
}
