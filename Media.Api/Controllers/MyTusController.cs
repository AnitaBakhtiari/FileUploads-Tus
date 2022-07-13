using Core.Provider;
using Media.Application.Action;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tusdotnet.ExternalMiddleware.EndpointRouting;

namespace Media.Api.Controllers;

public class MyTusController : TusController<MyTusConfigurator>
{
    private readonly ILogger<MyTusController> _logger;

    public MyTusController(StorageService<MyTusConfigurator> storage, ILogger<MyTusController> logger)
        : base(storage)
    {
        _logger = logger;
    }

    [Authorize(Policy = "create-file-policy")]
    public override async Task<IActionResult> Create(CreateContext context, CancellationToken cancellation)
    {
        await ServicesCall.CallAsync<UploadMediaAction, bool, CreateContext, CancellationToken>(context, cancellation);
        _logger.LogInformation($"File created with id {context.FileId}");
        return Ok();
    }

    public override async Task<IActionResult> Write(WriteContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Started writing file {context.FileId} at offset {context.UploadOffset}");

        var result = await base.Write(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation($"Done writing file {context.FileId}. New offset: {context.UploadOffset}");

        return result;
    }

    public override Task<IActionResult> FileCompleted(FileCompletedContext context, CancellationToken cancellation)
    {
        _logger.LogInformation($"Upload of file {context.FileId} is complete!");
        return base.FileCompleted(context, cancellation);
    }

    [Authorize]
    [HttpGet("files/{tusFileId}")]
    public override async Task<IActionResult> Download(string fileId)
    {
        var authService = HttpContext.RequestServices.GetService<IAuthorizationService>();
        if (authService != null)
        {
            var authResult = await authService.AuthorizeAsync(HttpContext.User, "create-file-policy");

            if (authResult.Succeeded)
                return Ok(await ServicesCall.CallAsync<DownloadFileAction, bool, HttpContext, string>(HttpContext,
                    fileId));
            return Ok(await ServicesCall.CallAsync<DownloadFileActionByUserId, bool, HttpContext, string>(HttpContext,
                fileId));
        }

        return BadRequest();
    }
}