using Microsoft.AspNetCore.Http;

namespace tusdotnet.ExternalMiddleware.EndpointRouting;

public interface ITusConfigurator
{
    Task<EndpointOptions> Configure(HttpContext context);
}