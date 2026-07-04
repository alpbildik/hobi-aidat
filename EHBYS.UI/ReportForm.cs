using System.Data.SQLite;
using EHBYS.Data;
using EHBYS.Models;
using EHBYS.Services;

namespace EHBYS.UI;

public sealed class ReportForm : Form
{
    private readonly TextBox txtReport = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };

    public ReportForm()
    {
        Text = "Raporlar";
        Width = 700;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;
        Controls.Add(txtReport);
        Load += (_, _) => LoadReport();
    }

    private void LoadReport()
    {
        if (!PermissionMatrix.CanViewReports)
        {
            txtReport.Text = "Raporlari goruntuleme yetkiniz yok.";
            return;
        }

        using var conn = Database.GetConnection();
        conn.Open();
        var parcelCount = Scalar(conn, "SELECT COUNT(*) FROM Parcels");
        var totalDebt = DebtService.CalculateTotalDebt(conn);
        var totalPayment = Scalar(conn, "SELECT IFNULL(SUM(Amount),0) FROM Payments");

        txtReport.Text =
            "EHBYS RAPOR OZETI\r\n" +
            "=================\r\n\r\n" +
            $"Toplam Parsel: {parcelCount}\r\n" +
            $"Toplam Borc: {totalDebt:0.00} TL\r\n" +
            $"Toplam Tahsilat: {totalPayment:0.00} TL\r\n";
    }

    private static decimal Scalar(SQLiteConnection conn, string sql)
    {
        using var cmd = new SQLiteCommand(sql, conn);
        return Convert.ToDecimal(cmd.ExecuteScalar());
    }

}
