using System.Text;
using Context.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using tusdotnet.ExternalMiddleware.EndpointRouting;
using tusdotnet.Interfaces;
using tusdotnet.Models;

namespace Media.Application.Action;

public class DownloadFileAction : Action2<Task<bool>, HttpContext, string>
{
    public override async Task<bool> Execute(HttpContext context, string fileId)
    {
        var configurator = context.RequestServices.GetRequiredService<ITusConfigurator>();
        var config = await configurator.Configure(context);

        if (!(config.Store is ITusReadableStore store)) return false;


        var fileId1 = (string) context.Request.RouteValues["fileId"];

        var file = await store.GetFileAsync(fileId, context.RequestAborted);

        if (file == null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync($"File with id {fileId} was not found.", context.RequestAborted);
            return false;
        }

        var fileStream = await file.GetContentAsync(context.RequestAborted);
        var metadata = await file.GetMetadataAsync(context.RequestAborted);

        context.Response.ContentType = GetContentTypeOrDefault(metadata);
        context.Response.ContentLength = fileStream.Length;

        if (metadata.TryGetValue("filename", out var nameMeta))
            context.Response.Headers.Add("Content-Disposition",
                new[] {$"attachment; filename=\"{nameMeta.GetString(Encoding.UTF8)}\""});

        using (fileStream)
        {
            await fileStream.CopyToAsync(context.Response.Body, 81920, context.RequestAborted);
        }

        return true;
    }

    private static string GetContentTypeOrDefault(Dictionary<string, Metadata> metadata)
    {
        if (metadata.TryGetValue("contentType", out var contentType)) return contentType.GetString(Encoding.UTF8);

        return "application/octet-stream";
    }
}