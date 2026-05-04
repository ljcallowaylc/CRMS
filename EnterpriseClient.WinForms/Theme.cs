namespace EnterpriseClient;

public static class Theme
{
    public static readonly Color Background   = Color.FromArgb(245, 246, 250);
    public static readonly Color Surface      = Color.White;
    public static readonly Color Primary      = Color.FromArgb(26, 95, 180);
    public static readonly Color PrimaryHover = Color.FromArgb(18, 72, 145);
    public static readonly Color Danger       = Color.FromArgb(192, 28, 40);
    public static readonly Color Success      = Color.FromArgb(38, 162, 105);
    public static readonly Color Warning      = Color.FromArgb(229, 165, 10);
    public static readonly Color TextPrimary  = Color.FromArgb(26, 26, 24);
    public static readonly Color TextMuted    = Color.FromArgb(100, 100, 100);
    public static readonly Color Border       = Color.FromArgb(210, 210, 210);
    public static readonly Color GridHeader   = Color.FromArgb(240, 242, 248);
    public static readonly Color GridAlt      = Color.FromArgb(250, 251, 254);

    public static readonly Font FontBase  = new("Segoe UI", 10f);
    public static readonly Font FontSmall = new("Segoe UI", 9f);
    public static readonly Font FontBold  = new("Segoe UI", 10f, FontStyle.Bold);
    public static readonly Font FontTitle = new("Segoe UI", 16f, FontStyle.Bold);
    public static readonly Font FontSub   = new("Segoe UI", 11f, FontStyle.Bold);

    public static Button MakeButton(string text, Color? bg = null, Color? fg = null)
    {
        var b = new Button
        {
            Text = text, FlatStyle = FlatStyle.Flat,
            BackColor = bg ?? Primary, ForeColor = fg ?? Color.White,
            Font = FontBold, Height = 36, Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = bg.HasValue ? ControlPaint.Light(bg.Value, 0.2f) : PrimaryHover;
        return b;
    }

    public static TextBox MakeTextBox(bool password = false)
    {
        var t = new TextBox
        {
            Font = FontBase, BorderStyle = BorderStyle.FixedSingle,
            BackColor = Surface, ForeColor = TextPrimary, Height = 28
        };
        if (password) t.UseSystemPasswordChar = true;
        return t;
    }

    public static DataGridView MakeGrid()
    {
        var g = new DataGridView
        {
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            BackgroundColor = Surface, GridColor = Border, Font = FontBase,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false
        };
        g.ColumnHeadersDefaultCellStyle.BackColor = GridHeader;
        g.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
        g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        g.ColumnHeadersDefaultCellStyle.Padding = new Padding(4);
        g.ColumnHeadersHeight = 34;
        g.DefaultCellStyle.BackColor = Surface;
        g.DefaultCellStyle.ForeColor = TextPrimary;
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 228, 255);
        g.DefaultCellStyle.SelectionForeColor = TextPrimary;
        g.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);
        g.RowTemplate.Height = 32;
        g.AlternatingRowsDefaultCellStyle.BackColor = GridAlt;
        return g;
    }

    public static Color StatusColor(string status) => status switch
    {
        "Available" or "Active" or "Approved" => Success,
        "Pending" or "Rented"                 => Warning,
        "Completed"                           => TextMuted,
        _                                     => Danger
    };

    public static void StyleForm(Form f)
    {
        f.BackColor = Background;
        f.Font = FontBase;
        f.ForeColor = TextPrimary;
    }
}