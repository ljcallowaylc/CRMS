namespace CustomerClient.Forms;

public class BookingDialog : Form
{
    private readonly CarResponse       _car;
    private readonly DateTimePicker    _dtpPickup;
    private readonly DateTimePicker    _dtpReturn;
    private readonly Label             _lblPreview;
    private readonly Label             _lblError;
    private readonly Button            _btnBook;

    public BookingDialog(CarResponse car)
    {
        _car = car;
        Theme.StyleForm(this);
        Text = $"Book — {car.Make} {car.Model} ({car.Year})";
        Size = new Size(420, 390);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;

        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Padding = new Padding(24) };

        var lblName    = new Label { Text = $"{car.Make} {car.Model} ({car.Year})", Font = Theme.FontSub, ForeColor = Theme.Primary, AutoSize = true, Location = new Point(24, 20) };
        var lblDetails = new Label { Text = $"{car.Category}  ·  {car.Colour}  ·  {car.LicencePlate}", Font = Theme.FontSmall, ForeColor = Theme.TextMuted, AutoSize = true, Location = new Point(24, 46) };
        var lblRate    = new Label { Text = $"${car.DailyRate:F2} / day", Font = Theme.FontBold, ForeColor = Theme.Primary, AutoSize = true, Location = new Point(24, 66) };
        var sep        = new Panel { BackColor = Theme.Border, Size = new Size(360, 1), Location = new Point(24, 94) };

        var lblPickupHdr = new Label { Text = "Pickup Date",  Font = Theme.FontSmall, ForeColor = Theme.TextMuted, AutoSize = true, Location = new Point(24, 110) };
        _dtpPickup = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1), MinDate = DateTime.Today, Size = new Size(360, 28), Location = new Point(24, 130), Font = Theme.FontBase };

        var lblReturnHdr = new Label { Text = "Return Date", Font = Theme.FontSmall, ForeColor = Theme.TextMuted, AutoSize = true, Location = new Point(24, 168) };
        _dtpReturn = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(3), MinDate = DateTime.Today.AddDays(1), Size = new Size(360, 28), Location = new Point(24, 188), Font = Theme.FontBase };

        _dtpPickup.ValueChanged += (_, _) => UpdatePreview();
        _dtpReturn.ValueChanged += (_, _) => UpdatePreview();

        _lblPreview = new Label { Text = "", Font = Theme.FontBold, ForeColor = Theme.Success, AutoSize = true, Location = new Point(24, 228) };
        _lblError   = new Label { Text = "", Font = Theme.FontSmall, ForeColor = Theme.Danger, AutoSize = false, Size = new Size(360, 28), Location = new Point(24, 252), Visible = false };

        var btnCancel = Theme.MakeButton("Cancel", Theme.Background, Theme.TextPrimary);
        btnCancel.Size = new Size(110, 36); btnCancel.Location = new Point(24, 292);
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnBook = Theme.MakeButton("Confirm Booking");
        _btnBook.Size = new Size(160, 36); _btnBook.Location = new Point(224, 292);
        _btnBook.Click += async (_, _) => await BookAsync();

        panel.Controls.AddRange(new Control[] { lblName, lblDetails, lblRate, sep, lblPickupHdr, _dtpPickup, lblReturnHdr, _dtpReturn, _lblPreview, _lblError, btnCancel, _btnBook });
        Controls.Add(panel);
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (_dtpReturn.Value <= _dtpPickup.Value) { _lblPreview.Text = ""; return; }
        var days  = (int)Math.Ceiling((_dtpReturn.Value - _dtpPickup.Value).TotalDays);
        var total = days * _car.DailyRate;
        _lblPreview.Text = $"Estimated total:  ${total:F2}  ({days} day{(days > 1 ? "s" : "")})";
    }

    private async Task BookAsync()
    {
        _lblError.Visible = false;
        if (_dtpReturn.Value <= _dtpPickup.Value)
        { _lblError.Text = "Return date must be after pickup date."; _lblError.Visible = true; return; }

        _btnBook.Enabled = false; _btnBook.Text = "Booking…";
        var (ok, booking, error) = await Program.Api.CreateBookingAsync(_car.Id, _dtpPickup.Value, _dtpReturn.Value);
        _btnBook.Enabled = true; _btnBook.Text = "Confirm Booking";

        if (ok && booking != null)
        {
            MessageBox.Show($"Booking confirmed!\n\nBooking ID: #{booking.Id}\nStatus: {booking.Status}\nTotal: ${booking.TotalAmount:F2}", "Booking Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            _lblError.Text = error ?? "Booking failed.";
            _lblError.Visible = true;
        }
    }
}