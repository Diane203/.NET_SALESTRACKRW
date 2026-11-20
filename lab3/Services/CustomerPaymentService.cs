using lab3.Data;

namespace lab3.Services;

public sealed class CustomerPaymentService
{
    private readonly MySqlConnectionFactory _connectionFactory;
    public CustomerPaymentService(MySqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task RecordPaymentAsync(int customerId, long? saleId, decimal amount, string paymentMethod, int userId, string? notes)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(paymentMethod)) throw new ArgumentException("Payment method is required.", nameof(paymentMethod));
        await using var connection = await _connectionFactory.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var payment = connection.CreateCommand())
        {
            payment.Transaction = transaction;
            payment.CommandText = "INSERT INTO customer_payments (customer_id, sale_id, amount, payment_method, user_id, notes) VALUES (@customer, @sale, @amount, @method, @user, @notes)";
            payment.Parameters.AddWithValue("@customer", customerId); payment.Parameters.AddWithValue("@sale", saleId ?? (object)DBNull.Value); payment.Parameters.AddWithValue("@amount", amount); payment.Parameters.AddWithValue("@method", paymentMethod); payment.Parameters.AddWithValue("@user", userId); payment.Parameters.AddWithValue("@notes", notes ?? (object)DBNull.Value);
            await payment.ExecuteNonQueryAsync();
        }
        if (saleId.HasValue)
        {
            await using var update = connection.CreateCommand(); update.Transaction = transaction;
            update.CommandText = "UPDATE sales SET payment_status = CASE WHEN amount_received + @amount >= subtotal THEN 'Paid' ELSE 'Partially Paid' END, amount_received = amount_received + @amount WHERE sale_id = @sale AND customer_id = @customer";
            update.Parameters.AddWithValue("@amount", amount); update.Parameters.AddWithValue("@sale", saleId.Value); update.Parameters.AddWithValue("@customer", customerId); await update.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }
}
