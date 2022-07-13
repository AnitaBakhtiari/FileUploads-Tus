using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using tusdotnet.Adapters;
using tusdotnet.Constants;
using tusdotnet.ExternalMiddleware.Core;
using tusdotnet.IntentHandlers;
using tusdotnet.Models;
using tusdotnet.Parsers;

namespace tusdotnet.ExternalMiddleware.EndpointRouting;

internal class TusProtocolHandlerEndpointBased<TController, TConfigurator>
    where TController : TusController<TConfigurator>
    where TConfigurator : ITusConfigurator
{
    internal async Task Invoke(HttpContext context)
    {
        var configurator = (ITusConfigurator) context.RequestServices.GetRequiredService<TConfigurator>();
        var options = await configurator.Configure(context);

        var controller = (TusController<TConfigurator>) context.RequestServices.GetRequiredService<TController>();

        var contextAdapter = CreateFakeContextAdapter(context, options);
        var responseStream = new MemoryStream();
        var responseHeaders = new Dictionary<string, string>();
        HttpStatusCode? responseStatus = null;


        contextAdapter.Response = new ResponseAdapter
        {
            Body = responseStream,
            SetHeader = (key, value) => responseHeaders[key] = value,
            SetStatus = status => responseStatus = status
        };

        var intentHandler = IntentAnalyzer.DetermineIntent(contextAdapter);

        if (intentHandler == IntentHandler.NotApplicable)
        {
            // Cannot determine intent so return not found.
            context.Response.StatusCode = 404;
            return;
        }

        var valid = await intentHandler.Validate();

        if (!valid)
        {
            // TODO: Optimize as there is not much worth in writing to a stream and then piping it to the response.
            context.Response.StatusCode = (int) responseStatus.Value;
            responseStream.Seek(0, SeekOrigin.Begin);
            await context.Response.BodyWriter.WriteAsync(responseStream.GetBuffer(), context.RequestAborted);

            return;
        }

        IActionResult result = null;
        IDictionary<string, string> headers = null;


        switch (intentHandler)
        {
            case CreateFileHandler c:
                (result, headers) = await HandleCreate(context, controller);
                break;
            case WriteFileHandler w:
                (result, headers) = await HandleWriteFile(context, controller);
                break;
            case GetFileInfoHandler f:
                (result, headers) = await HandleGetFileInfo(context, await controller.Storage.GetStore());
                break;
        }

        await context.Respond(result, headers);
    }

    private async Task<(IActionResult result, IDictionary<string, string> headers)> HandleGetFileInfo(
        HttpContext context, StoreAdapter store)
    {
        var fileId = (string) context.GetRouteValue("TusFileId");

        var result = new Dictionary<string, string>
        {
            {HeaderConstants.TusResumable, HeaderConstants.TusResumableValue},
            {HeaderConstants.CacheControl, HeaderConstants.NoStore}
        };

        var uploadMetadata = store.Extensions.Creation
            ? await store.GetUploadMetadataAsync(fileId, context.RequestAborted)
            : null;
        if (!string.IsNullOrEmpty(uploadMetadata)) result.Add(HeaderConstants.UploadMetadata, uploadMetadata);

        var uploadLength = await store.GetUploadLengthAsync(fileId, context.RequestAborted);

        if (uploadLength != null) result.Add(HeaderConstants.UploadLength, uploadLength.Value.ToString());
        //else if (context.Configuration.Store is ITusCreationDeferLengthStore)
        //{
        //    context.Response.SetHeader(HeaderConstants.UploadDeferLength, "1");
        //}

        var uploadOffset = await store.GetUploadOffsetAsync(fileId, context.RequestAborted);

        //FileConcat uploadConcat = null;
        var addUploadOffset = true;
        //if (Context.Configuration.Store is ITusConcatenationStore tusConcatStore)
        //{
        //    uploadConcat = await tusConcatStore.GetUploadConcatAsync(Request.FileId, CancellationToken);

        //    // Only add Upload-Offset to final files if they are complete.
        //    if (uploadConcat is FileConcatFinal && uploadLength != uploadOffset)
        //    {
        //        addUploadOffset = false;
        //    }
        //}

        if (addUploadOffset) result.Add(HeaderConstants.UploadOffset, uploadOffset.ToString());

        //if (uploadConcat != null)
        //{
        //    (uploadConcat as FileConcatFinal)?.AddUrlPathToFiles(Context.Configuration.UrlPath);
        //    Response.SetHeader(HeaderConstants.UploadConcat, uploadConcat.GetHeader());
        //}

        return (new NoContentResult(), result);
    }

    private async Task<(IActionResult content, IDictionary<string, string> headers)> HandleWriteFile(
        HttpContext context, TusController<TConfigurator> controller)
    {
        //private Task WriteUploadLengthIfDefered()
        //{
        //    var uploadLenghtHeader = Request.GetHeader(HeaderConstants.UploadLength);
        //    if (uploadLenghtHeader != null && Store is ITusCreationDeferLengthStore creationDeferLengthStore)
        //    {
        //        return creationDeferLengthStore.SetUploadLengthAsync(Request.FileId, long.Parse(uploadLenghtHeader), Context.CancellationToken);
        //    }

        //    return TaskHelper.Completed;
        //}

        var writeContext = new WriteContext
        {
            FileId = (string) context.GetRouteValue("TusFileId"),
            // Callback to later support trailing checksum headers
            GetChecksumProvidedByClient = () => GetChecksumFromContext(context),
            RequestStream = context.Request.Body,
            UploadOffset = long.Parse(context.Request.Headers["Upload-Offset"].First())
        };

        await controller.Write(writeContext, context.RequestAborted);

        if (writeContext.ClientDisconnectedDuringRead) return (new OkResult(), null);

        if (writeContext.IsComplete && !writeContext.IsPartialFile)
            await controller.FileCompleted(new FileCompletedContext {FileId = writeContext.FileId},
                context.RequestAborted);

        return (new NoContentResult(), GetCreateHeaders(writeContext.FileExpires, writeContext.UploadOffset));
    }

    private Checksum GetChecksumFromContext(HttpContext context)
    {
        var header = context.Request.Headers["Upload-Checksum"].FirstOrDefault();

        return header != null ? new Checksum(header) : null;
    }

    private async Task<(IActionResult content, IDictionary<string, string> headers)> HandleCreate(HttpContext context,
        TusController<TConfigurator> controller)
    {
        //if (!await controller.AuthorizeForAction(context, nameof(controller.Create)))
        //    return (new ForbidResult(), null);

        // TODO: Replace with typed headers
        var metadata = context.Request.Headers["Upload-Metadata"].FirstOrDefault();
        var uploadLength = context.Request.Headers["Upload-Length"].FirstOrDefault();
        var shares = "1/2/3/4/5/"; /*context.Request.Headers["Shares"].FirstOrDefault();*/
        //var authentication = context.Request.Headers["Authorization"].ToString();
        var authentication =
            @"Bearer eyJhbGciOiJSUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICJ6dWg0MU04T2FaamNvYjlNb0gtcE1Hc2EzSFJIYUE2djdNTWx0M2E1RU1nIn0.eyJleHAiOjE2NTU2MjUzMTksImlhdCI6MTY1NTYyNTI1OSwiYXV0aF90aW1lIjoxNjU1NjIzODA4LCJqdGkiOiI2OGRiNTVlNy02YTE5LTQxNWItOTQ5Zi0zZjJhNmIwOTFkMzAiLCJpc3MiOiJodHRwOi8vbG9jYWxob3N0OjgwODAvYXV0aC9yZWFsbXMvbWFzdGVyIiwiYXVkIjpbIm1hc3Rlci1yZWFsbSIsImFjY291bnQiXSwic3ViIjoiYWYwYjM3ZjMtZDkwNy00ZDZiLWFjMjItYWM0YjYyMWFlMTBlIiwidHlwIjoiQmVhcmVyIiwiYXpwIjoiZGVtby1hcHAiLCJzZXNzaW9uX3N0YXRlIjoiYmY1ZWU0MzEtZWU5YS00OTI4LThhMWEtZTgzODljNmYyM2U2IiwiYWNyIjoiMCIsInJlYWxtX2FjY2VzcyI6eyJyb2xlcyI6WyJjcmVhdGUtZmlsZSIsImNyZWF0ZS1yZWFsbSIsImRlZmF1bHQtcm9sZXMtbWFzdGVyIiwib2ZmbGluZV9hY2Nlc3MiLCJhZG1pbiIsInVtYV9hdXRob3JpemF0aW9uIl19LCJyZXNvdXJjZV9hY2Nlc3MiOnsiZGVtby1hcHAiOnsicm9sZXMiOlsidW1hX3Byb3RlY3Rpb24iXX0sIm1hc3Rlci1yZWFsbSI6eyJyb2xlcyI6WyJ2aWV3LWlkZW50aXR5LXByb3ZpZGVycyIsInZpZXctcmVhbG0iLCJtYW5hZ2UtaWRlbnRpdHktcHJvdmlkZXJzIiwiaW1wZXJzb25hdGlvbiIsImNyZWF0ZS1jbGllbnQiLCJtYW5hZ2UtdXNlcnMiLCJxdWVyeS1yZWFsbXMiLCJ2aWV3LWF1dGhvcml6YXRpb24iLCJxdWVyeS1jbGllbnRzIiwicXVlcnktdXNlcnMiLCJtYW5hZ2UtZXZlbnRzIiwibWFuYWdlLXJlYWxtIiwidmlldy1ldmVudHMiLCJ2aWV3LXVzZXJzIiwidmlldy1jbGllbnRzIiwibWFuYWdlLWF1dGhvcml6YXRpb24iLCJtYW5hZ2UtY2xpZW50cyIsInF1ZXJ5LWdyb3VwcyJdfSwiYWNjb3VudCI6eyJyb2xlcyI6WyJtYW5hZ2UtYWNjb3VudCIsIm1hbmFnZS1hY2NvdW50LWxpbmtzIiwidmlldy1wcm9maWxlIl19fSwic2NvcGUiOiJwcm9maWxlIGVtYWlsIiwic2lkIjoiYmY1ZWU0MzEtZWU5YS00OTI4LThhMWEtZTgzODljNmYyM2U2IiwiZW1haWxfdmVyaWZpZWQiOmZhbHNlLCJyb2xlcyI6WyJjcmVhdGUtZmlsZSIsImNyZWF0ZS1yZWFsbSIsImRlZmF1bHQtcm9sZXMtbWFzdGVyIiwib2ZmbGluZV9hY2Nlc3MiLCJhZG1pbiIsInVtYV9hdXRob3JpemF0aW9uIiwidW1hX3Byb3RlY3Rpb24iLCJ2aWV3LWlkZW50aXR5LXByb3ZpZGVycyIsInZpZXctcmVhbG0iLCJtYW5hZ2UtaWRlbnRpdHktcHJvdmlkZXJzIiwiaW1wZXJzb25hdGlvbiIsImNyZWF0ZS1jbGllbnQiLCJtYW5hZ2UtdXNlcnMiLCJxdWVyeS1yZWFsbXMiLCJ2aWV3LWF1dGhvcml6YXRpb24iLCJxdWVyeS1jbGllbnRzIiwicXVlcnktdXNlcnMiLCJtYW5hZ2UtZXZlbnRzIiwibWFuYWdlLXJlYWxtIiwidmlldy1ldmVudHMiLCJ2aWV3LXVzZXJzIiwidmlldy1jbGllbnRzIiwibWFuYWdlLWF1dGhvcml6YXRpb24iLCJtYW5hZ2UtY2xpZW50cyIsInF1ZXJ5LWdyb3VwcyIsIm1hbmFnZS1hY2NvdW50IiwibWFuYWdlLWFjY291bnQtbGlua3MiLCJ2aWV3LXByb2ZpbGUiXSwicHJlZmVycmVkX3VzZXJuYW1lIjoiYWRtaW4iLCJ1c2VyTmFtZSI6IjA5MTI5NDUwODc5IiwiYWdlIjoiMjMifQ.e04swMrX26p4ajvDfsS95kn68lOdJvKsIi27wzExltQ16enpbEb7h_WY9p87ZpC1fkyvmIJcuSgmIxzj1PabPUxD-B6XbuSTi5uyDCR0voxbt8knX-p6iXw4sC47sAUA2u6qS5Q8NiKNBYbsHysoGFWbEZsdf0EhsOML8ufQxIHFqxy4zKf_fkbRmsZfieX_CexxoyjJQ1jvyWANj7-qieKr-uCRvudmvW4mIG-N-np0bUCeo3K-6JTCUyynNZQruw3KiJjxhthL8zFmPcq26FDXKD5FDseKAoMoUtm1Hfvmyt0dxe8bkDFl9fqgmRbGCMoUeeZPs7N_-AgifqyQ6Q";
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadToken(authentication.Replace("Bearer ", "")) as JwtSecurityToken;
        var userId = token.Claims.First(claim => claim.Type == "sub").Value;
        var userName = token.Claims.First(claim => claim.Type == "userName").Value;

        var createContext = new CreateContext
        {
            UploadMetadata = metadata,
            Metadata = MetadataParser.ParseAndValidate(MetadataParsingStrategy.AllowEmptyValues, metadata).Metadata,
            UploadLength = long.Parse(uploadLength),
            Shares = shares,
            UserId = userId,
            Path = context.Request.Path,
            UserName = userName
        };

        var result = await controller.Create(createContext, context.RequestAborted);

        if (result is not OkResult)
            return (result, null);

        var isEmptyFile = createContext.UploadLength == 0;

        if (isEmptyFile)
        {
            result = await controller.FileCompleted(new FileCompletedContext {FileId = createContext.FileId},
                context.RequestAborted);

            if (result is not OkResult)
                return (result, null);
        }

        var createResult =
            new CreatedResult($"{context.Request.GetDisplayUrl().TrimEnd('/')}/{createContext.FileId}", null);

        return (createResult, GetCreateHeaders(createContext.FileExpires, createContext.UploadOffset));
    }

    private Dictionary<string, string> GetCreateHeaders(DateTimeOffset? expires, long? uploadOffset)
    {
        var result = new Dictionary<string, string>();
        if (expires != null) result.Add(HeaderConstants.UploadExpires, expires.Value.ToString("R"));

        if (uploadOffset != null) result.Add(HeaderConstants.UploadOffset, uploadOffset.Value.ToString());

        result.Add(HeaderConstants.TusResumable, HeaderConstants.TusResumableValue);

        return result;
    }

    private ContextAdapter CreateFakeContextAdapter(HttpContext context, EndpointOptions options)
    {
        var urlPath = (string) context.GetRouteValue("TusFileId");

        if (string.IsNullOrWhiteSpace(urlPath))
        {
            urlPath = context.Request.Path;
        }
        else
        {
            var span = context.Request.Path.ToString().TrimEnd('/').AsSpan();
            urlPath = span.Slice(0, span.LastIndexOf('/')).ToString();
        }

        var config = new DefaultTusConfiguration
        {
            Expiration = options.Expiration,
            Store = options.Store,
            UrlPath = urlPath
        };

        var adapter = ContextAdapterBuilder.FromHttpContext(context, config);
        adapter.EndpointOptions = options;

        return adapter;
    }
}