using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MixVel.Contracts;

namespace MixVel.Integrations.ProviderTwo;

[UsedImplicitly]
public class Module : IModule
{
    public void Configure(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ProviderTwoConfig>(configuration.GetSection("SearchProviders:ProviderTwo"));
        // services.AddScoped<IProviderClient, ProviderTwoClient>();
        services.AddHttpClient<IProviderClient, ProviderTwoClient>();
    }
}