$ErrorActionPreference = 'Stop'
$t=$null;$e=$null
$path=Join-Path $PSScriptRoot '../WindowsISODownloader/Kurulum.ps1'
$ast=[System.Management.Automation.Language.Parser]::ParseFile($path,[ref]$t,[ref]$e)
if ($e.Count) {throw ($e|Out-String)}
$node=$ast.Find({param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'HasSdk8'},$true)
Invoke-Expression $node.Extent.Text
$folder=Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N'))
$exe=Join-Path $folder 'fake-dotnet.ps1'
$env:WID_SDK_TEST_ROOT=Join-Path $folder 'sdk'
New-Item -ItemType Directory $folder | Out-Null
try {
 @'
$global:LASTEXITCODE=0
if($args[0] -eq '--list-sdks') {"8.0.425 [$env:WID_SDK_TEST_ROOT]"}
elseif ($args[0] -eq 'msbuild' -and $env:WID_SDK_TEST_FAIL -eq '1') {$global:LASTEXITCODE=1}
'@ | Set-Content -LiteralPath $exe -Encoding UTF8
 if (HasSdk8 $exe) {throw 'Missing SDK files were accepted'}
 $base=Join-Path $env:WID_SDK_TEST_ROOT '8.0.425'
 foreach($name in @('MSBuild.dll','Roslyn/bincore/csc.dll','Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.targets')) {
  $file=Join-Path $base $name
  New-Item -ItemType Directory -Force (Split-Path $file) | Out-Null
  [IO.File]::WriteAllText($file,'test stub')
 }
 if (!(HasSdk8 $exe)) {throw 'Healthy mock SDK rejected'}
 $env:WID_SDK_TEST_FAIL='1'
 if (HasSdk8 $exe) {throw 'Failed MSBuild probe accepted'}
 $env:WID_SDK_TEST_FAIL='0'
 [IO.File]::WriteAllText((Join-Path $base 'Roslyn/bincore/csc.dll'),'')
 if (HasSdk8 $exe) {throw 'Empty compiler accepted'}
 'PASS: 4 SDK-health checks (mock tool/files; no SDK installation).'
} finally {
 Remove-Item -LiteralPath $folder -Recurse -Force
 Remove-Item Env:WID_SDK_TEST_ROOT -ErrorAction SilentlyContinue
 Remove-Item Env:WID_SDK_TEST_FAIL -ErrorAction SilentlyContinue
}
