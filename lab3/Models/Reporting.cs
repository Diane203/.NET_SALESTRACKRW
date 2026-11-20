namespace lab3.Models;

public sealed class Expense
{
    public long ExpenseId { get; init; }
    public DateTime ExpenseDate { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}

public sealed class CustomerPayment
{
    public long PaymentId { get; init; }
    public int CustomerId { get; init; }
    public long? SaleId { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public DateTime PaymentDate { get; init; }
}

public sealed class DashboardSummary
{
    public decimal SalesTotal { get; init; }
    public int TransactionCount { get; init; }
    public int LowStockCount { get; init; }
    public decimal OutstandingCredit { get; init; }
    public int ActiveProductCount { get; init; }
    public decimal StockUnits { get; init; }
    public decimal InventoryValue { get; init; }
}

public sealed class SaleEditLine
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; init; }
}

public sealed class ReportTransaction
{
    public long SaleId { get; init; }
    public string TransactionNumber { get; init; } = string.Empty;
    public DateTime SaleDate { get; init; }
    public string CustomerName { get; init; } = "Walk-in customer";
    public string CashierName { get; init; } = string.Empty;
    public string SaleType { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public decimal Total { get; init; }
    public decimal AmountReceived { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
}
