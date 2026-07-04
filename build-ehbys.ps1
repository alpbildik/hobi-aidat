$ErrorActionPreference = "Stop"

Write-Host "EHBYS build basliyor..." -ForegroundColor Cyan

$ProjectPath = ".\EHBYS.UI\EHBYS.UI.csproj"
$OutputPath = ".\publish\EHBYS"

if (!(Test-Path $ProjectPath)) {
    Write-Host "HATA: EHBYS.UI.csproj bulunamadi." -ForegroundColor Red
    exit 1
}

Write-Host "Eski publish klasoru temizleniyor..."
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Recurse -Force
}

Write-Host "Restore yapiliyor..."
dotnet restore .\EHBYS.sln

Write-Host "Release build aliniyor..."
dotnet build .\EHBYS.sln -c Release

Write-Host "Windows EXE publish ediliyor..."
dotnet publish $ProjectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $OutputPath

$InnoScript = ".\installer\EHBYS.iss"
$InnoCompiler = Get-Command "iscc" -ErrorAction SilentlyContinue
if ($InnoCompiler -and (Test-Path $InnoScript)) {
    Write-Host "Inno Setup bulundu, Setup.exe olusturuluyor..."
    & $InnoCompiler.Source $InnoScript
} else {
    Write-Host "Inno Setup bulunamadi. Portable EXE olusturuldu, Setup.exe atlandi." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "ISLEM TAMAM!" -ForegroundColor Green
Write-Host "EXE konumu:" -ForegroundColor Yellow
Write-Host "$OutputPath"
if (Test-Path ".\publish\Setup\Setup.exe") {
    Write-Host "Setup.exe konumu:" -ForegroundColor Yellow
    Write-Host ".\publish\Setup\Setup.exe"
}

explorer $OutputPath
pause
