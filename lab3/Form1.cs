using lab3.Controllers;
using lab3.Models;
using lab3.Views;

namespace lab3;

public partial class Form1 : Form
{
    private readonly LoginController _loginController;
    private readonly ApplicationServices _services;
    private readonly TextBox _usernameTextBox = new();
    private readonly TextBox _passwordTextBox = new();
    private readonly ComboBox _roleComboBox = new();
    private readonly Label _messageLabel = new();
    private readonly Button _loginButton = new();

    public Form1(LoginController loginController, ApplicationServices services)
    {
        _loginController = loginController; _services = services;
        InitializeComponent();
        BuildLoginView();
    }

    private void BuildLoginView()
    {
        Text = "SalesTrackRW - Login";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(780, 520);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = UiStyle.Page;

        var background = new Panel { Dock = DockStyle.Fill, BackColor = UiStyle.Page };
        var brandPanel = new Panel { Location = new Point(0, 0), Size = new Size(300, 520), BackColor = UiStyle.Navy, Padding = new Padding(34) };
        var brandTitle = new Label { Text = "SalesTrackRW", ForeColor = Color.White, Font = new Font("Segoe UI", 25F, FontStyle.Bold), AutoSize = true, Location = new Point(34, 86) };
        var brandSubtitle = new Label { Text = "Sales & Business\nManagement", ForeColor = Color.FromArgb(210, 239, 218), Font = new Font("Segoe UI", 15F), AutoSize = true, Location = new Point(38, 140) };
        var brandLine = new Panel { Location = new Point(38, 222), Size = new Size(80, 4), BackColor = UiStyle.Green };
        var brandNote = new Label { Text = "Manage stock, customers,\nsales, payments and reports\nfrom one simple workspace.", ForeColor = Color.FromArgb(190, 224, 199), Font = new Font("Segoe UI", 10F), AutoSize = true, Location = new Point(38, 250) };
        brandPanel.Controls.AddRange([brandTitle, brandSubtitle, brandLine, brandNote]);

        var card = new Panel { Location = new Point(350, 32), Size = new Size(370, 450), BackColor = Color.White, Padding = new Padding(32) };
        var cardTitle = new Label { Text = "Welcome back", ForeColor = UiStyle.Navy, Font = new Font("Segoe UI", 20F, FontStyle.Bold), AutoSize = true, Location = new Point(32, 28) };
        var cardSubtitle = new Label { Text = "Sign in to continue to your workspace", ForeColor = UiStyle.Muted, AutoSize = true, Location = new Point(35, 67) };
        card.Controls.AddRange([cardTitle, cardSubtitle]);

        AddField(card, "Username", _usernameTextBox, 108);
        AddField(card, "Password", _passwordTextBox, 170);
        _passwordTextBox.UseSystemPasswordChar = true;
        AddRoleField(card);

        _loginButton.Text = "Login";
        _loginButton.Size = new Size(306, 40);
        _loginButton.Location = new Point(32, 290);
        _loginButton.BackColor = UiStyle.Green;
        _loginButton.ForeColor = Color.White;
        _loginButton.FlatStyle = FlatStyle.Flat;
        _loginButton.FlatAppearance.BorderSize = 0;
        _loginButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _loginButton.Click += LoginButton_Click;

        _messageLabel.AutoSize = false;
        _messageLabel.TextAlign = ContentAlignment.MiddleCenter;
        _messageLabel.Size = new Size(306, 48);
        _messageLabel.Location = new Point(32, 340);
        _messageLabel.ForeColor = UiStyle.Muted;

        var signupButton = UiStyle.Button("Create a new account", new Point(32, 395), new Size(306, 30));
        signupButton.FlatAppearance.BorderSize = 0;
        signupButton.Click += (_, _) => OpenSignup();

        card.Controls.AddRange([_loginButton, _messageLabel, signupButton]);
        background.Controls.AddRange([brandPanel, card]);
        Controls.Add(background);
        _usernameTextBox.KeyDown += LoginTextBox_KeyDown;
        _passwordTextBox.KeyDown += LoginTextBox_KeyDown;
        AcceptButton = _loginButton;
    }

    private static void AddField(Control parent, string labelText, TextBox textBox, int top)
    {
        var label = new Label { Text = labelText, AutoSize = true, ForeColor = UiStyle.Text, Location = new Point(32, top) };
        textBox.Location = new Point(32, top + 22);
        textBox.Size = new Size(306, 28);
        textBox.BorderStyle = BorderStyle.FixedSingle;
        parent.Controls.Add(label);
        parent.Controls.Add(textBox);
    }

    private void AddRoleField(Control parent)
    {
        parent.Controls.Add(new Label { Text = "Sign in as", AutoSize = true, ForeColor = UiStyle.Text, Location = new Point(32, 225) });
        _roleComboBox.Location = new Point(32, 247);
        _roleComboBox.Size = new Size(306, 28);
        _roleComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _roleComboBox.DataSource = new[] { UserRole.Cashier, UserRole.Administrator };
        _roleComboBox.Format += (_, args) => args.Value = args.ListItem is UserRole.Cashier ? "Salesperson / Cashier" : "Administrator";
        _roleComboBox.SelectedItem = UserRole.Cashier;
        parent.Controls.Add(_roleComboBox);
    }

    private async void LoginButton_Click(object? sender, EventArgs e)
    {
        await LoginAsync();
    }

    private async void LoginTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            await LoginAsync();
        }
    }

    private async Task LoginAsync()
    {
        _loginButton.Enabled = false;
        try
        {
            if (_roleComboBox.SelectedItem is not UserRole selectedRole)
            {
                _messageLabel.ForeColor = Color.Firebrick;
                _messageLabel.Text = "Choose a role before signing in.";
                return;
            }

            var result = await _loginController.LoginAsync(_usernameTextBox.Text, _passwordTextBox.Text, selectedRole);
            _messageLabel.ForeColor = result.Success ? Color.DarkGreen : Color.Firebrick;
            _messageLabel.Text = result.Message;

            if (result.Success && result.User is not null)
            {
                var dashboard = new DashboardForm(result.User, _services);
                dashboard.LogoutRequested += (_, _) =>
                {
                    dashboard.Hide();
                    Show();
                    _passwordTextBox.Clear();
                };
                dashboard.FormClosed += (_, _) =>
                {
                    if (!Visible)
                    {
                        Close();
                    }
                };
                Hide();
                dashboard.Show();
            }
        }
        catch (Exception exception)
        {
            _messageLabel.ForeColor = Color.Firebrick;
            _messageLabel.Text = "Unable to connect to the database.";
            MessageBox.Show(exception.Message, "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _loginButton.Enabled = true;
        }
    }

    private void OpenSignup()
    {
        using var signupForm = new SignupForm(_services.Users);
        if (signupForm.ShowDialog(this) == DialogResult.OK)
        {
            _usernameTextBox.Text = signupForm.RegisteredUsername;
            _passwordTextBox.Clear();
            _messageLabel.ForeColor = UiStyle.Navy;
            _messageLabel.Text = "Account created. Enter your password to sign in.";
            _passwordTextBox.Focus();
        }
    }
}
