using Reflect.ProviderDashboard.Web.Extensions;

namespace Reflect.ProviderDashboard.Web.Infrastructure;

public class CspMiddleware
{
    private readonly RequestDelegate _next;

    public CspMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        // Includes sign-in and error pages; never cache provider HTML in shared caches.
        context.Response.Headers["Cache-Control"] = "no-store";
        var nonce = context.GetCspNonce();
        context.Response.Headers["Content-Security-Policy"] =
            $"default-src 'self'; script-src 'self' 'nonce-{nonce}'; style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; font-src 'self' data:; connect-src 'self'; " +
            "frame-ancestors 'none'; base-uri 'self'; object-src 'none'; form-action 'self';";
        await _next(context);
    }
}
