#
# Fido v1.70-WID.2 - ISO Downloader, for Microsoft Windows and UEFI Shell
# Modified for Windows ISO Downloader 3.4.6 on 2026-10-13. This local fork is unsigned.
# Original copyright and GPL terms below remain in force.
# Copyright © 2019-2026 Pete Batard <pete@akeo.ie>
# Command line support: Copyright © 2021 flx5
# ConvertTo-ImageSource: Copyright © 2016 Chris Carter
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <http://www.gnu.org/licenses/>.
#

# NB: You must have a BOM on your .ps1 if you want Powershell to actually
# realise it should use Unicode for the UI rather than ISO-8859-1.

#region Parameters
param(
	# (Optional) The title to display on the application window.
	[string]$AppTitle = "Fido - ISO Downloader",
	# (Optional) '|' separated UI localization strings.
	[string]$LocData,
	# (Optional) Forced locale
	[string]$Locale = "en-US",
	# (Optional) Path to a file that should be used for the UI icon.
	[string]$Icon,
	# (Optional) Name of a pipe the download URL should be sent to.
	# If not provided, a browser window is opened instead.
	[string]$PipeName,
	# (Optional) Specify Windows version (e.g. "Windows 10") [Toggles commandline mode]
	[string]$Win,
	# (Optional) Specify Windows release (e.g. "21H1") [Toggles commandline mode]
	[string]$Rel,
	# (Optional) Specify Windows edition (e.g. "Pro") [Toggles commandline mode]
	[string]$Ed,
	# Override with the product edition ID currently published on Microsoft's download page.
	[int[]]$ProductEditionId = @(),
	# (Optional) Specify Windows language [Toggles commandline mode]
	[string]$Lang,
	# (Optional) Specify Windows architecture [Toggles commandline mode]
	[string]$Arch,
	# (Optional) Only display the download URL [Toggles commandline mode]
	[switch]$GetUrl = $false,
	# (Optional) Specify the architecture of the underlying CPU.
	# This avoids a VERY TIME CONSUMING call to WMI to autodetect the arch.
	[string]$PlatformArch,
	# (Optional) Increase verbosity
	[switch]$Verbose = $false,
	# (Optional) Produce debugging information
	[switch]$Debug = $false
)
#endregion

try {
	[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
} catch {}

$Cmd = $false
if ($Win -or $Rel -or $Ed -or $Lang -or $Arch -or $GetUrl -or $ProductEditionId.Count -gt 0) {
	$Cmd = $true
}

# Return a System.Version value for platform support comparisons.
# Note that because we don't want to have to support this script on anything
# other than Windows, this call returns version 0.0 on Linux/Mac.
function Get-Platform-Version()
{
	$version = [version]"0.0"
	$platform = [string][System.Environment]::OSVersion.Platform
	# This will filter out non Windows platforms
	if ($platform.StartsWith("Win")) {
		# Preserve major, minor and build components without floating-point arithmetic.
		$version = [System.Environment]::OSVersion.Version
	}
	return $version
}

$winver = Get-Platform-Version

# The default TLS for Windows 8.x doesn't work with Microsoft's servers so we must force it
if ($winver -lt [version]"10.0") {
	[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
}

#region Assembly Types
$Drawing_Assembly = "System.Drawing"
# PowerShell 7 altered the name of the Drawing assembly...
if ($host.version -ge "7.0") {
	$Drawing_Assembly += ".Common"
}

$Signature = @{
	Namespace            = "WinAPI"
	Name                 = "Utils"
	Language             = "CSharp"
	UsingNamespace       = "System.Runtime", "System.IO", "System.Text", "System.Drawing", "System.Globalization"
	ReferencedAssemblies = $Drawing_Assembly
	ErrorAction          = "Stop"
	WarningAction        = "Ignore"
	IgnoreWarnings       = $true
	MemberDefinition     = @"
		[DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
		internal static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);

		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr handle, int state);
		// Extract an icon from a DLL
		public static Icon ExtractIcon(string file, int number, bool largeIcon) {
			IntPtr large, small;
			ExtractIconEx(file, number, out large, out small, 1);
			try {
				return Icon.FromHandle(largeIcon ? large : small);
			} catch {
				return null;
			}
		}
"@
}

if (!$Cmd) {
	Write-Host Please Wait...

	if (!("WinAPI.Utils" -as [type]))
	{
		Add-Type @Signature
	}
	Add-Type -AssemblyName PresentationFramework

	# Keep the console visible: never hide a shared Windows Terminal window.
}
#endregion

#region Data
$WindowsVersions = @(
	@(
		@("Windows 11", "windows11"),
		@(
			"25H2 v2 (Build 26200.8037 - 2026.03)",
			# Thanks to Microsoft's hare-brained decision not to treat ARM64 as a CPU arch,
			# like they did for x86 and x64, we have to handle multiple IDs for each release...
			@("Windows 11 Home/Pro/Edu", @(3321, 3324)),
			@("Windows 11 Home China ", @(3322, 3325)),
			@("Windows 11 Pro China ", @(3323, 3326))
		)
	),
	@(
		@("Windows 10", "Windows10ISO"),
		@(
			"22H2 v1 (Build 19045.2965 - 2023.05)",
			@("Windows 10 Home/Pro/Edu", 2618),
			@("Windows 10 Home China ", 2378)
		)
	)
	@(
		@("UEFI Shell 2.2", "UEFI_SHELL 2.2"),
		@(
			"26H1 (edk2-stable202602)",
			@("Release", 0),
			@("Debug", 1)
		),
		@(
			"25H2 (edk2-stable202511)",
			@("Release", 0),
			@("Debug", 1)
		),
		@(
			"25H1 (edk2-stable202505)",
			@("Release", 0),
			@("Debug", 1)
		),
		@(
			"24H2 (edk2-stable202411)",
			@("Release", 0),
			@("Debug", 1)
		),
		@(
			"24H1 (edk2-stable202405)",
			@("Release", 0),
			@("Debug", 1)
		),
		@(
			"23H2 (edk2-stable202311)",
			@("Release", 0),
			@("Debug", 1)
		),
		@(
			"23H1 (edk2-stable202305)",
			@("Release", 0),
			@("Debug", 1)
		),
		@(
			"22H2 (edk2-stable202211)",
			@("Release", 0),
			@("Debug", 1)
		),
		@(
			"22H1 (edk2-stable202205)",
			@("Release", 0),
			@("Debug", 1)
		),
		@(
			"21H2 (edk2-stable202108)",
			@("Release", 0),
			@("Debug", 1)
		),
		@(
			"21H1 (edk2-stable202105)",
			@("Release", 0),
			@("Debug", 1)
		),
		@(
			"20H2 (edk2-stable202011)",
			@("Release", 0),
			@("Debug", 1)
		)
	),
	@(
		@("UEFI Shell 2.0", "UEFI_SHELL 2.0"),
		@(
			"4.632 [20100426]",
			@("Release", 0)
		)
	)
)
#endregion

#region Functions
function Select-Language([string]$LangName)
{
	# Use the system locale to try select the most appropriate language
	[string]$SysLocale = [System.Globalization.CultureInfo]::CurrentUICulture.Name
	if (($SysLocale.StartsWith("ar") -and $LangName -like "*Arabic*") -or `
		($SysLocale -eq "pt-BR" -and $LangName -like "*Brazil*") -or `
		($SysLocale.StartsWith("bg") -and $LangName -like "*Bulgar*") -or `
		($SysLocale -eq "zh-CN" -and $LangName -like "*Chinese*" -and $LangName -like "*simp*") -or `
		($SysLocale -eq "zh-TW" -and $LangName -like "*Chinese*" -and $LangName -like "*trad*") -or `
		($SysLocale.StartsWith("hr") -and $LangName -like "*Croat*") -or `
		($SysLocale.StartsWith("cs") -and $LangName -like "*Czech*") -or `
		($SysLocale.StartsWith("da") -and $LangName -like "*Danish*") -or `
		($SysLocale.StartsWith("nl") -and $LangName -like "*Dutch*") -or `
		($SysLocale -eq "en-US" -and $LangName -eq "English") -or `
		($SysLocale.StartsWith("en") -and $SysLocale -ne "en-US" -and $LangName -like "*English*" -and ($LangName -like "*inter*" -or $LangName -like "*ingdom*")) -or `
		($SysLocale.StartsWith("et") -and $LangName -like "*Eston*") -or `
		($SysLocale.StartsWith("fi") -and $LangName -like "*Finn*") -or `
		($SysLocale -eq "fr-CA" -and $LangName -like "*French*" -and $LangName -like "*Canad*") -or `
		($SysLocale.StartsWith("fr") -and $SysLocale -ne "fr-CA" -and $LangName -eq "French") -or `
		($SysLocale.StartsWith("de") -and $LangName -like "*German*") -or `
		($SysLocale.StartsWith("el") -and $LangName -like "*Greek*") -or `
		($SysLocale.StartsWith("he") -and $LangName -like "*Hebrew*") -or `
		($SysLocale.StartsWith("hu") -and $LangName -like "*Hungar*") -or `
		($SysLocale.StartsWith("id") -and $LangName -like "*Indones*") -or `
		($SysLocale.StartsWith("it") -and $LangName -like "*Italia*") -or `
		($SysLocale.StartsWith("ja") -and $LangName -like "*Japan*") -or `
		($SysLocale.StartsWith("ko") -and $LangName -like "*Korea*") -or `
		($SysLocale.StartsWith("lv") -and $LangName -like "*Latvia*") -or `
		($SysLocale.StartsWith("lt") -and $LangName -like "*Lithuania*") -or `
		($SysLocale.StartsWith("ms") -and $LangName -like "*Malay*") -or `
		($SysLocale.StartsWith("nb") -and $LangName -like "*Norw*") -or `
		($SysLocale.StartsWith("fa") -and $LangName -like "*Persia*") -or `
		($SysLocale.StartsWith("pl") -and $LangName -like "*Polish*") -or `
		($SysLocale -eq "pt-PT" -and $LangName -eq "Portuguese") -or `
		($SysLocale.StartsWith("ro") -and $LangName -like "*Romania*") -or `
		($SysLocale.StartsWith("ru") -and $LangName -like "*Russia*") -or `
		($SysLocale.StartsWith("sr") -and $LangName -like "*Serbia*") -or `
		($SysLocale.StartsWith("sk") -and $LangName -like "*Slovak*") -or `
		($SysLocale.StartsWith("sl") -and $LangName -like "*Slovenia*") -or `
		($SysLocale -eq "es-ES" -and $LangName -eq "Spanish") -or `
		($SysLocale.StartsWith("es") -and $SysLocale -ne "es-ES" -and $LangName -like "*Spanish*") -or `
		($SysLocale.StartsWith("sv") -and $LangName -like "*Swed*") -or `
		($SysLocale.StartsWith("th") -and $LangName -like "*Thai*") -or `
		($SysLocale.StartsWith("tr") -and $LangName -like "*Turk*") -or `
		($SysLocale.StartsWith("uk") -and $LangName -like "*Ukrain*") -or `
		($SysLocale.StartsWith("vi") -and $LangName -like "*Vietnam*")) {
		return $true
	}
	return $false
}

function Add-Entry([int]$pos, [string]$Name, [array]$Items, [string]$DisplayName)
{
	$Title = New-Object System.Windows.Controls.TextBlock
	$Title.FontSize = $WindowsVersionTitle.FontSize
	$Title.Height = $WindowsVersionTitle.Height;
	$Title.Width = $WindowsVersionTitle.Width;
	$Title.HorizontalAlignment = "Left"
	$Title.VerticalAlignment = "Top"
	$Margin = $WindowsVersionTitle.Margin
	$Margin.Top += $pos * $dh
	$Title.Margin = $Margin
	$Title.Text = Get-Translation($Name)
	$XMLGrid.Children.Insert(2 * $Stage + 2, $Title)

	$Combo = New-Object System.Windows.Controls.ComboBox
	$Combo.FontSize = $WindowsVersion.FontSize
	$Combo.Height = $WindowsVersion.Height;
	$Combo.Width = $WindowsVersion.Width;
	$Combo.HorizontalAlignment = "Left"
	$Combo.VerticalAlignment = "Top"
	$Margin = $WindowsVersion.Margin
	$Margin.Top += $pos * $script:dh
	$Combo.Margin = $Margin
	$Combo.SelectedIndex = 0
	if ($Items) {
		$Combo.ItemsSource = $Items
		if ($DisplayName) {
			$Combo.DisplayMemberPath = $DisplayName
		} else {
			$Combo.DisplayMemberPath = $Name
		}
	}
	$XMLGrid.Children.Insert(2 * $Stage + 3, $Combo)

	$XMLForm.Height += $dh;
	$Margin = $Continue.Margin
	$Margin.Top += $dh
	$Continue.Margin = $Margin
	$Margin = $Back.Margin
	$Margin.Top += $dh
	$Back.Margin = $Margin

	return $Combo
}

function Refresh-Control([object]$Control)
{
	$Control.Dispatcher.Invoke("Render", [Windows.Input.InputEventHandler] { $Continue.UpdateLayout() }, $null, $null) | Out-Null
}

function Send-Message([string]$PipeName, [string]$Message)
{
    $Pipe = New-Object -TypeName System.IO.Pipes.NamedPipeClientStream -ArgumentList ".", $PipeName, ([System.IO.Pipes.PipeDirection]::Out), ([System.IO.Pipes.PipeOptions]::None), ([System.Security.Principal.TokenImpersonationLevel]::Impersonation)
    try {
        $Pipe.Connect(1000)
        $request = [System.Text.Encoding]::UTF8.GetBytes($Message)
        $Pipe.Write($request, 0, $request.Length)
    } finally { $Pipe.Dispose() }
}

# From https://www.powershellgallery.com/packages/IconForGUI/1.5.2
# Copyright © 2016 Chris Carter. All rights reserved.
# License: https://creativecommons.org/licenses/by-sa/4.0/
function ConvertTo-ImageSource
{
	[CmdletBinding()]
	Param(
		[Parameter(Mandatory=$true,ValueFromPipeline=$true,ValueFromPipelineByPropertyName=$true)]
		[System.Drawing.Icon]$Icon
	)

	Process {
		foreach ($i in $Icon) {
			[System.Windows.Interop.Imaging]::CreateBitmapSourceFromHIcon(
				$i.Handle,
				(New-Object System.Windows.Int32Rect -Args 0,0,$i.Width, $i.Height),
				[System.Windows.Media.Imaging.BitmapSizeOptions]::FromEmptyOptions()
			)
		}
	}
}

# Translate a message string
function Get-Translation([string]$Text)
{
	if (!($English -contains $Text)) {
		Write-Host "Error: '$Text' is not a translatable string"
		return "(Untranslated)"
	}
	if ($Localized) {
		if ($Localized.Length -ne $English.Length) {
			Write-Host "Error: '$Text' is not a translatable string"
		}
		for ($i = 0; $i -lt $English.Length; $i++) {
			if ($English[$i] -eq $Text) {
				if ($Localized[$i]) {
					return $Localized[$i]
				} else {
					return $Text
				}
			}
		}
	}
	return $Text
}

# Get the underlying *native* CPU architecture
function Get-Arch
{
    $nativeArch = $env:PROCESSOR_ARCHITEW6432
    if (!$nativeArch) { $nativeArch = $env:PROCESSOR_ARCHITECTURE }
    switch ($nativeArch) {
        "AMD64" { return "x64" }
        "ARM64" { return "ARM64" }
        "x86" { return "x86" }
        "ARM" { return "ARM32" }
    }
	$Arch = Get-CimInstance -ClassName Win32_Processor | Select-Object -ExpandProperty Architecture
	switch($Arch) {
	0  { return "x86" }
	1  { return "MIPS" }
	2  { return "Alpha" }
	3  { return "PowerPC" }
	5  { return "ARM32" }
	6  { return "IA64" }
	9  { return "x64" }
	12 { return "ARM64" }
	default { return "Unknown"}
	}
}

# Convert a Microsoft arch type code to a formal architecture name
function Get-Arch-From-Type([int]$Type)
{
	switch($Type) {
	0 { return "x86" }
	1 { return "x64" }
	2 { return "ARM64" }
	default { return "Unknown"}
	}
}

function Error([string]$ErrorMessage)
{
	Write-Host Error: $ErrorMessage
	if (!$Cmd) {
		$XMLForm.Title = $(Get-Translation("Error")) + ": " + $ErrorMessage
		Refresh-Control($XMLForm)
		$XMLGrid.Children[2 * $script:Stage + 1].IsEnabled = $true
		$UserInput = [System.Windows.MessageBox]::Show($XMLForm.Title,  $(Get-Translation("Error")), "OK", "Error")
		$script:ExitCode = $script:Stage--
	} else {
		$script:ExitCode = 2
	}
}
#endregion

#region Form
[xml]$XAML = @"
<Window xmlns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation" Height = "162" Width = "384" ResizeMode = "NoResize">
	<Grid Name = "XMLGrid">
		<Button Name = "Continue" FontSize = "16" Height = "26" Width = "160" HorizontalAlignment = "Left" VerticalAlignment = "Top" Margin = "14,78,0,0"/>
		<Button Name = "Back" FontSize = "16" Height = "26" Width = "160" HorizontalAlignment = "Left" VerticalAlignment = "Top" Margin = "194,78,0,0"/>
		<TextBlock Name = "WindowsVersionTitle" FontSize = "16" Width="340" HorizontalAlignment="Left" VerticalAlignment="Top" Margin="16,8,0,0"/>
		<ComboBox Name = "WindowsVersion" FontSize = "14" Height = "24" Width = "340" HorizontalAlignment = "Left" VerticalAlignment="Top" Margin = "14,34,0,0" SelectedIndex = "0"/>
		<CheckBox Name = "Check" FontSize = "14" Width = "340" HorizontalAlignment = "Left" VerticalAlignment="Top" Margin = "14,0,0,0" Visibility="Collapsed" />
	</Grid>
</Window>
"@
#endregion

#region Globals
$ErrorActionPreference = "Stop"
$DefaultTimeout = 30
$dh = 58
$Stage = 0
$SelectedIndex = 0
$ltrm = "‎"
if ($Cmd) {
	$ltrm = ""
}
# Can't reuse the same sessionId for x64 and ARM64. The Microsoft servers
# are purposefully designed to ever process one specific download request
# that matches the last SKUs retrieved.
$SessionId = @($null) * 2
$ExitCode = 100
$OrgId = "y6jn8c31"
$ProfileId = "606624d44113"
$InstanceId = "560dc9f3-1aa5-4a2f-b63c-9e18f8d0e175"
$Verbosity = 1
if ($Debug) {
	$Verbosity = 5
} elseif ($Verbose) {
	$Verbosity = 2
} elseif ($Cmd -and $GetUrl) {
	$Verbosity = 0
}
if (!$PlatformArch) {
	$PlatformArch = Get-Arch
}
#endregion

# Localization
$EnglishMessages = "en-US|Version|Release|Edition|Language|Architecture|Download|Continue|Back|Close|Cancel|Error|Please wait...|" +
	"Download using a browser|Download of Windows ISOs is unavailable due to Microsoft having altered their website to prevent it.|" +
	"PowerShell 3.0 or later is required to run this script.|Do you want to go online and download it?|" +
	"This feature is not available on this platform."
[string[]]$English = $EnglishMessages.Split('|')
[string[]]$Localized = $null
if ($LocData -and !$LocData.StartsWith("en-US")) {
	$Localized = $LocData.Split('|')
	# Adjust the $Localized array if we have more or fewer strings than in $EnglishMessages
	if ($Localized.Length -lt $English.Length) {
		while ($Localized.Length -ne $English.Length) {
			$Localized += $English[$Localized.Length]
		}
	} elseif ($Localized.Length -gt $English.Length) {
		$Localized = $LocData.Split('|')[0..($English.Length - 1)]
	}
	$Locale = $Localized[0]
}
$QueryLocale = $Locale

# Convert a size in bytes to a human readable string
function Size-To-Human-Readable([uint64]$size)
{
	$suffix = "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"
	$i = 0
	while ($size -gt 1kb) {
		$size = $size / 1kb
		$i++
	}
	"{0:N1} {1}" -f $size, $suffix[$i]
}

# Check if the locale we want is available - Fall back to en-US otherwise
function Check-Locale
{
	try {
		$url = "https://www.microsoft.com/" + $QueryLocale + "/software-download/windows11"
		if ($Verbosity -ge 2) {
			Write-Host Querying $url
		}
		Invoke-WebRequest -UseBasicParsing -TimeoutSec $DefaultTimeout -MaximumRedirection 0 $url | Out-Null
	} catch {
		# Of course PowerShell 7 had to BREAK $_.Exception.Status on timeouts...
		if ($_.Exception.Status -eq "Timeout" -or $_.Exception.GetType().Name -eq "TaskCanceledException") {
			Write-Host Operation Timed out
		}
		$script:QueryLocale = "en-US"
	}
}

function Get-Code-715-123130-Message
{
	try {
		$url = "https://www.microsoft.com/" + $QueryLocale + "/software-download/windows11"
		if ($Verbosity -ge 2) {
			Write-Host Querying $url
		}
		$r = Invoke-WebRequest -UseBasicParsing -TimeoutSec $DefaultTimeout -MaximumRedirection 0 $url
		# Microsoft's handling of UTF-8 content is soooooooo *UTTERLY BROKEN*!!!
		$r = [System.Text.Encoding]::UTF8.GetString($r.RawContentStream.ToArray())
		# PowerShell 7 forces us to parse the HTML ourselves
		$r = $r -replace "`n" -replace "`r"
		$pattern = '.*<input id="msg-01" type="hidden" value="(.*?)"/>.*'
		$msg = [regex]::Match($r, $pattern).Groups[1].Value
		$msg = $msg -replace "&lt;", "<" -replace "<[^>]+>" -replace "\s+", " "
		if (($msg -eq $null) -or !($msg -match "715-123130")) {
			throw
		}
	} catch {
		$msg  = "Your IP address has been banned by Microsoft for issuing too many ISO download requests or for "
		$msg += "belonging to a region of the world where sanctions currently apply. Please try again later.`r`n"
		$msg += "If you believe this ban to be in error, you can try contacting Microsoft by referring to "
		$msg += "message code 715-123130 and session ID "
	}
	return $msg
}

# Return an array of releases (e.g. 20H2, 21H1, ...) for the selected Windows version
function Get-Windows-Releases([int]$SelectedVersion)
{
	$i = 0
	$releases = @()
	foreach ($version in $WindowsVersions[$SelectedVersion]) {
		if (($i -ne 0) -and ($version -is [array])) {
			$releases += @(New-Object PsObject -Property @{ Release = $ltrm + $version[0].Replace(")", ")" + $ltrm); Index = $i })
		}
		$i++
	}
	return $releases
}

# Return an array of editions (e.g. Home, Pro, etc) for the selected Windows release
function Get-Windows-Editions([int]$SelectedVersion, [int]$SelectedRelease)
{
	$editions = @()
	foreach ($release in $WindowsVersions[$SelectedVersion][$SelectedRelease])
	{
		if ($release -is [array]) {
			if (!($release[0].Contains("China")) -or ($Locale.StartsWith("zh"))) {
				$editions += @(New-Object PsObject -Property @{ Edition = $release[0]; Id = $release[1] })
			}
		}
	}
	return $editions
}

# Return an array of languages for the selected edition
function Get-Windows-Languages([int]$SelectedVersion, [object]$SelectedEdition)
{
    if ($ProductEditionId.Count -gt 0) {
        if ($WindowsVersions[$SelectedVersion][0][1].StartsWith("UEFI_SHELL")) {
            throw "ProductEditionId is only supported for Microsoft Windows products."
        }
        if (@($ProductEditionId | Where-Object { $_ -le 0 }).Count -gt 0) {
            throw "ProductEditionId values must be positive integers."
        }
        $SelectedEdition = @($ProductEditionId | Select-Object -Unique)
    }
	$langs = @()
	if ($WindowsVersions[$SelectedVersion][0][1].StartsWith("UEFI_SHELL")) {
		$langs += @(New-Object PsObject -Property @{ DisplayName = "English (US)"; Name = "en-us"; Data = @($null) })
	} else {
		$languages = [ordered]@{}
        $script:SessionId = @($null) * @($SelectedEdition).Count
		$SessionIndex = 0
		foreach ($EditionId in $SelectedEdition) {
			$SessionId[$SessionIndex] = [guid]::NewGuid()

			# Microsoft download "protection" requires the sessionId to be whitelisted through vlscppe.microsoft.com/tags
			$url = "https://vlscppe.microsoft.com/tags"
			$url += "?org_id=" + $OrgId
			$url += "&session_id=" + $SessionId[$SessionIndex]
			if ($Verbosity -ge 2) {
				Write-Host Querying $url
			}
			try {
				Invoke-WebRequest -UseBasicParsing -TimeoutSec $DefaultTimeout -MaximumRedirection 0 $url | Out-Null
			} catch {
				Error($_.Exception.Message)
				return @()
			}

			# Microsoft download "protection" also requires an ov-df.microsoft.com request/reply
			# 1) Request mdt.js to get w and rticks. InstanceId is (currently) constant.
			$url = "https://ov-df.microsoft.com/mdt.js"
			$url += "?instanceId=" + $InstanceId
			$url += "&PageId=si"
			$url += "&session_id=" + $SessionId[$SessionIndex]
			if ($Verbosity -ge 2) {
				Write-Host Querying $url
			}
			try {
				$r = Invoke-RestMethod -UseBasicParsing -TimeoutSec $DefaultTimeout -MaximumRedirection 0 $url
				if ($r -eq $null) {
					throw "Could not retrieve ov-df data"
				}
				# Extract w and rticks parameters
				if ($r -match '[?&]w=([A-F0-9]+)') {
					$w = $matches[1]
				}
				if ($r -match 'rticks\=\"\+?(\d+)') {
					$rticks = $matches[1]
				}
				if (!$w -or !$rticks) {
					throw "Could not extract ov-df data"
				}
			} catch {
				Error($_.Exception.Message)
				return @()
			}
			# 2) Send a reply with session ID, current epoch and previously retrieved w and rticks
			$url = "https://ov-df.microsoft.com/"
			$url += "?session_id=" + $SessionId[$SessionIndex]
			$url += "&CustomerId=" + $InstanceId
			$url += "&PageId=si"
			$url += "&w=" + $w
			$url += "&mdt=" + [DateTimeOffset]::Now.ToUnixTimeMilliSeconds()
			$url += "&rticks=" + $rticks
			if ($Verbosity -ge 2) {
				Write-Host Querying $url
			}
			try {
				Invoke-WebRequest -UseBasicParsing -TimeoutSec $DefaultTimeout -MaximumRedirection 0 $url | Out-Null
			} catch {
				Error($_.Exception.Message)
				return @()
			}

			$url = "https://www.microsoft.com/software-download-connector/api/getskuinformationbyproductedition"
			$url += "?profile=" + $ProfileId
			$url += "&productEditionId=" + $EditionId
			$url += "&SKU=undefined"
			$url += "&friendlyFileName=undefined"
			$url += "&Locale=" + $QueryLocale
			$url += "&sessionID=" + $SessionId[$SessionIndex]
			if ($Verbosity -ge 2) {
				Write-Host Querying $url
			}
			try {
				# May require a few attempts.
				$attempt = 0;
				while ($true) {
					if ($attempt -ne 0) { Start-Sleep -Seconds 2 }
					$r = Invoke-RestMethod -UseBasicParsing -TimeoutSec $DefaultTimeout -SessionVariable "Session" $url
					if ($r -eq $null) {
						if ($attempt++ -le 1) { continue }
						throw "Could not retrieve languages from server"
					}
					if ($Verbosity -ge 5) {
						Write-Host "=============================================================================="
						Write-Host ($r | ConvertTo-Json)
						Write-Host "=============================================================================="
					}
					if ($r.Errors) {
						if ($attempt++ -le 1) { continue }
						throw $r.Errors[0].Value
					}
					foreach ($Sku in $r.Skus) {
						if (!$languages.Contains($Sku.Language)) {
							$languages[$Sku.Language] = @{ DisplayName = $Sku.LocalizedLanguage; Data = @() }
						}
						$languages[$Sku.Language].Data += @{ SessionIndex = $SessionIndex; SkuId = $Sku.Id }
					}
					if ($languages.Count -eq 0) {
						if ($attempt++ -le 1) { continue }
						throw "Could not parse languages"
					}
					break;
				}
			} catch {
				Error($_.Exception.Message)
				return @()
			}
			$SessionIndex++
		}
		# Need to convert to an array since PowerShell treats them differently from hashtable
		$i = 0
		$script:SelectedIndex = 0
		$languageMatched = $false
		foreach($language in $languages.Keys) {
			$langs += @(New-Object PsObject -Property @{ DisplayName = $languages[$language].DisplayName; Name = $language; Data = $languages[$language].Data })
			if (!$languageMatched -and (Select-Language($language))) {
				$languageMatched = $true
				$script:SelectedIndex = $i
			}
			$i++
		}
	}
	return $langs
}

# Return an array of download links for each supported arch
function Get-Windows-Download-Links([int]$SelectedVersion, [int]$SelectedRelease, [object]$SelectedEdition, [PSCustomObject]$SelectedLanguage)
{
	$links = @()
	if ($WindowsVersions[$SelectedVersion][0][1].StartsWith("UEFI_SHELL")) {
		$tag = $WindowsVersions[$SelectedVersion][$SelectedRelease][0].Split(' ')[0]
		$shell_version = $WindowsVersions[$SelectedVersion][0][1].Split(' ')[1]
		$url = "https://github.com/pbatard/UEFI-Shell/releases/download/" + $tag
		$link = $url + "/UEFI-Shell-" + $shell_version + "-" + $tag
		if ($SelectedEdition -eq 0) {
			$link += "-RELEASE.iso"
		} else {
			$link += "-DEBUG.iso"
		}
		try {
			# Read the supported archs from the release URL
			$url += "/Version.xml"
			$xml = New-Object System.Xml.XmlDocument
			if ($Verbosity -ge 2) {
				Write-Host Querying $url
			}
			$xml.Load($url)
			$sep = ""
			$archs = ""
			foreach($arch in $xml.release.supported_archs.arch) {
				$archs += $sep + $arch
				$sep = ", "
			}
			$links += @(New-Object PsObject -Property @{ Arch = $archs; Url = $link })
		} catch {
			Error($_.Exception.Message)
			return @()
		}
	} else {
		foreach ($Entry in $SelectedLanguage.Data) {
			$url = "https://www.microsoft.com/software-download-connector/api/GetProductDownloadLinksBySku"
			$url += "?profile=" + $ProfileId
			$url += "&productEditionId=undefined"
			$url += "&SKU=" + $Entry.SkuId
			$url += "&friendlyFileName=undefined"
			$url += "&Locale=" + $QueryLocale
			$url += "&sessionID=" + $SessionId[$Entry.SessionIndex]
			if ($Verbosity -ge 2) {
				Write-Host Querying $url
			}
			try {
				# Must add a referer for this request, else Microsoft's servers may deny it
				$ref = "https://www.microsoft.com/software-download/windows11"
				$r = Invoke-RestMethod -Headers @{ "Referer" = $ref } -UseBasicParsing -TimeoutSec $DefaultTimeout -SessionVariable "Session" $url
				if ($r -eq $null) {
					throw "Could not retrieve architectures from server"
				}
				if ($Verbosity -ge 5) {
					Write-Host "=============================================================================="
					Write-Host ($r | ConvertTo-Json)
					Write-Host "=============================================================================="
				}
				if ($r.Errors) {
					if ( $r.Errors[0].Type -eq 9) {
						$msg = Get-Code-715-123130-Message
						throw $msg + $SessionId[$Entry.SessionIndex] + "."
					} else {
						throw $r.Errors[0].Value
					}
				}
				foreach ($ProductDownloadOption in $r.ProductDownloadOptions) {
					$links += @(New-Object PsObject -Property @{ Arch = (Get-Arch-From-Type $ProductDownloadOption.DownloadType); Url = $ProductDownloadOption.Uri })
				}
				if ($links.Length -eq 0) {
					throw "Could not retrieve ISO download links"
				}
			} catch {
				Error($_.Exception.Message)
				return @()
			}
		}
		$i = 0
		$script:SelectedIndex = 0
		foreach($link in $links) {
			if ($link.Arch -eq $PlatformArch) {
				$script:SelectedIndex = $i
			}
			$i++
		}
	}
	return $links
}

function Assert-Download-Url([string]$Url)
{
    $uri = $null
    if (![Uri]::TryCreate($Url, [UriKind]::Absolute, [ref]$uri) -or
        $uri.Scheme -ne "https" -or $uri.UserInfo -or $uri.Port -ne 443) {
        throw "Invalid download URL: HTTPS without credentials is required."
    }
    $isMicrosoft = $uri.DnsSafeHost -eq "microsoft.com" -or $uri.DnsSafeHost.EndsWith(".microsoft.com", [StringComparison]::OrdinalIgnoreCase)
    $isShell = $uri.DnsSafeHost -eq "github.com" -and $uri.AbsolutePath.StartsWith("/pbatard/UEFI-Shell/releases/download/", [StringComparison]::Ordinal)
    if ((!$isMicrosoft -and !$isShell) -or !$uri.AbsolutePath.EndsWith(".iso", [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unexpected ISO download host or path."
    }
    return $uri
}

# Process the download URL by either sending it through the pipe or by opening the browser
function Process-Download-Link([string]$Url)
{
	try {
        $uri = Assert-Download-Url $Url
		if ($PipeName -and !$Check.IsChecked) {
			Send-Message -PipeName $PipeName -Message $Url
		} else {
			if ($Cmd) {
                $File = [System.IO.Path]::GetFileName($uri.AbsolutePath)
                if (!$File -or $File.IndexOfAny([System.IO.Path]::GetInvalidFileNameChars()) -ge 0) { throw "Invalid ISO filename." }
                if (Test-Path -LiteralPath $File) { throw "Destination already exists: $File" }
                $size = $null
                try {
                    $header = Invoke-WebRequest -UseBasicParsing -TimeoutSec $DefaultTimeout -Uri $Url -Method Head
                    $length = 0L
                    if ([long]::TryParse([string]$header.Headers.'Content-Length', [ref]$length) -and $length -gt 0) { $size = $length }
                } catch { Write-Verbose "Server does not provide a usable HEAD response; continuing download." }
                if ($size -and !([System.IO.Path]::GetFullPath($File).StartsWith("\\"))) {
                    $drive = New-Object System.IO.DriveInfo ([System.IO.Path]::GetPathRoot([System.IO.Path]::GetFullPath($File)))
                    if ($drive.AvailableFreeSpace -lt $size) { throw "Not enough free disk space." }
                }
                Write-Host "Downloading '$File'..."
                $temporary = [System.IO.Path]::GetFullPath($File) + "." + [guid]::NewGuid().ToString("N") + ".part"
                try {
                    try { Start-BitsTransfer -Source $Url -Destination $temporary -ErrorAction Stop }
                    catch { Invoke-WebRequest -UseBasicParsing -Uri $Url -OutFile $temporary -ErrorAction Stop }
                    if ($size -and (Get-Item -LiteralPath $temporary).Length -ne $size) { throw "Incomplete ISO download." }
                    Move-Item -LiteralPath $temporary -Destination $File -ErrorAction Stop
                } finally { if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force } }

			} else {
				Write-Host Download Link: $Url
				Start-Process -FilePath $Url
			}
		}
	} catch {
		Error($_.Exception.Message)
		return 404
	}
	return 0
}

if ($Cmd) {
    if (!$Win -and $ProductEditionId.Count -gt 0) { $Win = "Windows 11" }
    if ($Win -in @("7", "8", "8.1", "10", "11")) { $Win = "Windows " + $Win }
	$winVersionId = $null
	$winReleaseId = $null
	$winEditionId = $null
	$winLanguageId = $null
	$winLanguageName = $null
	$winLink = $null

	# Windows 7 and non Windows platforms are too much of a liability
	if ($winver -le [version]"6.1") {
		Error(Get-Translation("This feature is not available on this platform."))
		exit 403
	}

	$i = 0
	$Selected = ""
	if ($Win -eq "List") {
		Write-Host "Please select a Windows Version (-Win):"
	}
	foreach($version in $WindowsVersions) {
		if ($Win -eq "List") {
			Write-Host " -" $version[0][0]
		} elseif ($version[0][0] -eq $Win) {
			$Selected += $version[0][0]
			$winVersionId = $i
			break;
		}
		$i++
	}
	if ($winVersionId -eq $null) {
		if ($Win -ne "List") {
			Write-Host "Invalid Windows version provided."
			Write-Host "Use '-Win List' for a list of available Windows versions."
		}
		exit 1
	}

	# Windows Version selection
	$releases = Get-Windows-Releases $winVersionId
	if ($Rel -eq "List") {
		Write-Host "Please select a Windows Release (-Rel) for ${Selected} (or use 'Latest' for most recent):"
	}
    if ($Rel -and $Rel -notin @("Latest", "List")) {
        $matches = @($releases | Where-Object { $_.Release -eq $Rel })
        if (!$matches.Count) { $matches = @($releases | Where-Object { $_.Release.StartsWith($Rel + " ", [StringComparison]::OrdinalIgnoreCase) }) }
        if ($matches.Count -eq 1) { $Rel = $matches[0].Release }
        else { throw "Release selection is invalid or ambiguous. Use -Rel List and specify the complete release name." }
    }
	foreach ($release in $releases) {
		if ($Rel -eq "List") {
			Write-Host " -" $release.Release
		} elseif (!$Rel -or $release.Release -eq $Rel -or $Rel -eq "Latest") {
			if (!$Rel -and $Verbosity -ge 1) {
				Write-Host "No release specified (-Rel). Defaulting to '$($release.Release)'."
			}
			$Selected += " " + $release.Release
			$winReleaseId = $release.Index
			break;
		}
	}
	if ($winReleaseId -eq $null) {
		if ($Rel -ne "List") {
			Write-Host "Invalid Windows release provided."
			Write-Host "Use '-Rel List' for a list of available $Selected releases or '-Rel Latest' for latest."
		}
		exit 1
	}

	# Windows Release selection => Populate Product Edition
	$editions = Get-Windows-Editions $winVersionId $winReleaseId
	if ($Ed -eq "List") {
		Write-Host "Please select a Windows Edition (-Ed) for ${Selected}:"
	}
    if ($Ed -and $Ed -ne "List" -and !(@($editions | Where-Object { $_.Edition -eq $Ed }).Count)) {
        $candidates = @($editions | Where-Object { $_.Edition.IndexOf($Ed, [StringComparison]::OrdinalIgnoreCase) -ge 0 })
        if ($candidates.Count -eq 1) { $Ed = $candidates[0].Edition }
    }
	foreach($edition in $editions) {
		if ($Ed -eq "List") {
			Write-Host " -" $edition.Edition
		} elseif (!$Ed -or $edition.Edition -eq $Ed) {
			if (!$Ed -and $Verbosity -ge 1) {
				Write-Host "No edition specified (-Ed). Defaulting to '$($edition.Edition)'."
			}
			$Selected += "," + $edition.Edition -replace "Windows [0-9\.]*"
			$winEditionId = $edition.Id
			break;
		}
	}
	if ($winEditionId -eq $null) {
		if ($Ed -ne "List") {
			Write-Host "Invalid Windows edition provided."
			Write-Host "Use '-Ed List' for a list of available editions or remove the -Ed parameter to use default."
		}
		exit 1
	}


	# Product Edition selection => Request and populate Languages
	$languages = Get-Windows-Languages $winVersionId $winEditionId
	if (!$languages) {
		exit 3
	}
	if ($Lang -eq "List") {
		Write-Host "Please select a Language (-Lang) for ${Selected}:"

	}
	$i = 0
	$winLanguage = $null
	foreach ($language in $languages) {
		if ($Lang -eq "List") {
			Write-Host " -" $language.Name
		} elseif ((!$Lang -and $script:SelectedIndex -eq $i) -or ($Lang -and $language.Name -eq $Lang)) {
			if (!$Lang -and $Verbosity -ge 1) {
				Write-Host "No language specified (-Lang). Defaulting to '$($language.Name)'."
			}
			$Selected += ", " + $language.Name
			$winLanguage = $language
			break;
		}
		$i++
	}
	if ($winLanguage -eq $null) {
		if ($Lang -ne "List") {
			Write-Host "Invalid Windows language provided."
			Write-Host "Use '-Lang List' for a list of available languages or remove the option to use system default."
		}
		exit 1
	}

	# Language selection => Request and populate Arch download links
	$links = Get-Windows-Download-Links $winVersionId $winReleaseId $winEditionId $winLanguage
	if (!$links) {
		exit 3
	}
	if ($Arch -eq "List") {
		Write-Host "Please select an Architecture (-Arch) for ${Selected}:"
	}
	$i = 0
	foreach ($link in $links) {
		if ($Arch -eq "List") {
			Write-Host " -" $link.Arch
		} elseif ((!$Arch -and $script:SelectedIndex -eq $i) -or ($Arch -and $link.Arch -eq $Arch)) {
			if (!$Arch -and $Verbosity -ge 1) {
				Write-Host "No architecture specified (-Arch). Defaulting to '$($link.Arch)'."
			}
			$Selected += ", [" + $link.Arch + "]"
			$winLink = $link
			break;
		}
		$i++
	}
	if ($winLink -eq $null) {
		if ($Arch -ne "List") {
			Write-Host "Invalid Windows architecture provided."
			Write-Host "Use '-Arch List' for a list of available architectures or remove the option to use system default."
		}
		exit 1
	}

	# Arch selection => Return selected download link
	if ($GetUrl) {
		$null = Assert-Download-Url $winLink.Url
		Write-Output $winLink.Url
		exit 0
	} else {
		Write-Host "Selected: $Selected"
		$ExitCode = Process-Download-Link $winLink.Url
	}

	# Clean up & exit
	exit $ExitCode
}

# Form creation
$XMLForm = [Windows.Markup.XamlReader]::Load((New-Object System.Xml.XmlNodeReader $XAML))
$XAML.SelectNodes("//*[@Name]") | ForEach-Object { Set-Variable -Name ($_.Name) -Value $XMLForm.FindName($_.Name) -Scope Script }
$XMLForm.Title = $AppTitle
if ($Icon) {
	$XMLForm.Icon = $Icon
} else {
	try {
        $formIcon = [WinAPI.Utils]::ExtractIcon("imageres.dll", -5205, $true)
        if ($null -ne $formIcon) {
            try { $XMLForm.Icon = $formIcon | ConvertTo-ImageSource } finally { $formIcon.Dispose() }
        }
    } catch { Write-Verbose "Optional window icon is unavailable." }
}
if ($Locale.StartsWith("ar") -or $Locale.StartsWith("fa") -or $Locale.StartsWith("he")) {
	$XMLForm.FlowDirection = "RightToLeft"
}
$WindowsVersionTitle.Text = Get-Translation("Version")
$Continue.Content = Get-Translation("Continue")
$Back.Content = Get-Translation("Close")

# Windows 7 and non Windows platforms are too much of a liability
if ($winver -le [version]"6.1") {
	Error(Get-Translation("This feature is not available on this platform."))
	exit 403
}

# Populate the Windows versions
$i = 0
$versions = @()
foreach($version in $WindowsVersions) {
	$versions += @(New-Object PsObject -Property @{ Version = $version[0][0]; PageType = $version[0][1]; Index = $i })
	$i++
}
$WindowsVersion.ItemsSource = $versions
$WindowsVersion.DisplayMemberPath = "Version"

# Button Action
$Continue.add_click({
	$script:Stage++
	$XMLGrid.Children[2 * $Stage + 1].IsEnabled = $false
	$Continue.IsEnabled = $false
	$Back.IsEnabled = $false
	Refresh-Control($Continue)
	Refresh-Control($Back)

	switch ($Stage) {

		1 { # Windows Version selection
			$XMLForm.Title = Get-Translation($English[12])
			Refresh-Control($XMLForm)
			if ($WindowsVersion.SelectedValue.Version.StartsWith("Windows")) {
				Check-Locale
			}
			$releases = Get-Windows-Releases $WindowsVersion.SelectedValue.Index
			$script:WindowsRelease = Add-Entry $Stage "Release" $releases
			$Back.Content = Get-Translation($English[8])
			$XMLForm.Title = $AppTitle
		}

		2 { # Windows Release selection => Populate Product Edition
			$editions = Get-Windows-Editions $WindowsVersion.SelectedValue.Index $WindowsRelease.SelectedValue.Index
			$script:ProductEdition = Add-Entry $Stage "Edition" $editions
		}

		3 { # Product Edition selection => Request and populate languages
			$XMLForm.Title = Get-Translation($English[12])
			Refresh-Control($XMLForm)
			$languages = Get-Windows-Languages $WindowsVersion.SelectedValue.Index $ProductEdition.SelectedValue.Id
			if ($languages.Count -eq 0) {
				break
			}
			$script:Language = Add-Entry $Stage "Language" $languages "DisplayName"
			$Language.SelectedIndex = $script:SelectedIndex
			$XMLForm.Title = $AppTitle
		}

		4 { # Language selection => Request and populate Arch download links
			$XMLForm.Title = Get-Translation($English[12])
			Refresh-Control($XMLForm)
			$links = Get-Windows-Download-Links $WindowsVersion.SelectedValue.Index $WindowsRelease.SelectedValue.Index $ProductEdition.SelectedValue.Id $Language.SelectedValue
			if ($links.Length -eq 0) {
				break
			}
			$script:Architecture = Add-Entry $Stage "Architecture" $links "Arch"
			if ($PipeName) {
				$XMLForm.Height += $dh / 2;
				$Margin = $Continue.Margin
				$top = $Margin.Top
				$Margin.Top += $dh /2
				$Continue.Margin = $Margin
				$Margin = $Back.Margin
				$Margin.Top += $dh / 2
				$Back.Margin = $Margin
				$Margin = $Check.Margin
				$Margin.Top = $top - 2
				$Check.Margin = $Margin
				$Check.Content = Get-Translation($English[13])
				$Check.Visibility = "Visible"
			}
			$Architecture.SelectedIndex = $script:SelectedIndex
			$Continue.Content = Get-Translation("Download")
			$XMLForm.Title = $AppTitle
		}

		5 { # Arch selection => Return selected download link
			$script:ExitCode = Process-Download-Link $Architecture.SelectedValue.Url
			$XMLForm.Close()
		}
	}
	$Continue.IsEnabled = $true
	if ($Stage -ge 0) {
		$Back.IsEnabled = $true
	}
})

$Back.add_click({
	if ($Stage -eq 0) {
		$XMLForm.Close()
	} else {
		$XMLGrid.Children.RemoveAt(2 * $Stage + 3)
		$XMLGrid.Children.RemoveAt(2 * $Stage + 2)
		$XMLGrid.Children[2 * $Stage + 1].IsEnabled = $true
		$dh2 = $dh
		if ($Stage -eq 4 -and $PipeName) {
			$Check.Visibility = "Collapsed"
			$dh2 += $dh / 2
		}
		$XMLForm.Height -= $dh2;
		$Margin = $Continue.Margin
		$Margin.Top -= $dh2
		$Continue.Margin = $Margin
		$Margin = $Back.Margin
		$Margin.Top -= $dh2
		$Back.Margin = $Margin
		$script:Stage = $Stage - 1
		$XMLForm.Title = $AppTitle
		if ($Stage -eq 0) {
			$Back.Content = Get-Translation("Close")
		} else {
			$Continue.Content = Get-Translation("Continue")
			Refresh-Control($Continue)
		}
	}
})

# Display the dialog
$XMLForm.Add_Loaded({$XMLForm.Activate()})
$XMLForm.ShowDialog() | Out-Null

# Clean up & exit
exit $ExitCode
