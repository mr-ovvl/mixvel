using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixVel.Contracts;
using MixVel.Integrations.ProviderOne.Contracts;

namespace MixVel.Integrations.ProviderOne;

public class ProviderOneClient : IProviderClient
{
    private readonly ILogger<ProviderOneClient> _logger;
    private readonly HttpClient _httpClient;

    public ProviderOneClient(
        ILogger<ProviderOneClient> logger,
        IOptions<ProviderOneConfig> config,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(config.Value.BaseUrl);
    }

    public string Name => "ProviderOne";

    public async Task<Route[]> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Searching {ProviderName}", Name);

        var req = new ProviderOneSearchRequest
        {
            From = request.Origin,
            To = request.Destination,
            DateFrom = request.OriginDateTime,
            DateTo = request.Filters?.DestinationDateTime,
            MaxPrice = request.Filters?.MaxPrice
        };

        var response = await _httpClient.PostAsJsonAsync("api/v1/search", req, cancellationToken);
        response.EnsureSuccessStatusCode();
        var providerResponse =
            await response.Content.ReadFromJsonAsync<ProviderOneSearchResponse>(cancellationToken: cancellationToken);

        return providerResponse?.Routes.Select(x => new Route
        {
            Id = Guid.NewGuid(),
            Origin = x.From,
            Destination = x.To,
            OriginDateTime = x.DateFrom,
            DestinationDateTime = x.DateTo,
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