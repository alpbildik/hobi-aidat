using System.Data.SQLite;
using EHBYS.Data;

namespace EHBYS.Services;

public static class DebtService
{
    public static decimal CalculateParcelDebt(int parcelId, SQLiteConnection? existingConnection = null)
    {
        var ownsConnection = existingConnection is null;
        var conn = existingConnection ?? Database.GetConnection();

        if (ownsConnection)
        {
            conn.Open();
        }

        try
        {
            var rate = SettingsService.GetDecimal("MonthlyInterestRate", 0.07m);
            using var cmd = new SQLiteCommand("""
                SELECT Principal, PaidAmount, DueDate
                FROM Aidat
                WHERE ParcelId=@p AND IsPaid=0 AND IsDeleted=0
                """, conn);
            cmd.Parameters.AddWithValue("@p", parcelId);

            using var reader = cmd.ExecuteReader();
            var total = 0m;
            while (reader.Read())
            {
                var principal = Convert.ToDecimal(reader.GetDouble(0)) - Convert.ToDecimal(reader.GetDouble(1));
                var dueDate = DateTime.Parse(reader.GetString(2));
                total += InterestService.CalculateCompoundDebt(Math.Max(0, principal), dueDate, DateTime.Today, rate);
            }

            return total;
        }
        finally
        {
            if (ownsConnection)
            {
                conn.Dispose();
            }
        }
    }

    public static decimal CalculateTotalDebt(SQLiteConnection? existingConnection = null)
    {
        var ownsConnection = existingConnection is null;
        var conn = existingConnection ?? Database.GetConnection();

        if (ownsConnection)
        {
            conn.Open();
        }

        try
        {
            using var cmd = new SQLiteCommand("SELECT Id FROM Parcels WHERE IsDeleted=0", conn);
            using var reader = cmd.ExecuteReader();
            var parcelIds = new List<int>();
            while (reader.Read())
            {
                parcelIds.Add(reader.GetInt32(0));
            }
            reader.Close();

            return parcelIds.Sum(parcelId => CalculateParcelDebt(parcelId, conn));
        }
        finally
        {
            if (ownsConnection)
            {
                conn.Dispose();
            }
        }
    }
}
