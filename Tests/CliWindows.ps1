param([Parameter(Mandatory=$true)][string]$Exe)
$ErrorActionPreference='Stop'
$work=Join-Path ([IO.Path]::GetTempPath()) ('wid-cli-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $work | Out-Null
try {
    foreach($test in @(@('--help',0),@('--product win11 --unknown --json',1),@('--product vcredist --list --json',0))) {
        $output=Join-Path $work 'out.txt';$errorFile=Join-Path $work 'err.txt'
        $start=New-Object Diagnostics.ProcessStartInfo
        $start.FileName=$Exe
        $start.Arguments=$test[0]
        $start.UseShellExecute=$false
        $start.CreateNoWindow=$true
        $start.RedirectStandardOutput=$true
        $start.RedirectStandardError=$true
        $start.StandardOutputEncoding=[Text.Encoding]::UTF8
        $start.StandardErrorEncoding=[Text.Encoding]::UTF8
        $p=New-Object Diagnostics.Process
        $p.StartInfo=$start
        if(!$p.Start()){throw 'CLI process could not start.'}
        $stdout=$p.StandardOutput.ReadToEndAsync()
        $stderr=$p.StandardError.ReadToEndAsync()
        if(!$p.WaitForExit(30000)){$p.Kill();throw 'CLI did not exit in 30 seconds.'}
        $p.WaitForExit()
        $text=$stdout.GetAwaiter().GetResult()
        $errorText=$stderr.GetAwaiter().GetResult()
        $code=$p.ExitCode
        $p.Dispose()
        if($code -ne $test[1]){throw ('Unexpected CLI exit: '+$code+' / '+$errorText)}
        if([string]::IsNullOrWhiteSpace($text)){throw 'CLI produced no stdout.'}
        if($test[0] -like '*--json*'){$null=$text | ConvertFrom-Json}
    }
    Write-Host 'PASS: Windows headless CLI help, JSON, validation and process exit. No media downloaded.'
} finally {Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue}
