using EHBYS.Data;
using EHBYS.Services;

namespace EHBYS.UI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) => ErrorManager.Handle(e.Exception);

        try
        {
            Database.Initialize();
            Application.Run(new LoginForm());
        }
        catch (Exception ex)
        {
            ErrorManager.Handle(ex);
        }
    }
}
