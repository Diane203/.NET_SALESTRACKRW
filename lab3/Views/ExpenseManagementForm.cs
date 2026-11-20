using lab3.Data;
using lab3.Models;

namespace lab3.Views;

public sealed class ExpenseManagementForm : Form
{
    private readonly ExpenseRepository repository;
    private readonly User user;
    private readonly DateTimePicker from = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker to = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker expenseDate = new() { Format = DateTimePickerFormat.Short };
    private readonly TextBox filterCategory = new();
    private readonly TextBox filterDescription = new();
    private readonly TextBox category = new();
    private readonly TextBox description = new();
    private readonly NumericUpDown amount = new() { DecimalPlaces = 2, Maximum = 100000000, Minimum = 0.01M };
    private readonly DataGridView grid = new();
    private long? selectedExpenseId;

    public ExpenseManagementForm(ExpenseRepository repository, User user)
    {
        this.repository = repository;
        this.user = user;
        from.Value = DateTime.Today.AddDays(-30);
        expenseDate.Value = DateTime.Today;
        BuildView();
        Shown += async (_, _) => await LoadAsync();
    }

    private void BuildView()
    {
        UiStyle.ConfigureForm(this, "Expenses", new Size(980, 620));
        Controls.Add(UiStyle.Header("Expense Management", "Record, search, update, and delete business expenses"));

        AddField("From", from, 28, 120, 130);
        AddField("To", to, 180, 120, 130);
        AddField("Category", filterCategory, 335, 120, 160);
        AddField("Description", filterDescription, 510, 120, 180);

        var filter = UiStyle.Button("Filter", new Point(705, 142), new Size(90, 32), true);
        filter.Click += async (_, _) => await LoadAsync();
        var back = UiStyle.Button("← Dashboard", new Point(815, 142), new Size(130, 32));
        back.Click += (_, _) => Close();
        Controls.AddRange([filter, back]);

        grid.Location = new Point(28, 175);
        grid.Size = new Size(917, 245);
        grid.ReadOnly = true;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.SelectionChanged += (_, _) => SelectExpense();
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.RowHeadersVisible = false;
        var idColumn = new DataGridViewTextBoxColumn();
        idColumn.HeaderText = "ID";
        idColumn.DataPropertyName = nameof(Expense.ExpenseId);
        grid.Columns.Add(idColumn);

        var dateColumn = new DataGridViewTextBoxColumn();
        dateColumn.HeaderText = "Date";
        dateColumn.DataPropertyName = nameof(Expense.ExpenseDate);
        dateColumn.DefaultCellStyle.Format = "d";
        grid.Columns.Add(dateColumn);

        var categoryColumn = new DataGridViewTextBoxColumn();
        categoryColumn.HeaderText = "Category";
        categoryColumn.DataPropertyName = nameof(Expense.Category);
        grid.Columns.Add(categoryColumn);

        var descriptionColumn = new DataGridViewTextBoxColumn();
        descriptionColumn.HeaderText = "Description";
        descriptionColumn.DataPropertyName = nameof(Expense.Description);
        grid.Columns.Add(descriptionColumn);

        var amountColumn = new DataGridViewTextBoxColumn();
        amountColumn.HeaderText = "Amount (RWF)";
        amountColumn.DataPropertyName = nameof(Expense.Amount);
        amountColumn.DefaultCellStyle.Format = "N2";
        grid.Columns.Add(amountColumn);
        Controls.Add(grid);

        var panel = new GroupBox();
        panel.Text = "Record or update expense";
        panel.Location = new Point(28, 440);
        panel.Size = new Size(917, 120);
        panel.ForeColor = UiStyle.Navy;
        AddGroupField(panel, "Date", expenseDate, 20);
        AddGroupField(panel, "Category", category, 170);
        AddGroupField(panel, "Description", description, 320);
        AddGroupField(panel, "Amount", amount, 570);

        var save = UiStyle.Button("Save expense", new Point(745, 47), new Size(140, 32), true);
        save.Click += async (_, _) => await SaveAsync();
        var update = UiStyle.Button("Update", new Point(590, 47), new Size(140, 32));
        update.Click += async (_, _) => await UpdateAsync();
        var delete = UiStyle.Button("Delete", new Point(430, 47), new Size(140, 32));
        delete.Click += async (_, _) => await DeleteAsync();
        panel.Controls.AddRange([save, update, delete]);
        Controls.Add(panel);
    }

    private void AddField(string label, Control control, int left, int top, int width)
    {
        var caption = new Label { Text = label, AutoSize = true, Location = new Point(left, top) };
        control.Location = new Point(left, top + 22);
        control.Size = new Size(width, 28);
        Controls.Add(caption);
        Controls.Add(control);
    }

    private static void AddGroupField(GroupBox panel, string label, Control control, int left)
    {
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Location = new Point(left, 25) });
        control.Location = new Point(left, 50);
        control.Size = new Size(210, 26);
        panel.Controls.Add(control);
    }

    private async Task LoadAsync()
    {
        try
        {
            var rows = await repository.SearchAsync(from.Value.Date, to.Value.Date, filterCategory.Text, filterDescription.Text);
            grid.DataSource = rows.ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Expense loading error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SelectExpense()
    {
        if (grid.CurrentRow?.DataBoundItem is Expense expense)
        {
            selectedExpenseId = expense.ExpenseId;
            expenseDate.Value = expense.ExpenseDate;
            category.Text = expense.Category;
            description.Text = expense.Description;
            amount.Value = expense.Amount >= amount.Minimum ? expense.Amount : amount.Minimum;
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(category.Text) || string.IsNullOrWhiteSpace(description.Text))
        {
            MessageBox.Show("Category and description are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            await repository.AddAsync(new Expense { ExpenseDate = expenseDate.Value.Date, Category = category.Text, Description = description.Text, Amount = amount.Value }, user.UserId);
            category.Clear();
            description.Clear();
            amount.Value = 0.01M;
            await LoadAsync();
            MessageBox.Show("Expense saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Expense save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task UpdateAsync()
    {
        if (!selectedExpenseId.HasValue)
        {
            MessageBox.Show("Select an expense first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            await repository.UpdateAsync(new Expense { ExpenseId = selectedExpenseId.Value, ExpenseDate = expenseDate.Value.Date, Category = category.Text, Description = description.Text, Amount = amount.Value });
            await LoadAsync();
            MessageBox.Show("Expense updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Expense update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task DeleteAsync()
    {
        if (!selectedExpenseId.HasValue)
        {
            MessageBox.Show("Select an expense first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show("Delete the selected expense?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await repository.DeleteAsync(selectedExpenseId.Value);
            await LoadAsync();
            MessageBox.Show("Expense deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Expense delete error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
