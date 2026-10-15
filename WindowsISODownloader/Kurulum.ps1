$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Work = Join-Path $env:LOCALAPPDATA 'WID'
$SdkRoot = Join-Path $Work 'dotnet'
$BuildRoot = Join-Path $Work 'build'
$TempPublish = Join-Path $Work 'publish'
$Log = Join-Path $Work 'kurulum.log'
$AppDir = Join-Path $Root 'bin\App'
$Script = Join-Path $Work 'dotnet-install.ps1'
$Sources = @((Get-ChildItem -LiteralPath $Root -File | Where-Object { $_.Extension -in @('.cs', '.xaml', '.csproj', '.resx') } | ForEach-Object { $_.Name })) + @('Fido.ps1', 'FIDO-LICENSE.txt', 'Office-Kur.template.ps1')
$Architecture = if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { 'arm64' } else { 'x64' }
$Rid = "win-$Architecture"

function Say([string]$Message) { Write-Host "`n[Windows ISO Downloader] $Message" -ForegroundColor Cyan }
function HasSdk8([string]$Exe) {
    if (-not (Test-Path -LiteralPath $Exe -PathType Leaf)) { return $false }
    try {
        $list = & $Exe --list-sdks 2>$null
        if ($LASTEXITCODE -ne 0) { return $false }
        $healthy = $false
        foreach ($line in $list) {
            if ($line -notmatch '^(8\.\d+\.\d+)\s+\[(.+)\]') { continue }
            $sdk = Join-Path $Matches[2] $Matches[1]
            $required = @('MSBuild.dll', 'Roslyn\bincore\csc.dll', 'Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.targets')
            $complete = $true
            foreach ($relative in $required) {
                $file = Join-Path $sdk $relative
                if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or (Get-Item -LiteralPath $file).Length -eq 0) { $complete = $false; break }
            }
            if ($complete) { $healthy = $true; break }
        }
        if (-not $healthy) { return $false }
        & $Exe msbuild -nologo -version 2>$null | Out-Null
        return ($LASTEXITCODE -eq 0)
    } catch { return $false }
}
function Install-Sdk {
    Say "Resmi .NET 8 SDK, kisa bir yola kuruluyor: $SdkRoot"
    if (Test-Path -LiteralPath $SdkRoot) { Remove-Item -LiteralPath $SdkRoot -Force -Recurse }
    New-Item -ItemType Directory -Path $SdkRoot -Force | Out-Null
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $Script -UseBasicParsing
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $Script -Channel 8.0 -Quality GA -Architecture $Architecture -InstallDir $SdkRoot -NoPath | Out-Host
    if ($LASTEXITCODE -ne 0) { throw '.NET SDK kurulumu tamamlanamadi. Yukaridaki indirme hatasini kontrol edin.' }
    $exe = Join-Path $SdkRoot 'dotnet.exe'
    if (-not (HasSdk8 $exe)) { throw 'Kurulum sonunda .NET 8 SDK listelenmedi.' }
    return $exe
}
function Publish-App([string]$Exe) {
    Say "Derleme baslatiliyor ($Rid): $Exe"
    if (Test-Path -LiteralPath $BuildRoot) { Remove-Item -LiteralPath $BuildRoot -Recurse -Force }
    if (Test-Path -LiteralPath $TempPublish) { Remove-Item -LiteralPath $TempPublish -Recurse -Force }
    New-Item -ItemType Directory -Path $BuildRoot -Force | Out-Null
    foreach ($name in $Sources) { Copy-Item -LiteralPath (Join-Path $Root $name) -Destination (Join-Path $BuildRoot $name) }
    Copy-Item -LiteralPath (Join-Path $Root 'Assets') -Destination (Join-Path $BuildRoot 'Assets') -Recurse -Force
    $project = Join-Path $BuildRoot 'WindowsISODownloader.csproj'
    & $Exe publish $project -c Release -r $Rid --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $TempPublish 2>&1 | Tee-Object -FilePath $Log | Out-Host
    if ($LASTEXITCODE -ne 0) { return $false }
    return (Test-Path -LiteralPath (Join-Path $TempPublish 'Windows ISO Downloader.exe') -PathType Leaf)
}

try {
    if (-not [Environment]::Is64BitOperatingSystem) { throw 'Bu paket 64-bit Windows gerektirir.' }
    foreach ($name in $Sources) {
        if (-not (Test-Path -LiteralPath (Join-Path $Root $name) -PathType Leaf)) { throw "Kaynak dosya eksik: $name" }
    }
    New-Item -ItemType Directory -Path $Work -Force | Out-Null
    Say 'Sistemde .NET 8 SDK ve temel derleme dosyalari kontrol ediliyor...'
    $system = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    $exe = if ($system -and (HasSdk8 $system.Source)) { $system.Source } else { $null }
    $local = Join-Path $SdkRoot 'dotnet.exe'
    if (-not $exe) {
        if (HasSdk8 $local) { $exe = $local } else { Say 'Yerel SDK eksik veya saglik kontrolu basarisiz; yeniden kuruluyor...'; $exe = Install-Sdk }
    }
    $ok = Publish-App $exe
    if (-not $ok) { throw "Derleme basarisiz. Ayrintilar: $Log" }
    New-Item -ItemType Directory -Path $AppDir -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $TempPublish 'Windows ISO Downloader.exe') -Destination (Join-Path $AppDir 'Windows ISO Downloader.exe') -Force
    foreach ($name in @('Fido.ps1', 'FIDO-LICENSE.txt')) { Copy-Item -LiteralPath (Join-Path $Root $name) -Destination (Join-Path $AppDir $name) -Force }
    [IO.File]::WriteAllText((Join-Path $AppDir 'wid.cmd'), "@echo off`r`n`"%~dp0Windows ISO Downloader.exe`" %*`r`nexit /b %errorlevel%`r`n", [Text.Encoding]::ASCII)
    try {
        $desktop = [Environment]::GetFolderPath('DesktopDirectory')
        if ($desktop -and (Test-Path -LiteralPath $desktop)) {
            $shortcut = (New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path $desktop 'Windows ISO Downloader.lnk'))
            $shortcut.TargetPath = Join-Path $AppDir 'Windows ISO Downloader.exe'
            $shortcut.WorkingDirectory = $AppDir
            $shortcut.IconLocation = (Join-Path $AppDir 'Windows ISO Downloader.exe') + ',0'
            $shortcut.Save()
            Say 'Masaustu kisayolu olusturuldu.'
        }
    } catch { Write-Warning "Masaustu kisayolu olusturulamadi: $($_.Exception.Message)" }
    Say 'EXE olusturuldu; uygulama aciliyor...' 
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $Root 'Baslat.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'EXE olusturuldu fakat program acilamadi. Yukaridaki hata gunlugunu kontrol edin.' }
    exit 0
} catch {
    Write-Host "`nHATA: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Derleme gunlugu (varsa): $Log" -ForegroundColor Yellow
    exit 1
}
