$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '../WindowsISODownloader/Fido.ps1'
$tokens=$null; $parseErrors=$null
$ast=[System.Management.Automation.Language.Parser]::ParseFile($scriptPath,[ref]$tokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
foreach ($name in @('Select-Language','Assert-Download-Url','Process-Download-Link','Get-Windows-Languages','Get-Arch')) {
 $node=$ast.Find({param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name},$true)
 Invoke-Expression $node.Extent.Text
}
$checks=0
function Check($condition,$message) {if (!$condition) {throw $message}; $script:checks++}
$oldCulture=[Globalization.CultureInfo]::CurrentUICulture
try {
 foreach($test in @(@('bg-BG','Bulgarian',$true),@('ar-SA','Bulgarian',$false),@('ar-SA','Arabic',$true),@('cs-CZ','Czech',$true),@('tr-TR','Turkish',$true),@('en-US','English',$true),@('en-US','English International',$false),@('fr-CA','French',$false),@('fr-CA','French Canadian',$true),@('es-ES','Spanish',$true))) {
  [Globalization.CultureInfo]::CurrentUICulture=[Globalization.CultureInfo]::GetCultureInfo($test[0]); Check ((Select-Language $test[1]) -eq $test[2]) "Locale failed: $test"
 }
} finally {[Globalization.CultureInfo]::CurrentUICulture=$oldCulture}
foreach($url in @('https://software.download.prss.microsoft.com/dbazure/test.iso?token=x','https://download.microsoft.com/file.ISO','https://github.com/pbatard/UEFI-Shell/releases/download/v1/shell.iso')) {Check ($null -ne (Assert-Download-Url $url)) "Expected valid URL"}
foreach($url in @('http://download.microsoft.com/test.iso','https://evil-microsoft.com/test.iso','https://microsoft.com.evil.test/test.iso','https://user:pass@download.microsoft.com/test.iso','file:///tmp/test.iso','https://github.com/attacker/project/test.iso','https://download.microsoft.com/test.exe','https://download.microsoft.com:444/test.iso')) {
 $rejected=$false;try {$null=Assert-Download-Url $url} catch {$rejected=$true};Check $rejected "URL was not rejected: $url"
}
$ProductEditionId=@(1,2);$WindowsVersions=@(@(@('UEFI Shell','UEFI_SHELL 2.2')))
$rejected=$false;try {Get-Windows-Languages 0 0} catch {$rejected=$true};Check $rejected 'UEFI override must fail'
$ProductEditionId=@(-1);$WindowsVersions=@(@(@('Windows 11','windows11')))
$rejected=$false;try {Get-Windows-Languages 0 0} catch {$rejected=$true};Check $rejected 'Negative edition ID must fail'
$Cmd=$true;$PipeName=$null;$DefaultTimeout=1
function Error($Message) {$script:LastError=$Message}
function Start-BitsTransfer {param($Source,$Destination,$ErrorAction) throw 'BITS unavailable'}
function Invoke-WebRequest {param([switch]$UseBasicParsing,$TimeoutSec,$Uri,$Method,$OutFile,$ErrorAction) if ($Method -eq 'Head') {throw 'HEAD unsupported'}; [IO.File]::WriteAllText($OutFile,'mock ISO payload')}
$dir=Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N'));$null=New-Item -ItemType Directory $dir
Push-Location $dir
try {
 Check ((Process-Download-Link 'https://download.microsoft.com/test.iso') -eq 0) 'HEAD failure and BITS fallback must succeed'
 Check (Test-Path ./test.iso) 'Fallback output missing'
 Check ((Process-Download-Link 'https://download.microsoft.com/test.iso') -eq 404) 'Must reject overwrite'
 Remove-Item ./test.iso
 function Invoke-WebRequest {throw 'Network failure'}
 Check ((Process-Download-Link 'https://download.microsoft.com/test.iso') -eq 404) 'Failure must propagate'
 Check (@(Get-ChildItem -Force).Count -eq 0) 'Failed transfer must clean temporary file'
} finally {Pop-Location;Remove-Item -LiteralPath $dir -Recurse -Force}
$text=Get-Content -LiteralPath $scriptPath -Raw
Check (!$text.Contains('# SIG # Begin signature block')) 'Stale signature must not be retained'
Check (!$text.Contains('$languages.Length')) 'Language dictionary must use Count'
Check (!$text.Contains('$link.Arch -match')) 'Architecture must use exact match'
Check (!$text.Contains('MainWindowHandle, 0')) 'Console must not be hidden'
"PASS: $checks regression checks; no Microsoft network request or actual ISO download."
