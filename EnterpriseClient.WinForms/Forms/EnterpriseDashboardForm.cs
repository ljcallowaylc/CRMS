namespace EnterpriseClient.Forms;

public class EnterpriseDashboardForm : Form
{
    private readonly TabControl _tabs;

    private readonly DataGridView    _gridBookings;
    private readonly Button          _btnRefreshBookings;
    private readonly Button          _btnApprove;
    private readonly Button          _btnReject;
    private readonly Button          _btnActivate;
    private readonly Button          _btnComplete;
    private readonly ComboBox        _cmbFilter;
    private readonly Label           _lblBookingsStatus;
    private List<BookingResponse>    _allBookings = [];

    private DataGridView?  _gridCars;
    private List<CarResponse> _cars = [];
    private DataGridView?  _gridUsers;

    public EnterpriseDashboardForm()
    {
        Theme.StyleForm(this);
        var isAdmin = Program.Api.Role == "Admin";
        Text = $"CRMS Enterprise — {Program.Api.Username}  [{Program.Api.Role}]";
        Size = new Size(1080, 680);
        MinimumSize = new Size(800, 520);
        StartPosition = FormStartPosition.CenterScreen;

        // Top bar
        var topBar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(46, 64, 87) };
        var lblTitle = new Label { Text = "⚙  CRMS Enterprise Portal", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(12, 14) };
        var lblRole  = new Label { Text = $"[{Program.Api.Role}]", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = isAdmin ? Color.FromArgb(255, 160, 155) : Color.FromArgb(160, 230, 220), AutoSize = true, Location = new Point(250, 17) };
        var btnSignOut = new Button { Text = "Sign Out", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(70, 95, 120), ForeColor = Color.White, Font = Theme.FontSmall, Size = new Size(80, 28), Cursor = Cursors.Hand };
        btnSignOut.FlatAppearance.BorderSize = 0;
        btnSignOut.Click += (_, _) => { Program.Api.ClearCredentials(); Close(); };
        topBar.Controls.AddRange(new Control[] { lblTitle, lblRole, btnSignOut });
        topBar.Resize += (_, _) => btnSignOut.Location = new Point(topBar.Width - 96, 10);

        _tabs = new TabControl { Dock = DockStyle.Fill, Font = Theme.FontBase, Padding = new Point(16, 6) };

        // ── Bookings tab ──────────────────────────────────────────────────────
        var tabBookings = new TabPage("  All Bookings  ") { BackColor = Theme.Background };

        _gridBookings = Theme.MakeGrid();
        _gridBookings.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID",       Name = "Id",       Width = 50 });
        _gridBookings.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Customer", Name = "Customer", FillWeight = 15 });
        _gridBookings.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Car",      Name = "Car",      FillWeight = 22 });
        _gridBookings.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Pickup",   Name = "Pickup",   FillWeight = 12 });
        _gridBookings.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Return",   Name = "Return",   FillWeight = 12 });
        _gridBookings.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Total",    Name = "Total",    FillWeight = 10 });
        _gridBookings.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status",   Name = "Status",   FillWeight = 10 });
        _gridBookings.CellFormatting += (_, e) =>
        {
            if (_gridBookings.Columns[e.ColumnIndex].Name == "Status" && e.Value is string s)
            { e.CellStyle.ForeColor = Theme.StatusColor(s); e.CellStyle.Font = Theme.FontBold; }
        };
        _gridBookings.SelectionChanged += (_, _) => UpdateBookingButtons();

        _cmbFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.FontSmall, Width = 140, FlatStyle = FlatStyle.Flat };
        _cmbFilter.Items.AddRange(["All Statuses", "Pending", "Approved", "Active", "Completed", "Cancelled", "Rejected"]);
        _cmbFilter.SelectedIndex = 0;
        _cmbFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        _btnRefreshBookings = Theme.MakeButton("↻  Refresh", Theme.Background, Theme.TextPrimary); _btnRefreshBookings.Size = new Size(110, 32);
        _btnApprove  = Theme.MakeButton("✓ Approve",  Theme.Success);                             _btnApprove.Size  = new Size(110, 32); _btnApprove.Enabled  = false;
        _btnReject   = Theme.MakeButton("✗ Reject",   Theme.Danger);                              _btnReject.Size   = new Size(100, 32); _btnReject.Enabled   = false;
        _btnActivate = Theme.MakeButton("▶ Activate", Theme.Primary);                             _btnActivate.Size = new Size(110, 32); _btnActivate.Enabled = false;
        _btnComplete = Theme.MakeButton("■ Complete", Color.FromArgb(80, 80, 80));                _btnComplete.Size = new Size(110, 32); _btnComplete.Enabled = false;

        _btnRefreshBookings.Click += async (_, _) => await LoadBookingsAsync();
        _btnApprove.Click  += async (_, _) => await DoBookingAction("approve");
        _btnReject.Click   += async (_, _) => await DoBookingAction("reject");
        _btnActivate.Click += async (_, _) => await DoBookingAction("activate");
        _btnComplete.Click += async (_, _) => await DoBookingAction("complete");

        _lblBookingsStatus = new Label { Text = "Loading…", Font = Theme.FontSmall, ForeColor = Theme.TextMuted, AutoSize = true };

        var bookingsBar = MakeToolbar(_lblBookingsStatus, _cmbFilter, _btnRefreshBookings, _btnApprove, _btnReject, _btnActivate, _btnComplete);
        _gridBookings.Dock = DockStyle.Fill;
        bookingsBar.Dock = DockStyle.Top;
        tabBookings.Controls.Add(_gridBookings);
        tabBookings.Controls.Add(bookingsBar);
        _tabs.TabPages.Add(tabBookings);

        // ── Fleet tab (Admin only) ─────────────────────────────────────────────
        if (isAdmin)
        {
            var tabCars = new TabPage("  Fleet Management  ") { BackColor = Theme.Background };
            _gridCars = Theme.MakeGrid();
            _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID",            Name = "Id",       Width = 50 });
            _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Make",          Name = "Make",     FillWeight = 12 });
            _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Model",         Name = "Model",    FillWeight = 12 });
            _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Year",          Name = "Year",     Width = 60  });
            _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Category",      Name = "Category", FillWeight = 10 });
            _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Daily Rate",    Name = "Rate",     FillWeight = 10 });
            _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Licence Plate", Name = "Plate",    FillWeight = 12 });
            _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Colour",        Name = "Colour",   FillWeight = 10 });
            _gridCars.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status",        Name = "Status",   FillWeight = 10 });
            _gridCars.CellFormatting += (_, e) =>
            {
                if (_gridCars.Columns[e.ColumnIndex].Name == "Status" && e.Value is string s)
                { e.CellStyle.ForeColor = Theme.StatusColor(s); e.CellStyle.Font = Theme.FontBold; }
            };

            var btnAdd    = Theme.MakeButton("+ Add Car");                                          btnAdd.Size    = new Size(110, 32);
            var btnEdit   = Theme.MakeButton("✎ Edit",   Theme.Background, Theme.TextPrimary);      btnEdit.Size   = new Size(90,  32); btnEdit.Enabled   = false;
            var btnDelete = Theme.MakeButton("✕ Delete", Theme.Danger);                             btnDelete.Size = new Size(100, 32); btnDelete.Enabled = false;
            var btnRefreshCars = Theme.MakeButton("↻  Refresh", Theme.Background, Theme.TextPrimary); btnRefreshCars.Size = new Size(110, 32);
            var lblCarsStatus  = new Label { Text = "Loading fleet…", Font = Theme.FontSmall, ForeColor = Theme.TextMuted, AutoSize = true };

            _gridCars.SelectionChanged += (_, _) =>
            {
                btnEdit.Enabled = btnDelete.Enabled = _gridCars.SelectedRows.Count > 0;
            };

            btnAdd.Click    += async (_, _) => await AddCarAsync();
            btnEdit.Click   += async (_, _) => await EditCarAsync();
            btnDelete.Click += async (_, _) => await DeleteCarAsync();
            btnRefreshCars.Click += async (_, _) => await LoadCarsAsync(lblCarsStatus);

            var carsBar = MakeToolbar(lblCarsStatus, btnRefreshCars, btnAdd, btnEdit, btnDelete);
            _gridCars.Dock = DockStyle.Fill;
            carsBar.Dock   = DockStyle.Top;
            tabCars.Controls.Add(_gridCars);
            tabCars.Controls.Add(carsBar);
            _tabs.TabPages.Add(tabCars);

            // ── Users tab (Admin only) ─────────────────────────────────────────
            var tabUsers = new TabPage("  Users  ") { BackColor = Theme.Background };
            _gridUsers = Theme.MakeGrid();
            _gridUsers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID",        Name = "Id",       Width = 50 });
            _gridUsers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Username",  Name = "Username", FillWeight = 14 });
            _gridUsers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Full Name", Name = "FullName", FillWeight = 18 });
            _gridUsers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Email",     Name = "Email",    FillWeight = 20 });
            _gridUsers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Phone",     Name = "Phone",    FillWeight = 12 });
            _gridUsers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Role",      Name = "Role",     FillWeight = 10 });
            _gridUsers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Created",   Name = "Created",  FillWeight = 12 });
            _gridUsers.CellFormatting += (_, e) =>
            {
                if (_gridUsers.Columns[e.ColumnIndex].Name == "Role" && e.Value is string r)
                { e.CellStyle.ForeColor = r == "Admin" ? Theme.Danger : r == "Staff" ? Theme.Warning : Theme.Primary; e.CellStyle.Font = Theme.FontBold; }
            };

            var btnRefreshUsers = Theme.MakeButton("↻  Refresh", Theme.Background, Theme.TextPrimary); btnRefreshUsers.Size = new Size(110, 32);
            var lblUsersStatus  = new Label { Text = "Loading users…", Font = Theme.FontSmall, ForeColor = Theme.TextMuted, AutoSize = true };
            btnRefreshUsers.Click += async (_, _) => await LoadUsersAsync(lblUsersStatus);

            var usersBar = MakeToolbar(lblUsersStatus, btnRefreshUsers);
            _gridUsers.Dock = DockStyle.Fill;
            usersBar.Dock   = DockStyle.Top;
            tabUsers.Controls.Add(_gridUsers);
            tabUsers.Controls.Add(usersBar);
            _tabs.TabPages.Add(tabUsers);
        }

        Controls.Add(_tabs);
        Controls.Add(topBar);

        _tabs.SelectedIndexChanged += async (_, _) =>
        {
            var title = _tabs.SelectedTab?.Text.Trim();
            if (title == "Fleet Management") await LoadCarsAsync(null);
            if (title == "Users")            await LoadUsersAsync(null);
        };

        Load += async (_, _) => await LoadBookingsAsync();
    }

    // ── Toolbar helper ────────────────────────────────────────────────────────

    private static Panel MakeToolbar(Label status, params Control[] controls)
    {
        var bar = new Panel { Height = 52, BackColor = Theme.Surface, Padding = new Padding(8) };
        bar.Paint += (_, e) => { using var pen = new Pen(Theme.Border); e.Graphics.DrawLine(pen, 0, bar.Height - 1, bar.Width, bar.Height - 1); };
        status.Location = new Point(10, 17);
        bar.Controls.Add(status);
        bar.Resize += (_, _) => LayoutRight(bar, controls);
        foreach (var c in controls) bar.Controls.Add(c);
        LayoutRight(bar, controls);
        return bar;
    }

    private static void LayoutRight(Panel bar, Control[] controls)
    {
        int x = bar.ClientSize.Width - 8;
        foreach (var c in controls.Reverse()) { x -= (c.Width + 8); c.Location = new Point(x, (bar.Height - c.Height) / 2); c.Anchor = AnchorStyles.Right | AnchorStyles.Top; }
    }

    // ── Bookings ──────────────────────────────────────────────────────────────

    private async Task LoadBookingsAsync()
    {
        _lblBookingsStatus.Text = "Loading…";
        _allBookings = await Program.Api.GetAllBookingsAsync();
        ApplyFilter();
        _lblBookingsStatus.Text = $"{_allBookings.Count} booking(s) total";
    }

    private void ApplyFilter()
    {
        var filter = _cmbFilter.SelectedItem?.ToString();
        var list = (filter == "All Statuses" || string.IsNullOrEmpty(filter))
            ? _allBookings
            : _allBookings.Where(b => b.Status == filter).ToList();
        _gridBookings.Rows.Clear();
        foreach (var b in list)
            _gridBookings.Rows.Add(b.Id, b.CustomerName, b.CarDescription,
                b.PickupDate.ToLocalTime().ToString("MMM dd, yyyy"),
                b.ReturnDate.ToLocalTime().ToString("MMM dd, yyyy"),
                $"${b.TotalAmount:F2}", b.Status);
        UpdateBookingButtons();
    }

    private void UpdateBookingButtons()
    {
        _btnApprove.Enabled = _btnReject.Enabled = _btnActivate.Enabled = _btnComplete.Enabled = false;
        if (_gridBookings.SelectedRows.Count == 0) return;
        var status = _gridBookings.SelectedRows[0].Cells["Status"].Value?.ToString();
        _btnApprove.Enabled  = status == "Pending";
        _btnReject.Enabled   = status == "Pending";
        _btnActivate.Enabled = status == "Approved";
        _btnComplete.Enabled = status == "Active";
    }

    private async Task DoBookingAction(string action)
    {
        if (_gridBookings.SelectedRows.Count == 0) return;
        var id  = (int)_gridBookings.SelectedRows[0].Cells["Id"].Value;
        var car = _gridBookings.SelectedRows[0].Cells["Car"].Value?.ToString();
        if (MessageBox.Show($"Action: {action.ToUpper()}\nBooking #{id} — {car}", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var (ok, error) = await Program.Api.BookingActionAsync(id, action);
        if (ok) { MessageBox.Show($"Booking #{id} updated.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information); await LoadBookingsAsync(); }
        else      MessageBox.Show(error ?? "Action failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    // ── Fleet (Admin) ─────────────────────────────────────────────────────────

    private async Task LoadCarsAsync(Label? status)
    {
        if (status != null) status.Text = "Loading…";
        _cars = await Program.Api.GetCarsAsync();
        if (_gridCars == null) return;
        _gridCars.Rows.Clear();
        foreach (var c in _cars)
            _gridCars.Rows.Add(c.Id, c.Make, c.Model, c.Year, c.Category, $"${c.DailyRate:F2}", c.LicencePlate, c.Colour, c.Status);
        if (status != null) status.Text = $"{_cars.Count} car(s) in fleet";
    }

    private async Task AddCarAsync()
    {
        var dlg = new CarDialog(null);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var (ok, _, error) = await Program.Api.CreateCarAsync(dlg.Make, dlg.Model, dlg.Year, dlg.Category, dlg.DailyRate, dlg.LicencePlate, dlg.Colour, dlg.CarStatus);
        if (ok) { MessageBox.Show("Car added.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); await LoadCarsAsync(null); }
        else      MessageBox.Show(error ?? "Failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private async Task EditCarAsync()
    {
        if (_gridCars?.SelectedRows.Count == 0) return;
        var id  = (int)_gridCars!.SelectedRows[0].Cells["Id"].Value;
        var car = _cars.FirstOrDefault(c => c.Id == id);
        if (car == null) return;
        var dlg = new CarDialog(car);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var (ok, _, error) = await Program.Api.UpdateCarAsync(id, dlg.Make, dlg.Model, dlg.Year, dlg.Category, dlg.DailyRate, dlg.LicencePlate, dlg.Colour, dlg.CarStatus);
        if (ok) { MessageBox.Show("Car updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); await LoadCarsAsync(null); }
        else      MessageBox.Show(error ?? "Failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private async Task DeleteCarAsync()
    {
        if (_gridCars?.SelectedRows.Count == 0) return;
        var id    = (int)_gridCars!.SelectedRows[0].Cells["Id"].Value;
        var make  = _gridCars.SelectedRows[0].Cells["Make"].Value?.ToString();
        var model = _gridCars.SelectedRows[0].Cells["Model"].Value?.ToString();
        if (MessageBox.Show($"Delete {make} {model} (#{id})?\nThis cannot be undone.", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var (ok, error) = await Program.Api.DeleteCarAsync(id);
        if (ok) { MessageBox.Show("Car deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information); await LoadCarsAsync(null); }
        else      MessageBox.Show(error ?? "Failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    // ── Users (Admin) ─────────────────────────────────────────────────────────

    private async Task LoadUsersAsync(Label? status)
    {
        if (status != null) status.Text = "Loading…";
        var users = await Program.Api.GetUsersAsync();
        if (_gridUsers == null) return;
        _gridUsers.Rows.Clear();
        foreach (var u in users)
            _gridUsers.Rows.Add(u.Id, u.Username, u.FullName, u.Email, u.Phone ?? "—", u.Role, u.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy"));
        if (status != null) status.Text = $"{users.Count} user(s)";
    }
}