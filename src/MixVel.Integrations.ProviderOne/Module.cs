using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MixVel.Contracts;
using Polly;
using Polly.Extensions.Http;

namespace MixVel.Integrations.ProviderOne;

[UsedImplicitly]
public class Module : IModule
{
    private const int DefaultRetryCount = 3;

    public void Configure(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ProviderOneConfig>(configuration.GetSection("SearchProviders:ProviderOne"));
        services.AddHttpClient<IProviderClient, ProviderOneClient>(client =>
            {
                client.BaseAddress = new Uri(
                    configuration["SearchProviders:ProviderOne:BaseUrl"]
                    ?? throw new ApplicationException("SearchProviders:ProviderOne:BaseUrl can not be empty"));
            })
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    configuration.GetValue("SearchProviders:ProviderOne:RetryCount", DefaultRetryCount),
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
    }
}