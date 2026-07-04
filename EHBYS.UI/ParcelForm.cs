using System.Data;
using System.Data.SQLite;
using EHBYS.Data;
using EHBYS.Models;
using EHBYS.Services;

namespace EHBYS.UI;

public sealed class ParcelForm : Form
{
    private readonly TextBox txtParcel = new() { Left = 90, Top = 20, Width = 120 };
    private readonly TextBox txtOwner = new() { Left = 300, Top = 20, Width = 220 };
    private readonly TextBox txtPhone = new() { Left = 610, Top = 20, Width = 160 };
    private readonly DataGridView grid = new() { Left = 20, Top = 70, Width = 830, Height = 420, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

    public ParcelForm()
    {
        Text = "Parsel ve Uye Yonetimi";
        Width = 900;
        Height = 560;
        StartPosition = FormStartPosition.CenterParent;

        Controls.Add(new Label { Left = 20, Top = 24, Width = 70, Text = "Parsel" });
        Controls.Add(txtParcel);
        Controls.Add(new Label { Left = 230, Top = 24, Width = 70, Text = "Uye" });
        Controls.Add(txtOwner);
        Controls.Add(new Label { Left = 550, Top = 24, Width = 60, Text = "Telefon" });
        Controls.Add(txtPhone);

        var btnAdd = new Button { Left = 780, Top = 18, Width = 70, Text = "Kaydet" };
        btnAdd.Click += (_, _) => AddParcel(closeAfterSave: false);
        Controls.Add(btnAdd);
        var btnSaveClose = new Button { Left = 650, Top = 46, Width = 200, Text = "Kaydet ve Cik" };
        btnSaveClose.Click += (_, _) => AddParcel(closeAfterSave: true);
        Controls.Add(btnSaveClose);
        Controls.Add(grid);
        Load += (_, _) => LoadRows();
    }

    private void AddParcel(bool closeAfterSave)
    {
        if (!PermissionMatrix.CanEditParcels)
        {
            MessageBox.Show("Bu islem icin yetkiniz yok.");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtParcel.Text) || string.IsNullOrWhiteSpace(txtOwner.Text))
        {
            MessageBox.Show("Parsel ve uye adi bos birakilamaz.");
            return;
        }

        var parcelNo = txtParcel.Text.Trim();
        using (var conn = Database.GetConnection())
        {
            conn.Open();
            using var cmd = new SQLiteCommand("INSERT INTO Parcels(ParcelNo, OwnerName, Phone) VALUES(@p, @o, @t)", conn);
            cmd.Parameters.AddWithValue("@p", parcelNo);
            cmd.Parameters.AddWithValue("@o", txtOwner.Text.Trim());
            cmd.Parameters.AddWithValue("@t", txtPhone.Text.Trim());
            cmd.ExecuteNonQuery();
        }

        LogService.Log("Parsel eklendi: " + parcelNo);
        txtParcel.Clear();
        txtOwner.Clear();
        txtPhone.Clear();
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
        var adapter = new SQLiteDataAdapter("SELECT Id, ParcelNo AS Parsel, OwnerName AS Uye, Phone AS Telefon FROM Parcels ORDER BY ParcelNo", conn);
        var table = new DataTable();
        adapter.Fill(table);
        table.Columns.Add("GuncelBorc", typeof(decimal));

        foreach (DataRow row in table.Rows)
        {
            row["GuncelBorc"] = DebtService.CalculateParcelDebt(Convert.ToInt32(row["Id"]), conn);
        }

        grid.DataSource = table;
        if (grid.Columns["Id"] is not null)
        {
            grid.Columns["Id"].Visible = false;
        }
    }
}
