using lab3.Models;
using lab3.Controllers;

namespace lab3.Views;

public sealed class DashboardForm : Form
{
    private readonly User _currentUser;
    private readonly ApplicationServices _services;
    private readonly Button _logoutButton = new();

    private static readonly Color Navy = UiStyle.Navy;
    private static readonly Color PageBackground = UiStyle.Page;
    private static readonly Color CardBackground = Color.White;
    private static readonly Color AccentGreen = UiStyle.Green;

    public event EventHandler? LogoutRequested;

    public DashboardForm(User currentUser, ApplicationServices services)
    {
        _currentUser = currentUser; _services = services;
        BuildView();
    }

    private void BuildView()
    {
        Text = "SalesTrackRW - Dashboard";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(980, 620);
        MinimumSize = new Size(900, 560);
        BackColor = PageBackground;
        Font = new Font("Segoe UI", 10F);

        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 108,
            BackColor = Navy,
            Padding = new Padding(34, 18, 34, 12)
        };

        var title = new Label
        {
            Text = "SalesTrackRW",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 23F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(34, 16)
        };

        var userLabel = new Label
        {
            Text = $"Welcome, {_currentUser.FullName}  •  {_currentUser.Role}",
            ForeColor = _currentUser.Role == UserRole.Administrator ? Color.FromArgb(253, 224, 71) : Color.FromArgb(134, 239, 172),
            AutoSize = true,
            Location = new Point(38, 63)
        };

        headerPanel.Controls.Add(title);
        headerPanel.Controls.Add(userLabel);
        headerPanel.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 5, BackColor = UiStyle.Green });
        Controls.Add(headerPanel);

        var sectionLabel = new Label
        {
            Text = "Business modules",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            AutoSize = true,
            ForeColor = Navy,
            Location = new Point(36, 140)
        };
        Controls.Add(sectionLabel);

        var salesButton = CreateModuleButton("Sales", 36, 180, true);
        salesButton.Click += (_, _) => new SalesForm(_services, _currentUser).ShowDialog(this);
        var productsButton = CreateModuleButton("Products & Stock", 220, 180, true);
        productsButton.Click += (_, _) => new ProductManagementForm(_services.Products, _currentUser).ShowDialog(this);
        var customersButton = CreateModuleButton("Customers & Credit", 404, 180, true);
        customersButton.Click += (_, _) => new CustomerManagementForm(_services.Customers, _services.Payments, _currentUser).ShowDialog(this);
        var expensesButton = CreateModuleButton("Expenses", 588, 180, true);
        expensesButton.Click += (_, _) => new ExpenseManagementForm(_services.Expenses, _currentUser).ShowDialog(this);
        var isAdministrator = _currentUser.Role == UserRole.Administrator;
        var adminButton = CreateModuleButton(isAdministrator ? "User Accounts" : "User Accounts\nAdministrator only", 36, 270, isAdministrator);
        adminButton.Click += (_, _) => new UserManagementForm(_services.Users).ShowDialog(this);

        var reportsButton = CreateModuleButton("Reports", 220, 270, true);
        reportsButton.Click += (_, _) => new ReportsForm(_services.Reports, _currentUser).ShowDialog(this);
        var information = new Label
        {
            Text = "All major business modules are available. Use the Back to Dashboard button inside each module to keep working without logging out.",
            AutoSize = false,
            Size = new Size(820, 42),
            Location = new Point(38, 372),
            ForeColor = UiStyle.Muted
        };

        var toolTip = new ToolTip { AutoPopDelay = 5000, InitialDelay = 400, ReshowDelay = 200 };
        toolTip.SetToolTip(salesButton, "Create sales and record payments");
        toolTip.SetToolTip(productsButton, "Search products and manage stock");
        toolTip.SetToolTip(customersButton, "Manage customers and credit records");
        toolTip.SetToolTip(expensesButton, "Record and review business expenses");
        toolTip.SetToolTip(reportsButton, "Review date-filtered business summaries");
        toolTip.SetToolTip(adminButton, isAdministrator ? "Create and deactivate staff accounts" : "Only administrators can manage user accounts");

        _logoutButton.Text = "Logout";
        _logoutButton.Size = new Size(140, 40);
        _logoutButton.Location = new Point(36, 490);
        _logoutButton.BackColor = Color.White;
        _logoutButton.ForeColor = Navy;
        _logoutButton.FlatStyle = FlatStyle.Flat;
        _logoutButton.FlatAppearance.BorderColor = Color.FromArgb(190, 199, 212);
        _logoutButton.Click += (_, _) => LogoutRequested?.Invoke(this, EventArgs.Empty);

        Controls.AddRange([salesButton, productsButton, customersButton, expensesButton,
            adminButton, reportsButton, _logoutButton, information]);
    }

    private static Button CreateModuleButton(string text, int left, int top, bool enabled)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(160, 68),
            Location = new Point(left, top),
            Enabled = enabled,
            BackColor = enabled ? Color.FromArgb(240, 253, 244) : Color.FromArgb(232, 235, 240),
            ForeColor = enabled ? Navy : Color.FromArgb(125, 145, 132),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, enabled ? FontStyle.Bold : FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleCenter
        };
        button.FlatAppearance.BorderColor = enabled ? AccentGreen : Color.FromArgb(195, 215, 201);
        button.FlatAppearance.BorderSize = enabled ? 1 : 0;
        return button;
    }
}
