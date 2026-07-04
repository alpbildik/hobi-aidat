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
    private readonly DateTimePicker paymentDate = new() { Left = 110, Top = 100, Width = 160 };
    private readonly TextBox txtMethod = new() { Left = 110, Top = 140, Width = 160, Text = "Nakit" };
    private readonly TextBox txtNote = new() { Left = 110, Top = 180, Width = 260 };
    private readonly DataGridView grid = new() { Left = 20, Top = 260, Width = 660, Height = 300, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private bool showDeleted;

    public PaymentForm()
    {
        Text = "Tahsilat";
        Width = 730;
        Height = 620;
        StartPosition = FormStartPosition.CenterParent;

        Controls.Add(new Label { Left = 20, Top = 24, Width = 90, Text = "Parsel" });
        Controls.Add(cmbParcel);
        Controls.Add(new Label { Left = 20, Top = 64, Width = 90, Text = "Tutar" });
        Controls.Add(numAmount);
        Controls.Add(new Label { Left = 20, Top = 104, Width = 90, Text = "Tarih" });
        Controls.Add(paymentDate);
        Controls.Add(new Label { Left = 20, Top = 144, Width = 90, Text = "Yontem" });
        Controls.Add(txtMethod);
        Controls.Add(new Label { Left = 20, Top = 184, Width = 90, Text = "Not" });
        Controls.Add(txtNote);

        var btnSave = new Button { Left = 110, Top = 220, Width = 125, Text = "Kaydet" };
        btnSave.Click += (_, _) => SavePayment(closeAfterSave: false);
        Controls.Add(btnSave);
        var btnClose = new Button { Left = 245, Top = 220, Width = 125, Text = "Cik" };
        btnClose.Click += (_, _) => Close();
        Controls.Add(btnClose);
        var btnDelete = new Button { Left = 390, Top = 20, Width = 90, Text = "Sil" };
        btnDelete.Click += (_, _) => DeleteSelectedPayment();
        Controls.Add(btnDelete);
        var btnShowDeleted = new Button { Left = 390, Top = 60, Width = 140, Text = "Silinenleri Goster" };
        btnShowDeleted.Click += (_, _) =>
        {
            showDeleted = !showDeleted;
            btnShowDeleted.Text = showDeleted ? "Aktifleri Goster" : "Silinenleri Goster";
            LoadPayments();
        };
        Controls.Add(btnShowDeleted);
        Controls.Add(grid);
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        Load += (_, _) =>
        {
            LoadParcels();
            LoadPayments();
        };
    }

    private void LoadParcels()
    {
        using var conn = Database.GetConnection();
        conn.Open();
        var adapter = new SQLiteDataAdapter("SELECT Id, ParcelNo || ' - ' || OwnerName AS Name FROM Parcels WHERE IsDeleted=0 ORDER BY ParcelNo", conn);
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
        var paidAt = paymentDate.Value.Date;
        using (var conn = Database.GetConnection())
        {
            conn.Open();
            using var tx = conn.BeginTransaction();

            using var insert = new SQLiteCommand("INSERT INTO Payments(ParcelId, Amount, Date, Method, Note) VALUES(@p, @a, @d, @m, @n); SELECT last_insert_rowid();", conn, tx);
            insert.Parameters.AddWithValue("@p", parcelId);
            insert.Parameters.AddWithValue("@a", amount);
            insert.Parameters.AddWithValue("@d", paidAt.ToString("yyyy-MM-dd"));
            insert.Parameters.AddWithValue("@m", txtMethod.Text.Trim());
            insert.Parameters.AddWithValue("@n", txtNote.Text.Trim());
            var paymentId = Convert.ToInt32(insert.ExecuteScalar());

            AllocatePayment(conn, tx, paymentId, parcelId, amount, paidAt);

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
            LoadPayments();
        }
    }

    private static void AllocatePayment(SQLiteConnection conn, SQLiteTransaction tx, int paymentId, int parcelId, decimal amount, DateTime paidAt)
    {
        var remaining = amount;
        using var select = new SQLiteCommand("""
            SELECT Id, Principal, PaidAmount, DueDate
            FROM Aidat
            WHERE ParcelId=@p AND IsPaid=0 AND IsDeleted=0
            ORDER BY DueDate, Id
            """, conn, tx);
        select.Parameters.AddWithValue("@p", parcelId);

        using var reader = select.ExecuteReader();
        var rows = new List<(int Id, decimal Principal, decimal Paid, DateTime DueDate)>();
        while (reader.Read())
        {
            rows.Add((reader.GetInt32(0), Convert.ToDecimal(reader.GetDouble(1)), Convert.ToDecimal(reader.GetDouble(2)), DateTime.Parse(reader.GetString(3))));
        }
        reader.Close();

        var rate = SettingsService.GetDecimal("MonthlyInterestRate", 0.07m);
        foreach (var row in rows)
        {
            if (remaining <= 0)
            {
                break;
            }

            var openPrincipal = Math.Max(0, row.Principal - row.Paid);
            if (openPrincipal <= 0)
            {
                continue;
            }

            var debtAtPaymentDate = InterestService.CalculateCompoundDebt(openPrincipal, row.DueDate, paidAt, rate);
            var appliedDebt = Math.Min(debtAtPaymentDate, remaining);
            var principalReduction = debtAtPaymentDate <= 0
                ? 0
                : Math.Min(openPrincipal, openPrincipal * appliedDebt / debtAtPaymentDate);
            var newPaid = row.Paid + principalReduction;
            remaining -= appliedDebt;

            using var update = new SQLiteCommand("""
                UPDATE Aidat
                SET PaidAmount=@paid, IsPaid=@closed
                WHERE Id=@id
                """, conn, tx);
            update.Parameters.AddWithValue("@paid", newPaid);
            update.Parameters.AddWithValue("@closed", newPaid >= row.Principal - 0.01m ? 1 : 0);
            update.Parameters.AddWithValue("@id", row.Id);
            update.ExecuteNonQuery();

            using var allocation = new SQLiteCommand("""
                INSERT INTO PaymentAllocations(PaymentId, AidatId, PrincipalApplied, DebtApplied)
                VALUES(@payment, @aidat, @principal, @debt)
                """, conn, tx);
            allocation.Parameters.AddWithValue("@payment", paymentId);
            allocation.Parameters.AddWithValue("@aidat", row.Id);
            allocation.Parameters.AddWithValue("@principal", principalReduction);
            allocation.Parameters.AddWithValue("@debt", appliedDebt);
            allocation.ExecuteNonQuery();
        }
    }

    private void LoadPayments()
    {
        using var conn = Database.GetConnection();
        conn.Open();
        var adapter = new SQLiteDataAdapter("""
            SELECT pay.Id,
                   p.ParcelNo AS Parsel,
                   p.OwnerName AS Uye,
                   pay.Amount AS Tutar,
                   pay.Date AS Tarih,
                   pay.Method AS Yontem,
                   IFNULL(pay.Note, '') AS Not,
                   CASE WHEN pay.IsDeleted = 1 THEN 'Silinmis' ELSE 'Aktif' END AS Durum
            FROM Payments pay
            JOIN Parcels p ON p.Id = pay.ParcelId
            WHERE pay.IsDeleted = @deleted
            ORDER BY pay.Date DESC, pay.Id DESC
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

    private void DeleteSelectedPayment()
    {
        if (!PermissionMatrix.CanDeleteData)
        {
            MessageBox.Show("Silme islemi icin admin yetkisi gerekir.");
            return;
        }

        if (grid.CurrentRow?.DataBoundItem is not DataRowView row)
        {
            MessageBox.Show("Silmek icin listeden bir tahsilat secin.");
            return;
        }

        if (row["Durum"].ToString() == "Silinmis")
        {
            MessageBox.Show("Bu kayit zaten silinenlerde.");
            return;
        }

        var result = MessageBox.Show(
            "Secili tahsilat silinenlere tasinsin mi? Bu tahsilatin aidatlara etkisi geri alinacak.",
            "Silme Onayi",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        var paymentId = Convert.ToInt32(row["Id"]);
        using (var conn = Database.GetConnection())
        {
            conn.Open();
            using var tx = conn.BeginTransaction();

            using var allocations = new SQLiteCommand("SELECT AidatId, PrincipalApplied FROM PaymentAllocations WHERE PaymentId=@p", conn, tx);
            allocations.Parameters.AddWithValue("@p", paymentId);
            using var reader = allocations.ExecuteReader();
            var rows = new List<(int AidatId, decimal PrincipalApplied)>();
            while (reader.Read())
            {
                rows.Add((reader.GetInt32(0), Convert.ToDecimal(reader.GetDouble(1))));
            }
            reader.Close();

            foreach (var allocation in rows)
            {
                using var updateAidat = new SQLiteCommand("""
                    UPDATE Aidat
                    SET PaidAmount = MAX(0, PaidAmount - @amount),
                        IsPaid = 0
                    WHERE Id=@id
                    """, conn, tx);
                updateAidat.Parameters.AddWithValue("@amount", allocation.PrincipalApplied);
                updateAidat.Parameters.AddWithValue("@id", allocation.AidatId);
                updateAidat.ExecuteNonQuery();
            }

            using var updatePayment = new SQLiteCommand("UPDATE Payments SET IsDeleted=1 WHERE Id=@id", conn, tx);
            updatePayment.Parameters.AddWithValue("@id", paymentId);
            updatePayment.ExecuteNonQuery();

            tx.Commit();
        }

        LogService.Log("Tahsilat silinenlere tasindi: " + row["Tutar"] + " TL");
        LoadPayments();
    }
}
