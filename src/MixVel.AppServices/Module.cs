using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MixVel.Contracts;

namespace MixVel.AppServices;

[UsedImplicitly]
public class Module : IModule
{
    public void Configure(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SearchConfig>(configuration.GetSection("Search"));
        services.AddScoped<IClock, MachineClock>();
        services.AddScoped<ISearchService, SearchService>();
    }
}