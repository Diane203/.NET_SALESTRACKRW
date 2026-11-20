using lab3.Models;

namespace lab3.Data;

public sealed class ExpenseRepository : RepositoryBase
{
    public ExpenseRepository(MySqlConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<IReadOnlyList<Expense>> SearchAsync(DateTime from, DateTime to, string? category = null, string? description = null)
    {
        var result = new List<Expense>();
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand(); command.CommandText = "SELECT expense_id, expense_date, category, description, amount FROM expenses WHERE expense_date >= @from AND expense_date < @to AND (@category = '' OR category LIKE @categoryPattern) AND (@description = '' OR description LIKE @descriptionPattern) ORDER BY expense_date DESC, expense_id DESC";
        AddParameter(command, "@from", from.Date); AddParameter(command, "@to", to.Date.AddDays(1));
        var categoryFilter = category?.Trim() ?? string.Empty;
        var descriptionFilter = description?.Trim() ?? string.Empty;
        AddParameter(command, "@category", categoryFilter); AddParameter(command, "@categoryPattern", $"%{categoryFilter}%");
        AddParameter(command, "@description", descriptionFilter); AddParameter(command, "@descriptionPattern", $"%{descriptionFilter}%");
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(new Expense { ExpenseId = reader.GetInt64("expense_id"), ExpenseDate = reader.GetDateTime("expense_date"), Category = reader.GetString("category"), Description = reader.GetString("description"), Amount = reader.GetDecimal("amount") });
        return result;
    }

    public async Task AddAsync(Expense expense, int userId)
    {
        if (expense.Amount <= 0) throw new ArgumentOutOfRangeException(nameof(expense.Amount));
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand(); command.CommandText = "INSERT INTO expenses (expense_date, category, description, amount, user_id) VALUES (@date, @category, @description, @amount, @user)";
        AddParameter(command, "@date", expense.ExpenseDate.Date); AddParameter(command, "@category", expense.Category.Trim()); AddParameter(command, "@description", expense.Description.Trim()); AddParameter(command, "@amount", expense.Amount); AddParameter(command, "@user", userId); await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(Expense expense)
    {
        if (expense.Amount <= 0) throw new ArgumentOutOfRangeException(nameof(expense.Amount));
        await using var connection = await ConnectionFactory.OpenConnectionAsync(); await using var command = connection.CreateCommand(); command.CommandText = "UPDATE expenses SET expense_date = @date, category = @category, description = @description, amount = @amount WHERE expense_id = @id"; AddParameter(command, "@id", expense.ExpenseId); AddParameter(command, "@date", expense.ExpenseDate.Date); AddParameter(command, "@category", expense.Category.Trim()); AddParameter(command, "@description", expense.Description.Trim()); AddParameter(command, "@amount", expense.Amount); if (await command.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Expense was not found.");
    }

    public async Task DeleteAsync(long expenseId)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(); await using var command = connection.CreateCommand(); command.CommandText = "DELETE FROM expenses WHERE expense_id = @id"; AddParameter(command, "@id", expenseId); if (await command.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Expense was not found.");
    }
}
