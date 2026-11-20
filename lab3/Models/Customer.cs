namespace lab3.Models;

public sealed class Customer
{
    public int CustomerId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Telephone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public decimal CreditBalance { get; init; }
}
