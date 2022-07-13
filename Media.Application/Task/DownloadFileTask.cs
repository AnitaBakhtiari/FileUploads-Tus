using DataCore.Tasks;
using Media.Infrastructure.Repository;

namespace Media.Application.Task;

public class DownloadFileTask : RepositoryTask2<IMediaRepository, bool, string, string>
{
    public override bool Execute(string param1, string param2)
    {
        return GetRepository().CheckUserValidation(param1, param2);
    }
}