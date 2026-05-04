namespace EnterpriseClient.Forms;

public class CarDialog : Form
{
    public string  Make         { get; private set; } = "";
    public string  Model        { get; private set; } = "";
    public int     Year         { get; private set; }
    public string  Category     { get; private set; } = "";
    public decimal DailyRate    { get; private set; }
    public string  LicencePlate { get; private set; } = "";
    public string  Colour       { get; private set; } = "";
    public string  CarStatus    { get; private set; } = "";

    private readonly TextBox  _txtMake;
    private readonly TextBox  _txtModel;
    private readonly TextBox  _txtYear;
    private readonly ComboBox _cmbCategory;
    private readonly TextBox  _txtRate;
    private readonly TextBox  _txtPlate;
    private readonly TextBox  _txtColour;
    private readonly ComboBox _cmbStatus;
    private readonly Button   _btnSave;
    private readonly Label    _lblError;

    public CarDialog(CarResponse? car)
    {
        Theme.StyleForm(this);
        Text = car == null ? "Add New Car" : $"Edit Car — {car.Make} {car.Model}";
        Size = new Size(420, 470);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;

        var table = new TableLayoutPanel
        {
            Location = new Point(20, 16), Width = 370,
            AutoSize = true, ColumnCount = 2, Padding = new Padding(0)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _txtMake     = Theme.MakeTextBox(); _txtMake.Text  = car?.Make  ?? "";
        _txtModel    = Theme.MakeTextBox(); _txtModel.Text = car?.Model ?? "";
        _txtYear     = Theme.MakeTextBox(); _txtYear.Text  = car?.Year.ToString() ?? DateTime.Today.Year.ToString();
        _txtRate     = Theme.MakeTextBox(); _txtRate.Text  = car?.DailyRate.ToString("F2") ?? "";
        _txtPlate    = Theme.MakeTextBox(); _txtPlate.Text = car?.LicencePlate ?? "";
        _txtColour   = Theme.MakeTextBox(); _txtColour.Text= car?.Colour ?? "";

        _cmbCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.FontBase, FlatStyle = FlatStyle.Flat };
        _cmbCategory.Items.AddRange(["Sedan", "SUV", "Van", "Truck", "Hatchback", "Coupe", "Convertible"]);
        _cmbCategory.SelectedItem = car?.Category ?? "Sedan";

        _cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.FontBase, FlatStyle = FlatStyle.Flat };
        _cmbStatus.Items.AddRange(["Available", "Rented", "Maintenance"]);
        _cmbStatus.SelectedItem = car?.Status ?? "Available";

        void AddRow(string lbl, Control ctrl)
        {
            table.Controls.Add(new Label { Text = lbl, Font = Theme.FontSmall, ForeColor = Theme.TextMuted, Anchor = AnchorStyles.Right | AnchorStyles.Top, AutoSize = true, Margin = new Padding(0, 10, 8, 0) });
            ctrl.Dock = DockStyle.Fill; ctrl.Margin = new Padding(0, 6, 0, 0);
            table.Controls.Add(ctrl);
        }

        AddRow("Make",           _txtMake);
        AddRow("Model",          _txtModel);
        AddRow("Year",           _txtYear);
        AddRow("Category",       _cmbCategory);
        AddRow("Daily Rate ($)", _txtRate);
        AddRow("Licence Plate",  _txtPlate);
        AddRow("Colour",         _txtColour);
        AddRow("Status",         _cmbStatus);

        _lblError = new Label { ForeColor = Theme.Danger, Font = Theme.FontSmall, AutoSize = false, Size = new Size(370, 26), Location = new Point(20, 360), Visible = false };

        var btnCancel = Theme.MakeButton("Cancel", Theme.Background, Theme.TextPrimary);
        btnCancel.Size = new Size(110, 36); btnCancel.Location = new Point(20, 394);
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnSave = Theme.MakeButton(car == null ? "Add Car" : "Save Changes");
        _btnSave.Size = new Size(150, 36); _btnSave.Location = new Point(240, 394);
        _btnSave.Click += (_, _) => TrySave();

        Controls.AddRange(new Control[] { table, _lblError, btnCancel, _btnSave });
    }

    private void TrySave()
    {
        _lblError.Visible = false;
        if (string.IsNullOrWhiteSpace(_txtMake.Text) || string.IsNullOrWhiteSpace(_txtModel.Text) || string.IsNullOrWhiteSpace(_txtPlate.Text))
        { ShowError("Make, Model, and Licence Plate are required."); return; }
        if (!int.TryParse(_txtYear.Text, out var year) || year < 1990 || year > 2035)
        { ShowError("Enter a valid year (1990–2035)."); return; }
        if (!decimal.TryParse(_txtRate.Text, out var rate) || rate <= 0)
        { ShowError("Enter a valid daily rate greater than 0."); return; }

        Make = _txtMake.Text.Trim(); Model = _txtModel.Text.Trim(); Year = year;
        Category = _cmbCategory.SelectedItem?.ToString() ?? "Sedan";
        DailyRate = rate; LicencePlate = _txtPlate.Text.Trim();
        Colour = _txtColour.Text.Trim(); CarStatus = _cmbStatus.SelectedItem?.ToString() ?? "Available";
        DialogResult = DialogResult.OK; Close();
    }

    private void ShowError(string msg) { _lblError.Text = "✗  " + msg; _lblError.Visible = true; }
}