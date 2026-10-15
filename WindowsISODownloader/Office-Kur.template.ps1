$ErrorActionPreference = 'Stop'
$setup = Join-Path $PSScriptRoot 'setup.exe'
$config = Join-Path $PSScriptRoot 'configuration.xml'
$runtimeConfig = Join-Path $PSScriptRoot ('configuration-install-' + [guid]::NewGuid().ToString('N') + '.xml')
$lock = $null
try {
    if (!(Test-Path -LiteralPath $setup) -or !(Test-Path -LiteralPath $config)) { throw 'Kurulum paketi eksik / Installation package is incomplete.' }
    $lock = New-Object System.IO.FileStream($setup, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $signature = Get-AuthenticodeSignature -LiteralPath $setup
    if ($signature.Status -ne 'Valid' -or $null -eq $signature.SignerCertificate -or
        $signature.SignerCertificate.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::SimpleName, $false) -ne 'Microsoft Corporation') {
        throw 'Microsoft imzasi dogrulanamadi; kurulum durduruldu / Microsoft signature could not be verified.'
    }
    [xml]$xml = Get-Content -LiteralPath $config -Raw
    $xml.Configuration.Add.SetAttribute('SourcePath', $PSScriptRoot)
    $xml.Save($runtimeConfig)
    Write-Host 'Office kurulumu baslatiliyor; yonetici onayini kabul edin / Starting Office installation; approve the administrator prompt.'
    $process = Start-Process -FilePath $setup -ArgumentList '/configure', ('"' + $runtimeConfig + '"') -WorkingDirectory $PSScriptRoot -Verb RunAs -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Office kurulum hata kodu / Installation error code: $($process.ExitCode)" }
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
} finally {
    if ($null -ne $lock) { $lock.Dispose() }
    if (Test-Path -LiteralPath $runtimeConfig) { Remove-Item -LiteralPath $runtimeConfig -Force -ErrorAction SilentlyContinue }
}
