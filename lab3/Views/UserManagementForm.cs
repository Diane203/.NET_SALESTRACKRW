using lab3.Data;
using lab3.Models;

namespace lab3.Views;

public sealed class UserManagementForm : Form
{
    private readonly UserRepository _repository;
    private readonly DataGridView _grid = new();
    private readonly TextBox _username = new();
    private readonly TextBox _fullName = new();
    private readonly TextBox _password = new();
    private readonly ComboBox _role = new();
    private int? _selectedUserId;

    public UserManagementForm(UserRepository repository)
    {
        _repository = repository; BuildView(); Shown += async (_, _) => await LoadUsersAsync();
    }

    private void BuildView()
    {
        UiStyle.ConfigureForm(this, "User Accounts", new Size(980, 620)); Controls.Add(UiStyle.Header("User Accounts", "Administrator access • create and deactivate staff accounts"));
        var back = UiStyle.Button("← Dashboard", new Point(815, 118), new Size(130, 32)); back.Click += (_, _) => Close(); Controls.Add(back);
        _grid.Location = new Point(28, 170); _grid.Size = new Size(917, 245); _grid.ReadOnly = true; _grid.AutoGenerateColumns = true; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.BackgroundColor = Color.White; _grid.BorderStyle = BorderStyle.None; _grid.RowHeadersVisible = false; _grid.SelectionChanged += (_, _) => SelectUser(); Controls.Add(_grid);
        var group = new GroupBox { Text = "Create account", Location = new Point(28, 440), Size = new Size(917, 120), ForeColor = UiStyle.Navy };
        AddField(group, "Username", _username, 20); AddField(group, "Full name", _fullName, 185); AddField(group, "Password", _password, 350); AddField(group, "Role", _role, 515); _password.UseSystemPasswordChar = true; _role.Items.AddRange([UserRole.Administrator, UserRole.Cashier]); _role.SelectedIndex = 1;
        var create = UiStyle.Button("Create account", new Point(745, 47), new Size(140, 32), true); create.Click += async (_, _) => await CreateUserAsync(); group.Controls.Add(create); Controls.Add(group);
        var update = UiStyle.Button("Update selected", new Point(220, 575), new Size(150, 32)); update.Click += async (_, _) => await UpdateUserAsync(); Controls.Add(update);
        var deactivate = UiStyle.Button("Deactivate selected", new Point(28, 575), new Size(170, 32)); deactivate.Click += async (_, _) => await DeactivateUserAsync(); Controls.Add(deactivate);
    }

    private static void AddField(Control parent, string label, Control field, int left)
    {
        parent.Controls.Add(new Label { Text = label, AutoSize = true, Location = new Point(left, 25) }); field.Location = new Point(left, 50); field.Size = new Size(140, 26); parent.Controls.Add(field);
    }

    private async Task LoadUsersAsync()
    {
        try { _grid.DataSource = (await _repository.GetAllAsync()).ToList(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "User loading error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task CreateUserAsync()
    {
        if (string.IsNullOrWhiteSpace(_username.Text) || string.IsNullOrWhiteSpace(_fullName.Text) || _password.Text.Length < 6) { MessageBox.Show("Username and full name are required. Password must contain at least 6 characters.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try { await _repository.CreateAsync(_username.Text, _password.Text, _fullName.Text, (UserRole)_role.SelectedItem!); _username.Clear(); _fullName.Clear(); _password.Clear(); await LoadUsersAsync(); MessageBox.Show("User account created.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "User creation error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task DeactivateUserAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not User user) { MessageBox.Show("Select a user first."); return; }
        if (MessageBox.Show($"Deactivate {user.FullName}?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try { await _repository.DeactivateAsync(user.UserId); await LoadUsersAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Deactivation error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void SelectUser()
    {
        if (_grid.CurrentRow?.DataBoundItem is not User user) return;
        _selectedUserId = user.UserId; _username.Text = user.Username; _fullName.Text = user.FullName; _role.SelectedItem = user.Role; _password.Clear();
    }

    private async Task UpdateUserAsync()
    {
        if (_selectedUserId is null) { MessageBox.Show("Select a user first."); return; }
        try { await _repository.UpdateAsync(_selectedUserId.Value, _fullName.Text, _password.Text, (UserRole)_role.SelectedItem!); await LoadUsersAsync(); MessageBox.Show("User account updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "User update error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
