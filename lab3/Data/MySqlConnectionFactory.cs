using MySqlConnector;

namespace lab3.Data;

public sealed class MySqlConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(DatabaseSettings settings)
    {
        _connectionString = settings.ConnectionString;
    }

    public MySqlConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    public async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = CreateConnection();
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
