using System.Data;

namespace Savage.Db
{
    public interface IDbConnectionProvider
    {
        IDbConnection GetDbConnection();
    }
}
