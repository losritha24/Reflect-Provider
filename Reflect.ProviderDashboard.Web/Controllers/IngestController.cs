using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Web.Models.Integration;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Web.Controllers;

[ApiController]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[EnableRateLimiting("account")]
[RequestSizeLimit(32_768)]
[Route("api/v1/ingest")]
public sealed class IngestController : ControllerBase
{
    public const string KeyHeader = "X-Reflect-Integration-Key";

    private readonly IConfiguration _configuration;
    private readonly SessionIngestService _ingest;

    public IngestController(IConfiguration configuration, SessionIngestService ingest)
    {
        _configuration = configuration;
        _ingest = ingest;
    }

    [HttpPost("sessions")]
    public IActionResult Sessions([FromBody] SessionIngestRequest request)
    {
        if (!_configuration.GetValue<bool>("Integration:Enabled"))
            return NotFound();
        if (!KeyMatches(Request.Headers[KeyHeader].ToString(), _configuration["Integration:ApiKey"]))
            return Unauthorized();
        if (request is null)
            return BadRequest();

        try
        {
            var session = _ingest.Ingest(request);
            return Accepted(new
            {
                session.Id,
                startedAtUtc = session.StartedAtUtc.UtcDateTime.ToString("o"),
                storage = "UTC"
            });
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public static bool KeyMatches(string provided, string expected)
    {
        if (string.IsNullOrEmpty(provided) || string.IsNullOrEmpty(expected))
            return false;
        var left = Encoding.UTF8.GetBytes(provided);
        var right = Encoding.UTF8.GetBytes(expected);
        if (left.Length != right.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(left, right);
    }
}
