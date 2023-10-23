namespace MixVel.Contracts;

public interface IProviderClient
{
    string Name { get; }
    Task<Route[]> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
}