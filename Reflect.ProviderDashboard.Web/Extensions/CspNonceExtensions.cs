namespace Reflect.ProviderDashboard.Web.Extensions;

public static class CspNonceExtensions
{
    private const string NonceKey = "CspNonce";

    public static string GetCspNonce(this HttpContext context)
    {
        if (context.Items.TryGetValue(NonceKey, out var existing) && existing is string nonce)
        {
            return nonce;
        }

        nonce = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        context.Items[NonceKey] = nonce;
        return nonce;
    }
}
