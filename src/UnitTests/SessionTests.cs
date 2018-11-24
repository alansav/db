using Moq;
using Moq.AutoMock;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Savage.Db
{
    public class SessionTests
    {
        private readonly AutoMocker _mocker;

        public SessionTests()
        {
            _mocker = new AutoMocker();
        }

        [Fact]
        public async Task StartTransaction_OpensConnection_and_BeginsTransaction()
        {
            var mockConnection = _mocker.GetMock<IDbConnection>();

            var sut = new Session(mockConnection.Object);
            await sut.BeginTransaction(CancellationToken.None);

            mockConnection.Verify(x => x.Open(), Times.Once);
            mockConnection.Verify(x => x.BeginTransaction(), Times.Once);
        }

        [Fact]
        public async Task OpensConnection_calls_Open_on_connection()
        {
            var mockConnection = _mocker.GetMock<IDbConnection>();

            var sut = new Session(mockConnection.Object);
            await sut.BeginTransaction(CancellationToken.None);

            mockConnection.Verify(x => x.Open(), Times.Once);
        }

        [Fact]
        public async Task CommitTransaction_calls_Commit_on_transaction()
        {
            var mockConnection = _mocker.GetMock<IDbConnection>();
            var mockTransaction = _mocker.GetMock<IDbTransaction>();
            mockConnection.Setup(x => x.BeginTransaction()).Returns(mockTransaction.Object);

            var sut = new Session(mockConnection.Object);
            await sut.BeginTransaction(CancellationToken.None);
            sut.CommitTransaction();

            mockTransaction.Verify(x => x.Commit(), Times.Once);
        }

        [Fact]
        public async Task RollbackTransaction_calls_Rollback_on_transaction()
        {
            var mockConnection = _mocker.GetMock<IDbConnection>();
            var mockTransaction = _mocker.GetMock<IDbTransaction>();
            mockConnection.Setup(x => x.BeginTransaction()).Returns(mockTransaction.Object);

            var sut = new Session(mockConnection.Object);
            await sut.BeginTransaction(CancellationToken.None);
            sut.RollbackTransaction();

            mockTransaction.Verify(x => x.Rollback(), Times.Once);
        }

        [Theory]
        [InlineData(ConnectionState.Open)]
        [InlineData(ConnectionState.Closed)]
        public void CloseConnection_calls_CloseConnection_When_Expected(ConnectionState connectionState)
        {
            var mockConnection = _mocker.GetMock<IDbConnection>();
            mockConnection.Setup(x => x.State).Returns(connectionState);

            var sut = new Session(mockConnection.Object);
            sut.CloseConnection();

            var times = connectionState == ConnectionState.Open ? Times.Once() : Times.Never();
            mockConnection.Verify(x => x.Close(), times);
        }

        [Theory]
        [InlineData(ConnectionState.Open)]
        [InlineData(ConnectionState.Closed)]
        public void Dispose_calls_CloseConnection(ConnectionState connectionState)
        {
            var mockConnection = _mocker.GetMock<IDbConnection>();
            mockConnection.Setup(x => x.State).Returns(connectionState);

            var sut = new Session(mockConnection.Object);
            sut.Dispose();

            var times = connectionState == ConnectionState.Open ? Times.Once() : Times.Never();
            mockConnection.Verify(x => x.Close(), times);
        }

        [Theory]
        [InlineData(ConnectionState.Closed, true)]
        [InlineData(ConnectionState.Open, true)]
        [InlineData(ConnectionState.Closed, false)]
        [InlineData(ConnectionState.Open, false)]
        public async Task ExecuteNonQuery_executes_as_expected(ConnectionState initialConnectionState, bool useTransaction)
        {
            var parameters = new FakeCommandBuilderParameters { Id = 123 };

            var mockTransaction = _mocker.GetMock<IDbTransaction>();
            var mockConnection = _mocker.GetMock<IDbConnection>();
            mockConnection.Setup(x => x.State).Returns(initialConnectionState);
            mockConnection.Setup(x => x.Open()).Callback(() => mockConnection.Setup(x => x.State).Returns(ConnectionState.Open));
            
            var mockCommand = _mocker.GetMock<IDbCommand>();
            mockCommand.SetupSet(x => x.Connection = mockConnection.Object).Verifiable();
            mockCommand.Setup(x => x.ExecuteNonQuery()).Returns(321);

            if (useTransaction)
            {
                mockConnection.Setup(x => x.BeginTransaction()).Returns(mockTransaction.Object);
                mockCommand.SetupSet(x => x.Transaction = mockTransaction.Object).Verifiable();
            }
            
            var mockCommandBuilder = _mocker.GetMock<ICommandBuilder<FakeCommandBuilderParameters>>();
            mockCommandBuilder.Setup(x => x.BuildCommand(parameters)).Returns(mockCommand.Object);
            
            var sut = new Session(mockConnection.Object);
            if (useTransaction)
            {
                await sut.BeginTransaction(CancellationToken.None);
            }

            var result = await sut.ExecuteNonQueryAsync(mockCommandBuilder.Object, parameters, CancellationToken.None);

            Assert.Equal(321, result.AffectedRows);

            var times = initialConnectionState == ConnectionState.Open ? Times.Never() : Times.Once();
            mockConnection.Verify(x => x.Open(), times);

            mockCommandBuilder.Verify(x => x.BuildCommand(parameters), Times.Once);
            mockCommand.Verify();
            mockCommand.Verify(x => x.ExecuteNonQuery(), Times.Once());
        }

        [Theory]
        [InlineData(ConnectionState.Closed, true)]
        [InlineData(ConnectionState.Open, true)]
        [InlineData(ConnectionState.Closed, false)]
        [InlineData(ConnectionState.Open, false)]
        public async Task ExecuteScalar_executes_as_expected(ConnectionState initialConnectionState, bool useTransaction)
        {
            var parameters = new FakeCommandBuilderParameters { Id = 123 };

            var mockTransaction = _mocker.GetMock<IDbTransaction>();
            var mockConnection = _mocker.GetMock<IDbConnection>();
            mockConnection.Setup(x => x.State).Returns(initialConnectionState);
            mockConnection.Setup(x => x.Open()).Callback(() => mockConnection.Setup(x => x.State).Returns(ConnectionState.Open));

            var mockCommand = _mocker.GetMock<IDbCommand>();
            mockCommand.SetupSet(x => x.Connection = mockConnection.Object).Verifiable();
            mockCommand.Setup(x => x.ExecuteScalar()).Returns("hello");

            if (useTransaction)
            {
                mockConnection.Setup(x => x.BeginTransaction()).Returns(mockTransaction.Object);
                mockCommand.SetupSet(x => x.Transaction = mockTransaction.Object).Verifiable();
            }

            var mockCommandBuilder = _mocker.GetMock<ICommandBuilder<FakeCommandBuilderParameters>>();
            mockCommandBuilder.Setup(x => x.BuildCommand(parameters)).Returns(mockCommand.Object);

            var sut = new Session(mockConnection.Object);
            if (useTransaction)
            {
                await sut.BeginTransaction(CancellationToken.None);
            }

            var result = await sut.ExecuteScalarAsync<FakeCommandBuilderParameters, string>(mockCommandBuilder.Object, parameters, CancellationToken.None);

            Assert.Equal("hello", result);

            var times = initialConnectionState == ConnectionState.Open ? Times.Never() : Times.Once();
            mockConnection.Verify(x => x.Open(), times);

            mockCommandBuilder.Verify(x => x.BuildCommand(parameters), Times.Once);
            mockCommand.Verify();
            mockCommand.Verify(x => x.ExecuteScalar(), Times.Once());
        }

        [Theory]
        [InlineData(ConnectionState.Closed, true)]
        [InlineData(ConnectionState.Open, true)]
        [InlineData(ConnectionState.Closed, false)]
        [InlineData(ConnectionState.Open, false)]
        public async Task ExecuteReader_executes_as_expected(ConnectionState initialConnectionState, bool useTransaction)
        {
            var parameters = new FakeCommandBuilderParameters { Id = 123 };

            var mockTransaction = _mocker.GetMock<IDbTransaction>();
            var mockConnection = _mocker.GetMock<IDbConnection>();
            mockConnection.Setup(x => x.State).Returns(initialConnectionState);
            mockConnection.Setup(x => x.Open()).Callback(() => mockConnection.Setup(x => x.State).Returns(ConnectionState.Open));

            var mockCommand = _mocker.GetMock<IDbCommand>();
            mockCommand.SetupSet(x => x.Connection = mockConnection.Object).Verifiable();
            mockCommand.Setup(x => x.ExecuteScalar()).Returns("hello");

            if (useTransaction)
            {
                mockConnection.Setup(x => x.BeginTransaction()).Returns(mockTransaction.Object);
                mockCommand.SetupSet(x => x.Transaction = mockTransaction.Object).Verifiable();
            }

            var mockCommandBuilder = _mocker.GetMock<ICommandBuilder<FakeCommandBuilderParameters>>();
            mockCommandBuilder.Setup(x => x.BuildCommand(parameters)).Returns(mockCommand.Object);

            var sut = new Session(mockConnection.Object);
            if (useTransaction)
            {
                await sut.BeginTransaction(CancellationToken.None);
            }

            var readerHandler = new FakeReaderHandler();

            var result = await sut.ExecuteReaderAsync(mockCommandBuilder.Object, parameters, readerHandler, CancellationToken.None);

            Assert.Equal("handled", result.Id);

            var times = initialConnectionState == ConnectionState.Open ? Times.Never() : Times.Once();
            mockConnection.Verify(x => x.Open(), times);

            mockCommandBuilder.Verify(x => x.BuildCommand(parameters), Times.Once);
            mockCommand.Verify();
            mockCommand.Verify(x => x.ExecuteReader(), Times.Once());
        }

        public class FakeResultSet : IResultSet
        {
            public string Id { get; set; }
        }

        public class FakeReaderHandler : IReaderHandler<FakeResultSet>
        {
            public Task<FakeResultSet> Handle(IDataReader reader, CancellationToken cancellationToken)
            {
                var result = new FakeResultSet { Id = "handled" };
                return Task.FromResult(result);
            }
        }

        public class FakeCommandBuilderParameters : ICommandBuilderParameters
        {
            public int Id { get; set; }
        }
    }
}
