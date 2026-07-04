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
    private bool showDeleted;

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
        var btnDelete = new Button { Left = 500, Top = 65, Width = 80, Height = 35, Text = "Sil" };
        btnDelete.Click += (_, _) => DeleteSelectedAidat();
        Controls.Add(btnDelete);
        var btnShowDeleted = new Button { Left = 590, Top = 65, Width = 120, Height = 35, Text = "Silinenleri Goster" };
        btnShowDeleted.Click += (_, _) =>
        {
            showDeleted = !showDeleted;
            btnShowDeleted.Text = showDeleted ? "Aktifleri Goster" : "Silinenleri Goster";
            LoadRows();
        };
        Controls.Add(btnShowDeleted);

        Controls.Add(grid);
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
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

            using var select = new SQLiteCommand("SELECT Id FROM Parcels WHERE IsDeleted=0", conn, tx);
            using var reader = select.ExecuteReader();
            var parcelIds = new List<int>();
            while (reader.Read())
            {
                parcelIds.Add(reader.GetInt32(0));
            }
            reader.Close();

            foreach (var parcelId in parcelIds)
            {
                using var exists = new SQLiteCommand("SELECT COUNT(*) FROM Aidat WHERE ParcelId=@p AND Period=@period AND IsDeleted=0", conn, tx);
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
            SELECT a.Id,
                   p.ParcelNo AS Parsel, p.OwnerName AS Uye, a.Period AS Donem,
                   a.Principal AS AnaPara, a.DueDate AS Vade, a.PaidAmount AS Odenen,
                   CASE
                       WHEN a.IsDeleted = 1 THEN 'Silinmis'
                       WHEN a.IsPaid = 1 THEN 'Kapandi'
                       ELSE 'Acik'
                   END AS Durum
            FROM Aidat a
            JOIN Parcels p ON p.Id = a.ParcelId
            WHERE a.IsDeleted = @deleted
              AND p.IsDeleted = 0
            ORDER BY a.DueDate DESC, p.ParcelNo
            """, conn);
        adapter.SelectCommand.Parameters.AddWithValue("@deleted", showDeleted ? 1 : 0);
        var table = new DataTable();
        adapter.Fill(table);
        grid.DataSource = table;
        if (grid.Columns["Id"] is not null)
        {
            grid.Columns["Id"].Visible = false;
        }
        grid.ClearSelection();
    }

    private void DeleteSelectedAidat()
    {
        if (!PermissionMatrix.CanDeleteData)
        {
            MessageBox.Show("Silme islemi icin admin yetkisi gerekir.");
            return;
        }

        if (grid.CurrentRow?.DataBoundItem is not DataRowView row)
        {
            MessageBox.Show("Silmek icin listeden bir aidat secin.");
            return;
        }

        if (row["Durum"].ToString() == "Silinmis")
        {
            MessageBox.Show("Bu kayit zaten silinenlerde.");
            return;
        }

        var result = MessageBox.Show(
            "Secili aidat kaydi silinenlere tasinsin mi?",
            "Silme Onayi",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        using var conn = Database.GetConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("UPDATE Aidat SET IsDeleted=1 WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", Convert.ToInt32(row["Id"]));
        cmd.ExecuteNonQuery();

        LogService.Log("Aidat silinenlere tasindi: " + row["Donem"]);
        LoadRows();
    }
}
