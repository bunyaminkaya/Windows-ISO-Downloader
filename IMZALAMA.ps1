param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string]$CertificateThumbprint,
    [string]$ExePath = (Join-Path $PSScriptRoot 'Windows ISO Downloader.exe'),
    [string]$TimestampUrl = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $ExePath -PathType Leaf)) {
    throw "EXE bulunamadı: $ExePath"
}
$signtool = (Get-Command signtool.exe -ErrorAction SilentlyContinue).Source
if (-not $signtool) { throw 'SignTool bulunamadı. Windows SDK içindeki SignTool aracını kurup PATH değişkenine ekleyin.' }

$thumbprint = $CertificateThumbprint.ToUpperInvariant()
$cert = Get-ChildItem Cert:\CurrentUser\My,Cert:\LocalMachine\My |
    Where-Object Thumbprint -eq $thumbprint | Select-Object -First 1
if (-not $cert) { throw 'Belirtilen sertifika sertifika deposunda bulunamadı.' }
if (-not $cert.HasPrivateKey) { throw 'Sertifikanın özel anahtarına erişilemiyor.' }
$eku = @($cert.EnhancedKeyUsageList | ForEach-Object ObjectId)
if ($eku -notcontains '1.3.6.1.5.5.7.3.3') { throw 'Sertifika kod imzalama amacı taşımıyor.' }

& $signtool sign /sha1 $thumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 /v $ExePath
if ($LASTEXITCODE -ne 0) { throw 'Kod imzalama başarısız.' }
& $signtool verify /pa /all /tw /v $ExePath
if ($LASTEXITCODE -ne 0) { throw 'Authenticode imza doğrulaması başarısız.' }
$signature = Get-AuthenticodeSignature -LiteralPath $ExePath
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Thumbprint -ne $thumbprint) {
    throw "İmza veya yayımlayıcı uyuşmuyor: $($signature.Status)"
}
Write-Host "İmza doğrulandı. Yayımlayıcı: $($signature.SignerCertificate.Subject)"
Get-FileHash -LiteralPath $ExePath -Algorithm SHA256
