using System.Windows.Forms;

namespace EHBYS.Services;

public static class ErrorManager
{
    public static void Handle(Exception ex)
    {
        try
        {
            LogService.Log("HATA: " + ex.Message);
        }
        catch
        {
            // Avoid recursive failures while handling startup/database errors.
        }

        MessageBox.Show("Sistem hatasi olustu: " + ex.Message, "EHBYS", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
