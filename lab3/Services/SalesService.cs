using lab3.Data;
using lab3.Models;
using MySqlConnector;

namespace lab3.Services;

public sealed class SalesService
{
    private readonly MySqlConnectionFactory _connectionFactory;
    public SalesService(MySqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<string> CompleteSaleAsync(SaleRequest request)
    {
        if (request.Lines.Count == 0) throw new InvalidOperationException("A sale must contain at least one product.");
        if (request.Lines.Any(line => line.Quantity <= 0)) throw new ArgumentException("Quantities must be greater than zero.");
        if (request.AmountReceived < 0) throw new ArgumentException("Amount received cannot be negative.");

        await using var connection = await _connectionFactory.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        decimal total = 0;
        var transactionNumber = $"SAL-{DateTime.Now:yyyyMMddHHmmssfff}";
        try
        {
            var prices = new Dictionary<int, decimal>();
            foreach (var line in request.Lines)
            {
                await using var check = connection.CreateCommand(); check.Transaction = transaction;
                check.CommandText = "SELECT name, selling_price, stock_quantity FROM products WHERE product_id = @id AND is_active = TRUE FOR UPDATE"; check.Parameters.AddWithValue("@id", line.ProductId);
                await using var reader = await check.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) throw new InvalidOperationException("A selected product is not available.");
                var name = reader.GetString("name"); var price = reader.GetDecimal("selling_price"); var stock = reader.GetDecimal("stock_quantity");
                if (stock <= 0) throw new InvalidOperationException($"{name} is completely out of stock.");
                if (stock < line.Quantity) throw new InvalidOperationException($"Only {stock:N3} of {name} remain in stock.");
                prices[line.ProductId] = price; total += price * line.Quantity;
            }
            if (request.AmountReceived > total) throw new InvalidOperationException("Amount received cannot exceed the sale total.");
            var status = request.AmountReceived == total ? "Paid" : request.AmountReceived > 0 ? "Partially Paid" : "Credit";
            long saleId;
            await using (var sale = connection.CreateCommand())
            {
                sale.Transaction = transaction; sale.CommandText = "INSERT INTO sales (transaction_number, customer_id, user_id, sale_type, subtotal, amount_received, payment_method, payment_status, notes) VALUES (@number, @customer, @user, @saleType, @total, @received, @method, @status, @notes); SELECT LAST_INSERT_ID();";
                sale.Parameters.AddWithValue("@number", transactionNumber); sale.Parameters.AddWithValue("@customer", request.CustomerId ?? (object)DBNull.Value); sale.Parameters.AddWithValue("@user", request.UserId); sale.Parameters.AddWithValue("@saleType", request.SaleType.ToString()); sale.Parameters.AddWithValue("@total", total); sale.Parameters.AddWithValue("@received", request.AmountReceived); sale.Parameters.AddWithValue("@method", request.PaymentMethod ?? (object)DBNull.Value); sale.Parameters.AddWithValue("@status", status); sale.Parameters.AddWithValue("@notes", request.Notes ?? (object)DBNull.Value);
                saleId = Convert.ToInt64(await sale.ExecuteScalarAsync());
            }
            foreach (var line in request.Lines)
            {
                await using var item = connection.CreateCommand(); item.Transaction = transaction; item.CommandText = "INSERT INTO sale_items (sale_id, product_id, quantity, unit_price, line_total) VALUES (@sale, @product, @quantity, @price, @lineTotal)"; item.Parameters.AddWithValue("@sale", saleId); item.Parameters.AddWithValue("@product", line.ProductId); item.Parameters.AddWithValue("@quantity", line.Quantity); item.Parameters.AddWithValue("@price", prices[line.ProductId]); item.Parameters.AddWithValue("@lineTotal", prices[line.ProductId] * line.Quantity); await item.ExecuteNonQueryAsync();
                await using var stock = connection.CreateCommand(); stock.Transaction = transaction; stock.CommandText = "UPDATE products SET stock_quantity = stock_quantity - @quantity WHERE product_id = @product"; stock.Parameters.AddWithValue("@quantity", line.Quantity); stock.Parameters.AddWithValue("@product", line.ProductId); await stock.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync(); return transactionNumber;
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task UpdateSaleAsync(long saleId, SaleRequest request)
    {
        if (request.Lines.Count == 0) throw new InvalidOperationException("A sale must contain at least one product.");
        await using var connection = await _connectionFactory.OpenConnectionAsync(); await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using (var restore = connection.CreateCommand()) { restore.Transaction = transaction; restore.CommandText = "UPDATE products p INNER JOIN sale_items si ON si.product_id = p.product_id SET p.stock_quantity = p.stock_quantity + si.quantity WHERE si.sale_id = @sale"; restore.Parameters.AddWithValue("@sale", saleId); await restore.ExecuteNonQueryAsync(); }
            var prices = new Dictionary<int, decimal>(); decimal total = 0;
            foreach (var line in request.Lines)
            {
                await using var check = connection.CreateCommand(); check.Transaction = transaction; check.CommandText = "SELECT selling_price, stock_quantity FROM products WHERE product_id = @id AND is_active = TRUE FOR UPDATE"; check.Parameters.AddWithValue("@id", line.ProductId); await using var reader = await check.ExecuteReaderAsync(); if (!await reader.ReadAsync()) throw new InvalidOperationException("A selected product is not available."); var price = reader.GetDecimal("selling_price"); var stock = reader.GetDecimal("stock_quantity"); if (line.Quantity <= 0 || stock < line.Quantity) throw new InvalidOperationException("One or more requested quantities exceed available stock."); prices[line.ProductId] = price; total += price * line.Quantity;
            }
            if (request.AmountReceived < 0 || request.AmountReceived > total) throw new ArgumentException("Amount received must be between zero and the sale total.");
            var status = request.AmountReceived == total ? "Paid" : request.AmountReceived > 0 ? "Partially Paid" : "Credit";
            await ExecuteAsync(connection, transaction, "DELETE FROM customer_payments WHERE sale_id = @sale", saleId);
            await ExecuteAsync(connection, transaction, "DELETE FROM sale_items WHERE sale_id = @sale", saleId);
            await using (var update = connection.CreateCommand()) { update.Transaction = transaction; update.CommandText = "UPDATE sales SET customer_id = @customer, sale_type = @type, subtotal = @total, amount_received = @received, payment_method = @method, payment_status = @status WHERE sale_id = @sale"; update.Parameters.AddWithValue("@sale", saleId); update.Parameters.AddWithValue("@customer", request.CustomerId ?? (object)DBNull.Value); update.Parameters.AddWithValue("@type", request.SaleType.ToString()); update.Parameters.AddWithValue("@total", total); update.Parameters.AddWithValue("@received", request.AmountReceived); update.Parameters.AddWithValue("@method", request.PaymentMethod ?? (object)DBNull.Value); update.Parameters.AddWithValue("@status", status); if (await update.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Sale was not found."); }
            foreach (var line in request.Lines)
            {
                await using var item = connection.CreateCommand(); item.Transaction = transaction; item.CommandText = "INSERT INTO sale_items (sale_id, product_id, quantity, unit_price, line_total) VALUES (@sale, @product, @quantity, @price, @lineTotal)"; item.Parameters.AddWithValue("@sale", saleId); item.Parameters.AddWithValue("@product", line.ProductId); item.Parameters.AddWithValue("@quantity", line.Quantity); item.Parameters.AddWithValue("@price", prices[line.ProductId]); item.Parameters.AddWithValue("@lineTotal", prices[line.ProductId] * line.Quantity); await item.ExecuteNonQueryAsync();
                await using var stock = connection.CreateCommand(); stock.Transaction = transaction; stock.CommandText = "UPDATE products SET stock_quantity = stock_quantity - @quantity WHERE product_id = @product"; stock.Parameters.AddWithValue("@quantity", line.Quantity); stock.Parameters.AddWithValue("@product", line.ProductId); await stock.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    private static async Task ExecuteAsync(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction, string sql, long saleId)
    {
        await using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = sql; command.Parameters.AddWithValue("@sale", saleId); await command.ExecuteNonQueryAsync();
    }
}
