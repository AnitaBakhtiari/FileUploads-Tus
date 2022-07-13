using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace tusdotnet.ExternalMiddleware.EndpointRouting;

internal static class HttpContextExtensions
{
    internal static async Task Respond(this HttpContext context, IActionResult result,
        IDictionary<string, string> headers)
    {
        if (headers != null)
            foreach (var item in headers)
                context.Response.Headers[item.Key] = item.Value;

        await result.ExecuteResultAsync(new ActionContext {HttpContext = context});
    }
}