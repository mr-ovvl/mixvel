using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MixVel.Contracts;

namespace MixVel.Integrations.ProviderOne;

[UsedImplicitly]
public class Module : IModule
{
    public void Configure(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ProviderOneConfig>(configuration.GetSection("SearchProviders:ProviderOne"));
        // services.AddScoped<IProviderClient, ProviderOneClient>();
        services.AddHttpClient<IProviderClient, ProviderOneClient>();
    }
}