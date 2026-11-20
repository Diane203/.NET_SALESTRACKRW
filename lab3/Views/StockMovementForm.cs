using lab3.Controllers;
using lab3.Models;

namespace lab3.Views;

public sealed class StockMovementForm : Form
{
    private readonly ProductController _controller;
    private readonly User _user;
    private readonly ComboBox _product = new();
    private readonly ComboBox _type = new();
    private readonly NumericUpDown _quantity = new() { DecimalPlaces = 3, Minimum = -100000000, Maximum = 100000000 };
    private readonly TextBox _reason = new();
    private readonly DataGridView _grid = new();

    public StockMovementForm(ProductController controller, User user) { _controller = controller; _user = user; BuildView(); Shown += async (_, _) => await LoadAsync(); }
    private void BuildView()
    {
        UiStyle.ConfigureForm(this, "Stock Movements", new Size(1000, 650)); Controls.Add(UiStyle.Header("Stock Movements", "Record received stock or adjustments with reasons"));
        Add("Product", _product, 28, 120, 250); Add("Movement", _type, 300, 120, 150); Add("Quantity", _quantity, 470, 120, 130); Add("Reason", _reason, 610, 120, 220);
        _type.Items.AddRange(["Received", "Adjustment"]); _type.SelectedIndex = 0;
        var save = UiStyle.Button("Record movement", new Point(845, 142), new Size(125, 34), true); save.Click += async (_, _) => await SaveAsync(); Controls.Add(save);
        _grid.Location = new Point(28, 205); _grid.Size = new Size(942, 350); _grid.ReadOnly = true; _grid.AutoGenerateColumns = true; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.BackgroundColor = Color.White; _grid.RowHeadersVisible = false; Controls.Add(_grid);
    }
    private void Add(string label, Control control, int left, int top, int width) { Controls.Add(new Label { Text = label, Location = new Point(left, top), AutoSize = true }); control.Location = new Point(left, top + 22); control.Size = new Size(width, 28); Controls.Add(control); }
    private async Task LoadAsync() { var products = (await _controller.SearchAsync(string.Empty)).ToList(); _product.DataSource = products; _product.DisplayMember = nameof(Product.Name); _product.ValueMember = nameof(Product.ProductId); _grid.DataSource = (await _controller.GetMovementsAsync()).ToList(); }
    private async Task SaveAsync()
    {
        if (_product.SelectedValue is null || string.IsNullOrWhiteSpace(_reason.Text) || _quantity.Value == 0) { MessageBox.Show("Select a product and enter a non-zero quantity and reason."); return; }
        try { var id = Convert.ToInt32(_product.SelectedValue); if (_type.SelectedItem?.ToString() == "Received") await _controller.AddStockAsync(id, Math.Abs(_quantity.Value), _reason.Text, _user.UserId); else await _controller.AddAdjustmentAsync(id, _quantity.Value, _reason.Text, _user.UserId); _reason.Clear(); _quantity.Value = 0; _grid.DataSource = (await _controller.GetMovementsAsync()).ToList(); MessageBox.Show("Stock movement recorded.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch (Exception ex) { MessageBox.Show(ex.Message, "Stock movement error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
