using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MixVel.Contracts;

public interface IModule
{
    void Configure(IServiceCollection services, IConfiguration configuration);
}