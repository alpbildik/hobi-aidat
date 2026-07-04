namespace EHBYS.Models;

public static class Session
{
    public static string Username { get; set; } = "";
    public static UserRole Role { get; set; } = UserRole.Kullanici;

    public static bool IsAdmin => Role == UserRole.Admin;
}
