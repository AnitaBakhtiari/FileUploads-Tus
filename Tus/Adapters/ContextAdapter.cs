using Microsoft.AspNetCore.Http;
using tusdotnet.ExternalMiddleware.EndpointRouting;
using tusdotnet.Models;
#if endpointrouting
using tusdotnet.ExternalMiddleware.EndpointRouting;
#endif
#if netfull
using Microsoft.Owin;
#endif

namespace tusdotnet.Adapters
{
    /// <summary>
    ///     Context adapter that handles different pipeline contexts.
    /// </summary>
    internal sealed class ContextAdapter
    {
        public RequestAdapter Request { get; set; }

        public ResponseAdapter Response { get; set; }

        public DefaultTusConfiguration Configuration { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public HttpContext HttpContext { get; set; }

        public EndpointOptions EndpointOptions { get; set; }
    }
}