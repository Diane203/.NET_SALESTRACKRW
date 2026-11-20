namespace lab3.Models;

public sealed class Category
{
    public int CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public sealed class StockMovement
{
    public long MovementId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string MovementType { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class Product
{
    public int ProductId { get; init; }
    public string ProductCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal PurchasePrice { get; init; }
    public decimal SellingPrice { get; init; }
    public decimal StockQuantity { get; init; }
    public decimal MinimumStockLevel { get; init; }
    public bool IsActive { get; init; }
    public string StockStatus => StockQuantity <= 0 ? "Out of stock" : StockQuantity <= MinimumStockLevel ? "Low stock" : "In stock";
}
