using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Savage.Db
{
    public interface ISession : IDisposable
    {
        Task OpenConnection(CancellationToken cancellationToken);
        Task BeginTransaction(CancellationToken cancellationToken);
        void CommitTransaction();
        void RollbackTransaction();

        Task<ResultSet> ExecuteReaderAsync<CommandBuilderParameters, ResultSet>(ICommandBuilder<CommandBuilderParameters> commandBuilder, CommandBuilderParameters parameters, IReaderHandler<ResultSet> readerHandler, CancellationToken cancellationToken)
            where CommandBuilderParameters : ICommandBuilderParameters
            where ResultSet : IResultSet;
        Task<IAffectedRowsResultSet> ExecuteNonQueryAsync<CommandBuilderParameters>(ICommandBuilder<CommandBuilderParameters> commandBuilder, CommandBuilderParameters parameters, CancellationToken cancellationToken)
            where CommandBuilderParameters : ICommandBuilderParameters;
        Task<T> ExecuteScalarAsync<CommandBuilderParameters, T>(ICommandBuilder<CommandBuilderParameters> commandBuilder, CommandBuilderParameters parameters, CancellationToken cancellationToken)
            where CommandBuilderParameters : ICommandBuilderParameters;
    }

    public class Session : ISession
    {
        private readonly IDbConnection _connection;
        private IDbTransaction _transaction;

        public Session(IDbConnection connection)
        {
            _connection = connection;
        }

        public void CommitTransaction()
        {
            _transaction.Commit();
        }

        public void RollbackTransaction()
        {
            _transaction.Rollback();
        }

        public async Task OpenConnection(CancellationToken cancellationToken)
        {
            await Task.Run(() => _connection.Open(), cancellationToken).ConfigureAwait(false);
        }

        public async Task BeginTransaction(CancellationToken cancellationToken)
        {
            await OpenConnectionIfNotAlreadyOpen(cancellationToken);
            _transaction = _connection.BeginTransaction();
        }

        public async Task<ResultSet> ExecuteReaderAsync<CommandBuilderParameters, ResultSet>(ICommandBuilder<CommandBuilderParameters> commandBuilder, CommandBuilderParameters parameters, IReaderHandler<ResultSet> readerHandler, CancellationToken cancellationToken)
            where CommandBuilderParameters : ICommandBuilderParameters
            where ResultSet : IResultSet
        {
            await OpenConnectionIfNotAlreadyOpen(cancellationToken).ConfigureAwait(false);
            var cmd = BuildCommandWithTransaction(commandBuilder, parameters);

            using (var reader = await Task.Run(() => cmd.ExecuteReader(), cancellationToken).ConfigureAwait(false))
            {
                return await readerHandler.Handle(reader, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IAffectedRowsResultSet> ExecuteNonQueryAsync<CommandBuilderParameters>(ICommandBuilder<CommandBuilderParameters> commandBuilder, CommandBuilderParameters parameters, CancellationToken cancellationToken)
            where CommandBuilderParameters : ICommandBuilderParameters
        {
            await OpenConnectionIfNotAlreadyOpen(cancellationToken).ConfigureAwait(false);
            var cmd = BuildCommandWithTransaction(commandBuilder, parameters);

            var affectedRows = await Task.Run(() => cmd.ExecuteNonQuery(), cancellationToken).ConfigureAwait(false);

            return new AffectedRowsResultSet(affectedRows);
        }

        public async Task<T> ExecuteScalarAsync<CommandBuilderParameters, T>(ICommandBuilder<CommandBuilderParameters> commandBuilder, CommandBuilderParameters parameters, CancellationToken cancellationToken)
            where CommandBuilderParameters : ICommandBuilderParameters
        {
            await OpenConnectionIfNotAlreadyOpen(cancellationToken).ConfigureAwait(false);
            var cmd = BuildCommandWithTransaction(commandBuilder, parameters);

            var result = await Task.Run(() => cmd.ExecuteScalar(), cancellationToken).ConfigureAwait(false);

            return (T)result;
        }

        public void CloseConnection()
        {
            if (_connection.State == ConnectionState.Open)
            {
                _connection.Close();
            }
        }

        public void Dispose()
        {
            CloseConnection();
            _transaction = null;
        }

        private async Task OpenConnectionIfNotAlreadyOpen(CancellationToken cancellationToken)
        {
            if (_connection.State != ConnectionState.Open)
                await OpenConnection(cancellationToken).ConfigureAwait(false);
        }

        private IDbCommand BuildCommandWithTransaction<CommandBuilderParameters>(ICommandBuilder<CommandBuilderParameters> commandBuilder, CommandBuilderParameters parameters)
            where CommandBuilderParameters : ICommandBuilderParameters
        {
            var cmd = commandBuilder.BuildCommand(parameters);
            cmd.Transaction = _transaction;
            cmd.Connection = _connection;
            
            return cmd;
        }
    }
}
