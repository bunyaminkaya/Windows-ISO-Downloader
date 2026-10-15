$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Exe = Join-Path $Root 'bin\App\Windows ISO Downloader.exe'
$Log = Join-Path $env:LOCALAPPDATA 'WID\hata.log'
if (-not (Test-Path -LiteralPath $Exe)) {
    Write-Host 'EXE bulunamadi. Kurulum.cmd dosyasini calistirin.' -ForegroundColor Red
    exit 1
}
try {
    $process = Start-Process -FilePath $Exe -WorkingDirectory (Split-Path $Exe) -PassThru
    Start-Sleep -Seconds 3
    $process.Refresh()
    if ($process.HasExited) {
        Write-Host "Program acilistan hemen sonra kapandi (kod: $($process.ExitCode))." -ForegroundColor Red
        if (Test-Path -LiteralPath $Log) {
            Write-Host "Hata gunlugu: $Log" -ForegroundColor Yellow
            Get-Content -LiteralPath $Log -Tail 30
        }
        exit 1
    }
    exit 0
} catch {
    Write-Host "Program baslatilamadi: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
