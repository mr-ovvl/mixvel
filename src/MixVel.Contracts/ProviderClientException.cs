namespace MixVel.Contracts;

public class ProviderClientException : ApplicationException
{
    public ProviderClientException(string providerName)
    {
        ProviderName = providerName;
    }

    public ProviderClientException(string providerName, string message)
        : base(message)
    {
        ProviderName = providerName;
    }

    public ProviderClientException(string providerName, string message, Exception inner)
        : base(message, inner)
    {
        ProviderName = providerName;
    }

    public string ProviderName { get; }
}