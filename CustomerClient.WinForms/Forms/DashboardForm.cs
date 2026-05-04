namespace CustomerClient.Forms;

public class DashboardForm : Form
{
    private readonly TabControl _tabs;
    private readonly TabPage    _tabCars;
    private readonly TabPage    _tabBookings;

    private readonly DataGridView _gridCars;
    private readonly Button       _btnRefreshCars;
    private readonly Button       _btnBookSelected;
    private readonly Label        _lblCarsStatus;
    private List<CarResponse>     _cars = [];

    private readonly DataGridView  _gridBookings;
    private readonly Button        _btnRefreshBookings;
    private readonly Button        _btnCancelBooking;
    private readonly Label         _lblBookingsStatus;
    private List<BookingResponse>  _bookings = [];

    public DashboardForm()
    {
        Theme.StyleForm(this);
        Text = $"CRMS — Customer Dashboard  [{Program.Api.Username}]";
        Size = new Size(920, 640);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(750, 500);

        // Top bar
        var topBar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Theme.Primary };
        var lblTitle = new Label
        {
            Text = "🚗  CRMS Customer Portal", Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.White, AutoSize = true, Location = new Point(12, 14)
        };
        var btnSignOut = new Button
        {
            Text = "Sign Out", FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(80, 120, 180), ForeColor = Color.White,
            Font = Theme.FontSmall, Size = new Size(80, 28), Cursor = Cursors.Hand
        };
        btnSignOut.FlatAppearance.BorderSize = 0;
        btnSignOut.Click += (_, _) => { Program.Api.ClearCredentials(); Close(); };
        topBar.Controls.AddRange(new Control[] { lblTitle, btnSignOut });
        topBar.Resize += (_, _) => btnSignOut.Location = new Point(topBar.Width - 96, 10);

        // Tabs
        _tabs = new TabControl { Dock = DockStyle.Fill, Font = Theme.FontBase, Padding = new Point(16, 6) };
        _tabCars     = new TabPage("  Available Cars  ") { BackColor = Theme.Background };
        _tabBookings = new TabPage("  My Bookings  ")    { BackColor = Theme.Background };

        // Cars tab
        _gridCars = Theme.MakeGrid();
        _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID",           Name = "Id",      Width = 50  });
        _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Make",         Name = "Make",    FillWeight = 12 });
        _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Model",        Name = "Model",   FillWeight = 12 });
        _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Year",         Name = "Year",    Width = 60  });
        _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Category",     Name = "Category",FillWeight = 10 });
        _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Colour",       Name = "Colour",  FillWeight = 10 });
        _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Daily Rate",   Name = "Rate",    FillWeight = 10 });
        _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Licence Plate",Name = "Plate",   FillWeight = 12 });
        _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status",       Name = "Status",  FillWeight = 10 });
        _gridCars.CellFormatting += (_, e) =>
        {
            if (_gridCars.Columns[e.ColumnIndex].Name == "Status" && e.Value is string s)
            { e.CellStyle.ForeColor = Theme.StatusColor(s); e.CellStyle.Font = Theme.FontBold; }
        };
        _gridCars.SelectionChanged += (_, _) => UpdateCarButtons();

        _btnRefreshCars  = Theme.MakeButton("↻  Refresh", Theme.Background, Theme.TextPrimary);
        _btnRefreshCars.Size = new Size(110, 32);
        _btnRefreshCars.Click += async (_, _) => await LoadCarsAsync();

        _btnBookSelected = Theme.MakeButton("Book This Car →");
        _btnBookSelected.Size = new Size(150, 32);
        _btnBookSelected.Enabled = false;
        _btnBookSelected.Click += (_, _) => OpenBookingDialog();

        _lblCarsStatus = new Label { Text = "Loading…", Font = Theme.FontSmall, ForeColor = Theme.TextMuted, AutoSize = true };

        var carsBar = MakeToolbar(_lblCarsStatus, _btnRefreshCars, _btnBookSelected);
        _gridCars.Dock = DockStyle.Fill;
        carsBar.Dock = DockStyle.Top;
        _tabCars.Controls.Add(_gridCars);
        _tabCars.Controls.Add(carsBar);

        // Bookings tab
        _gridBookings = Theme.MakeGrid();
        _gridBookings.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID",          Name = "Id",     Width = 50  });
        _gridBookings.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Car",          Name = "Car",    FillWeight = 28 });
        _gridBookings.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Pickup Date",  Name = "Pickup", FillWeight = 14 });
        _gridBookings.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Return Date",  Name = "Return", FillWeight = 14 });
        _gridBookings.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Total",        Name = "Total",  FillWeight = 10 });
        _gridBookings.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status",       Name = "Status", FillWeight = 12 });
        _gridBookings.CellFormatting += (_, e) =>
        {
            if (_gridBookings.Columns[e.ColumnIndex].Name == "Status" && e.Value is string s)
            { e.CellStyle.ForeColor = Theme.StatusColor(s); e.CellStyle.Font = Theme.FontBold; }
        };
        _gridBookings.SelectionChanged += (_, _) => UpdateBookingButtons();

        _btnRefreshBookings = Theme.MakeButton("↻  Refresh", Theme.Background, Theme.TextPrimary);
        _btnRefreshBookings.Size = new Size(110, 32);
        _btnRefreshBookings.Click += async (_, _) => await LoadBookingsAsync();

        _btnCancelBooking = Theme.MakeButton("Cancel Booking", Theme.Danger);
        _btnCancelBooking.Size = new Size(140, 32);
        _btnCancelBooking.Enabled = false;
        _btnCancelBooking.Click += async (_, _) => await CancelBookingAsync();

        _lblBookingsStatus = new Label { Text = "Loading…", Font = Theme.FontSmall, ForeColor = Theme.TextMuted, AutoSize = true };

        var bookingsBar = MakeToolbar(_lblBookingsStatus, _btnRefreshBookings, _btnCancelBooking);
        _gridBookings.Dock = DockStyle.Fill;
        bookingsBar.Dock = DockStyle.Top;
        _tabBookings.Controls.Add(_gridBookings);
        _tabBookings.Controls.Add(bookingsBar);

        _tabs.TabPages.Add(_tabCars);
        _tabs.TabPages.Add(_tabBookings);
        _tabs.SelectedIndexChanged += async (_, _) =>
        {
            if (_tabs.SelectedTab == _tabBookings) await LoadBookingsAsync();
        };

        Controls.Add(_tabs);
        Controls.Add(topBar);
        Load += async (_, _) => await LoadCarsAsync();
    }

    private static Panel MakeToolbar(Label status, params Button[] buttons)
    {
        var p = new Panel { Height = 48, BackColor = Theme.Surface, Padding = new Padding(8) };
        p.Paint += (_, e) => { using var pen = new Pen(Theme.Border); e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1); };
        status.Location = new Point(8, 14);
        p.Controls.Add(status);
        p.Resize += (_, _) => LayoutRight(p, buttons);
        foreach (var b in buttons) p.Controls.Add(b);
        LayoutRight(p, buttons);
        return p;
    }

    private static void LayoutRight(Panel p, Button[] buttons)
    {
        int x = p.ClientSize.Width - 8;
        foreach (var b in buttons.Reverse()) { x -= (b.Width + 8); b.Location = new Point(x, 8); b.Anchor = AnchorStyles.Right | AnchorStyles.Top; }
    }

    private async Task LoadCarsAsync()
    {
        _lblCarsStatus.Text = "Loading…";
        _btnRefreshCars.Enabled = false;
        _cars = await Program.Api.GetCarsAsync();
        _gridCars.Rows.Clear();
        foreach (var c in _cars)
            _gridCars.Rows.Add(c.Id, c.Make, c.Model, c.Year, c.Category, c.Colour, $"${c.DailyRate:F2}", c.LicencePlate, c.Status);
        _lblCarsStatus.Text = $"{_cars.Count} car(s) found";
        _btnRefreshCars.Enabled = true;
        UpdateCarButtons();
    }

    private void UpdateCarButtons()
    {
        if (_gridCars.SelectedRows.Count == 0) { _btnBookSelected.Enabled = false; return; }
        _btnBookSelected.Enabled = _gridCars.SelectedRows[0].Cells["Status"].Value?.ToString() == "Available";
    }

    private void OpenBookingDialog()
    {
        if (_gridCars.SelectedRows.Count == 0) return;
        var id  = (int)_gridCars.SelectedRows[0].Cells["Id"].Value;
        var car = _cars.FirstOrDefault(c => c.Id == id);
        if (car == null) return;
        var dlg = new BookingDialog(car);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            _tabs.SelectedTab = _tabBookings;
    }

    private async Task LoadBookingsAsync()
    {
        _lblBookingsStatus.Text = "Loading…";
        _btnRefreshBookings.Enabled = false;
        _bookings = await Program.Api.GetMyBookingsAsync();
        _gridBookings.Rows.Clear();
        foreach (var b in _bookings)
            _gridBookings.Rows.Add(b.Id, b.CarDescription,
                b.PickupDate.ToLocalTime().ToString("MMM dd, yyyy"),
                b.ReturnDate.ToLocalTime().ToString("MMM dd, yyyy"),
                $"${b.TotalAmount:F2}", b.Status);
        _lblBookingsStatus.Text = $"{_bookings.Count} booking(s)";
        _btnRefreshBookings.Enabled = true;
        UpdateBookingButtons();
    }

    private void UpdateBookingButtons()
    {
        if (_gridBookings.SelectedRows.Count == 0) { _btnCancelBooking.Enabled = false; return; }
        _btnCancelBooking.Enabled = _gridBookings.SelectedRows[0].Cells["Status"].Value?.ToString() == "Pending";
    }

    private async Task CancelBookingAsync()
    {
        if (_gridBookings.SelectedRows.Count == 0) return;
        var id  = (int)_gridBookings.SelectedRows[0].Cells["Id"].Value;
        var car = _gridBookings.SelectedRows[0].Cells["Car"].Value?.ToString();
        if (MessageBox.Show($"Cancel booking #{id} for:\n{car}?", "Confirm Cancellation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var (ok, error) = await Program.Api.CancelBookingAsync(id);
        if (ok) { MessageBox.Show("Booking cancelled.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information); await LoadBookingsAsync(); }
        else      MessageBox.Show(error ?? "Failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}