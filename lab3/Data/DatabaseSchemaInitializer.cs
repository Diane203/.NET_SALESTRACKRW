using MySqlConnector;

namespace lab3.Data;

public static class DatabaseSchemaInitializer
{
    public static async Task EnsureAsync(MySqlConnectionFactory connectionFactory)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync();
        await using var check = connection.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'sales' AND column_name = 'sale_type'";
        var exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;
        if (exists) return;

        await using var alter = connection.CreateCommand();
        alter.CommandText = "ALTER TABLE sales ADD COLUMN sale_type ENUM('Retail', 'Wholesale') NOT NULL DEFAULT 'Retail' AFTER user_id";
        await alter.ExecuteNonQueryAsync();
    }
}
