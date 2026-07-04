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
    private readonly TextBox txtTcNo = new() { Left = 90, Top = 55, Width = 120 };
    private readonly TextBox txtAddress = new() { Left = 300, Top = 55, Width = 320 };
    private readonly DataGridView grid = new() { Left = 20, Top = 100, Width = 880, Height = 430, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private int? editingParcelId;
    private bool loadingRows;

    public ParcelForm()
    {
        Text = "Parsel ve Uye Yonetimi";
        Width = 950;
        Height = 610;
        StartPosition = FormStartPosition.CenterParent;

        Controls.Add(new Label { Left = 20, Top = 24, Width = 70, Text = "Parsel" });
        Controls.Add(txtParcel);
        Controls.Add(new Label { Left = 230, Top = 24, Width = 70, Text = "Uye" });
        Controls.Add(txtOwner);
        Controls.Add(new Label { Left = 550, Top = 24, Width = 60, Text = "Telefon" });
        Controls.Add(txtPhone);
        Controls.Add(new Label { Left = 20, Top = 59, Width = 70, Text = "TC No" });
        Controls.Add(txtTcNo);
        Controls.Add(new Label { Left = 230, Top = 59, Width = 70, Text = "Adres" });
        Controls.Add(txtAddress);

        var btnNew = new Button { Left = 820, Top = 18, Width = 80, Text = "Yeni" };
        btnNew.Click += (_, _) => ClearForm();
        Controls.Add(btnNew);
        var btnAdd = new Button { Left = 690, Top = 53, Width = 100, Text = "Kaydet" };
        btnAdd.Click += (_, _) => SaveParcel();
        Controls.Add(btnAdd);
        var btnClose = new Button { Left = 800, Top = 53, Width = 100, Text = "Cik" };
        btnClose.Click += (_, _) => Close();
        Controls.Add(btnClose);
        Controls.Add(grid);
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.CellDoubleClick += (_, _) => LoadSelectedParcel();
        grid.SelectionChanged += (_, _) => LoadSelectedParcel();
        Load += (_, _) => LoadRows();
    }

    private void SaveParcel()
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
        var owner = txtOwner.Text.Trim();
        var phone = txtPhone.Text.Trim();
        var tcNo = txtTcNo.Text.Trim();
        var address = txtAddress.Text.Trim();
        using (var conn = Database.GetConnection())
        {
            conn.Open();
            using var tx = conn.BeginTransaction();
            int parcelId;
            if (editingParcelId.HasValue)
            {
                using var cmd = new SQLiteCommand("UPDATE Parcels SET ParcelNo=@p, OwnerName=@o, Phone=@t WHERE Id=@id", conn, tx);
                cmd.Parameters.AddWithValue("@p", parcelNo);
                cmd.Parameters.AddWithValue("@o", owner);
                cmd.Parameters.AddWithValue("@t", phone);
                cmd.Parameters.AddWithValue("@id", editingParcelId.Value);
                cmd.ExecuteNonQuery();
                parcelId = editingParcelId.Value;
            }
            else
            {
                using var cmd = new SQLiteCommand("INSERT INTO Parcels(ParcelNo, OwnerName, Phone) VALUES(@p, @o, @t); SELECT last_insert_rowid();", conn, tx);
                cmd.Parameters.AddWithValue("@p", parcelNo);
                cmd.Parameters.AddWithValue("@o", owner);
                cmd.Parameters.AddWithValue("@t", phone);
                parcelId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            UpsertMember(conn, tx, parcelId, owner, tcNo, address, phone);
            tx.Commit();
        }

        LogService.Log((editingParcelId.HasValue ? "Parsel guncellendi: " : "Parsel eklendi: ") + parcelNo);
        ClearForm();
        LoadRows();
    }

    private void ClearForm()
    {
        editingParcelId = null;
        txtParcel.Clear();
        txtOwner.Clear();
        txtPhone.Clear();
        txtTcNo.Clear();
        txtAddress.Clear();
        grid.ClearSelection();
    }

    private void LoadSelectedParcel()
    {
        if (loadingRows || grid.SelectedRows.Count == 0 || grid.CurrentRow?.DataBoundItem is not DataRowView row)
        {
            return;
        }

        editingParcelId = Convert.ToInt32(row["Id"]);
        txtParcel.Text = row["Parsel"].ToString();
        txtOwner.Text = row["Uye"].ToString();
        txtPhone.Text = row["Telefon"].ToString();
        txtTcNo.Text = row["TCNo"].ToString();
        txtAddress.Text = row["Adres"].ToString();
    }

    private void LoadRows()
    {
        using var conn = Database.GetConnection();
        conn.Open();
        var adapter = new SQLiteDataAdapter("""
            SELECT p.Id,
                   p.ParcelNo AS Parsel,
                   p.OwnerName AS Uye,
                   IFNULL(p.Phone, '') AS Telefon,
                   IFNULL(m.TcNo, '') AS TCNo,
                   IFNULL(m.Address, '') AS Adres
            FROM Parcels p
            LEFT JOIN Members m ON m.Id = (
                SELECT Id FROM Members WHERE ParcelId = p.Id ORDER BY Id DESC LIMIT 1
            )
            ORDER BY p.ParcelNo
            """, conn);
        var table = new DataTable();
        adapter.Fill(table);
        table.Columns.Add("GuncelBorc", typeof(decimal));

        foreach (DataRow row in table.Rows)
        {
            row["GuncelBorc"] = DebtService.CalculateParcelDebt(Convert.ToInt32(row["Id"]), conn);
        }

        loadingRows = true;
        grid.DataSource = table;
        if (grid.Columns["Id"] is not null)
        {
            grid.Columns["Id"].Visible = false;
        }
        grid.ClearSelection();
        loadingRows = false;
    }

    private static void UpsertMember(SQLiteConnection conn, SQLiteTransaction tx, int parcelId, string owner, string tcNo, string address, string phone)
    {
        using var select = new SQLiteCommand("SELECT Id FROM Members WHERE ParcelId=@p ORDER BY Id DESC LIMIT 1", conn, tx);
        select.Parameters.AddWithValue("@p", parcelId);
        var existingId = select.ExecuteScalar();

        if (existingId is null)
        {
            using var insert = new SQLiteCommand("""
                INSERT INTO Members(ParcelId, FullName, TcNo, Address, Phone)
                VALUES(@p, @name, @tc, @address, @phone)
                """, conn, tx);
            insert.Parameters.AddWithValue("@p", parcelId);
            insert.Parameters.AddWithValue("@name", owner);
            insert.Parameters.AddWithValue("@tc", tcNo);
            insert.Parameters.AddWithValue("@address", address);
            insert.Parameters.AddWithValue("@phone", phone);
            insert.ExecuteNonQuery();
            return;
        }

        using var update = new SQLiteCommand("""
            UPDATE Members
            SET FullName=@name, TcNo=@tc, Address=@address, Phone=@phone
            WHERE Id=@id
            """, conn, tx);
        update.Parameters.AddWithValue("@name", owner);
        update.Parameters.AddWithValue("@tc", tcNo);
        update.Parameters.AddWithValue("@address", address);
        update.Parameters.AddWithValue("@phone", phone);
        update.Parameters.AddWithValue("@id", Convert.ToInt32(existingId));
        update.ExecuteNonQuery();
    }
}
