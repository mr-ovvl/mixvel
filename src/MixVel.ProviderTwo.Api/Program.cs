using Bogus;
using Microsoft.OpenApi.Models;
using MixVel.Integrations.ProviderTwo.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(config =>
{
    config.SwaggerDoc("v1", new OpenApiInfo { Title = "ProviderTwo API", Version = "v1" });
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

static ProviderTwoSearchResponse GenerateResponse(ProviderTwoSearchRequest request)
{
    var faker = new Faker();

    var routeFaker = new Faker<ProviderTwoRoute>()
        .RuleFor(x => x.Departure, f => new ProviderTwoPoint
        {
            Point = request.Departure,
            Date = f.Date.Soon(0, request.DepartureDate)
        })
        .RuleFor(x => x.Arrival, (f, x) => new ProviderTwoPoint
        {
            Point = request.Arrival,
            Date = x.Departure.Date.AddHours(f.Random.Double(1, 30))
        })
        .RuleFor(x => x.Price, f => f.Finance.Amount(1, 5000))
        .RuleFor(x => x.TimeLimit, DateTime.Now.AddMinutes(2));

    return new ProviderTwoSearchResponse { Routes = routeFaker.Generate(faker.Random.Int(0, 10)).ToArray() };
}