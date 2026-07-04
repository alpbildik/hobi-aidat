using System.Data.SQLite;
using EHBYS.Data;

namespace EHBYS.Services;

public static class LogService
{
    public static void Log(string action)
    {
        using var conn = Database.GetConnection();
        conn.Open();

        using var cmd = new SQLiteCommand("INSERT INTO Logs(Action, Date) VALUES(@a, @d)", conn);
        cmd.Parameters.AddWithValue("@a", action);
        cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("s"));
        cmd.ExecuteNonQuery();
    }
}
