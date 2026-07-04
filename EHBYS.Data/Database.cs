using System.Data.SQLite;

namespace EHBYS.Data;

public static class Database
{
    public static string AppFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EHBYS");

    public static string DatabasePath { get; } = Path.Combine(AppFolder, "EHBYS.db");

    public static SQLiteConnection GetConnection()
    {
        return new SQLiteConnection($"Data Source={DatabasePath};Version=3;");
    }

    public static void Initialize()
    {
        Directory.CreateDirectory(AppFolder);

        if (!File.Exists(DatabasePath))
        {
            SQLiteConnection.CreateFile(DatabasePath);
        }

        using var conn = GetConnection();
        conn.Open();

        Execute(conn, """
            CREATE TABLE IF NOT EXISTS Users(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL,
                Role TEXT NOT NULL
            );
            """);

        Execute(conn, """
            CREATE TABLE IF NOT EXISTS Parcels(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ParcelNo TEXT NOT NULL UNIQUE,
                OwnerName TEXT NOT NULL,
                Phone TEXT,
                Debt REAL NOT NULL DEFAULT 0
            );
            """);

        Execute(conn, """
            CREATE TABLE IF NOT EXISTS Members(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ParcelId INTEGER NOT NULL,
                FullName TEXT NOT NULL,
                TcNo TEXT,
                Address TEXT,
                Phone TEXT,
                FOREIGN KEY(ParcelId) REFERENCES Parcels(Id)
            );
            """);

        Execute(conn, """
            CREATE TABLE IF NOT EXISTS Aidat(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ParcelId INTEGER NOT NULL,
                Period TEXT NOT NULL,
                Principal REAL NOT NULL,
                DueDate TEXT NOT NULL,
                PaidAmount REAL NOT NULL DEFAULT 0,
                IsPaid INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY(ParcelId) REFERENCES Parcels(Id)
            );
            """);

        Execute(conn, """
            CREATE TABLE IF NOT EXISTS Payments(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ParcelId INTEGER NOT NULL,
                Amount REAL NOT NULL,
                Date TEXT NOT NULL,
                Method TEXT NOT NULL,
                Note TEXT,
                FOREIGN KEY(ParcelId) REFERENCES Parcels(Id)
            );
            """);

        Execute(conn, """
            CREATE TABLE IF NOT EXISTS Settings(
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
            """);

        Execute(conn, """
            CREATE TABLE IF NOT EXISTS Logs(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Action TEXT NOT NULL,
                Date TEXT NOT NULL
            );
            """);

        Execute(conn, "INSERT OR IGNORE INTO Users(Username, Password, Role) VALUES('admin', 'admin', 'admin');");
        Execute(conn, "INSERT OR IGNORE INTO Settings(Key, Value) VALUES('MonthlyAidat', '600');");
        Execute(conn, "INSERT OR IGNORE INTO Settings(Key, Value) VALUES('MonthlyInterestRate', '0.07');");
        Execute(conn, "INSERT OR IGNORE INTO Settings(Key, Value) VALUES('DueDay', '20');");
    }

    private static void Execute(SQLiteConnection conn, string sql)
    {
        using var cmd = new SQLiteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }
}
