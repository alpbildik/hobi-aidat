using System.Data;
using System.Data.SQLite;
using EHBYS.Data;
using EHBYS.Models;
using EHBYS.Services;

namespace EHBYS.UI;

public sealed class PaymentForm : Form
{
    private readonly ComboBox cmbParcel = new() { Left = 110, Top = 20, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown numAmount = new() { Left = 110, Top = 60, Width = 120, Maximum = 1_000_000, DecimalPlaces = 2 };
    private readonly TextBox txtMethod = new() { Left = 110, Top = 100, Width = 160, Text = "Nakit" };
    private readonly TextBox txtNote = new() { Left = 110, Top = 140, Width = 260 };

    public PaymentForm()
    {
        Text = "Tahsilat";
        Width = 430;
        Height = 260;
        StartPosition = FormStartPosition.CenterParent;

        Controls.Add(new Label { Left = 20, Top = 24, Width = 90, Text = "Parsel" });
        Controls.Add(cmbParcel);
        Controls.Add(new Label { Left = 20, Top = 64, Width = 90, Text = "Tutar" });
        Controls.Add(numAmount);
        Controls.Add(new Label { Left = 20, Top = 104, Width = 90, Text = "Yontem" });
        Controls.Add(txtMethod);
        Controls.Add(new Label { Left = 20, Top = 144, Width = 90, Text = "Not" });
        Controls.Add(txtNote);

        var btnSave = new Button { Left = 110, Top = 180, Width = 125, Text = "Kaydet" };
        btnSave.Click += (_, _) => SavePayment(closeAfterSave: false);
        Controls.Add(btnSave);
        var btnSaveClose = new Button { Left = 245, Top = 180, Width = 125, Text = "Kaydet ve Cik" };
        btnSaveClose.Click += (_, _) => SavePayment(closeAfterSave: true);
        Controls.Add(btnSaveClose);
        Load += (_, _) => LoadParcels();
    }

    private void LoadParcels()
    {
        using var conn = Database.GetConnection();
        conn.Open();
        var adapter = new SQLiteDataAdapter("SELECT Id, ParcelNo || ' - ' || OwnerName AS Name FROM Parcels ORDER BY ParcelNo", conn);
        var table = new DataTable();
        adapter.Fill(table);
        cmbParcel.DataSource = table;
        cmbParcel.DisplayMember = "Name";
        cmbParcel.ValueMember = "Id";
    }

    private void SavePayment(bool closeAfterSave)
    {
        if (!PermissionMatrix.CanTakePayment)
        {
            MessageBox.Show("Tahsilat yetkiniz yok.");
            return;
        }

        if (cmbParcel.SelectedValue is null || numAmount.Value <= 0)
        {
            MessageBox.Show("Parsel ve tutar secilmelidir.");
            return;
        }

        var parcelId = Convert.ToInt32(cmbParcel.SelectedValue);
        var amount = numAmount.Value;
        using (var conn = Database.GetConnection())
        {
            conn.Open();
            using var tx = conn.BeginTransaction();

            using var insert = new SQLiteCommand("INSERT INTO Payments(ParcelId, Amount, Date, Method, Note) VALUES(@p, @a, @d, @m, @n)", conn, tx);
            insert.Parameters.AddWithValue("@p", parcelId);
            insert.Parameters.AddWithValue("@a", amount);
            insert.Parameters.AddWithValue("@d", DateTime.Today.ToString("yyyy-MM-dd"));
            insert.Parameters.AddWithValue("@m", txtMethod.Text.Trim());
            insert.Parameters.AddWithValue("@n", txtNote.Text.Trim());
            insert.ExecuteNonQuery();

            AllocatePayment(conn, tx, parcelId, amount);

            tx.Commit();
        }

        LogService.Log("Tahsilat kaydedildi: " + amount + " TL");
        MessageBox.Show("Tahsilat kaydedildi.");
        if (closeAfterSave)
        {
            Close();
        }
        else
        {
            numAmount.Value = 0;
            txtNote.Clear();
        }
    }

    private static void AllocatePayment(SQLiteConnection conn, SQLiteTransaction tx, int parcelId, decimal amount)
    {
        var remaining = amount;
        using var select = new SQLiteCommand("""
            SELECT Id, Principal, PaidAmount
            FROM Aidat
            WHERE ParcelId=@p AND IsPaid=0
            ORDER BY DueDate, Id
            """, conn, tx);
        select.Parameters.AddWithValue("@p", parcelId);

        using var reader = select.ExecuteReader();
        var rows = new List<(int Id, decimal Principal, decimal Paid)>();
        while (reader.Read())
        {
            rows.Add((reader.GetInt32(0), Convert.ToDecimal(reader.GetDouble(1)), Convert.ToDecimal(reader.GetDouble(2))));
        }
        reader.Close();

        foreach (var row in rows)
        {
            if (remaining <= 0)
            {
                break;
            }

            var open = Math.Max(0, row.Principal - row.Paid);
            var applied = Math.Min(open, remaining);
            var newPaid = row.Paid + applied;
            remaining -= applied;

            using var update = new SQLiteCommand("""
                UPDATE Aidat
                SET PaidAmount=@paid, IsPaid=@closed
                WHERE Id=@id
                """, conn, tx);
            update.Parameters.AddWithValue("@paid", newPaid);
            update.Parameters.AddWithValue("@closed", newPaid >= row.Principal ? 1 : 0);
            update.Parameters.AddWithValue("@id", row.Id);
            update.ExecuteNonQuery();
        }
    }
}
