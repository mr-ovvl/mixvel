using EasyCaching.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixVel.Contracts;

namespace MixVel.AppServices;

public class SearchService : ISearchService
{
    private readonly ILogger<SearchService> _logger;
    private readonly SearchConfig _config;
    private readonly IClock _clock;
    private readonly IEasyCachingProvider _cachingProvider;
    private readonly IEnumerable<IProviderClient> _providerClients;

    public SearchService(
        ILogger<SearchService> logger,
        IOptions<SearchConfig> config,
        IClock clock,
        IEasyCachingProvider cachingProvider,
        IEnumerable<IProviderClient> providerClients)
    {
        _logger = logger;
        _config = config.Value;
        _cachingProvider = cachingProvider;
        _providerClients = providerClients;
        _clock = clock;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        if (request.Filters is { OnlyCached: true })
        {
            _logger.LogInformation("Getting routes from cache");
            var cachedRoutes = await GetRoutesFromCache(request, cancellationToken);
            return CreateResponse(cachedRoutes);
        }

        _logger.LogInformation("Getting routes from providers");

        var results = await Task.WhenAll(_providerClients.Select(x => x.SearchAsync(request, cancellationToken)));
        var routes = results.SelectMany(x => x)
            .Where(x => MatchFilters(x, request.Filters))
            .DistinctBy(x => new { x.Origin, x.Destination, x.OriginDateTime, x.DestinationDateTime, x.Price })
            .ToArray();

        if (routes.Length > 0)
        {
            await _cachingProvider.SetAllAsync(
                routes.ToDictionary(x => x.Id.ToString(), x => x),
                routes.Max(x => x.TimeLimit) - _clock.GetNow(),
                cancellationToken);

            await _cachingProvider.SetAsync(
                GetSearchKey(request),
                routes.Select(x => x.Id).ToArray(),
                routes.Max(x => x.TimeLimit) - _clock.GetNow(),
                cancellationToken);
        }

        return CreateResponse(routes);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking providers availability");
        var values = await Task.WhenAll(_providerClients.Select(x => x.IsAvailableAsync(cancellationToken)));
        var notAvailableProviders = _providerClients
            .Select((provider, index) => (provider.Name, IsAvailable: values[index]))
            .Where(x => !x.IsAvailable)
            .ToArray();

        if (notAvailableProviders.Length == _providerClients.Count())
        {
            _logger.LogWarning("All providers are not available");
            return false;
        }

        if (notAvailableProviders.Length > 0)
        {
            _logger.LogWarning(
                "Some providers are not available: {Providers}",
                notAvailableProviders.Select(x => x.Name));
        }

        return _config.AvailabilityStrategy switch
        {
            AvailabilityStrategy.None =>
                throw new ApplicationException("Provider availability strategy can't be none."),
            AvailabilityStrategy.Any => true,
            AvailabilityStrategy.All => notAvailableProviders.Length == 0,
            _ => throw new ApplicationException("Provider availability strategy not set or incorrect")
        };
    }

    private async Task<Route[]> GetRoutesFromCache(SearchRequest request, CancellationToken cancellationToken)
    {
        var cacheValueIds =
            await _cachingProvider.GetByPrefixAsync<Guid[]>(GetSearchKeyPrefix(request), cancellationToken);
        var ids = cacheValueIds.Values
            .Where(x => x.HasValue)
            .SelectMany(x => x.Value)
            .Distinct()
            .Select(x => x.ToString())
            .ToArray();

        if (ids.Length == 0)
        {
            return Array.Empty<Route>();
        }

        var cachedRoutes = await _cachingProvider.GetAllAsync<Route>(ids, cancellationToken);
        return cachedRoutes.Count > 0
            ? cachedRoutes.Values
                .Where(x => x.HasValue && !x.IsNull && MatchFilters(x.Value, request.Filters))
                .Select(x => x.Value)
                .ToArray()
            : Array.Empty<Route>();
    }

    private static bool MatchFilters(Route route, SearchFilters? filter) =>
        route.DestinationDateTime == (filter?.DestinationDateTime ?? route.DestinationDateTime)
        && route.Price <= (filter?.MaxPrice ?? route.Price)
        && route.TimeLimit >= (filter?.MinTimeLimit ?? route.TimeLimit);

    private static SearchResponse CreateResponse(Route[] routes) =>
        new()
        {
            Routes = routes,
            MinPrice = routes.Length > 0 ? routes.Min(x => x.Price) : 0,
            MaxPrice = routes.Length > 0 ? routes.Max(x => x.Price) : 0,
            MinMinutesRoute = routes.Length > 0 ? routes.Min(x => (int)(x.DestinationDateTime - x.OriginDateTime).TotalMinutes) : 0,
            MaxMinutesRoute = routes.Length > 0 ? routes.Max(x => (int)(x.DestinationDateTime - x.OriginDateTime).TotalMinutes) : 0
        };

    private static string GetSearchKey(SearchRequest request) =>
        $"{GetSearchKeyPrefix(request)}-{GetSearchKeySuffix(request.Filters)}";

    private static string GetSearchKeyPrefix(SearchRequest request) =>
        $"{request.Origin}-{request.Destination}-{request.OriginDateTime.Ticks}";

    private static string GetSearchKeySuffix(SearchFilters? filters) =>
        $"{(filters?.DestinationDateTime is not null ? filters.DestinationDateTime.Value.Ticks : DefaultFiller)}-"
        + $"{(filters?.MaxPrice is not null ? filters.MaxPrice.Value : DefaultFiller)}-"
        + $"{(filters?.MinTimeLimit is not null ? filters.MinTimeLimit.Value : DefaultFiller)}";

    private const string DefaultFiller = "+";
}
