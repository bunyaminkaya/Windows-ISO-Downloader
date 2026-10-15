$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
try {
    # Repair rebuilds the application without deleting a working SDK.
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $Root 'Kurulum.ps1')
    exit $LASTEXITCODE
} catch {
    Write-Host "HATA: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
