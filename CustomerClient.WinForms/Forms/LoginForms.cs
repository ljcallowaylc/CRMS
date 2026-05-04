namespace CustomerClient.Forms;

public class LoginForm : Form
{
    private readonly TabControl _tabs;
    private readonly TabPage _tabLogin;
    private readonly TabPage _tabRegister;

    private readonly TextBox _txtLoginUser;
    private readonly TextBox _txtLoginPass;
    private readonly Button  _btnLogin;
    private readonly Label   _lblLoginError;

    private readonly TextBox _txtRegUser;
    private readonly TextBox _txtRegPass;
    private readonly TextBox _txtRegName;
    private readonly TextBox _txtRegEmail;
    private readonly TextBox _txtRegPhone;
    private readonly Button  _btnRegister;
    private readonly Label   _lblRegError;

    public LoginForm()
    {
        Theme.StyleForm(this);
        Text = "CRMS — Customer Portal";
        Size = new Size(440, 560);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var banner = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Theme.Primary };
        var lblLogo = new Label
        {
            Text = "🚗  Car Rental Management System",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.White, AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill
        };
        banner.Controls.Add(lblLogo);

        _tabs = new TabControl { Dock = DockStyle.Fill, Font = Theme.FontBase, Padding = new Point(16, 6) };
        _tabLogin    = new TabPage("  Sign In  ");
        _tabRegister = new TabPage("  Register  ");

        // ── Login tab ──────────────────────────────────────────────────────────
        _lblLoginError = MakeErrorLabel();
        _txtLoginUser  = Theme.MakeTextBox();
        _txtLoginPass  = Theme.MakeTextBox(password: true);
        _btnLogin      = Theme.MakeButton("Sign In");
        _btnLogin.Dock = DockStyle.Top;
        _btnLogin.Click += async (_, _) => await LoginAsync();
        _txtLoginPass.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) _btnLogin.PerformClick(); };

        _tabLogin.Padding = new Padding(16);
        _tabLogin.BackColor = Theme.Surface;
        AddRow(_tabLogin, "Username", _txtLoginUser);
        AddRow(_tabLogin, "Password", _txtLoginPass);
        _tabLogin.Controls.Add(Spacer(10));
        _tabLogin.Controls.Add(_btnLogin);
        _tabLogin.Controls.Add(_lblLoginError);

        // ── Register tab ───────────────────────────────────────────────────────
        _lblRegError = MakeErrorLabel();
        _txtRegUser  = Theme.MakeTextBox();
        _txtRegPass  = Theme.MakeTextBox(password: true);
        _txtRegName  = Theme.MakeTextBox();
        _txtRegEmail = Theme.MakeTextBox();
        _txtRegPhone = Theme.MakeTextBox();
        _btnRegister = Theme.MakeButton("Create Account");
        _btnRegister.Dock = DockStyle.Top;
        _btnRegister.Click += async (_, _) => await RegisterAsync();

        _tabRegister.Padding = new Padding(16);
        _tabRegister.BackColor = Theme.Surface;
        AddRow(_tabRegister, "Username",  _txtRegUser);
        AddRow(_tabRegister, "Password",  _txtRegPass);
        AddRow(_tabRegister, "Full Name", _txtRegName);
        AddRow(_tabRegister, "Email",     _txtRegEmail);
        AddRow(_tabRegister, "Phone",     _txtRegPhone);
        _tabRegister.Controls.Add(Spacer(10));
        _tabRegister.Controls.Add(_btnRegister);
        _tabRegister.Controls.Add(_lblRegError);

        _tabs.TabPages.Add(_tabLogin);
        _tabs.TabPages.Add(_tabRegister);

        Controls.Add(_tabs);
        Controls.Add(banner);
    }

    private static void AddRow(TabPage page, string labelText, Control ctrl)
    {
        ctrl.Dock = DockStyle.Top;
        var lbl = new Label
        {
            Text = labelText, Font = Theme.FontSmall, ForeColor = Theme.TextMuted,
            AutoSize = false, Height = 22, Dock = DockStyle.Top,
            TextAlign = ContentAlignment.BottomLeft
        };
        page.Controls.Add(Spacer(6));
        page.Controls.Add(ctrl);
        page.Controls.Add(lbl);
    }

    private static Label MakeErrorLabel() => new()
    {
        ForeColor = Theme.Danger, Font = Theme.FontSmall, AutoSize = false,
        Height = 32, Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(4, 0, 0, 0), Visible = false
    };

    private static Panel Spacer(int height) =>
        new() { Dock = DockStyle.Top, Height = height, BackColor = Color.Transparent };

    private async Task LoginAsync()
    {
        _lblLoginError.Visible = false;
        var username = _txtLoginUser.Text.Trim();
        var password = _txtLoginPass.Text;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        { ShowError(_lblLoginError, "Username and password are required."); return; }

        _btnLogin.Enabled = false; _btnLogin.Text = "Signing in…";
        var (ok, role, error) = await Program.Api.LoginAsync(username, password);
        _btnLogin.Enabled = true; _btnLogin.Text = "Sign In";

        if (!ok) { ShowError(_lblLoginError, error ?? "Login failed."); return; }
        if (role != "Customer")
        {
            Program.Api.ClearCredentials();
            ShowError(_lblLoginError, "This portal is for Customers only. Use the Enterprise client for Staff/Admin.");
            return;
        }

        Hide();
        var dashboard = new DashboardForm();
        dashboard.FormClosed += (_, _) => { _txtLoginPass.Clear(); Show(); };
        dashboard.Show();
    }

    private async Task RegisterAsync()
    {
        _lblRegError.Visible = false;
        var username = _txtRegUser.Text.Trim();
        var password = _txtRegPass.Text;
        var fullName = _txtRegName.Text.Trim();
        var email    = _txtRegEmail.Text.Trim();
        var phone    = _txtRegPhone.Text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)
            || string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email))
        { ShowError(_lblRegError, "Username, password, full name, and email are required."); return; }

        if (password.Length < 6)
        { ShowError(_lblRegError, "Password must be at least 6 characters."); return; }

        _btnRegister.Enabled = false; _btnRegister.Text = "Creating account…";
        var (ok, error) = await Program.Api.RegisterAsync(username, password, fullName, email, phone);
        _btnRegister.Enabled = true; _btnRegister.Text = "Create Account";

        if (ok)
        {
            _tabs.SelectedTab = _tabLogin;
            _txtLoginUser.Text = username;
            ShowError(_lblLoginError, "✓ Account created! Please sign in.");
            _lblLoginError.ForeColor = Theme.Success;
            _txtRegUser.Clear(); _txtRegPass.Clear();
            _txtRegName.Clear(); _txtRegEmail.Clear(); _txtRegPhone.Clear();
        }
        else
        {
            ShowError(_lblRegError, error ?? "Registration failed.");
        }
    }

    private static void ShowError(Label lbl, string msg)
    {
        lbl.ForeColor = Theme.Danger;
        lbl.Text = "✗  " + msg;
        lbl.Visible = true;
    }
}