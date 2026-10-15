# PowerShell 5.1+ on Windows. Runs after `az login` under the identity with
# Artifact Signing Certificate Profile Signer on a Public Trust profile.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$Endpoint,
    [Parameter(Mandatory=$true)][string]$AccountName,
    [Parameter(Mandatory=$true)][string]$CertificateProfileName,
    [Parameter(Mandatory=$true)][string]$SignToolPath,
    [Parameter(Mandatory=$true)][string]$DlibPath,
    [string]$InputZip = (Join-Path $PSScriptRoot 'Windows-ISO-Downloader-win-x64.zip'),
    [string]$OutputZip = (Join-Path $PSScriptRoot 'Windows-ISO-Downloader-win-x64-IMZALI.zip')
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$endpointUri = $null
if (-not [Uri]::TryCreate($Endpoint,[UriKind]::Absolute,[ref]$endpointUri) -or
    $endpointUri.Scheme -ne 'https' -or
    $endpointUri.Host -notmatch '^[a-z0-9-]+\.codesigning\.azure\.net$') {
    throw 'Endpoint Azure portalındaki Artifact Signing Account URI olmalıdır.'
}
foreach ($p in @($InputZip,$SignToolPath,$DlibPath)) {
    if (-not (Test-Path -LiteralPath $p -PathType Leaf)) { throw "Dosya bulunamadı: $p" }
}
if (Test-Path -LiteralPath $OutputZip) { throw "Çıktı zaten mevcut; üzerine yazılmadı: $OutputZip" }
$cli = Get-Command az -ErrorAction SilentlyContinue
if (-not $cli) { throw 'Azure CLI bulunamadı. Azure CLI kurup kendi hesabınızla az login çalıştırın.' }
& $cli.Source account show --only-show-errors --output none
if ($LASTEXITCODE -ne 0) { throw 'Azure CLI oturumu yok. az login çalıştırın.' }
$work = Join-Path ([IO.Path]::GetTempPath()) ('wid-artifact-sign-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
    $package = Join-Path $work 'package'
    Expand-Archive -LiteralPath $InputZip -DestinationPath $package
    $files = @(Get-ChildItem -LiteralPath $package -Filter 'Windows ISO Downloader.exe' -File -Recurse)
    if ($files.Count -ne 1) { throw 'Pakette tam olarak bir EXE olmalıdır.' }
    $exe = $files[0].FullName
    $prior = Get-AuthenticodeSignature -LiteralPath $exe
    if ($prior.Status -ne 'NotSigned') { throw "Beklenen imzasız EXE bulunamadı: $($prior.Status)" }
    $metadata = Join-Path $work 'metadata.json'
    $metadataJson = @{Endpoint=$endpointUri.AbsoluteUri;CodeSigningAccountName=$AccountName;CertificateProfileName=$CertificateProfileName} | ConvertTo-Json -Compress
    [IO.File]::WriteAllText($metadata, $metadataJson, (New-Object System.Text.UTF8Encoding($false)))
    & $SignToolPath sign /v /fd SHA256 /tr 'http://timestamp.acs.microsoft.com/' /td SHA256 /dlib $DlibPath /dmdf $metadata $exe
    if ($LASTEXITCODE -ne 0) { throw 'Artifact Signing ile imzalama başarısız. Profilin Public Trust ve etkin; kimlik doğrulamanın tamamlanmış; rolün Certificate Profile Signer olduğundan emin olun.' }
    & $SignToolPath verify /pa /all /tw /v $exe
    if ($LASTEXITCODE -ne 0) { throw 'SignTool imza doğrulaması başarısız.' }
    $signature = Get-AuthenticodeSignature -LiteralPath $exe
    if ($signature.Status -ne 'Valid' -or -not $signature.SignerCertificate) {
        throw "Authenticode doğrulaması başarısız: $($signature.Status)"
    }
    Compress-Archive -Path (Join-Path $package '*') -DestinationPath $OutputZip -CompressionLevel Optimal
    $hash = Get-FileHash -LiteralPath $exe -Algorithm SHA256
    Write-Host "İmzalı paket: $OutputZip"
    Write-Host "Yayımlayıcı: $($signature.SignerCertificate.Subject)"
    Write-Host "İmzalı EXE SHA-256: $($hash.Hash)"
} finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
