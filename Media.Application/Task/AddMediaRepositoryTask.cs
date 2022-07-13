using DataCore.Tasks;
using Media.Infrastructure.Repository;
using Media.Infrastructure.ViewModel;

namespace Media.Application.Task;

public class
    AddMediaRepositoryTask : RepositoryTask6<IMediaRepository, bool, string, string, string, string, string, string>
{
    public override bool Execute(string fileTusId, string size, string userId, string shares, string userName,
        string uri)
    {
        return GetRepository().AddMedia(new MediaModel
        {
            FileId = fileTusId,
            Size = size,
            UserId = userId,
            Shares = shares,
            UserName = userName,
            URI = uri
        });
    }
}