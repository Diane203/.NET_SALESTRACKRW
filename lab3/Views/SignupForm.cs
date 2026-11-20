using lab3.Data;
using lab3.Models;

namespace lab3.Views;

public sealed class SignupForm : Form
{
    private readonly UserRepository _repository;
    private readonly TextBox _fullName = new();
    private readonly TextBox _username = new();
    private readonly TextBox _password = new();
    private readonly TextBox _confirmPassword = new();
    private readonly ComboBox _role = new();

    public string RegisteredUsername => _username.Text.Trim();

    public SignupForm(UserRepository repository)
    {
        _repository = repository;
        BuildView();
    }

    private void BuildView()
    {
        UiStyle.ConfigureForm(this, "Create Account", new Size(560, 580));
        Controls.Add(UiStyle.Header("Create your account", "Choose Administrator or Salesperson access"));

        var card = new Panel { Location = new Point(55, 125), Size = new Size(450, 360), BackColor = Color.White, Padding = new Padding(28) };
        AddField(card, "Full name", _fullName, 25);
        AddField(card, "Username", _username, 78);
        AddField(card, "Password", _password, 131);
        AddField(card, "Confirm password", _confirmPassword, 184);
        _password.UseSystemPasswordChar = true;
        _confirmPassword.UseSystemPasswordChar = true;
        var roleLabel = new Label { Text = "Privilege / role", AutoSize = true, ForeColor = UiStyle.Text, Location = new Point(28, 230) };
        _role.Location = new Point(28, 250);
        _role.Size = new Size(330, 28);
        _role.DropDownStyle = ComboBoxStyle.DropDownList;
        _role.DataSource = new[] { UserRole.Cashier, UserRole.Administrator };
        _role.Format += (_, args) => args.Value = args.ListItem is UserRole.Cashier ? "Salesperson / Cashier" : "Administrator";
        _role.SelectedItem = UserRole.Cashier;
        var note = new Label { Text = "Choose the dashboard access this account should receive.", AutoSize = true, ForeColor = UiStyle.Muted, Location = new Point(28, 285) };
        var register = UiStyle.Button("Create account", new Point(28, 310), new Size(170, 35), true);
        register.Click += async (_, _) => await RegisterAsync();
        var cancel = UiStyle.Button("Cancel", new Point(210, 310), new Size(100, 35));
        cancel.Click += (_, _) => Close();
        card.Controls.AddRange([roleLabel, _role, note, register, cancel]);
        Controls.Add(card);
    }

    private static void AddField(Control parent, string label, Control field, int top)
    {
        parent.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = UiStyle.Text, Location = new Point(28, top) });
        field.Location = new Point(28, top + 20); field.Size = new Size(330, 26); parent.Controls.Add(field);
    }

    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(_fullName.Text) || string.IsNullOrWhiteSpace(_username.Text))
        {
            MessageBox.Show("Full name and username are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_password.Text.Length < 6)
        {
            MessageBox.Show("Password must contain at least 6 characters.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_password.Text != _confirmPassword.Text)
        {
            MessageBox.Show("Passwords do not match.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            if (_role.SelectedItem is not UserRole role)
            {
                MessageBox.Show("Choose Administrator or Salesperson / Cashier.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            await _repository.RegisterAsync(_username.Text, _password.Text, _fullName.Text, role);
            var roleName = role == UserRole.Administrator ? "Administrator" : "Salesperson / Cashier";
            MessageBox.Show($"Account created successfully as {roleName}. You can now sign in.", "Registration complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Registration error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
