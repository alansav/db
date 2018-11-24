using Moq;
using Xunit;

namespace Savage.Db
{
    public class SessionProviderTests
    {
        [Fact]
        public void NewSession_returns_Session()
        {
            var mockConnectionProvider = new Mock<IDbConnectionProvider>();

            var sut = new SessionProvider(mockConnectionProvider.Object);

            var session = sut.NewSession();
            Assert.IsType<Session>(session);
            mockConnectionProvider.Verify(x => x.GetDbConnection(), Times.Once);
        }
    }
}
