using Microsoft.EntityFrameworkCore;

namespace Media.Domain;

public class Context : DbContext
{
    public Context(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Entities.Media> Medias { get; set; }
}