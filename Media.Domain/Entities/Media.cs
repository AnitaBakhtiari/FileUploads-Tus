using DataSql.Model;

namespace Media.Domain.Entities;

public class Media : BaseEntity
{
    public string Shares { get; set; }
    public string URI { get; set; }
    public string FileId { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Size { get; set; }
}