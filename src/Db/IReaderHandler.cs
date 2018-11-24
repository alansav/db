using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Savage.Db
{
    public interface IReaderHandler<ResultSet> where ResultSet : IResultSet
    {
        Task<ResultSet> Handle(IDataReader reader, CancellationToken cancellationToken);
    }
}
