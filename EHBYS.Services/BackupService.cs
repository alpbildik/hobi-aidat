using EHBYS.Data;

namespace EHBYS.Services;

public static class BackupService
{
    public static string CreateBackup()
    {
        Directory.CreateDirectory(Database.AppFolder);
        var backupPath = Path.Combine(Database.AppFolder, $"EHBYS_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
        File.Copy(Database.DatabasePath, backupPath, true);
        LogService.Log("Backup olusturuldu: " + backupPath);
        return backupPath;
    }
}
