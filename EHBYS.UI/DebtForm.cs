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

        using var cmd = new SQLiteCommand("""
            SELECT p.Id, p.ParcelNo, p.OwnerName, p.Phone
            FROM Parcels p
            ORDER BY p.ParcelNo
            """, conn);
        using var reader = cmd.ExecuteReader();
        var rows = new List<(int Id, string Parcel, string Owner, string Phone)>();
        while (reader.Read())
        {
            rows.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? "" : reader.GetString(3)));
        }
        reader.Close();

        var debtRows = rows
            .Select(row => new DebtRow(row.Parcel, row.Owner, row.Phone, DebtService.CalculateParcelDebt(row.Id, conn)))
            .Where(row => row.Total > 0)
            .OrderByDescending(row => row.Total);

        foreach (var item in debtRows)
        {
            table.Rows.Add(item.Parcel, item.Owner, item.Phone, item.Total);
        }

        grid.DataSource = table;
    }
}
