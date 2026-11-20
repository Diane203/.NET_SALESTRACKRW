using lab3.Controllers;
using lab3.Models;

namespace lab3.Views;

public sealed class CategoryManagementForm : Form
{
    private readonly ProductController _controller;
    private readonly DataGridView _grid = new();
    private readonly TextBox _name = new();
    private readonly TextBox _description = new();
    private int? _selectedId;

    public CategoryManagementForm(ProductController controller)
    {
        _controller = controller; BuildView(); Shown += async (_, _) => await LoadAsync();
    }

    private void BuildView()
    {
        UiStyle.ConfigureForm(this, "Categories", new Size(760, 520)); Controls.Add(UiStyle.Header("Category Management", "Create, edit, and deactivate product categories"));
        _grid.Location = new Point(28, 120); _grid.Size = new Size(690, 230); _grid.ReadOnly = true; _grid.AutoGenerateColumns = false; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.BackgroundColor = Color.White; _grid.RowHeadersVisible = false; _grid.SelectionChanged += (_, _) => Select();
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = nameof(Category.Name) }); _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Description", DataPropertyName = nameof(Category.Description) }); Controls.Add(_grid);
        Controls.Add(new Label { Text = "Name", Location = new Point(28, 375), AutoSize = true }); Controls.Add(new Label { Text = "Description", Location = new Point(210, 375), AutoSize = true });
        _name.Location = new Point(28, 398); _name.Size = new Size(160, 28); _description.Location = new Point(210, 398); _description.Size = new Size(240, 28); Controls.AddRange([_name, _description]);
        var save = UiStyle.Button("Save category", new Point(475, 395), new Size(125, 34), true); save.Click += async (_, _) => await SaveAsync(); var deactivate = UiStyle.Button("Deactivate", new Point(610, 395), new Size(108, 34)); deactivate.Click += async (_, _) => await DeactivateAsync(); Controls.AddRange([save, deactivate]);
    }

    private async Task LoadAsync() { _grid.DataSource = (await _controller.GetCategoriesAsync()).ToList(); }
    private void Select() { if (_grid.CurrentRow?.DataBoundItem is Category category) { _selectedId = category.CategoryId; _name.Text = category.Name; _description.Text = category.Description; } }
    private async Task SaveAsync()
    {
        try { if (_selectedId.HasValue) await _controller.UpdateCategoryAsync(new Category { CategoryId = _selectedId.Value, Name = _name.Text, Description = _description.Text }); else await _controller.CreateCategoryAsync(_name.Text, _description.Text); _selectedId = null; _name.Clear(); _description.Clear(); await LoadAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Category error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private async Task DeactivateAsync()
    {
        if (!_selectedId.HasValue) return; if (MessageBox.Show("Deactivate this category?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try { await _controller.DeactivateCategoryAsync(_selectedId.Value); await LoadAsync(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Category error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
