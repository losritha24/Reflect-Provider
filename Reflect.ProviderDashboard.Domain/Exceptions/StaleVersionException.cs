namespace Reflect.ProviderDashboard.Domain.Exceptions;

public sealed class StaleVersionException(string message) : Exception(message);
