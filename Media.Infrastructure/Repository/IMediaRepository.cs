using Media.Domain;
using Media.Infrastructure.ViewModel;

namespace Media.Infrastructure.Repository;

public interface IMediaRepository
{
    bool CheckUserValidation(string userId, string uri);
    bool AddMedia(MediaModel model);
}

public class MediaRepository : IMediaRepository
{
    private readonly Domain.Context _context;

    public MediaRepository(Domain.Context context)
    {
        _context = context;
    }

    public bool AddMedia(MediaModel model)
    {
        try
        {
            _context.Medias.Add(new Domain.Entities.Media
            {
                UserId = model.UserId,
                FileId = model.FileId,
                Shares = model.Shares,
                Size = model.Size,
                URI = model.URI,
                UserName = model.UserName
            });

            _context.SaveChanges();
            return true;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public bool CheckUserValidation(string userId, string uri)
    {
        return _context.Medias.Any(a => a.FileId == uri && (a.Shares.Contains(userId) || a.UserId == userId));
    }
}