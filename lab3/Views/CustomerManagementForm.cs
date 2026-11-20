using lab3.Controllers;
using lab3.Models;
using lab3.Services;

namespace lab3.Views;

public sealed class CustomerManagementForm : Form
{
    private readonly CustomerController _controller;
    private readonly TextBox _search = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _name = new();
    private readonly TextBox _telephone = new();
    private readonly TextBox _address = new();
    private readonly NumericUpDown _paymentAmount = new() { DecimalPlaces = 2, Minimum = 0.01M, Maximum = 100000000 };
    private readonly ComboBox _paymentMethod = new();
    private readonly CustomerPaymentService _payments;
    private readonly User _user;
    private int? _selectedCustomerId;

    public CustomerManagementForm(CustomerController controller, CustomerPaymentService payments, User user)
    {
        _controller = controller; _payments = payments; _user = user;
        BuildView();
        Shown += async (_, _) => await LoadCustomersAsync();
    }

    private void BuildView()
    {
        UiStyle.ConfigureForm(this, "Customers & Credit", new Size(980, 700));
        Controls.Add(UiStyle.Header("Customers & Credit", "Manage customer records and credit relationships"));

        _search.Location = new Point(28, 120); _search.Size = new Size(320, 28); _search.PlaceholderText = "Search name or telephone";
        var searchButton = UiStyle.Button("Search", new Point(360, 118), new Size(95, 32), true);
        searchButton.Click += async (_, _) => await LoadCustomersAsync();
        var backButton = UiStyle.Button("← Dashboard", new Point(815, 118), new Size(130, 32));
        backButton.Click += (_, _) => Close();
        Controls.AddRange([_search, searchButton, backButton]);

        _grid.Location = new Point(28, 170); _grid.Size = new Size(917, 245); _grid.ReadOnly = true; _grid.AutoGenerateColumns = false; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.SelectionChanged += (_, _) => SelectCustomer();
        _grid.BackgroundColor = Color.White; _grid.BorderStyle = BorderStyle.None; _grid.RowHeadersVisible = false;
        AddGridColumn("Customer", nameof(Customer.FullName), ""); AddGridColumn("Telephone", nameof(Customer.Telephone), ""); AddGridColumn("Address", nameof(Customer.Address), ""); AddGridColumn("Credit balance (RWF)", nameof(Customer.CreditBalance), "N2");
        Controls.Add(_grid);

        var group = new GroupBox { Text = "New customer", Location = new Point(28, 440), Size = new Size(917, 120), ForeColor = UiStyle.Navy };
        AddField(group, "Full name", _name, 20); AddField(group, "Telephone", _telephone, 250); AddField(group, "Address", _address, 480);
        var save = UiStyle.Button("Save customer", new Point(745, 47), new Size(140, 32), true); save.Click += async (_, _) => await SaveCustomerAsync(); var update = UiStyle.Button("Update selected", new Point(590, 47), new Size(145, 32)); update.Click += async (_, _) => await UpdateCustomerAsync(); group.Controls.AddRange([save, update]);
        Controls.Add(group);
        var payment = new GroupBox { Text = "Record customer payment", Location = new Point(28, 570), Size = new Size(917, 90), ForeColor = UiStyle.Navy }; AddField(payment, "Amount", _paymentAmount, 20); AddField(payment, "Method", _paymentMethod, 250); _paymentMethod.Items.AddRange(["Cash", "Mobile Money", "Bank Transfer", "Other"]); _paymentMethod.SelectedIndex = 0; var record = UiStyle.Button("Record payment", new Point(510, 47), new Size(145, 32), true); record.Click += async (_, _) => await RecordPaymentAsync(); payment.Controls.Add(record); Controls.Add(payment);
    }

    private void AddGridColumn(string header, string property, string format) => _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, DataPropertyName = property, DefaultCellStyle = new DataGridViewCellStyle { Format = format } });

    private static void AddField(Control parent, string label, Control field, int left)
    {
        parent.Controls.Add(new Label { Text = label, AutoSize = true, Location = new Point(left, 25) });
        field.Location = new Point(left, 50); field.Size = new Size(210, 26); parent.Controls.Add(field);
    }

    private async Task LoadCustomersAsync()
    {
        try { _grid.DataSource = (await _controller.SearchAsync(_search.Text)).ToList(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Customer loading error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task SaveCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show("Customer name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try
        {
            await _controller.CreateAsync(new Customer { FullName = _name.Text, Telephone = _telephone.Text, Address = _address.Text });
            _name.Clear(); _telephone.Clear(); _address.Clear(); await LoadCustomersAsync();
            MessageBox.Show("Customer saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Customer save error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void SelectCustomer() { if (_grid.CurrentRow?.DataBoundItem is Customer customer) { _selectedCustomerId = customer.CustomerId; _name.Text = customer.FullName; _telephone.Text = customer.Telephone; _address.Text = customer.Address; } }
    private async Task UpdateCustomerAsync()
    {
        if (!_selectedCustomerId.HasValue) { MessageBox.Show("Select a customer first."); return; }
        try { await _controller.UpdateAsync(new Customer { CustomerId = _selectedCustomerId.Value, FullName = _name.Text, Telephone = _telephone.Text, Address = _address.Text }); await LoadCustomersAsync(); MessageBox.Show("Customer updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch (Exception ex) { MessageBox.Show(ex.Message, "Customer update error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private async Task RecordPaymentAsync()
    {
        if (!_selectedCustomerId.HasValue) { MessageBox.Show("Select a customer first."); return; }
        try { await _payments.RecordPaymentAsync(_selectedCustomerId.Value, null, _paymentAmount.Value, _paymentMethod.SelectedItem?.ToString() ?? "Cash", _user.UserId, null); await LoadCustomersAsync(); MessageBox.Show("Customer payment recorded.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch (Exception ex) { MessageBox.Show(ex.Message, "Payment error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
