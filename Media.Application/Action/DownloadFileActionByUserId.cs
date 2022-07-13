using System.IdentityModel.Tokens.Jwt;
using Context.Actions;
using Context.Extensions;
using Core.Provider;
using Media.Application.Task;
using Microsoft.AspNetCore.Http;

namespace Media.Application.Action;

public class DownloadFileActionByUserId : Action2<Task<bool>, HttpContext, string>
{
    public override async Task<bool> Execute(HttpContext context, string tusFileId)
    {
        var accessor = ServicesCall.GetService<IHttpContextAccessor>();
        var userId = accessor.GetUserId();

        var validate = ServicesCall.Call<DownloadFileTask, bool, string, string>(userId, tusFileId);
        if (!validate) return false;

        await ServicesCall.CallAsync<DownloadFileAction, bool, HttpContext, string>(context, tusFileId);
        return true;
    }
}