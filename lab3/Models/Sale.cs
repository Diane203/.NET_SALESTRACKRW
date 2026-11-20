namespace lab3.Models;

public enum SaleType
{
    Retail,
    Wholesale
}

public sealed class SaleLine
{
    public int ProductId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public sealed class SaleRequest
{
    public int UserId { get; init; }
    public int? CustomerId { get; init; }
    public SaleType SaleType { get; init; } = SaleType.Retail;
    public IReadOnlyList<SaleLine> Lines { get; init; } = [];
    public decimal AmountReceived { get; init; }
    public string? PaymentMethod { get; init; }
    public string? Notes { get; init; }
}
