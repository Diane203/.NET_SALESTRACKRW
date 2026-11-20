using lab3.Data;
using lab3.Services;

namespace lab3.Controllers;

public sealed class ApplicationServices
{
    public required ProductController Products { get; init; }
    public required CustomerController Customers { get; init; }
    public required SalesService Sales { get; init; }
    public required ExpenseRepository Expenses { get; init; }
    public required ReportController Reports { get; init; }
    public required UserRepository Users { get; init; }
    public required CustomerPaymentService Payments { get; init; }
}
