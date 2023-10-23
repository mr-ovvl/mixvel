using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using MixVel.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(config =>
{
    config.SwaggerDoc("v1", new OpenApiInfo { Title = "ProviderTwo API", Version = "v1" });
});

builder.Services.AddEasyCaching(options =>
{
    if (builder.Configuration.GetSection("Cache:InMemory").Exists())
    {
        options.UseInMemory(builder.Configuration, "InMemoryCache", "Cache:InMemory");
    } else if (builder.Configuration.GetSection("Cache:Redis").Exists())
    {
        options.UseRedis(builder.Configuration, "RedisCache", "Cache:Redis");
    }
    else throw new ApplicationException("Cache not configured.");
});

foreach (var module in GetModules())
{
    module.Configure(builder.Services, builder.Configuration);
}

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet(
        "/api/v1/ping",
        async (ISearchService searchService, CancellationToken cancellationToken) =>
            await searchService.IsAvailableAsync(cancellationToken))
    .WithSummary("Check service availability.")
    .WithOpenApi();
app.MapPost(
        "/api/v1/search",
        async (
                [FromBody] SearchRequest request,
                [FromServices] ISearchService searchService,
                CancellationToken cancellationToken) =>
            await searchService.SearchAsync(request, cancellationToken))
    .WithSummary("Search routes")
    .WithOpenApi();

app.Run();


IEnumerable<IModule> GetModules() =>
    Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "MixVel.*.dll")
        .Select(Assembly.LoadFrom)
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => typeof(IModule).IsAssignableFrom(type) && !type.IsInterface)
        .Select(type => Activator.CreateInstance(type) as IModule)!;
