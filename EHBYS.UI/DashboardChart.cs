using System.Data.SQLite;
using EHBYS.Data;
using EHBYS.Services;

namespace EHBYS.UI;

public sealed class DashboardChart : Form
{
    public DashboardChart()
    {
        Text = "Dashboard";
        Width = 520;
        Height = 320;
        StartPosition = FormStartPosition.CenterParent;

        var label = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(FontFamily.GenericSansSerif, 16, FontStyle.Bold)
        };
        Controls.Add(label);
        Load += (_, _) => label.Text = BuildText();
    }

    private static string BuildText()
    {
        using var conn = Database.GetConnection();
        conn.Open();
        var parcelCount = Scalar(conn, "SELECT COUNT(*) FROM Parcels");
        var totalDebt = DebtService.CalculateTotalDebt(conn);
        var totalPayment = Scalar(conn, "SELECT IFNULL(SUM(Amount),0) FROM Payments");

        return $"Toplam Parsel: {parcelCount}\n\nToplam Borc: {totalDebt:0.00} TL\n\nToplam Tahsilat: {totalPayment:0.00} TL";
    }

    private static decimal Scalar(SQLiteConnection conn, string sql)
    {
        using var cmd = new SQLiteCommand(sql, conn);
        return Convert.ToDecimal(cmd.ExecuteScalar());
    }

}
