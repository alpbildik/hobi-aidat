using System.Data;
using System.Data.SQLite;
using EHBYS.Data;
using EHBYS.Services;

namespace EHBYS.UI;

public sealed class DebtForm : Form
{
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

    private sealed record DebtRow(string Parcel, string Owner, string Phone, decimal Total);

    public DebtForm()
    {
        Text = "Borc Listesi";
        Width = 850;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;
        Controls.Add(grid);
        Load += (_, _) => LoadRows();
    }

    private void LoadRows()
    {
        using var conn = Database.GetConnection();
        conn.Open();
        var table = new DataTable();
        table.Columns.Add("Parsel");
        table.Columns.Add("Uye");
        table.Columns.Add("Telefon");
        table.Columns.Add("AidatBorc", typeof(decimal));

        var rate = SettingsService.GetDecimal("MonthlyInterestRate", 0.07m);
        using var cmd = new SQLiteCommand("""
            SELECT p.Id, p.ParcelNo, p.OwnerName, p.Phone,
                   a.Principal, a.PaidAmount, a.DueDate, a.IsPaid
            FROM Parcels p
            LEFT JOIN Aidat a ON a.ParcelId = p.Id
            ORDER BY p.ParcelNo
            """, conn);
        using var reader = cmd.ExecuteReader();
        var totals = new Dictionary<int, DebtRow>();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            var row = totals.TryGetValue(id, out var existing)
                ? existing
                : new DebtRow(reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? "" : reader.GetString(3), 0m);

            if (!reader.IsDBNull(4) && reader.GetInt32(7) == 0)
            {
                var principal = Convert.ToDecimal(reader.GetDouble(4)) - Convert.ToDecimal(reader.GetDouble(5));
                var dueDate = DateTime.Parse(reader.GetString(6));
                row = row with
                {
                    Total = row.Total + InterestService.CalculateCompoundDebt(Math.Max(0, principal), dueDate, DateTime.Today, rate)
                };
            }

            totals[id] = row;
        }

        foreach (var item in totals.Values.Where(x => x.Total > 0).OrderByDescending(x => x.Total))
        {
            table.Rows.Add(item.Parcel, item.Owner, item.Phone, item.Total);
        }

        grid.DataSource = table;
    }
}
