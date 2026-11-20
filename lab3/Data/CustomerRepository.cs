using lab3.Models;

namespace lab3.Data;

public sealed class CustomerRepository : RepositoryBase
{
    public CustomerRepository(MySqlConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<IReadOnlyList<Customer>> SearchAsync(string? text)
    {
        var customers = new List<Customer>();
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.customer_id, c.full_name, c.telephone, c.address, c.is_active,
                   GREATEST(COALESCE(SUM(CASE WHEN s.payment_status <> 'Paid' THEN s.subtotal - s.amount_received ELSE 0 END), 0), 0) AS credit_balance
            FROM customers c
            LEFT JOIN sales s ON s.customer_id = c.customer_id
            WHERE c.is_active = TRUE AND (@search = '' OR c.full_name LIKE @pattern OR c.telephone LIKE @pattern)
            GROUP BY c.customer_id, c.full_name, c.telephone, c.address, c.is_active
            ORDER BY c.full_name
            """;
        var search = text?.Trim() ?? string.Empty; AddParameter(command, "@search", search); AddParameter(command, "@pattern", $"%{search}%");
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) customers.Add(new Customer { CustomerId = reader.GetInt32("customer_id"), FullName = reader.GetString("full_name"), Telephone = reader.IsDBNull(reader.GetOrdinal("telephone")) ? "" : reader.GetString("telephone"), Address = reader.IsDBNull(reader.GetOrdinal("address")) ? "" : reader.GetString("address"), IsActive = reader.GetBoolean("is_active"), CreditBalance = reader.GetDecimal("credit_balance") });
        return customers;
    }

    public async Task<int> CreateAsync(Customer customer)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand(); command.CommandText = "INSERT INTO customers (full_name, telephone, address) VALUES (@name, @phone, @address); SELECT LAST_INSERT_ID();";
        AddParameter(command, "@name", customer.FullName.Trim()); AddParameter(command, "@phone", customer.Telephone.Trim()); AddParameter(command, "@address", customer.Address.Trim());
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task UpdateAsync(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.FullName)) throw new ArgumentException("Customer name is required.");
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand(); command.CommandText = "UPDATE customers SET full_name = @name, telephone = @phone, address = @address WHERE customer_id = @id AND is_active = TRUE";
        AddParameter(command, "@id", customer.CustomerId); AddParameter(command, "@name", customer.FullName.Trim()); AddParameter(command, "@phone", customer.Telephone.Trim()); AddParameter(command, "@address", customer.Address.Trim());
        if (await command.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Customer was not found.");
    }

    public async Task<IReadOnlyList<CustomerPayment>> GetPaymentsAsync(int customerId)
    {
        var result = new List<CustomerPayment>();
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand(); command.CommandText = "SELECT payment_id, customer_id, sale_id, amount, payment_method, payment_date FROM customer_payments WHERE customer_id = @id ORDER BY payment_date DESC"; AddParameter(command, "@id", customerId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(new CustomerPayment { PaymentId = reader.GetInt64("payment_id"), CustomerId = reader.GetInt32("customer_id"), SaleId = reader.IsDBNull(reader.GetOrdinal("sale_id")) ? null : reader.GetInt64("sale_id"), Amount = reader.GetDecimal("amount"), PaymentMethod = reader.GetString("payment_method"), PaymentDate = reader.GetDateTime("payment_date") });
        return result;
    }
}
