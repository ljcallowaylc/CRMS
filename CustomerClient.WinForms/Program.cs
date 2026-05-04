using CustomerClient.Forms;

namespace CustomerClient;

static class Program
{
    // ← Change this to your live hosted URL after deploying to MonsterASP
    public static readonly ApiClient Api = new("http://localhost:5000");

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new LoginForm());
    }
}