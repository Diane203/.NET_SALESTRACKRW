using lab3.Models;
using MySqlConnector;

namespace lab3.Data;

public sealed class ProductRepository : RepositoryBase
{
    public ProductRepository(MySqlConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync()
    {
        var categories = new List<Category>();
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT category_id, name, COALESCE(description, '') AS description FROM categories WHERE is_active = TRUE ORDER BY name";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            categories.Add(new Category { CategoryId = reader.GetInt32("category_id"), Name = reader.GetString("name"), Description = reader.GetString("description") });
        }
        return categories;
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string? searchText, int? categoryId = null)
    {
        var products = new List<Product>();
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.product_id, p.product_code, p.name, p.category_id, c.name AS category_name,
                   p.purchase_price, p.selling_price, p.stock_quantity, p.minimum_stock_level, p.is_active
            FROM products p INNER JOIN categories c ON c.category_id = p.category_id
            WHERE p.is_active = TRUE
              AND (@search = '' OR p.name LIKE @pattern OR p.product_code LIKE @pattern)
              AND (@categoryId IS NULL OR p.category_id = @categoryId)
            ORDER BY p.name;
            """;
        var search = searchText?.Trim() ?? string.Empty;
        AddParameter(command, "@search", search);
        AddParameter(command, "@pattern", $"%{search}%");
        AddParameter(command, "@categoryId", categoryId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            products.Add(new Product
            {
                ProductId = reader.GetInt32("product_id"), ProductCode = reader.GetString("product_code"),
                Name = reader.GetString("name"), CategoryId = reader.GetInt32("category_id"),
                CategoryName = reader.GetString("category_name"), PurchasePrice = reader.GetDecimal("purchase_price"),
                SellingPrice = reader.GetDecimal("selling_price"), StockQuantity = reader.GetDecimal("stock_quantity"),
                MinimumStockLevel = reader.GetDecimal("minimum_stock_level"), IsActive = reader.GetBoolean("is_active")
            });
        }
        return products;
    }

    public async Task CreateAsync(Product product)
    {
        ValidateProduct(product);
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO products (product_code, name, category_id, purchase_price, selling_price, stock_quantity, minimum_stock_level)
            VALUES (@code, @name, @category, @purchase, @selling, @stock, @minimum);
            """;
        AddParameter(command, "@code", product.ProductCode.Trim()); AddParameter(command, "@name", product.Name.Trim());
        AddParameter(command, "@category", product.CategoryId); AddParameter(command, "@purchase", product.PurchasePrice);
        AddParameter(command, "@selling", product.SellingPrice); AddParameter(command, "@stock", product.StockQuantity);
        AddParameter(command, "@minimum", product.MinimumStockLevel);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        ValidateProduct(product);
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE products
            SET product_code = @code, name = @name, category_id = @category,
                purchase_price = @purchase, selling_price = @selling,
                stock_quantity = @stock, minimum_stock_level = @minimum
            WHERE product_id = @id AND is_active = TRUE;
            """;
        AddParameter(command, "@id", product.ProductId); AddParameter(command, "@code", product.ProductCode.Trim()); AddParameter(command, "@name", product.Name.Trim());
        AddParameter(command, "@category", product.CategoryId); AddParameter(command, "@purchase", product.PurchasePrice); AddParameter(command, "@selling", product.SellingPrice);
        AddParameter(command, "@stock", product.StockQuantity); AddParameter(command, "@minimum", product.MinimumStockLevel);
        if (await command.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("The product was not found or is inactive.");
    }

    public async Task DeactivateAsync(int productId)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE products SET is_active = FALSE WHERE product_id = @id AND is_active = TRUE";
        AddParameter(command, "@id", productId);
        if (await command.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("The product was not found or is already inactive.");
    }

    public async Task DeleteAsync(int productId)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM products WHERE product_id = @id";
        AddParameter(command, "@id", productId);
        try
        {
            if (await command.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("The product was not found.");
        }
        catch (MySqlException exception) when (exception.Number == 1451)
        {
            throw new InvalidOperationException("This product has stock or sales history and cannot be permanently deleted. Use Deactivate instead.", exception);
        }
    }

    private static void ValidateProduct(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.ProductCode)) throw new ArgumentException("Product code is required.");
        if (string.IsNullOrWhiteSpace(product.Name)) throw new ArgumentException("Product name is required.");
        if (product.PurchasePrice < 0 || product.SellingPrice < 0) throw new ArgumentException("Prices cannot be negative.");
        if (product.SellingPrice < product.PurchasePrice) throw new ArgumentException("Selling price cannot be below purchase price.");
        if (product.StockQuantity < 0 || product.MinimumStockLevel < 0) throw new ArgumentException("Stock values cannot be negative.");
    }

    public async Task AddStockAsync(int productId, decimal quantity, string reason, int userId)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var update = connection.CreateCommand())
        {
            update.Transaction = transaction;
            update.CommandText = "UPDATE products SET stock_quantity = stock_quantity + @quantity WHERE product_id = @id AND is_active = TRUE";
            AddParameter(update, "@quantity", quantity); AddParameter(update, "@id", productId);
            if (await update.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Product was not found.");
        }
        await using (var movement = connection.CreateCommand())
        {
            movement.Transaction = transaction;
            movement.CommandText = "INSERT INTO stock_movements (product_id, movement_type, quantity, reason, user_id) VALUES (@id, 'Received', @quantity, @reason, @user)";
            AddParameter(movement, "@id", productId); AddParameter(movement, "@quantity", quantity);
            AddParameter(movement, "@reason", reason.Trim()); AddParameter(movement, "@user", userId);
            await movement.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }

    public async Task<IReadOnlyList<StockMovement>> GetMovementsAsync(int? productId = null)
    {
        var result = new List<StockMovement>();
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand(); command.CommandText = "SELECT m.movement_id, m.product_id, p.name AS product_name, m.movement_type, m.quantity, m.reason, u.full_name AS user_name, m.created_at FROM stock_movements m INNER JOIN products p ON p.product_id = m.product_id INNER JOIN users u ON u.user_id = m.user_id WHERE (@product IS NULL OR m.product_id = @product) ORDER BY m.created_at DESC"; AddParameter(command, "@product", productId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(new StockMovement { MovementId = reader.GetInt64("movement_id"), ProductId = reader.GetInt32("product_id"), ProductName = reader.GetString("product_name"), MovementType = reader.GetString("movement_type"), Quantity = reader.GetDecimal("quantity"), Reason = reader.GetString("reason"), UserName = reader.GetString("user_name"), CreatedAt = reader.GetDateTime("created_at") });
        return result;
    }

    public async Task AddAdjustmentAsync(int productId, decimal quantity, string reason, int userId)
    {
        if (quantity == 0 || string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Adjustment quantity and reason are required.");
        await using var connection = await ConnectionFactory.OpenConnectionAsync(); await using var transaction = await connection.BeginTransactionAsync();
        await using (var update = connection.CreateCommand()) { update.Transaction = transaction; update.CommandText = "UPDATE products SET stock_quantity = stock_quantity + @quantity WHERE product_id = @id AND is_active = TRUE AND stock_quantity + @quantity >= 0"; AddParameter(update, "@quantity", quantity); AddParameter(update, "@id", productId); if (await update.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Adjustment would create negative stock or product was not found."); }
        await using (var movement = connection.CreateCommand()) { movement.Transaction = transaction; movement.CommandText = "INSERT INTO stock_movements (product_id, movement_type, quantity, reason, user_id) VALUES (@id, 'Adjustment', @quantity, @reason, @user)"; AddParameter(movement, "@id", productId); AddParameter(movement, "@quantity", Math.Abs(quantity)); AddParameter(movement, "@reason", reason.Trim()); AddParameter(movement, "@user", userId); await movement.ExecuteNonQueryAsync(); }
        await transaction.CommitAsync();
    }

    public async Task<int> CreateCategoryAsync(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.");
        await using var connection = await ConnectionFactory.OpenConnectionAsync(); await using var command = connection.CreateCommand(); command.CommandText = "INSERT INTO categories (name, description) VALUES (@name, @description); SELECT LAST_INSERT_ID();"; AddParameter(command, "@name", name.Trim()); AddParameter(command, "@description", description.Trim()); return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task UpdateCategoryAsync(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name)) throw new ArgumentException("Category name is required.");
        await using var connection = await ConnectionFactory.OpenConnectionAsync(); await using var command = connection.CreateCommand(); command.CommandText = "UPDATE categories SET name = @name, description = @description WHERE category_id = @id AND is_active = TRUE"; AddParameter(command, "@id", category.CategoryId); AddParameter(command, "@name", category.Name.Trim()); AddParameter(command, "@description", category.Description.Trim()); if (await command.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Category was not found.");
    }

    public async Task DeactivateCategoryAsync(int categoryId)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(); await using var command = connection.CreateCommand(); command.CommandText = "UPDATE categories SET is_active = FALSE WHERE category_id = @id"; AddParameter(command, "@id", categoryId); if (await command.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Category was not found.");
    }
}
