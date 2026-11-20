using MySqlConnector;

namespace lab3.Data;

public abstract class RepositoryBase
{
    protected RepositoryBase(MySqlConnectionFactory connectionFactory)
    {
        ConnectionFactory = connectionFactory;
    }

    protected MySqlConnectionFactory ConnectionFactory { get; }

    protected static void AddParameter(MySqlCommand command, string name, object? value)
    {
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }
}
