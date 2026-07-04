using System.Data.SQLite;
using EHBYS.Data;

namespace EHBYS.Services;

public static class SettingsService
{
    public static decimal GetDecimal(string key, decimal fallback)
    {
        using var conn = Database.GetConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("SELECT Value FROM Settings WHERE Key=@k", conn);
        cmd.Parameters.AddWithValue("@k", key);
        var value = cmd.ExecuteScalar()?.ToString();
        return decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : fallback;
    }

    public static void Set(string key, decimal value)
    {
        using var conn = Database.GetConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("INSERT OR REPLACE INTO Settings(Key, Value) VALUES(@k, @v)", conn);
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        cmd.ExecuteNonQuery();
    }
}
