# EHBYS - Ege Hobi Bahceleri Yonetim Sistemi

Bu proje, paylasilan ChatGPT konusmasindaki aidat otomasyonu fikrinden olusturulan .NET 8 WinForms + SQLite masaustu uygulama iskeletidir.

## Moduller

- Admin girisi: `admin / admin`
- Parsel ve uye kaydi
- Aylik aidat tahakkuku
- Vade tarihine gore aylik bilesik faiz hesabi
- Tahsilat girisi
- Borc listesi
- Rapor ozeti
- Ayarlar: aylik aidat, aylik bilesik faiz orani, son odeme gunu
- SQLite veritabani: `%APPDATA%\EHBYS\EHBYS.db`
- Tek tik Windows publish scripti: `build-ehbys.bat`

## Windows'ta calistirma

1. Visual Studio 2022/2025 kur.
2. `.NET desktop development` workload'unu sec.
3. `EHBYS.sln` dosyasini ac.
4. NuGet restore yap.
5. `EHBYS.UI` projesini baslat.

## Tek tik EXE uretme

Windows'ta proje klasorunde `build-ehbys.bat` dosyasina cift tikla.

Cikti:

```text
publish\EHBYS\EHBYS.exe
```

Not: Bu portable EXE uretir. Gercek `Setup.exe` icin Visual Studio Installer Project veya Inno Setup eklenebilir.

## Setup.exe uretme

Inno Setup kurulu bir Windows cihazda once `build-ehbys.bat` calistir, sonra `installer\EHBYS.iss` dosyasini Inno Setup ile compile et.

Cikti:

```text
publish\Setup\Setup.exe
```

## Windows cihaz olmadan EXE uretme

GitHub Actions workflow'u eklendi: `.github/workflows/windows-exe.yml`.

Kullanim:

1. Projeyi GitHub repository'sine push et.
2. GitHub'da `Actions` sekmesine gir.
3. `Windows EXE Build` workflow'unu sec.
4. `Run workflow` ile calistir.
5. Build bitince `Artifacts` bolumunden indir:
   - `EHBYS-portable-win-x64`
   - `EHBYS-setup-win-x64`
