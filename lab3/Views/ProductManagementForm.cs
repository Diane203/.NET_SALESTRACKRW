using lab3.Controllers;
using lab3.Models;

namespace lab3.Views;

public sealed class ProductManagementForm : Form
{
    private readonly ProductController _controller;
    private readonly User _user;
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = new();
    private readonly TextBox _code = new();
    private readonly TextBox _name = new();
    private readonly NumericUpDown _purchase = new() { DecimalPlaces = 2, Maximum = 100000000 };
    private readonly NumericUpDown _selling = new() { DecimalPlaces = 2, Maximum = 100000000 };
    private readonly NumericUpDown _stock = new() { DecimalPlaces = 3, Maximum = 100000000 };
    private readonly NumericUpDown _minimum = new() { DecimalPlaces = 3, Maximum = 100000000 };
    private readonly ComboBox _category = new();
    private readonly ComboBox _categoryFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    private int? _selectedProductId;

    public ProductManagementForm(ProductController controller, User user)
    {
        _controller = controller; _user = user;
        BuildView();
        Shown += async (_, _) => await LoadDataAsync();
    }

    private void BuildView()
    {
        UiStyle.ConfigureForm(this, "Products and Stock", new Size(1040, 820));
        Controls.Add(UiStyle.Header("Products & Stock", "Manage products, pricing, categories, and inventory levels"));

        _search.Location = new Point(28, 120); _search.Size = new Size(330, 28); _search.PlaceholderText = "Search by product code or name";
        var searchButton = UiStyle.Button("Search", new Point(530, 118), new Size(100, 32), true);
        var categoryLabel = new Label { Text = "Category", Location = new Point(370, 118), AutoSize = true, ForeColor = UiStyle.Muted };
        _categoryFilter.Location = new Point(370, 138); _categoryFilter.Width = 150;
        searchButton.Click += async (_, _) => await LoadProductsAsync();
        var backButton = UiStyle.Button("← Dashboard", new Point(875, 118), new Size(135, 32));
        backButton.Click += (_, _) => Close();
        var categoriesButton = UiStyle.Button("Categories", new Point(485, 118), new Size(110, 32)); categoriesButton.Click += (_, _) => new CategoryManagementForm(_controller).ShowDialog(this);
        var movementsButton = UiStyle.Button("Stock movements", new Point(605, 118), new Size(145, 32)); movementsButton.Click += (_, _) => new StockMovementForm(_controller, _user).ShowDialog(this);
        _grid.Location = new Point(28, 170); _grid.Size = new Size(982, 300); _grid.ReadOnly = true; _grid.AutoGenerateColumns = false; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.AllowUserToAddRows = false; _grid.BackgroundColor = Color.White; _grid.BorderStyle = BorderStyle.None; _grid.RowHeadersVisible = false;
        _grid.DefaultCellStyle.SelectionBackColor = UiStyle.Green; _grid.DefaultCellStyle.SelectionForeColor = Color.White;
        AddGridColumn("Code", nameof(Product.ProductCode), 105); AddGridColumn("Product name", nameof(Product.Name), 180); AddGridColumn("Category", nameof(Product.CategoryName), 120);
        AddGridColumn("Purchase (RWF)", nameof(Product.PurchasePrice), 120, "N2"); AddGridColumn("Selling (RWF)", nameof(Product.SellingPrice), 120, "N2");
        AddGridColumn("Stock", nameof(Product.StockQuantity), 85, "N3"); AddGridColumn("Minimum", nameof(Product.MinimumStockLevel), 85, "N3"); AddGridColumn("Status", nameof(Product.StockStatus), 110);
        _grid.SelectionChanged += (_, _) => SelectProduct();
        Controls.AddRange([_search, categoryLabel, _categoryFilter, searchButton, backButton, categoriesButton, movementsButton, _grid]);

        var group = new GroupBox { Text = "Add or edit product", Location = new Point(28, 500), Size = new Size(982, 245), ForeColor = UiStyle.Navy };
        AddControl(group, "Code", _code, 20, 25); AddControl(group, "Name", _name, 180, 25); AddControl(group, "Category", _category, 340, 25);
        AddControl(group, "Purchase price", _purchase, 500, 25); AddControl(group, "Selling price", _selling, 660, 25);
        AddControl(group, "Opening stock", _stock, 20, 80); AddControl(group, "Minimum stock", _minimum, 180, 80);
        var addButton = UiStyle.Button("Save product", new Point(660, 78), new Size(130, 32), true);
        addButton.Click += async (_, _) => await CreateProductAsync();
        var updateButton = UiStyle.Button("Update selected", new Point(340, 125), new Size(150, 32));
        updateButton.Enabled = _user.Role == UserRole.Administrator;
        updateButton.Click += async (_, _) => await UpdateProductAsync();
        var deactivateButton = UiStyle.Button("Deactivate selected", new Point(500, 125), new Size(150, 32));
        deactivateButton.Enabled = _user.Role == UserRole.Administrator;
        deactivateButton.Click += async (_, _) => await DeactivateProductAsync();
        var lowStockButton = UiStyle.Button("Mark as low stock", new Point(820, 125), new Size(150, 32));
        lowStockButton.Enabled = _user.Role == UserRole.Administrator;
        lowStockButton.Click += async (_, _) => await MarkLowStockAsync();
        var deleteButton = UiStyle.Button("Delete selected", new Point(660, 125), new Size(150, 32));
        deleteButton.Enabled = _user.Role == UserRole.Administrator;
        deleteButton.Click += async (_, _) => await DeleteProductAsync();
        group.Controls.AddRange([addButton, updateButton, deactivateButton, lowStockButton, deleteButton]); Controls.Add(group);
    }

    private async Task MarkLowStockAsync()
    {
        if (_user.Role != UserRole.Administrator) return;
        if (_selectedProductId is null) { MessageBox.Show("Select a product from the table first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        _minimum.Value = Math.Max(_stock.Value, 1);
        await UpdateProductAsync();
    }

    private void AddGridColumn(string header, string property, int width, string? format = null)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            Width = width,
            DefaultCellStyle = new DataGridViewCellStyle { Format = format, Alignment = DataGridViewContentAlignment.MiddleLeft }
        });
    }

    private static void AddControl(Control parent, string labelText, Control control, int left, int top)
    {
        parent.Controls.Add(new Label { Text = labelText, AutoSize = true, Location = new Point(left, top) });
        control.Location = new Point(left, top + 20); control.Size = new Size(140, 25); parent.Controls.Add(control);
    }
    private async Task LoadDataAsync()
    {
        var categories = (await _controller.GetCategoriesAsync()).ToList();
        _category.DataSource = categories.ToList(); _category.DisplayMember = nameof(Category.Name); _category.ValueMember = nameof(Category.CategoryId);
        categories.Insert(0, new Category { CategoryId = 0, Name = "All categories" });
        _categoryFilter.DataSource = categories; _categoryFilter.DisplayMember = nameof(Category.Name); _categoryFilter.ValueMember = nameof(Category.CategoryId); _categoryFilter.SelectedIndex = 0;
        await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            int? categoryId = _categoryFilter.SelectedValue is int id && id > 0 ? id : null;
            _grid.DataSource = (await _controller.SearchAsync(_search.Text, categoryId)).ToList();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.DataBoundItem is Product product)
                {
                    row.DefaultCellStyle.BackColor = product.StockQuantity <= 0 ? Color.FromArgb(254, 226, 226) : product.StockQuantity <= product.MinimumStockLevel ? Color.FromArgb(254, 243, 199) : Color.FromArgb(240, 253, 244);
                    row.DefaultCellStyle.SelectionBackColor = UiStyle.Green;
                    row.DefaultCellStyle.SelectionForeColor = Color.White;
                }
            }
            ClearSelection();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Products error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task CreateProductAsync()
    {
        if (string.IsNullOrWhiteSpace(_code.Text) || string.IsNullOrWhiteSpace(_name.Text) || _category.SelectedValue is null)
        { MessageBox.Show("Code, name, and category are required."); return; }
        if (_selling.Value < _purchase.Value)
        { MessageBox.Show("Selling price cannot be below purchase price.", "Invalid pricing", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try
        {
            await _controller.CreateAsync(new Product { ProductCode = _code.Text, Name = _name.Text, CategoryId = (int)_category.SelectedValue, PurchasePrice = _purchase.Value, SellingPrice = _selling.Value, StockQuantity = _stock.Value, MinimumStockLevel = _minimum.Value });
            MessageBox.Show("Product saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _code.Clear(); _name.Clear(); await LoadProductsAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void SelectProduct()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Product product) return;
        _selectedProductId = product.ProductId;
        _code.Text = product.ProductCode; _name.Text = product.Name; _category.SelectedValue = product.CategoryId;
        _purchase.Value = product.PurchasePrice; _selling.Value = product.SellingPrice; _stock.Value = product.StockQuantity; _minimum.Value = product.MinimumStockLevel;
    }

    private async Task UpdateProductAsync()
    {
        if (_user.Role != UserRole.Administrator) return;
        if (_selectedProductId is null) { MessageBox.Show("Select a product from the table first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (_selling.Value < _purchase.Value) { MessageBox.Show("Selling price cannot be below purchase price.", "Invalid pricing", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try
        {
            await _controller.UpdateAsync(new Product { ProductId = _selectedProductId.Value, ProductCode = _code.Text, Name = _name.Text, CategoryId = (int)_category.SelectedValue!, PurchasePrice = _purchase.Value, SellingPrice = _selling.Value, StockQuantity = _stock.Value, MinimumStockLevel = _minimum.Value });
            MessageBox.Show("Product updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); await LoadProductsAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task DeactivateProductAsync()
    {
        if (_user.Role != UserRole.Administrator) return;
        if (_selectedProductId is null) { MessageBox.Show("Select a product from the table first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (MessageBox.Show($"Deactivate {_name.Text}? It will no longer appear in active stock.", "Confirm deactivation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try { await _controller.DeactivateAsync(_selectedProductId.Value); MessageBox.Show("Product deactivated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); await LoadProductsAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Deactivation error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task DeleteProductAsync()
    {
        if (_user.Role != UserRole.Administrator) return;
        if (_selectedProductId is null) { MessageBox.Show("Select a product from the table first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (MessageBox.Show($"Permanently delete {_name.Text}? This is only possible if the product has no history.", "Confirm permanent deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            await _controller.DeleteAsync(_selectedProductId.Value);
            MessageBox.Show("Product permanently deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadProductsAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Delete error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ClearSelection()
    {
        _selectedProductId = null; _code.Clear(); _name.Clear(); _purchase.Value = 0; _selling.Value = 0; _stock.Value = 0; _minimum.Value = 0;
    }
}
