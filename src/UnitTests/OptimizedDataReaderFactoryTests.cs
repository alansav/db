using Moq;
using System.Data;
using Xunit;

namespace Savage.Db
{
    public class OptimizedDataReaderFactoryTests
    {
        [Fact]
        public void GetOptimizedDataReader_returns_OptimizedDataReader()
        {
            var mockDataReader = new Mock<IDataReader>();
            var sut = new OptimizedDataReaderFactory();

            var reader = sut.GetOptimizedDataReader(mockDataReader.Object);
            reader.GetString("test");

            mockDataReader.Verify(x => x.GetOrdinal("test"), Times.Once);
        }
    }
}
