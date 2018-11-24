using System.Data;

namespace Savage.Db
{
    public interface ICommandBuilder<CommandBuilderParameters>
        where CommandBuilderParameters : ICommandBuilderParameters
    {
        IDbCommand BuildCommand(CommandBuilderParameters parameters);
    }
}
