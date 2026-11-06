namespace Reflect.ProviderDashboard.Web.Infrastructure;

public static class AccountRateLimit
{
    public static bool IsCredentialAttempt(HttpContext context) =>
        HttpMethods.IsPost(context.Request.Method) &&
        context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute>()?.PolicyName == "account";

    // Only the connection address, after explicitly trusted proxy processing. Never raw headers.
    public static string ClientKey(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        if (address?.IsIPv4MappedToIPv6 == true) address = address.MapToIPv4();
        return address?.ToString() ?? "unknown-client";
    }
}
