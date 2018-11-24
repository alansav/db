using Xunit;

namespace Savage.Db
{
    public class AffectedRowsResultSetTests
    {
        [Fact]
        public void Constructor_sets_AffectedRows()
        {
            var expectedAffectedRows = 12345;

            var sut = new AffectedRowsResultSet(expectedAffectedRows);

            Assert.Equal(expectedAffectedRows, sut.AffectedRows);
        }
    }
}
