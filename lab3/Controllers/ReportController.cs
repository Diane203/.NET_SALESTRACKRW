using lab3.Data;
using lab3.Models;

namespace lab3.Controllers;

public sealed class ReportController
{
    private readonly ReportRepository _repository;
    public ReportController(ReportRepository repository) => _repository = repository;
    public Task<DashboardSummary> GetSummaryAsync(DateTime from, DateTime to) => _repository.GetSummaryAsync(from, to);
    public Task<IReadOnlyList<ReportTransaction>> GetTransactionsAsync(DateTime from, DateTime to) => _repository.GetTransactionsAsync(from, to);
    public Task DeleteSaleAsync(long saleId) => _repository.DeleteSaleAsync(saleId);
    public Task UpdateSalePaymentAsync(long saleId, decimal amountReceived, string paymentMethod) => _repository.UpdateSalePaymentAsync(saleId, amountReceived, paymentMethod);
    public Task<IReadOnlyList<SaleEditLine>> GetSaleLinesAsync(long saleId) => _repository.GetSaleLinesAsync(saleId);
    public Task UpdateSaleLinesAsync(long saleId, IReadOnlyList<SaleEditLine> lines) => _repository.UpdateSaleLinesAsync(saleId, lines);
}
