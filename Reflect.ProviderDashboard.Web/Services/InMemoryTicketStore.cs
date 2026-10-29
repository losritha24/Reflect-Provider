using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace Reflect.ProviderDashboard.Web.Services;

// Single-instance synthetic demo only. Multi-instance hosting needs a shared store.
public sealed class InMemoryTicketStore(IMemoryCache cache, TimeProvider clock) : ITicketStore
{
    private readonly object gate = new();
    private sealed record Entry(byte[] Ticket, DateTimeOffset ExpiresAt);
    private static string CacheKey(string key) => "auth-ticket:" + key;

    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expires = ticket.Properties.ExpiresUtc ?? clock.GetUtcNow().AddHours(1);
        lock (gate) Save(key, ticket, expires);
        return Task.FromResult(key);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        lock (gate)
        {
            // A request completing after logout must not resurrect a removed session.
            if (cache.TryGetValue<Entry>(CacheKey(key), out var entry) && entry.ExpiresAt > clock.GetUtcNow())
                Save(key, ticket, entry.ExpiresAt);
        }
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket> RetrieveAsync(string key)
    {
        lock (gate)
        {
            if (cache.TryGetValue<Entry>(CacheKey(key), out var entry) && entry.ExpiresAt > clock.GetUtcNow())
                return Task.FromResult(TicketSerializer.Default.Deserialize(entry.Ticket));
            cache.Remove(CacheKey(key));
            return Task.FromResult<AuthenticationTicket>(null);
        }
    }

    public Task RemoveAsync(string key)
    {
        lock (gate) cache.Remove(CacheKey(key));
        return Task.CompletedTask;
    }

    private void Save(string key, AuthenticationTicket ticket, DateTimeOffset expires)
    {
        if (expires <= clock.GetUtcNow()) return;
        // Serialize a copy so callers cannot mutate the cached identity/properties.
        var copy = TicketSerializer.Default.Deserialize(TicketSerializer.Default.Serialize(ticket));
        copy.Properties.ExpiresUtc = expires;
        cache.Set(CacheKey(key), new Entry(TicketSerializer.Default.Serialize(copy), expires), expires);
    }
}
