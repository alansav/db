using Savage.Data;
using System.Data;

namespace Savage.Db
{
    public interface IOptimizedDataReaderFactory
    {
        IOptimizedDataReader GetOptimizedDataReader(IDataReader dataReader);
    }

    public class OptimizedDataReaderFactory : IOptimizedDataReaderFactory
    {
        public IOptimizedDataReader GetOptimizedDataReader(IDataReader dataReader)
        {
            return new OptimizedDataReader(dataReader);
        }
    }
}
