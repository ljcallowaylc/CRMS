namespace EnterpriseClient.Forms;

public class EnterpriseLoginForm : Form
{
    private readonly TextBox _txtUsername;
    private readonly TextBox _txtPassword;
    private readonly Button  _btnLogin;
    private readonly Label   _lblError;

    public EnterpriseLoginForm()
    {
        Theme.StyleForm(this);
        Text = "CRMS — Enterprise Portal";
        Size = new Size(400, 360);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var banner = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.FromArgb(46, 64, 87) };
        var lblTitle = new Label { Text = "⚙  CRMS Enterprise", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = Color.White, AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        banner.Controls.Add(lblTitle);

        var card = new Panel { BackColor = Theme.Surface, Location = new Point(30, 110), Size = new Size(320, 210) };

        var lblUser = new Label { Text = "Username", Font = Theme.FontSmall, ForeColor = Theme.TextMuted, AutoSize = true, Location = new Point(0, 8) };
        _txtUsername = Theme.MakeTextBox();
        _txtUsername.Location = new Point(0, 28); _txtUsername.Width = 320;

        var lblPass = new Label { Text = "Password", Font = Theme.FontSmall, ForeColor = Theme.TextMuted, AutoSize = true, Location = new Point(0, 66) };
        _txtPassword = Theme.MakeTextBox(password: true);
        _txtPassword.Location = new Point(0, 86); _txtPassword.Width = 320;

        _lblError = new Label { Text = "", Font = Theme.FontSmall, ForeColor = Theme.Danger, AutoSize = false, Size = new Size(320, 28), Location = new Point(0, 124), Visible = false };

        _btnLogin = Theme.MakeButton("Sign In");
        _btnLogin.Size = new Size(320, 36); _btnLogin.Location = new Point(0, 160);
        _btnLogin.Click += async (_, _) => await LoginAsync();
        _txtPassword.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) _btnLogin.PerformClick(); };

        card.Controls.AddRange(new Control[] { lblUser, _txtUsername, lblPass, _txtPassword, _lblError, _btnLogin });
        Controls.Add(card);
        Controls.Add(banner);
    }

    private async Task LoginAsync()
    {
        _lblError.Visible = false;
        var username = _txtUsername.Text.Trim();
        var password = _txtPassword.Text;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        { ShowError("Username and password are required."); return; }

        _btnLogin.Enabled = false; _btnLogin.Text = "Signing in…";
        var (ok, role, error) = await Program.Api.LoginAsync(username, password);
        _btnLogin.Enabled = true; _btnLogin.Text = "Sign In";

        if (!ok) { ShowError(error ?? "Login failed."); return; }
        if (role == "Customer") { Program.Api.ClearCredentials(); ShowError("This portal is for Staff and Admin only."); return; }

        Hide();
        var main = new EnterpriseDashboardForm();
        main.FormClosed += (_, _) => { _txtPassword.Clear(); Show(); };
        main.Show();
    }

    private void ShowError(string msg) { _lblError.Text = "✗  " + msg; _lblError.Visible = true; }
}