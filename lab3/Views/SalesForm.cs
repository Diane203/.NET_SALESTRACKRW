using lab3.Controllers;
using lab3.Models;
using lab3.Services;

namespace lab3.Views;

public sealed class SalesForm : Form
{
    private readonly ApplicationServices _services;
    private readonly User _user;
    private readonly ComboBox _product = new();
    private readonly NumericUpDown _quantity = new() { Minimum = 1, Maximum = 100000, DecimalPlaces = 3, Value = 1 };
    private readonly ComboBox _customer = new();
    private readonly ComboBox _saleType = new();
    private readonly ComboBox _paymentMethod = new();
    private readonly NumericUpDown _received = new() { Minimum = 0, Maximum = 100000000, DecimalPlaces = 2 };
    private readonly DataGridView _cart = new();
    private readonly Label _totalLabel = new();
    private readonly Label _sellingPriceLabel = new();
    private readonly Label _amountDueLabel = new();
    private readonly Button _completeButton = new();
    private readonly List<SaleCartLine> _lines = [];
    private List<Product> _products = [];

    public SalesForm(ApplicationServices services, User user)
    {
        _services = services; _user = user;
        BuildView();
        Shown += async (_, _) => await LoadLookupsAsync();
    }

    private void BuildView()
    {
        UiStyle.ConfigureForm(this, "Sales", new Size(1080, 680));
        Controls.Add(UiStyle.Header("Sales Processing", "Create a sale, validate stock, and record payment status"));
        var back = UiStyle.Button("← Dashboard", new Point(900, 114), new Size(135, 32)); back.Click += (_, _) => Close(); Controls.Add(back);

        AddField("Product", _product, 28, 122, 300); AddField("Quantity", _quantity, 350, 122, 110);
        _sellingPriceLabel.AutoSize = true; _sellingPriceLabel.Location = new Point(480, 160); _sellingPriceLabel.ForeColor = UiStyle.Navy; Controls.Add(_sellingPriceLabel);
        var add = UiStyle.Button("+ Add selected", new Point(480, 120), new Size(140, 32), true); add.Click += (_, _) => AddLine(); Controls.Add(add);
        AddField("Customer", _customer, 28, 180, 300); AddField("Payment method", _paymentMethod, 350, 180, 180); AddField("Amount received", _received, 550, 180, 150); AddField("Sale type", _saleType, 750, 180, 135);
        _amountDueLabel.AutoSize = true; _amountDueLabel.Location = new Point(900, 180); _amountDueLabel.ForeColor = UiStyle.Navy; Controls.Add(_amountDueLabel);
        _cart.Location = new Point(28, 245); _cart.Size = new Size(1007, 270); _cart.ReadOnly = true; _cart.AutoGenerateColumns = false; _cart.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _cart.BackgroundColor = Color.White; _cart.BorderStyle = BorderStyle.None; _cart.RowHeadersVisible = false;
        AddCartColumn("Product ID", nameof(SaleCartLine.ProductId), 110);
        AddCartColumn("Product name", nameof(SaleCartLine.ProductName), 260);
        AddCartColumn("Quantity", nameof(SaleCartLine.Quantity), 160, "N3");
        AddCartColumn("Unit price (selling)", nameof(SaleCartLine.UnitPrice), 210, "N2");
        AddCartColumn("Line total", nameof(SaleCartLine.LineTotal), 210, "N2");
        Controls.Add(_cart);
        _totalLabel.Text = "Total: 0.00 RWF"; _totalLabel.Font = new Font("Segoe UI", 15F, FontStyle.Bold); _totalLabel.ForeColor = UiStyle.Navy; _totalLabel.AutoSize = true; _totalLabel.Location = new Point(28, 545); Controls.Add(_totalLabel);
        var remove = UiStyle.Button("Remove selected", new Point(700, 540), new Size(145, 36)); remove.Click += (_, _) => RemoveSelected(); Controls.Add(remove);
        _completeButton.Text = "Complete sale"; _completeButton.Location = new Point(870, 540); _completeButton.Size = new Size(165, 36); _completeButton.BackColor = UiStyle.Green; _completeButton.ForeColor = Color.White; _completeButton.FlatStyle = FlatStyle.Flat; _completeButton.FlatAppearance.BorderSize = 0; _completeButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold); _completeButton.Enabled = false; _completeButton.Click += async (_, _) => await CompleteSaleAsync(); Controls.Add(_completeButton);
    }

    private void AddField(string label, Control control, int left, int top, int width)
    {
        var caption = new Label { Text = label, AutoSize = true, Location = new Point(left, top) };
        control.Location = new Point(left, top + 22); control.Size = new Size(width, 28);
        Controls.Add(caption);
    }

    private void AddCartColumn(string header, string property, int width, string? format = null)
    {
        _cart.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            Width = width,
            DefaultCellStyle = new DataGridViewCellStyle { Format = format }
        });
    }

    private async Task LoadLookupsAsync()
    {
        try
        {
            _products = (await _services.Products.SearchAsync(string.Empty)).ToList(); _product.DataSource = _products; _product.DisplayMember = nameof(Product.Name); _product.ValueMember = nameof(Product.ProductId); _product.SelectedIndexChanged += (_, _) => UpdatePreview(); _quantity.ValueChanged += (_, _) => UpdatePreview(); _received.ValueChanged += (_, _) => UpdatePreview(); UpdatePreview();
            var customers = (await _services.Customers.SearchAsync(string.Empty)).ToList(); customers.Insert(0, new Customer { CustomerId = 0, FullName = "Walk-in customer" }); _customer.DataSource = customers; _customer.DisplayMember = nameof(Customer.FullName); _customer.ValueMember = nameof(Customer.CustomerId);
            _paymentMethod.Items.AddRange(["Cash", "Mobile Money", "Bank Transfer", "Other"]); _paymentMethod.SelectedIndex = 0;
            _saleType.DataSource = Enum.GetValues<SaleType>(); _saleType.SelectedItem = SaleType.Retail;
            Controls.AddRange([_product, _quantity, _customer, _paymentMethod, _received, _saleType]);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Sales setup error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void AddLine()
    {
        if (_product.SelectedItem is not Product product)
        {
            MessageBox.Show("Select a product before adding it to the sale.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var existingQuantity = _lines.FirstOrDefault(x => x.ProductId == product.ProductId)?.Quantity ?? 0;
        var requestedQuantity = existingQuantity + _quantity.Value;
        if (product.StockQuantity <= 0)
        {
            MessageBox.Show($"{product.Name} is completely out of stock.", "Out of stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (requestedQuantity > product.StockQuantity)
        {
            MessageBox.Show($"Only {product.StockQuantity:N3} of {product.Name} remain in stock.", "Insufficient stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var existing = _lines.FirstOrDefault(x => x.ProductId == product.ProductId);
        if (existing is not null) existing.Quantity += _quantity.Value;
        else _lines.Add(new SaleCartLine { ProductId = product.ProductId, ProductName = product.Name, Quantity = _quantity.Value, UnitPrice = product.SellingPrice });
        RefreshCart();
    }

    private void RemoveSelected()
    {
        if (_cart.CurrentRow?.DataBoundItem is SaleCartLine line) _lines.Remove(line);
        RefreshCart();
    }

    private void RefreshCart()
    {
        _cart.DataSource = null; _cart.DataSource = _lines.ToList(); _totalLabel.Text = $"Total: {_lines.Sum(x => x.LineTotal):N2} RWF"; _completeButton.Enabled = _lines.Count > 0; UpdatePreview();
    }

    private void UpdatePreview()
    {
        var product = _product.SelectedItem as Product;
        _sellingPriceLabel.Text = product is null ? "Selling price: —" : $"Selling price: {product.SellingPrice:N2} RWF";
        var total = _lines.Sum(x => x.LineTotal);
        _amountDueLabel.Text = $"Amount due: {Math.Max(0, total - _received.Value):N2} RWF";
    }

    private async Task CompleteSaleAsync()
    {
        var total = _lines.Sum(x => x.LineTotal);
        if (total <= 0) { MessageBox.Show("Add at least one product to the sale before completing it.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        var customerId = int.TryParse(Convert.ToString(_customer.SelectedValue), out var selectedCustomerId) && selectedCustomerId > 0 ? selectedCustomerId : (int?)null;
        if (_received.Value < total && customerId is null) { MessageBox.Show("A customer is required for credit or partial payment.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (_received.Value > total) { MessageBox.Show("Amount received cannot be greater than the sale total.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try
        {
            var number = await _services.Sales.CompleteSaleAsync(new SaleRequest { UserId = _user.UserId, CustomerId = customerId, SaleType = (SaleType)_saleType.SelectedItem!, Lines = _lines.Select(x => new SaleLine { ProductId = x.ProductId, Quantity = x.Quantity, UnitPrice = x.UnitPrice }).ToList(), AmountReceived = _received.Value, PaymentMethod = _paymentMethod.SelectedItem?.ToString() });
            MessageBox.Show($"Sale {number} completed successfully.", "Sale completed", MessageBoxButtons.OK, MessageBoxIcon.Information); _lines.Clear(); _received.Value = 0; RefreshCart();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Sale error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private sealed class SaleCartLine
    {
        public int ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; init; }
        public decimal LineTotal => Quantity * UnitPrice;
    }
}
