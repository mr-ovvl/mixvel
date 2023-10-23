using Bogus;
using EasyCaching.Core;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixVel.Contracts;
using NSubstitute;

namespace MixVel.AppServices.Tests;

public class SearchServiceTests
{
    [Fact]
    public async Task ShouldBeAvailable_When_AvailabilityStrategyAll_And_AllProvidersAreAvailable()
    {
        // Arrange.
        var options = Substitute.For<IOptions<SearchConfig>>();
        options.Value.Returns(new SearchConfig { AvailabilityStrategy = AvailabilityStrategy.All });

        var providerOne = Substitute.For<IProviderClient>();
        providerOne.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        var providerTwo = Substitute.For<IProviderClient>();
        providerTwo.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        var providers = new[] { providerOne, providerTwo };

        var searchService = new SearchService(
            Substitute.For<ILogger<SearchService>>(),
            options,
            Substitute.For<IClock>(),
            Substitute.For<IEasyCachingProvider>(),
            providers);

        // Act.
        var isAvailable = await searchService.IsAvailableAsync(CancellationToken.None);

        // Assert.
        isAvailable.Should().BeTrue();
        await providerOne.Received(1).IsAvailableAsync(CancellationToken.None);
        await providerTwo.Received(1).IsAvailableAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldBeNotAvailable_When_AvailabilityStrategyAll_And_NotAllProvidersAreAvailable()
    {
        // Arrange.
        var options = Substitute.For<IOptions<SearchConfig>>();
        options.Value.Returns(new SearchConfig { AvailabilityStrategy = AvailabilityStrategy.All });

        var providerOne = Substitute.For<IProviderClient>();
        providerOne.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        var providerTwo = Substitute.For<IProviderClient>();
        providerTwo.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        var providers = new[] { providerOne, providerTwo };

        var searchService = new SearchService(
            Substitute.For<ILogger<SearchService>>(),
            options,
            Substitute.For<IClock>(),
            Substitute.For<IEasyCachingProvider>(),
            providers);

        // Act.
        var isAvailable = await searchService.IsAvailableAsync(CancellationToken.None);

        // Assert.
        isAvailable.Should().BeFalse();
        await providerOne.Received(1).IsAvailableAsync(CancellationToken.None);
        await providerTwo.Received(1).IsAvailableAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldBeAvailable_When_AvailabilityStrategyAny_And_SomeProvidersAreAvailable()
    {
        // Arrange.
        var options = Substitute.For<IOptions<SearchConfig>>();
        options.Value.Returns(new SearchConfig { AvailabilityStrategy = AvailabilityStrategy.Any });

        var providerOne = Substitute.For<IProviderClient>();
        providerOne.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        var providerTwo = Substitute.For<IProviderClient>();
        providerTwo.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        var providers = new[] { providerOne, providerTwo };

        var searchService = new SearchService(
            Substitute.For<ILogger<SearchService>>(),
            options,
            Substitute.For<IClock>(),
            Substitute.For<IEasyCachingProvider>(),
            providers);

        // Act.
        var isAvailable = await searchService.IsAvailableAsync(CancellationToken.None);

        // Assert.
        isAvailable.Should().BeTrue();
        await providerOne.Received(1).IsAvailableAsync(CancellationToken.None);
        await providerTwo.Received(1).IsAvailableAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldBeNotAvailable_When_AvailabilityStrategyAny_And_AllProvidersAreNotAvailable()
    {
        // Arrange.
        var options = Substitute.For<IOptions<SearchConfig>>();
        options.Value.Returns(new SearchConfig { AvailabilityStrategy = AvailabilityStrategy.Any });

        var providerOne = Substitute.For<IProviderClient>();
        providerOne.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        var providerTwo = Substitute.For<IProviderClient>();
        providerTwo.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        var providers = new[] { providerOne, providerTwo };

        var searchService = new SearchService(
            Substitute.For<ILogger<SearchService>>(),
            options,
            Substitute.For<IClock>(),
            Substitute.For<IEasyCachingProvider>(),
            providers);

        // Act.
        var isAvailable = await searchService.IsAvailableAsync(CancellationToken.None);

        // Assert.
        isAvailable.Should().BeFalse();
        await providerOne.Received(1).IsAvailableAsync(CancellationToken.None);
        await providerTwo.Received(1).IsAvailableAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldGetRoutesFromCache_WhenFiltersHaveOnlyCachedFlag()
    {
        // Arrange.
        var searchRequest = new Faker<SearchRequest>()
            .RuleFor(x => x.Origin, f => f.Address.City())
            .RuleFor(x => x.Destination, f => f.Address.City())
            .RuleFor(x => x.OriginDateTime, f => f.Date.Soon())
            .RuleFor(x => x.Filters, new SearchFilters { OnlyCached = true })
            .Generate();
        var routeFaker = CreateRouteFaker(searchRequest.Origin, searchRequest.Destination,  searchRequest.OriginDateTime);
        var expectedRoutes = routeFaker.Generate(3).ToArray();

        var cacheProvider = Substitute.For<IEasyCachingProvider>();
        cacheProvider.GetByPrefixAsync<Guid[]>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateNonEmptyIdsCacheValueDictionary()));
        cacheProvider.GetAllAsync<Route>(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                (IDictionary<string, CacheValue<Route>>)expectedRoutes.ToDictionary(
                    x => x.Id.ToString(),
                    x => new CacheValue<Route>(x, true))
            ));

        var providerOne = Substitute.For<IProviderClient>();
        var providerTwo = Substitute.For<IProviderClient>();
        var providers = new[] { providerOne, providerTwo };

        var searchService = new SearchService(
            Substitute.For<ILogger<SearchService>>(),
            Substitute.For<IOptions<SearchConfig>>(),
            Substitute.For<IClock>(),
            cacheProvider,
            providers);

        // Act.
        var response = await searchService.SearchAsync(searchRequest, CancellationToken.None);

        // Assert.
        response.Routes.Should().NotBeNull()
            .And.NotBeEmpty()
            .And.BeEquivalentTo(expectedRoutes);
        response.MinPrice.Should().Be(expectedRoutes.Min(x => x.Price));
        response.MaxPrice.Should().Be(expectedRoutes.Max(x => x.Price));
        response.MinMinutesRoute.Should().Be(expectedRoutes.Min(x => x.DestinationDateTime - x.OriginDateTime).Minutes);
        response.MaxMinutesRoute.Should().Be(expectedRoutes.Max(x => x.DestinationDateTime - x.OriginDateTime).Minutes);
    }

    [Fact]
    public async Task ShouldGetRoutesFroProviders_WhenFiltersDontHaveOnlyCachedFlag()
    {
        // Arrange.
        var searchRequest = new Faker<SearchRequest>()
            .RuleFor(x => x.Origin, f => f.Address.City())
            .RuleFor(x => x.Destination, f => f.Address.City())
            .RuleFor(x => x.OriginDateTime, f => f.Date.Soon())
            .Generate();
        var routeFaker = CreateRouteFaker(searchRequest.Origin, searchRequest.Destination,  searchRequest.OriginDateTime);

        var providerOneRoutes = routeFaker.Generate(3).ToArray();
        var providerOne = Substitute.For<IProviderClient>();
        providerOne.SearchAsync(searchRequest, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(providerOneRoutes));

        var providerTwoRoutes = routeFaker.Generate(3).ToArray();
        var providerTwo = Substitute.For<IProviderClient>();
        providerTwo.SearchAsync(searchRequest, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(providerTwoRoutes));
        var providers = new[] { providerOne, providerTwo };

        var clock = Substitute.For<IClock>();
        clock.GetNow().Returns(DateTime.Now);

        var cacheProvider = Substitute.For<IEasyCachingProvider>();

        var searchService = new SearchService(
            Substitute.For<ILogger<SearchService>>(),
            Substitute.For<IOptions<SearchConfig>>(),
            Substitute.For<IClock>(),
            cacheProvider,
            providers);

        // Act.
        var response = await searchService.SearchAsync(searchRequest, CancellationToken.None);

        // Assert.
        response.Routes.Should().HaveCount(providerOneRoutes.Length + providerTwoRoutes.Length)
            .And.BeEquivalentTo(providerOneRoutes.Concat(providerTwoRoutes));
    }

    // private Route[] GenerateRoutes(int count)
    private Faker<Route> CreateRouteFaker(string origin, string destination, DateTime originDateTime) =>
        new Faker<Route>()
            .RuleFor(x => x.Id, Guid.NewGuid)
            .RuleFor(x => x.Origin, origin)
            .RuleFor(x => x.Destination, destination)
            .RuleFor(x => x.OriginDateTime, originDateTime)
            .RuleFor(x => x.DestinationDateTime, (f, x) => f.Date.Soon(1, x.OriginDateTime))
            .RuleFor(x => x.Price, f => f.Finance.Amount())
            .RuleFor(x => x.TimeLimit, f => f.Date.Soon());

    private Faker<CacheValue<T>> CreateCacheValueFaker<T>(Faker<T> faker) where T : class => new Faker<CacheValue<T>>()
        .RuleFor(x => x.HasValue, true)
        .RuleFor(x => x.IsNull, false)
        .RuleFor(x => x.Value, faker.Generate());

    private static IDictionary<string, CacheValue<Guid[]>> CreateNonEmptyIdsCacheValueDictionary() =>
        new Dictionary<string, CacheValue<Guid[]>>
        {
            { "123", new CacheValue<Guid[]>( new []{ Guid.NewGuid() }, true) }
        };
}