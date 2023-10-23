using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MixVel.Contracts;
using Polly;
using Polly.Extensions.Http;

namespace MixVel.Integrations.ProviderTwo;

[UsedImplicitly]
public class Module : IModule
{
    private const int DefaultRetryCount = 3;

    public void Configure(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ProviderTwoConfig>(configuration.GetSection("SearchProviders:ProviderTwo"));
        services.AddHttpClient<IProviderClient, ProviderTwoClient>(client =>
            {
                client.BaseAddress = new Uri(
                    configuration["SearchProviders:ProviderTwo:BaseUrl"]
                    ?? throw new ApplicationException("SearchProviders:ProviderTwo:BaseUrl can not be empty"));
            })
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    configuration.GetValue("SearchProviders:ProviderTwo:RetryCount", DefaultRetryCount),
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
    }
}