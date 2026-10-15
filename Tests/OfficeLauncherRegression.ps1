$ErrorActionPreference = 'Stop'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('wid-office-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $testRoot | Out-Null
try {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../WindowsISODownloader/Office-Kur.template.ps1') -Destination (Join-Path $testRoot 'Office-Kur.ps1')
    [IO.File]::WriteAllText((Join-Path $testRoot 'setup.exe'),'mock setup, never executed')
    [IO.File]::WriteAllText((Join-Path $testRoot 'configuration.xml'),'<Configuration><Add SourcePath="old-path"><Product ID="O365ProPlusRetail"><Language ID="tr-tr" /></Product></Add></Configuration>')
    function Get-AuthenticodeSignature {
        param($LiteralPath)
        $certificate = New-Object PSObject
        $certificate | Add-Member ScriptMethod GetNameInfo {param($type,$issuer) return 'Microsoft Corporation'}
        return [pscustomobject]@{Status='Valid';SignerCertificate=$certificate}
    }
    function Start-Process {
        param($FilePath,$ArgumentList,$WorkingDirectory,$Verb,[switch]$Wait,[switch]$PassThru)
        if ($Verb -ne 'RunAs' -or $ArgumentList[0] -ne '/configure') {throw 'Expected user-initiated administrator install'}
        $runtimePath = $ArgumentList[1].Trim('"')
        [xml]$runtime = Get-Content -LiteralPath $runtimePath -Raw
        if ($runtime.Configuration.Add.SourcePath -ne $testRoot) {throw 'Moved package source path was not rebased'}
        $global:WID_OfficeLauncherChecked = $true
        return [pscustomobject]@{ExitCode=0}
    }
    & (Join-Path $testRoot 'Office-Kur.ps1')
    if (!$global:WID_OfficeLauncherChecked) {throw 'Mock installation command was not called'}
    if (!(Test-Path -LiteralPath (Join-Path $testRoot 'configuration.xml'))) {throw 'Stable configuration was deleted'}
    if (@(Get-ChildItem -LiteralPath $testRoot -Filter 'configuration-install-*').Count -ne 0) {throw 'Runtime configuration was not cleaned'}
    Write-Host 'PASS: Office launcher command, moved source path, durable config and temporary cleanup (all signature/process calls mocked; no Office installed).'
} finally { Remove-Variable WID_OfficeLauncherChecked -Scope Global -ErrorAction SilentlyContinue; Remove-Item -LiteralPath $testRoot -Recurse -Force -ErrorAction SilentlyContinue }
