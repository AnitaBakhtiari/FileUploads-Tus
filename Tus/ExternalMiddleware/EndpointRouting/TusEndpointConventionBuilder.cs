using Microsoft.AspNetCore.Builder;

namespace tusdotnet.ExternalMiddleware.EndpointRouting;

public class TusEndpointConventionBuilder : IEndpointConventionBuilder
{
    public void Add(Action<EndpointBuilder> convention)
    {
        // Do nothing for now
    }
}