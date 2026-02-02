namespace ErpCloud.Api.Services;

/// <summary>
/// Registry for e-Invoice providers
/// </summary>
public class EInvoiceProviderRegistry
{
    private readonly Dictionary<string, IEInvoiceProvider> _providers = new();

    public void Register(IEInvoiceProvider provider)
    {
        _providers[provider.Code.ToUpperInvariant()] = provider;
    }

    public IEInvoiceProvider GetProvider(string code)
    {
        var key = code.ToUpperInvariant();
        if (!_providers.TryGetValue(key, out var provider))
        {
            throw new InvalidOperationException($"Provider '{code}' not found");
        }
        return provider;
    }

    public IEnumerable<string> GetAvailableProviders()
    {
        return _providers.Keys;
    }
}
