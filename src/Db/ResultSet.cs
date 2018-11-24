namespace Savage.Db
{
    public interface IResultSet
    {
    }

    public interface IAffectedRowsResultSet : IResultSet
    {
        int AffectedRows { get; }
    }

    public class AffectedRowsResultSet : IAffectedRowsResultSet
    {
        public AffectedRowsResultSet(int affectedRows)
        {
            AffectedRows = affectedRows;
        }

        public int AffectedRows { get; private set; }
    }
}
