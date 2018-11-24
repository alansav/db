namespace Savage.Db
{
    public interface ISessionProvider
    {
        ISession NewSession();
    }

    public class SessionProvider : ISessionProvider
    {
        private readonly IDbConnectionProvider _connectionProvider;
        public SessionProvider(IDbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public ISession NewSession()
        {
            return new Session(_connectionProvider.GetDbConnection());
        }
    }
}
