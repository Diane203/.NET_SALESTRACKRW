namespace lab3.Views;

public static class UiStyle
{
    public static readonly Color Navy = Color.FromArgb(20, 83, 45);
    public static readonly Color Green = Color.FromArgb(22, 163, 74);
    public static readonly Color Page = Color.FromArgb(232, 247, 237);
    public static readonly Color Surface = Color.White;
    public static readonly Color Text = Color.FromArgb(31, 63, 42);
    public static readonly Color Muted = Color.FromArgb(76, 105, 86);

    public static void ConfigureForm(Form form, string title, Size size)
    {
        form.Text = $"SalesTrackRW - {title}";
        form.StartPosition = FormStartPosition.CenterParent;
        form.ClientSize = size;
        form.BackColor = Page;
        form.Font = new Font("Segoe UI", 10F);
        form.KeyPreview = true;
        form.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode == Keys.Escape)
            {
                form.Close();
            }
        };
    }

    public static Panel Header(string title, string subtitle)
    {
        var panel = new Panel { Dock = DockStyle.Top, Height = 96, BackColor = Navy, Padding = new Padding(28, 14, 28, 10) };
        panel.Controls.Add(new Label { Text = title, ForeColor = Color.White, Font = new Font("Segoe UI", 21F, FontStyle.Bold), AutoSize = true, Location = new Point(28, 12) });
        panel.Controls.Add(new Label { Text = subtitle, ForeColor = Color.FromArgb(205, 235, 214), AutoSize = true, Location = new Point(31, 57) });
        return panel;
    }

    public static Button Button(string text, Point location, Size size, bool primary = false)
    {
        var button = new Button { Text = text, Location = location, Size = size, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, primary ? FontStyle.Bold : FontStyle.Regular), BackColor = primary ? Green : Surface, ForeColor = primary ? Color.White : Navy };
        button.FlatAppearance.BorderColor = primary ? Green : Color.FromArgb(170, 205, 181);
        return button;
    }
}
