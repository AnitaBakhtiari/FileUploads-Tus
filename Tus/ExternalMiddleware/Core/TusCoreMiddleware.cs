using Microsoft.AspNetCore.Http;
using tusdotnet.ExternalMiddleware.Core;
using tusdotnet.Models;

// ReSharper disable once CheckNamespace
namespace tusdotnet;

/// <summary>
///     Processes tus.io requests for ASP.NET Core.
/// </summary>
public class TusCoreMiddleware
{
    private readonly Func<HttpContext, Task<DefaultTusConfiguration>> _configFactory;
    private readonly RequestDelegate _next;

    /// <summary>Creates a new instance of TusCoreMiddleware.</summary>
    /// <param name="next"></param>
    /// <param name="configFactory"></param>
    public TusCoreMiddleware(RequestDelegate next, Func<HttpContext, Task<DefaultTusConfiguration>> configFactory)
    {
        _next = next;
        _configFactory = configFactory;
    }

    /// <summary>
    ///     Handles the tus.io request.
    /// </summary>
    /// <param name="context">The HttpContext</param>
    /// <returns></returns>
    public async Task Invoke(HttpContext context)
    {
        var config = await _configFactory(context);

        if (config == null)
        {
            await _next(context);
            return;
        }

        var requestUri = ContextAdapterBuilder.GetRequestUri(context);

        if (!TusProtocolHandlerIntentBased.RequestIsForTusEndpoint(requestUri, config))
        {
            await _next(context);
            return;
        }

        var handled =
            await TusProtocolHandlerIntentBased.Invoke(ContextAdapterBuilder.FromHttpContext(context, config));

        if (handled == ResultType.ContinueExecution) await _next(context);
    }
}