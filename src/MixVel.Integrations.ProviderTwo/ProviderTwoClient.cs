using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using MixVel.Contracts;
using MixVel.Integrations.ProviderTwo.Contracts;

namespace MixVel.Integrations.ProviderTwo;

public class ProviderTwoClient : IProviderClient
{
    private readonly ILogger<ProviderTwoClient> _logger;
    private readonly HttpClient _httpClient;

    public ProviderTwoClient(ILogger<ProviderTwoClient> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public string Name => "ProviderTwo";

    public async Task<Route[]> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Searching in {ProviderName}", Name);

        var req = new ProviderTwoSearchRequest
        {
            Departure = request.Origin,
            Arrival = request.Destination,
            DepartureDate = request.OriginDateTime,
            MinTimeLimit = request.Filters?.MinTimeLimit,
        };

        var response = await _httpClient.PostAsJsonAsync("api/v1/search", req, cancellationToken);
        response.EnsureSuccessStatusCode();
        var providerResponse =
            await response.Content.ReadFromJsonAsync<ProviderTwoSearchResponse>(cancellationToken: cancellationToken);

        return providerResponse?.Routes.Select(x => new Route
        {
            Id = Guid.NewGuid(),
            Origin = x.Departure.Point,
            Destination = x.Arrival.Point,
            OriginDateTime = x.Departure.Date,
            DestinationDateTime = x.Arrival.Date,
            Price = x.Price,
            TimeLimit = x.TimeLimit
        }).ToArray() ?? Array.Empty<Route>();
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking {ProviderName} status", Name);

        var response = await _httpClient.GetAsync("api/v1/ping", cancellationToken);
        return response.IsSuccessStatusCode;
    }

}