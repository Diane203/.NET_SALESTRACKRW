using lab3.Data;
using lab3.Models;

namespace lab3.Controllers;

public sealed class CustomerController
{
    private readonly CustomerRepository _repository;
    public CustomerController(CustomerRepository repository) => _repository = repository;
    public Task<IReadOnlyList<Customer>> SearchAsync(string? text) => _repository.SearchAsync(text);
    public Task<int> CreateAsync(Customer customer) => _repository.CreateAsync(customer);
    public Task UpdateAsync(Customer customer) => _repository.UpdateAsync(customer);
    public Task<IReadOnlyList<CustomerPayment>> GetPaymentsAsync(int customerId) => _repository.GetPaymentsAsync(customerId);
}
