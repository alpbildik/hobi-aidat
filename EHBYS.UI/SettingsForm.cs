using EHBYS.Models;
using EHBYS.Services;

namespace EHBYS.UI;

public sealed class SettingsForm : Form
{
    private readonly NumericUpDown numAidat = new() { Left = 170, Top = 25, Width = 120, Maximum = 100000, DecimalPlaces = 2 };
    private readonly NumericUpDown numInterest = new() { Left = 170, Top = 65, Width = 120, Maximum = 100, DecimalPlaces = 2 };
    private readonly NumericUpDown numDueDay = new() { Left = 170, Top = 105, Width = 120, Minimum = 1, Maximum = 28 };

    public SettingsForm()
    {
        Text = "Ayarlar";
        Width = 360;
        Height = 220;
        StartPosition = FormStartPosition.CenterParent;

        Controls.Add(new Label { Left = 25, Top = 28, Width = 140, Text = "Aylik aidat" });
        Controls.Add(numAidat);
        Controls.Add(new Label { Left = 25, Top = 68, Width = 140, Text = "Aylik faiz (%)" });
        Controls.Add(numInterest);
        Controls.Add(new Label { Left = 25, Top = 108, Width = 140, Text = "Son odeme gunu" });
        Controls.Add(numDueDay);

        var btnSave = new Button { Left = 170, Top = 145, Width = 120, Text = "Kaydet" };
        btnSave.Click += (_, _) => Save();
        Controls.Add(btnSave);
        Load += (_, _) => LoadSettings();
    }

    private void LoadSettings()
    {
        if (!Session.IsAdmin)
        {
            MessageBox.Show("Ayarlar icin admin yetkisi gerekir.");
            Close();
            return;
        }

        numAidat.Value = SettingsService.GetDecimal("MonthlyAidat", 600);
        numInterest.Value = SettingsService.GetDecimal("MonthlyInterestRate", 0.07m) * 100;
        numDueDay.Value = SettingsService.GetDecimal("DueDay", 20);
    }

    private void Save()
    {
        SettingsService.Set("MonthlyAidat", numAidat.Value);
        SettingsService.Set("MonthlyInterestRate", numInterest.Value / 100);
        SettingsService.Set("DueDay", numDueDay.Value);
        LogService.Log("Ayarlar guncellendi.");
        MessageBox.Show("Ayarlar kaydedildi.");
    }
}
