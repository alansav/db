using System.Collections.Generic;
using System.Data;

namespace Savage.Db
{
    public interface ICommandParameters
    {
        IEnumerable<IDbDataParameter> Parameters { get; }
    }
}
