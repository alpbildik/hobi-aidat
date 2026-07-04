using EHBYS.Models;
using EHBYS.Services;

namespace EHBYS.UI;

public sealed class MainForm : Form
{
    private readonly Label lblSummary = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
    private readonly ToolStripMenuItem paymentMenu = new("Tahsilat");
    private readonly ToolStripMenuItem reportMenu = new("Raporlar");
    private readonly ToolStripMenuItem settingsMenu = new("Ayarlar");

    public MainForm()
    {
        Text = "EHBYS - Ege Hobi Bahceleri Yonetim Sistemi";
        Width = 1000;
        Height = 650;
        StartPosition = FormStartPosition.CenterScreen;

        var menu = new MenuStrip();
        var parcels = new ToolStripMenuItem("Parseller");
        parcels.Click += (_, _) => new ParcelForm().ShowDialog(this);
        var aidat = new ToolStripMenuItem("Aidatlar");
        aidat.Click += (_, _) => new AidatForm().ShowDialog(this);
        paymentMenu.Click += (_, _) => new PaymentForm().ShowDialog(this);
        var debts = new ToolStripMenuItem("Borclar");
        debts.Click += (_, _) => new DebtForm().ShowDialog(this);
        reportMenu.Click += (_, _) => new ReportForm().ShowDialog(this);
        settingsMenu.Click += (_, _) => new SettingsForm().ShowDialog(this);
        var dashboard = new ToolStripMenuItem("Dashboard");
        dashboard.Click += (_, _) => new DashboardChart().ShowDialog(this);
        var backup = new ToolStripMenuItem("Yedek Al");
        backup.Click += (_, _) => MessageBox.Show("Yedek olusturuldu:\n" + BackupService.CreateBackup());

        menu.Items.AddRange(new ToolStripItem[] { parcels, aidat, paymentMenu, debts, reportMenu, dashboard, settingsMenu, backup });
        Controls.Add(menu);
        MainMenuStrip = menu;

        lblSummary.Font = new Font(FontFamily.GenericSansSerif, 18, FontStyle.Bold);
        Controls.Add(lblSummary);
        ApplyPermissions();
        RefreshSummary();
    }

    private void ApplyPermissions()
    {
        settingsMenu.Enabled = Session.IsAdmin;
        reportMenu.Enabled = PermissionMatrix.CanViewReports;
        paymentMenu.Enabled = PermissionMatrix.CanTakePayment && Modules.Payments;
    }

    private void RefreshSummary()
    {
        lblSummary.Text =
            "EHBYS v1.0\n\n" +
            "Kullanici: " + Session.Username + " (" + Session.Role + ")\n" +
            "Moduller: Parsel, Aidat, Tahsilat, Bilesik Faiz, Borc, Rapor, Yedek\n\n" +
            "Varsayilan giris: admin / admin";
    }
}
