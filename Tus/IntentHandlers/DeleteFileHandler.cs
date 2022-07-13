﻿using System.Net;
using tusdotnet.Adapters;
using tusdotnet.Constants;
using tusdotnet.Helpers;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using tusdotnet.Validation;
using tusdotnet.Validation.Requirements;

namespace tusdotnet.IntentHandlers;
/* 
* When receiving a DELETE request for an existing upload the Server SHOULD free associated resources and MUST 
* respond with the 204 No Content status confirming that the upload was terminated. 
* For all future requests to this URL the Server SHOULD respond with the 404 Not Found or 410 Gone status.
*/

internal class DeleteFileHandler : IntentHandler
{
    private readonly ITusTerminationStore _terminationStore;

    public DeleteFileHandler(ContextAdapter context, ITusTerminationStore terminationStore)
        : base(context, IntentType.DeleteFile, LockType.RequiresLock)
    {
        _terminationStore = terminationStore;
    }

    internal override Requirement[] Requires => new Requirement[]
    {
        new FileExist(),
        new FileHasNotExpired()
    };

    internal override async Task Invoke()
    {
        if (await EventHelper.Validate<BeforeDeleteContext>(Context) == ResultType.StopExecution) return;

        await _terminationStore.DeleteFileAsync(Request.FileId, CancellationToken);

        await EventHelper.Notify<DeleteCompleteContext>(Context);

        Response.SetStatus(HttpStatusCode.NoContent);
        Response.SetHeader(HeaderConstants.TusResumable, HeaderConstants.TusResumableValue);
    }
}