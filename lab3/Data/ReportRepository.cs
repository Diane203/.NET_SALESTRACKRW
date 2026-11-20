using lab3.Models;

namespace lab3.Data;

public sealed class ReportRepository : RepositoryBase
{
    public ReportRepository(MySqlConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<DashboardSummary> GetSummaryAsync(DateTime from, DateTime to)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand(); command.CommandText = """
            SELECT
              (SELECT COALESCE(SUM(subtotal), 0) FROM sales WHERE sale_date >= @from AND sale_date < @to) AS sales_total,
              (SELECT COUNT(*) FROM sales WHERE sale_date >= @from AND sale_date < @to) AS transaction_count,
              (SELECT COUNT(*) FROM products WHERE is_active = TRUE AND stock_quantity <= minimum_stock_level) AS low_stock_count,
              (SELECT COALESCE(SUM(subtotal - amount_received), 0) FROM sales WHERE payment_status <> 'Paid' AND sale_date >= @from AND sale_date < @to) AS outstanding_credit,
              (SELECT COUNT(*) FROM products WHERE is_active = TRUE) AS active_product_count,
              (SELECT COALESCE(SUM(stock_quantity), 0) FROM products WHERE is_active = TRUE) AS stock_units,
              (SELECT COALESCE(SUM(stock_quantity * selling_price), 0) FROM products WHERE is_active = TRUE) AS inventory_value;
            """;
        AddParameter(command, "@from", from); AddParameter(command, "@to", to);
        await using var reader = await command.ExecuteReaderAsync(); await reader.ReadAsync();
        return new DashboardSummary { SalesTotal = reader.GetDecimal("sales_total"), TransactionCount = Convert.ToInt32(reader.GetInt64("transaction_count")), LowStockCount = Convert.ToInt32(reader.GetInt64("low_stock_count")), OutstandingCredit = reader.GetDecimal("outstanding_credit"), ActiveProductCount = Convert.ToInt32(reader.GetInt64("active_product_count")), StockUnits = reader.GetDecimal("stock_units"), InventoryValue = reader.GetDecimal("inventory_value") };
    }

    public async Task UpdateSalePaymentAsync(long saleId, decimal amountReceived, string paymentMethod)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE sales SET amount_received = @received, payment_method = @method, payment_status = CASE WHEN @received = subtotal THEN 'Paid' WHEN @received > 0 THEN 'Partially Paid' ELSE 'Credit' END WHERE sale_id = @sale AND @received BETWEEN 0 AND subtotal";
        AddParameter(command, "@sale", saleId); AddParameter(command, "@received", amountReceived); AddParameter(command, "@method", paymentMethod);
        if (await command.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("The payment amount is invalid or the sale was not found.");
    }

    public async Task<IReadOnlyList<SaleEditLine>> GetSaleLinesAsync(long saleId)
    {
        var result = new List<SaleEditLine>();
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT si.product_id, p.name AS product_name, si.quantity, si.unit_price FROM sale_items si INNER JOIN products p ON p.product_id = si.product_id WHERE si.sale_id = @sale ORDER BY si.sale_item_id";
        AddParameter(command, "@sale", saleId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(new SaleEditLine { ProductId = reader.GetInt32("product_id"), ProductName = reader.GetString("product_name"), Quantity = reader.GetDecimal("quantity"), UnitPrice = reader.GetDecimal("unit_price") });
        return result;
    }

    public async Task UpdateSaleLinesAsync(long saleId, IReadOnlyList<SaleEditLine> lines)
    {
        if (lines.Count == 0) throw new InvalidOperationException("A sale must contain at least one product.");
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using (var restore = connection.CreateCommand())
            {
                restore.Transaction = transaction;
                restore.CommandText = "UPDATE products p INNER JOIN sale_items si ON si.product_id = p.product_id SET p.stock_quantity = p.stock_quantity + si.quantity WHERE si.sale_id = @sale";
                AddParameter(restore, "@sale", saleId); await restore.ExecuteNonQueryAsync();
            }

            decimal total = 0;
            foreach (var line in lines)
            {
                if (line.Quantity <= 0) throw new ArgumentException("Sale quantities must be greater than zero.");
                await using var stock = connection.CreateCommand(); stock.Transaction = transaction;
                stock.CommandText = "SELECT stock_quantity FROM products WHERE product_id = @product AND is_active = TRUE FOR UPDATE"; AddParameter(stock, "@product", line.ProductId);
                await using var reader = await stock.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) throw new InvalidOperationException("A selected product is not available.");
                var available = reader.GetDecimal("stock_quantity");
                await reader.CloseAsync();
                if (available < line.Quantity) throw new InvalidOperationException($"Only {available:N3} units of {line.ProductName} remain in stock.");
                total += line.UnitPrice * line.Quantity;
            }

            await ExecuteSaleChangeAsync(connection, transaction, "DELETE FROM sale_items WHERE sale_id = @sale", saleId);
            foreach (var line in lines)
            {
                await using var insert = connection.CreateCommand(); insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO sale_items (sale_id, product_id, quantity, unit_price, line_total) VALUES (@sale, @product, @quantity, @price, @lineTotal)";
                AddParameter(insert, "@sale", saleId); AddParameter(insert, "@product", line.ProductId); AddParameter(insert, "@quantity", line.Quantity); AddParameter(insert, "@price", line.UnitPrice); AddParameter(insert, "@lineTotal", line.UnitPrice * line.Quantity); await insert.ExecuteNonQueryAsync();
                await using var deduct = connection.CreateCommand(); deduct.Transaction = transaction;
                deduct.CommandText = "UPDATE products SET stock_quantity = stock_quantity - @quantity WHERE product_id = @product";
                AddParameter(deduct, "@quantity", line.Quantity); AddParameter(deduct, "@product", line.ProductId); await deduct.ExecuteNonQueryAsync();
            }

            await using var saleUpdate = connection.CreateCommand(); saleUpdate.Transaction = transaction;
            saleUpdate.CommandText = "UPDATE sales SET subtotal = @total, amount_received = CASE WHEN amount_received > @total THEN @total ELSE amount_received END, payment_status = CASE WHEN amount_received > @total THEN 'Paid' WHEN amount_received >= @total THEN 'Paid' WHEN amount_received > 0 THEN 'Partially Paid' ELSE 'Credit' END WHERE sale_id = @sale";
            AddParameter(saleUpdate, "@total", total); AddParameter(saleUpdate, "@sale", saleId); await saleUpdate.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task<IReadOnlyList<ReportTransaction>> GetTransactionsAsync(DateTime from, DateTime to)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT s.sale_id, s.transaction_number, s.sale_date,
                   COALESCE(c.full_name, 'Walk-in customer') AS customer_name,
                   u.full_name AS cashier_name, s.sale_type,
                   COUNT(si.sale_item_id) AS item_count, s.subtotal,
                   s.amount_received, COALESCE(s.payment_method, '') AS payment_method,
                   s.payment_status
            FROM sales s
            LEFT JOIN customers c ON c.customer_id = s.customer_id
            INNER JOIN users u ON u.user_id = s.user_id
            LEFT JOIN sale_items si ON si.sale_id = s.sale_id
            WHERE s.sale_date >= @from AND s.sale_date < @to
            GROUP BY s.sale_id, s.transaction_number, s.sale_date, c.full_name,
                     u.full_name, s.sale_type, s.subtotal, s.amount_received,
                     s.payment_method, s.payment_status
            ORDER BY s.sale_date DESC, s.sale_id DESC;
            """;
        AddParameter(command, "@from", from);
        AddParameter(command, "@to", to);

        var transactions = new List<ReportTransaction>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            transactions.Add(new ReportTransaction
            {
                SaleId = reader.GetInt64("sale_id"),
                TransactionNumber = reader.GetString("transaction_number"),
                SaleDate = reader.GetDateTime("sale_date"),
                CustomerName = reader.GetString("customer_name"),
                CashierName = reader.GetString("cashier_name"),
                SaleType = reader.GetString("sale_type"),
                ItemCount = Convert.ToInt32(reader.GetInt64("item_count")),
                Total = reader.GetDecimal("subtotal"),
                AmountReceived = reader.GetDecimal("amount_received"),
                PaymentMethod = reader.GetString("payment_method"),
                PaymentStatus = reader.GetString("payment_status")
            });
        }

        return transactions;
    }

    public async Task DeleteSaleAsync(long saleId)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using (var restore = connection.CreateCommand())
            {
                restore.Transaction = transaction;
                restore.CommandText = "UPDATE products p INNER JOIN sale_items si ON si.product_id = p.product_id SET p.stock_quantity = p.stock_quantity + si.quantity WHERE si.sale_id = @sale";
                AddParameter(restore, "@sale", saleId);
                await restore.ExecuteNonQueryAsync();
            }
            await ExecuteSaleChangeAsync(connection, transaction, "DELETE FROM customer_payments WHERE sale_id = @sale", saleId);
            await ExecuteSaleChangeAsync(connection, transaction, "DELETE FROM sale_items WHERE sale_id = @sale", saleId);
            await ExecuteSaleChangeAsync(connection, transaction, "DELETE FROM sales WHERE sale_id = @sale", saleId);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task ExecuteSaleChangeAsync(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction, string sql, long saleId)
    {
        await using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = sql;
        command.Parameters.AddWithValue("@sale", saleId); await command.ExecuteNonQueryAsync();
    }
}
