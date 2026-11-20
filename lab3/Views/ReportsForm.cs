using lab3.Controllers;
using lab3.Models;
using System.Text;

namespace lab3.Views;

public sealed class ReportsForm : Form
{
    private readonly ReportController _controller;
    private readonly User _user;
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd MMM yyyy", Width = 145 };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd MMM yyyy", Width = 145 };
    private readonly Label _sales = CreateValueLabel("0.00");
    private readonly Label _transactions = CreateValueLabel("0");
    private readonly Label _lowStock = CreateValueLabel("0");
    private readonly Label _credit = CreateValueLabel("0.00");
    private readonly Label _products = CreateValueLabel("0");
    private readonly Label _inventoryValue = CreateValueLabel("0.00");
    private readonly Label _status = new();
    private readonly ComboBox _period = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 125 };
    private readonly DataGridView _transactionsGrid = new();
    private IReadOnlyList<ReportTransaction> _transactionRows = [];

    public ReportsForm(ReportController controller, User user)
    {
        _controller = controller; _user = user; _from.Value = DateTime.Today.AddDays(-30); _to.Value = DateTime.Today;
        BuildView(); Shown += async (_, _) => await LoadSummaryAsync();
    }

    private void BuildView()
    {
        UiStyle.ConfigureForm(this, "Reports", new Size(1040, 760)); Controls.Add(UiStyle.Header("Business Reports", "Monitor sales performance and current inventory"));
        var filterPanel = new Panel { Location = new Point(28, 112), Size = new Size(650, 66), BackColor = Color.White, Padding = new Padding(14) };
        filterPanel.Controls.Add(new Label { Text = "Reporting period", AutoSize = true, Location = new Point(14, 8), ForeColor = UiStyle.Navy, Font = new Font("Segoe UI", 10F, FontStyle.Bold) });
        filterPanel.Controls.Add(new Label { Text = "From", AutoSize = true, Location = new Point(145, 8), ForeColor = UiStyle.Muted });
        filterPanel.Controls.Add(new Label { Text = "To", AutoSize = true, Location = new Point(310, 8), ForeColor = UiStyle.Muted });
        _from.Location = new Point(145, 28); _to.Location = new Point(310, 28);
        filterPanel.Controls.AddRange([_from, _to]);
        _period.Items.AddRange(["Daily", "Weekly", "Monthly", "Annually", "Custom"]); _period.SelectedIndex = 0; _period.Location = new Point(14, 28); _period.SelectedIndexChanged += (_, _) => ApplyPeriod();
        filterPanel.Controls.Add(_period);
        var refresh = UiStyle.Button("⟳  Refresh", new Point(475, 27), new Size(85, 34), true); refresh.Click += async (_, _) => await LoadSummaryAsync();
        filterPanel.Controls.Add(refresh);
        Controls.Add(filterPanel);
        var back = UiStyle.Button("← Dashboard", new Point(875, 130), new Size(130, 32)); back.Click += (_, _) => Close(); Controls.Add(back);
        AddCard("Sales total", _sales, "RWF", 28, 185, Color.FromArgb(220, 252, 231)); AddCard("Transactions", _transactions, "sales", 220, 185, Color.FromArgb(209, 250, 229)); AddCard("Low-stock items", _lowStock, "items", 412, 185, Color.FromArgb(254, 243, 199)); AddCard("Outstanding credit", _credit, "RWF", 604, 185, Color.FromArgb(254, 226, 226)); AddCard("Active products", _products, "products", 796, 185, Color.FromArgb(236, 253, 245));
        var note = new Label { Text = "Sales and credit use the selected date range. Inventory metrics show your current active products.", AutoSize = true, Location = new Point(30, 350), ForeColor = UiStyle.Muted }; Controls.Add(note);
        _status.Text = "No sales have been recorded for this period yet."; _status.AutoSize = true; _status.Location = new Point(30, 380); _status.ForeColor = UiStyle.Muted; Controls.Add(_status);
        var inventoryNote = new Label { Text = "Current inventory value: 0.00 RWF", AutoSize = true, Location = new Point(30, 410), ForeColor = UiStyle.Navy, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Name = "inventoryNote" }; Controls.Add(inventoryNote);
        var export = UiStyle.Button("Download Excel report", new Point(815, 400), new Size(190, 36), true); export.Click += (_, _) => ExportReport(); Controls.Add(export);
        var delete = UiStyle.Button("Delete selected sale", new Point(600, 400), new Size(195, 36)); delete.Enabled = _user.Role == UserRole.Administrator; delete.Click += async (_, _) => await DeleteSelectedSaleAsync(); Controls.Add(delete);
        var edit = UiStyle.Button("Edit payment", new Point(405, 400), new Size(180, 36)); edit.Enabled = _user.Role == UserRole.Administrator; edit.Click += async (_, _) => await EditSelectedPaymentAsync(); Controls.Add(edit);
        var editLines = UiStyle.Button("Edit sale items", new Point(210, 400), new Size(180, 36)); editLines.Enabled = _user.Role == UserRole.Administrator; editLines.Click += async (_, _) => await EditSelectedSaleLinesAsync(); Controls.Add(editLines);
        ConfigureTransactionsGrid();
    }

    private async Task EditSelectedPaymentAsync()
    {
        if (_transactionsGrid.CurrentRow?.DataBoundItem is not ReportTransaction selected) return;
        using var dialog = new Form { Text = "Edit sale payment", StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(360, 190), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var amount = new NumericUpDown { DecimalPlaces = 2, Minimum = 0, Maximum = 100000000, Value = Math.Min(100000000, selected.AmountReceived), Location = new Point(24, 45), Width = 145 };
        var method = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(190, 45), Width = 145 }; method.Items.AddRange(["Cash", "Mobile Money", "Bank Transfer", "Other"]); method.SelectedItem = selected.PaymentMethod; if (method.SelectedIndex < 0) method.SelectedIndex = 0;
        dialog.Controls.Add(new Label { Text = "Amount received", Location = new Point(24, 20), AutoSize = true }); dialog.Controls.Add(new Label { Text = "Payment method", Location = new Point(190, 20), AutoSize = true }); dialog.Controls.Add(amount); dialog.Controls.Add(method);
        var save = new Button { Text = "Save", DialogResult = DialogResult.OK, Location = new Point(190, 110), Width = 145 }; var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(24, 110), Width = 145 }; dialog.Controls.AddRange([save, cancel]); dialog.AcceptButton = save; dialog.CancelButton = cancel;
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { await _controller.UpdateSalePaymentAsync(selected.SaleId, amount.Value, method.SelectedItem?.ToString() ?? "Cash"); await LoadSummaryAsync(); MessageBox.Show("Sale payment updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch (Exception ex) { MessageBox.Show(ex.Message, "Sale update error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task DeleteSelectedSaleAsync()
    {
        if (_transactionsGrid.CurrentRow?.DataBoundItem is not ReportTransaction selected) return;
        if (MessageBox.Show($"Delete sale {selected.TransactionNumber}? The stock will be restored and its credit record removed.", "Confirm sale deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            await _controller.DeleteSaleAsync(selected.SaleId);
            await LoadSummaryAsync();
            MessageBox.Show("The sale was deleted and inventory was restored.", "Sale deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Sale deletion error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ConfigureTransactionsGrid()
    {
        _transactionsGrid.Location = new Point(28, 450); _transactionsGrid.Size = new Size(977, 260); _transactionsGrid.ReadOnly = true; _transactionsGrid.AutoGenerateColumns = false; _transactionsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _transactionsGrid.BackgroundColor = Color.White; _transactionsGrid.BorderStyle = BorderStyle.None; _transactionsGrid.RowHeadersVisible = false;
        AddTransactionColumn("Transaction", nameof(ReportTransaction.TransactionNumber), "");
        AddTransactionColumn("Date", nameof(ReportTransaction.SaleDate), "g");
        AddTransactionColumn("Customer", nameof(ReportTransaction.CustomerName), "");
        AddTransactionColumn("Items", nameof(ReportTransaction.ItemCount), "N0");
        AddTransactionColumn("Total (RWF)", nameof(ReportTransaction.Total), "N2");
        AddTransactionColumn("Received (RWF)", nameof(ReportTransaction.AmountReceived), "N2");
        AddTransactionColumn("Payment", nameof(ReportTransaction.PaymentStatus), "");
        Controls.Add(_transactionsGrid);
    }

    private void AddTransactionColumn(string header, string property, string format) => _transactionsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, DataPropertyName = property, DefaultCellStyle = new DataGridViewCellStyle { Format = format } });

    private void ApplyPeriod()
    {
        var today = DateTime.Today;
        switch (_period.SelectedItem?.ToString())
        {
            case "Daily": _from.Value = today; _to.Value = today; break;
            case "Weekly": _from.Value = today.AddDays(-(int)today.DayOfWeek); _to.Value = today; break;
            case "Monthly": _from.Value = new DateTime(today.Year, today.Month, 1); _to.Value = today; break;
            case "Annually": _from.Value = new DateTime(today.Year, 1, 1); _to.Value = today; break;
        }
    }

    private void AddCard(string title, Label value, string suffix, int left, int top, Color color)
    {
        var card = new Panel { Location = new Point(left, top), Size = new Size(210, 145), BackColor = color, Padding = new Padding(18) };
        card.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = UiStyle.Navy, Location = new Point(18, 18) }); value.Location = new Point(18, 55); card.Controls.Add(value); card.Controls.Add(new Label { Text = suffix, AutoSize = true, ForeColor = UiStyle.Muted, Location = new Point(18, 105) }); Controls.Add(card);
    }

    private async Task LoadSummaryAsync()
    {
        try
        {
            if (_from.Value.Date > _to.Value.Date)
            {
                MessageBox.Show("The From date cannot be after the To date.", "Invalid date range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var from = _from.Value.Date; var to = _to.Value.Date.AddDays(1);
            var summary = await _controller.GetSummaryAsync(from, to);
            _transactionRows = await _controller.GetTransactionsAsync(from, to);
            _transactionsGrid.DataSource = _transactionRows.ToList();
            _sales.Text = summary.SalesTotal.ToString("N2"); _transactions.Text = summary.TransactionCount.ToString(); _lowStock.Text = summary.LowStockCount.ToString(); _credit.Text = summary.OutstandingCredit.ToString("N2"); _products.Text = summary.ActiveProductCount.ToString();
            _status.Text = summary.TransactionCount == 0 ? "No sales have been recorded for this period yet." : $"Showing {summary.TransactionCount:N0} transaction(s) in the selected period.";
            if (Controls["inventoryNote"] is Label inventoryNote) inventoryNote.Text = $"Current inventory: {summary.StockUnits:N3} units • Estimated value: {summary.InventoryValue:N2} RWF";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Report error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ExportReport()
    {
        if (_transactionRows.Count == 0)
        {
            MessageBox.Show("There are no transactions in the selected period to export.", "Export report", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog { Filter = "Excel CSV file (*.csv)|*.csv", FileName = $"sales-report-{_from.Value:yyyyMMdd}-{_to.Value:yyyyMMdd}.csv", AddExtension = true, DefaultExt = "csv" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var csv = new StringBuilder();
        csv.AppendLine("Transaction,Date,Customer,Cashier,Sale Type,Items,Total (RWF),Received (RWF),Payment Method,Payment Status");
        foreach (var transaction in _transactionRows)
        {
            var values = new[] { transaction.TransactionNumber, transaction.SaleDate.ToString("yyyy-MM-dd HH:mm:ss"), transaction.CustomerName, transaction.CashierName, transaction.SaleType, transaction.ItemCount.ToString(), transaction.Total.ToString("F2"), transaction.AmountReceived.ToString("F2"), transaction.PaymentMethod, transaction.PaymentStatus };
            csv.AppendLine(string.Join(",", values.Select(EscapeCsv)));
        }
        File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(true));
        MessageBox.Show("The report was exported successfully and can be opened in Excel.", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private async Task EditSelectedSaleLinesAsync()
    {
        if (_transactionsGrid.CurrentRow?.DataBoundItem is not ReportTransaction selected) return;
        var lines = (await _controller.GetSaleLinesAsync(selected.SaleId)).ToList();
        if (lines.Count == 0) { MessageBox.Show("This sale has no editable line items.", "Edit sale", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        using var dialog = new Form { Text = $"Edit sale items - {selected.TransactionNumber}", StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(520, 360), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var grid = new DataGridView { Location = new Point(20, 20), Size = new Size(480, 240), ReadOnly = false, AllowUserToAddRows = false, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White, RowHeadersVisible = false };
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Product", DataPropertyName = nameof(SaleEditLine.ProductName), ReadOnly = true, Width = 220 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Quantity", DataPropertyName = nameof(SaleEditLine.Quantity), Width = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unit price", DataPropertyName = nameof(SaleEditLine.UnitPrice), ReadOnly = true, Width = 120 });
        grid.DataSource = lines;
        var save = new Button { Text = "Save and update stock", DialogResult = DialogResult.OK, Location = new Point(320, 290), Width = 180 }; var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(20, 290), Width = 120 }; dialog.Controls.AddRange([grid, save, cancel]); dialog.AcceptButton = save; dialog.CancelButton = cancel;
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        grid.EndEdit();
        try { await _controller.UpdateSaleLinesAsync(selected.SaleId, lines); await LoadSummaryAsync(); MessageBox.Show("Sale items updated and stock recalculated.", "Sale updated", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch (Exception ex) { MessageBox.Show(ex.Message, "Sale edit error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private static Label CreateValueLabel(string value) => new() { Text = value, AutoSize = true, Font = new Font("Segoe UI", 21F, FontStyle.Bold), ForeColor = UiStyle.Navy };
}
