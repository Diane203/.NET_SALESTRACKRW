using lab3.Data;
using lab3.Models;

namespace lab3.Controllers;

public sealed class ProductController
{
    private readonly ProductRepository _repository;
    public ProductController(ProductRepository repository) => _repository = repository;
    public Task<IReadOnlyList<Product>> SearchAsync(string? text, int? categoryId = null) => _repository.SearchAsync(text, categoryId);
    public Task<IReadOnlyList<Category>> GetCategoriesAsync() => _repository.GetCategoriesAsync();
    public Task CreateAsync(Product product) => _repository.CreateAsync(product);
    public Task UpdateAsync(Product product) => _repository.UpdateAsync(product);
    public Task DeactivateAsync(int productId) => _repository.DeactivateAsync(productId);
    public Task DeleteAsync(int productId) => _repository.DeleteAsync(productId);
    public Task AddStockAsync(int productId, decimal quantity, string reason, int userId) => _repository.AddStockAsync(productId, quantity, reason, userId);
    public Task<IReadOnlyList<StockMovement>> GetMovementsAsync(int? productId = null) => _repository.GetMovementsAsync(productId);
    public Task AddAdjustmentAsync(int productId, decimal quantity, string reason, int userId) => _repository.AddAdjustmentAsync(productId, quantity, reason, userId);
    public Task<int> CreateCategoryAsync(string name, string description) => _repository.CreateCategoryAsync(name, description);
    public Task UpdateCategoryAsync(Category category) => _repository.UpdateCategoryAsync(category);
    public Task DeactivateCategoryAsync(int categoryId) => _repository.DeactivateCategoryAsync(categoryId);
}
