using System.Data;
using System.Data.SQLite;
using EHBYS.Data;
using EHBYS.Services;

namespace EHBYS.UI;

public sealed class AidatForm : Form
{
    private readonly TextBox txtPeriod = new() { Left = 130, Top = 20, Width = 120, Text = DateTime.Today.ToString("yyyy-MM") };
    private readonly NumericUpDown numAmount = new() { Left = 130, Top = 60, Width = 120, Maximum = 100000, DecimalPlaces = 2 };
    private readonly DateTimePicker dueDate = new() { Left = 130, Top = 100, Width = 160 };
    private readonly DataGridView grid = new() { Left = 20, Top = 160, Width = 760, Height = 330, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

    public AidatForm()
    {
        Text = "Aidat Tahakkuku";
        Width = 830;
        Height = 560;
        StartPosition = FormStartPosition.CenterParent;

        Controls.Add(new Label { Left = 20, Top = 24, Width = 110, Text = "Donem" });
        Controls.Add(txtPeriod);
        Controls.Add(new Label { Left = 20, Top = 64, Width = 110, Text = "Aidat tutari" });
        Controls.Add(numAmount);
        Controls.Add(new Label { Left = 20, Top = 104, Width = 110, Text = "Vade tarihi" });
        Controls.Add(dueDate);

        var btnCreate = new Button { Left = 310, Top = 20, Width = 170, Height = 35, Text = "Kaydet" };
        btnCreate.Click += (_, _) => CreateMonthlyDues(closeAfterSave: false);
        Controls.Add(btnCreate);

        var btnRefresh = new Button { Left = 310, Top = 65, Width = 170, Height = 35, Text = "Listeyi Yenile" };
        btnRefresh.Click += (_, _) => LoadRows();
        Controls.Add(btnRefresh);
        var btnClose = new Button { Left = 500, Top = 20, Width = 170, Height = 35, Text = "Cik" };
        btnClose.Click += (_, _) => Close();
        Controls.Add(btnClose);

        Controls.Add(grid);
        Load += (_, _) =>
        {
            numAmount.Value = SettingsService.GetDecimal("MonthlyAidat", 600);
            var day = (int)SettingsService.GetDecimal("DueDay", 20);
            dueDate.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, Math.Min(day, 28));
            LoadRows();
        };
    }

    private void CreateMonthlyDues(bool closeAfterSave)
    {
        if (string.IsNullOrWhiteSpace(txtPeriod.Text) || numAmount.Value <= 0)
        {
            MessageBox.Show("Donem ve tutar zorunludur.");
            return;
        }

        var period = txtPeriod.Text.Trim();
        using (var conn = Database.GetConnection())
        {
            conn.Open();
            using var tx = conn.BeginTransaction();

            using var select = new SQLiteCommand("SELECT Id FROM Parcels", conn, tx);
            using var reader = select.ExecuteReader();
            var parcelIds = new List<int>();
            while (reader.Read())
            {
                parcelIds.Add(reader.GetInt32(0));
            }
            reader.Close();

            foreach (var parcelId in parcelIds)
            {
                using var exists = new SQLiteCommand("SELECT COUNT(*) FROM Aidat WHERE ParcelId=@p AND Period=@period", conn, tx);
                exists.Parameters.AddWithValue("@p", parcelId);
                exists.Parameters.AddWithValue("@period", period);
                if (Convert.ToInt32(exists.ExecuteScalar()) > 0)
                {
                    continue;
                }

                using var insert = new SQLiteCommand("""
                    INSERT INTO Aidat(ParcelId, Period, Principal, DueDate)
                    VALUES(@p, @period, @principal, @due)
                    """, conn, tx);
                insert.Parameters.AddWithValue("@p", parcelId);
                insert.Parameters.AddWithValue("@period", period);
                insert.Parameters.AddWithValue("@principal", numAmount.Value);
                insert.Parameters.AddWithValue("@due", dueDate.Value.ToString("yyyy-MM-dd"));
                insert.ExecuteNonQuery();
            }

            tx.Commit();
        }

        LogService.Log("Aidat tahakkuku olusturuldu: " + period);
        MessageBox.Show("Aidat tahakkuku olusturuldu.");
        LoadRows();
        if (closeAfterSave)
        {
            Close();
        }
    }

    private void LoadRows()
    {
        using var conn = Database.GetConnection();
        conn.Open();
        var adapter = new SQLiteDataAdapter("""
            SELECT p.ParcelNo AS Parsel, p.OwnerName AS Uye, a.Period AS Donem,
                   a.Principal AS AnaPara, a.DueDate AS Vade, a.PaidAmount AS Odenen,
                   CASE WHEN a.IsPaid = 1 THEN 'Kapandi' ELSE 'Acik' END AS Durum
            FROM Aidat a
            JOIN Parcels p ON p.Id = a.ParcelId
            ORDER BY a.DueDate DESC, p.ParcelNo
            """, conn);
        var table = new DataTable();
        adapter.Fill(table);
        grid.DataSource = table;
    }
}
