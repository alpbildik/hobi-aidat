using System.Data.SQLite;
using EHBYS.Data;
using EHBYS.Models;
using EHBYS.Services;

namespace EHBYS.UI;

public sealed class LoginForm : Form
{
    private readonly TextBox txtUser = new() { Left = 120, Top = 30, Width = 180, Text = "admin" };
    private readonly TextBox txtPassword = new() { Left = 120, Top = 70, Width = 180, PasswordChar = '*', Text = "admin" };

    public LoginForm()
    {
        Text = "EHBYS - Giris";
        Width = 360;
        Height = 180;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(new Label { Left = 30, Top = 34, Width = 80, Text = "Kullanici" });
        Controls.Add(txtUser);
        Controls.Add(new Label { Left = 30, Top = 74, Width = 80, Text = "Sifre" });
        Controls.Add(txtPassword);

        var btnLogin = new Button { Left = 120, Top = 105, Width = 180, Text = "Giris" };
        btnLogin.Click += (_, _) => Login();
        Controls.Add(btnLogin);
        AcceptButton = btnLogin;
    }

    private void Login()
    {
        using var conn = Database.GetConnection();
        conn.Open();

        using var cmd = new SQLiteCommand("SELECT Username, Role FROM Users WHERE Username=@u AND Password=@p", conn);
        cmd.Parameters.AddWithValue("@u", txtUser.Text.Trim());
        cmd.Parameters.AddWithValue("@p", txtPassword.Text);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            MessageBox.Show("Kullanici adi veya sifre hatali.", "EHBYS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Session.Username = reader.GetString(0);
        Session.Role = reader.GetString(1).ToLowerInvariant() switch
        {
            "admin" => UserRole.Admin,
            "muhasebe" => UserRole.Muhasebe,
            _ => UserRole.Kullanici
        };

        LogService.Log("Kullanici giris yapti: " + Session.Username);
        Hide();
        using var main = new MainForm();
        main.ShowDialog();
        Close();
    }
}
