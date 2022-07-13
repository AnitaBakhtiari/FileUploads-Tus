using tusdotnet.Interfaces;
using tusdotnet.Models.Expiration;

namespace tusdotnet.ExternalMiddleware.EndpointRouting;

public class EndpointOptions
{
    private DateTimeOffset? _systemTime;
    public ITusStore Store { get; set; }

    public ExpirationBase Expiration { get; set; }

    internal void MockSystemTime(DateTimeOffset systemTime)
    {
        _systemTime = systemTime;
    }

    internal DateTimeOffset GetSystemTime()
    {
        return _systemTime ?? DateTimeOffset.UtcNow;
    }
}