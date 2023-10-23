namespace MixVel.AppServices;

public enum AvailabilityStrategy
{
    None,
    Any,
    All
}

public class SearchConfig
{
    public AvailabilityStrategy AvailabilityStrategy { get; set; }
}