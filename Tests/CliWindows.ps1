param([Parameter(Mandatory=$true)][string]$Exe)
$ErrorActionPreference='Stop'
$work=Join-Path ([IO.Path]::GetTempPath()) ('wid-cli-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $work | Out-Null
try {
    foreach($test in @(@('--help',0),@('--product win11 --unknown --json',1),@('--product vcredist --list --json',0))) {
        $output=Join-Path $work 'out.txt';$errorFile=Join-Path $work 'err.txt'
        $p=Start-Process -FilePath $Exe -ArgumentList $test[0] -PassThru -RedirectStandardOutput $output -RedirectStandardError $errorFile
        if(!$p.WaitForExit(30000)){$p.Kill();throw 'CLI did not exit in 30 seconds.'}
        if($p.ExitCode -ne $test[1]){throw ('Unexpected CLI exit: '+$p.ExitCode+' / '+(Get-Content $errorFile -Raw))}
        $text=Get-Content $output -Raw -Encoding UTF8
        if([string]::IsNullOrWhiteSpace($text)){throw 'CLI produced no stdout.'}
        if($test[0] -like '*--json*'){$null=$text | ConvertFrom-Json}
    }
    Write-Host 'PASS: Windows headless CLI help, JSON, validation and process exit. No media downloaded.'
} finally {Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue}
