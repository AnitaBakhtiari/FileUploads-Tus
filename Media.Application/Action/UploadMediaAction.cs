using Context.Actions;
using Core.Provider;
using Media.Application.Task;
using tusdotnet.ExternalMiddleware.EndpointRouting;
using tusdotnet.Models;

namespace Media.Application.Action;

public class UploadMediaAction : Action2<Task<bool>, CreateContext, CancellationToken>
{
    public override async Task<bool> Execute(CreateContext context, CancellationToken cancellation)
    {
        var errors =
            ServicesCall.Call<ValidateMetadataMediaTask, List<string>, IDictionary<string, Metadata>>(context.Metadata);
        if (errors.Count > 0)
            //return new BadRequestObjectResult(errors);
            return false;

        try
        {
            var Storage =
                (StorageService<ITusConfigurator>) ServicesCall.GetServiceWithoutInterface(
                    typeof(StorageService<ITusConfigurator>));
            await Storage.Create(context, cancellation).ConfigureAwait(false);
            ServicesCall.Call<AddMediaRepositoryTask, bool, string, string, string, string, string, string>(
                context.FileId, "", context.UserId, context.Shares, context.UserName, context.Uri);

            return true;
        }
        catch (Exception ex)
        {
            //throw new BadRequestException(ex.Message);
            return false;
        }
    }
}