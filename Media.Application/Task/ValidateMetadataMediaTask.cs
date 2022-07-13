using Context.Tasks;
using tusdotnet.Models;

namespace Media.Application.Task;

public class ValidateMetadataMediaTask : Task1<List<string>, IDictionary<string, Metadata>>
{
    public override List<string> Execute(IDictionary<string, Metadata> metadata)
    {
        var errors = new List<string>();

        if (!metadata.ContainsKey("filename") || metadata["filename"].HasEmptyValue)
            errors.Add("name metadata must be specified.");

        if (!metadata.ContainsKey("filetype") || metadata["filetype"].HasEmptyValue)
            errors.Add("contentType metadata must be specified.");

        return errors;
    }
}