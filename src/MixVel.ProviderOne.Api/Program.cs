using Bogus;
using Microsoft.OpenApi.Models;
using MixVel.Integrations.ProviderOne.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(config =>
{
    config.SwaggerDoc("v1", new OpenApiInfo { Title = "ProviderOne API", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/v1/ping", () => new Faker().Random.Number(10) < 3 ? Results.Problem() : Results.Ok())
    .WithSummary("Check service availability.")
    .WithOpenApi();
app.MapPost("/api/v1/search", GenerateResponse)
    .WithSummary("Search routes")
    .WithOpenApi();

app.Run();

static ProviderOneSearchResponse GenerateResponse(ProviderOneSearchRequest request)
{
    var faker = new Faker();

    var routeFaker = new Faker<ProviderOneRoute>()
        .RuleFor(x => x.From, request.From)
        .RuleFor(x => x.To, f => request.To)
        .RuleFor(x => x.DateFrom, request.DateFrom)
        .RuleFor(x => x.DateTo, f => request.DateTo ?? request.DateFrom.AddDays(f.Random.Int(1, 30)))
        .RuleFor(x => x.Price, f => f.Finance.Amount(1, request.MaxPrice ?? 5000))
        .RuleFor(x => x.TimeLimit, DateTime.Now.AddMinutes(1));

    return new ProviderOneSearchResponse { Routes = routeFaker.Generate(faker.Random.Int(0, 10)).ToArray() };
}