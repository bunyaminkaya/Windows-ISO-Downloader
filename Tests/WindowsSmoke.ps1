param([Parameter(Mandatory=$true)][string]$Exe, [string]$OdtPath)
$ErrorActionPreference = 'Stop'
$lock = $null
$gui = $null
$work = Join-Path ([IO.Path]::GetTempPath()) ('wid-smoke-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $work | Out-Null
try {
    $command = Join-Path $env:SystemRoot 'System32\cmd.exe'
    if ((Get-AuthenticodeSignature -LiteralPath $command).Status -ne 'Valid') { throw 'Windows command signature is not valid.' }
    $lock = [IO.File]::Open($command,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = $command
    $start.Arguments = '/d /c exit 0'
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $process = [Diagnostics.Process]::Start($start)
    if (!$process.WaitForExit(15000) -or $process.ExitCode -ne 0) { throw 'Launching a read-locked executable failed.' }
    $lock.Dispose(); $lock = $null
    $gui = Start-Process -FilePath $Exe -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    do {
        Start-Sleep -Milliseconds 250
        $gui.Refresh()
        if ($gui.HasExited) { throw 'WPF application exited before its window appeared.' }
    } while ($gui.MainWindowHandle -eq [IntPtr]::Zero -and [DateTime]::UtcNow -lt $deadline)
    if ($gui.MainWindowHandle -eq [IntPtr]::Zero) { throw 'WPF window was not created within 30 seconds.' }
    if ($OdtPath) {
        $lock = [IO.File]::Open($OdtPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        $sig = Get-AuthenticodeSignature -LiteralPath $OdtPath
        if ($sig.Status -ne 'Valid' -or $sig.SignerCertificate.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::SimpleName,$false) -ne 'Microsoft Corporation') { throw 'ODT Microsoft signature was not verified.' }
        $extract = Start-Process -FilePath $OdtPath -ArgumentList '/quiet', ('/extract:"'+$work+'"') -PassThru
        if (!$extract.WaitForExit(60000)) { $extract.Kill(); throw 'ODT extraction timed out.' }
        if ($extract.ExitCode -ne 0) { throw 'ODT could not start under the read lock.' }
        $setup = Join-Path $work 'setup.exe'
        if (!(Test-Path -LiteralPath $setup) -or (Get-AuthenticodeSignature -LiteralPath $setup).Status -ne 'Valid') { throw 'Extracted setup.exe is not valid.' }
        Write-Host 'PASS: ODT signature and read-locked extraction. Office was not installed.'
    }
    Write-Host 'PASS: Windows read-lock process launch and WPF startup.'
} finally {
    if ($null -ne $lock) { $lock.Dispose() }
    if ($null -ne $gui) {
        $gui.Refresh()
        if (!$gui.HasExited) { $null = $gui.CloseMainWindow(); if (!$gui.WaitForExit(5000)) { $gui.Kill() } }
    }
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
