using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using System.Runtime.InteropServices;
namespace WindowsISODownloader;
public partial class MainWindow
{
    private string? _lastFile, _lastHash;
    private int _resultActions;
    private void BeginResultAction() { _resultActions++; DownloadButton.IsEnabled=false; }
    private void EndResultAction() { _resultActions--; DownloadButton.IsEnabled=_cancel==null && _resultActions==0; }
    private UserSelections? _savedSelections = UserSelections.Load();
    private string _lastFolder = "";
    private System.Windows.Forms.NotifyIcon? _notification;
    private void InitializeFeatures()
    {
        TaskbarItemInfo = new TaskbarItemInfo();
        _lastFolder = _savedSelections?.Folder ?? "";
        if (_savedSelections != null) foreach (ComboBoxItem item in ProductCombo.Items) if (item.Content?.ToString() == _savedSelections.Product) {ProductCombo.SelectedItem = item;break;}
        SelectTag(ArchitectureCombo, _savedSelections?.Architecture ?? (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "ARM64" : "x64"));
        if (_savedSelections != null) SelectTag(LanguageCombo, _savedSelections.Language);
        ProductCombo.SelectionChanged += (_, _) => PersistSelections();
        ArchitectureCombo.SelectionChanged += (_, _) => PersistSelections();
        LanguageCombo.SelectionChanged += (_, _) => PersistSelections();
        Closed += (_, _) => {_cancel?.Cancel();_notification?.Dispose();};
        LocalizeFeatures();
        Loaded += async (_,_) => await CheckUpdateMetadataAsync(false);
    }
    private void PersistSelections() { if (ProductCombo.SelectedItem == null || ArchitectureCombo.SelectedItem == null || LanguageCombo.SelectedItem == null) return; new UserSelections(SelectedProduct(), SelectedArchitecture(), SelectedLanguage(), _lastFolder).Save(); }
    private void LocalizeFeatures()
    {
        if(OfficeOptionsButton!=null)OfficeOptionsButton.Content=App.T("Text_e29b2717dcaf");
        if(InstallOfficeButton!=null)InstallOfficeButton.Content=App.T("Text_cc333b5e2e21");
        if(UpdateButton!=null)UpdateButton.Content=App.T("Text_f26f3275e735");
        FolderButton.Content=App.T("Text_6a9084025c4b");MountButton.Content=App.T("Text_cb8ef63904fd");SignatureButton.Content=App.T("Text_a18ed47aec4a");HashCopyButton.Content=App.T("Text_7c911eb31320");HashCompareButton.Content=App.T("Text_e888155d8c7d");HashHint.Text=App.T("Text_612abf4b6ea8");ExpectedHashLabel.Text=App.T("Text_1311fc9489a3");
    }
    private void ShowCompletedFile(string path, string hash)
    {
        _lastFile=path;_lastHash=hash;HashValue.Text=hash;ResultPanel.Visibility=Visibility.Visible;
        MountButton.IsEnabled=Path.GetExtension(path).Equals(".iso",StringComparison.OrdinalIgnoreCase);
        SignatureButton.IsEnabled=Path.GetExtension(path).ToLowerInvariant() is ".exe" or ".msi" or ".zip";
        TaskbarItemInfo.ProgressState=TaskbarItemProgressState.None;
        _notification?.Dispose();
        try { _notification = new System.Windows.Forms.NotifyIcon {Icon=System.Drawing.SystemIcons.Information,Visible=true,BalloonTipTitle="Windows ISO Downloader",BalloonTipText=App.T("Text_538c473b5f10")+Path.GetFileName(path)};_notification.ShowBalloonTip(4000);
            var shown = _notification;
            var cleanup = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
            cleanup.Tick += (_, _) => { cleanup.Stop(); shown.Dispose(); if (ReferenceEquals(_notification, shown)) _notification = null; };
            cleanup.Start(); } catch(Exception ex) {App.WriteCrash(ex.Message);}
    }
    private void FolderButton_Click(object sender,RoutedEventArgs e) {try {if(_lastFile!=null) Process.Start(new ProcessStartInfo("explorer.exe",'"'+Path.GetDirectoryName(_officeConfig ?? _lastFile)+'"'){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(this,ex.Message);} }
    private async void MountButton_Click(object sender,RoutedEventArgs e)
    {
        if(_lastFile==null || !File.Exists(_lastFile))return;
        BeginResultAction();
        MountButton.IsEnabled=false;
        try {var start=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true};string code="Mount-DiskImage -ImagePath '"+_lastFile.Replace("'","''")+"' -ErrorAction Stop";foreach(var arg in new[]{"-NoProfile","-EncodedCommand",Convert.ToBase64String(Encoding.Unicode.GetBytes(code))})start.ArgumentList.Add(arg);using var p=Process.Start(start)!;var error=p.StandardError.ReadToEndAsync();using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(45));try{await p.WaitForExitAsync(timeout.Token);}catch{if(!p.HasExited)p.Kill(true);throw;}if(p.ExitCode!=0)throw new IOException(await error);DetailsText.Text=App.T("Text_2b8a28da3e98");}catch(Exception ex){MessageBox.Show(this,ex.Message,App.T("Text_6d2cd6bbfe69"));}finally{MountButton.IsEnabled=true;EndResultAction();}
    }
    private void HashCopyButton_Click(object sender,RoutedEventArgs e) {try {if(_lastHash!=null)System.Windows.Clipboard.SetText(_lastHash);}catch(Exception ex){MessageBox.Show(this,ex.Message);} }
    private async void HashCompareButton_Click(object sender,RoutedEventArgs e) {BeginResultAction();HashCompareButton.IsEnabled=false;try{var expected=DownloadSafety.ParseSha256(ExpectedHash.Text.Trim());if(expected==null)throw new IOException(App.T("Text_b36e4abc7de3"));if (_lastFile != null) { await using var current = File.OpenRead(_lastFile); _lastHash = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(current)); HashValue.Text = _lastHash; } MessageBox.Show(this,expected.Equals(_lastHash,StringComparison.OrdinalIgnoreCase)?App.T("Text_0613080b8329"):App.T("Text_3f312af12605"));}catch(Exception ex){MessageBox.Show(this,ex.Message);} finally { HashCompareButton.IsEnabled=true;EndResultAction(); } }
    private async void SignatureButton_Click(object sender,RoutedEventArgs e)
    {
        var file=_lastFile;if(file==null)return;BeginResultAction();SignatureButton.IsEnabled=false;
        var temp=Path.Combine(Path.GetTempPath(),"wid-signatures-"+Guid.NewGuid().ToString("N"));
        try {
            Directory.CreateDirectory(temp);
            var paths=new List<string>();
            long extractedBytes=0;
            if(Path.GetExtension(file).Equals(".zip",StringComparison.OrdinalIgnoreCase)) {
                Directory.CreateDirectory(temp);
                using var zip=ZipFile.OpenRead(file);
                foreach(var entry in zip.Entries.Where(x=>x.Name.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))) {
                    if(paths.Count>=500 || entry.Length>100_000_000 || extractedBytes + entry.Length > 2_000_000_000)throw new IOException(App.T("Text_b0f6330c0d90"));
                    var destination=Path.Combine(temp,Guid.NewGuid().ToString("N")+"-"+Path.GetFileName(entry.Name));entry.ExtractToFile(destination);paths.Add(destination);extractedBytes+=entry.Length;
                }
            } else paths.Add(file);
            if(paths.Count==0)throw new IOException(App.T("Text_d3ffa28d4d42"));
            var pathList = Path.Combine(temp,"files.json");
            await File.WriteAllTextAsync(pathList,JsonSerializer.Serialize(paths));
            string code="[Console]::OutputEncoding=[Text.Encoding]::UTF8; $files=Get-Content -LiteralPath '"+pathList.Replace("'","''")+"' -Raw -Encoding UTF8 | ConvertFrom-Json; $r=@(foreach($f in @($files)){$s=Get-AuthenticodeSignature -LiteralPath $f;[pscustomobject]@{File=[IO.Path]::GetFileName($f);Status=[string]$s.Status;Signer=if($s.SignerCertificate){$s.SignerCertificate.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::SimpleName,$false)}else{''}}});ConvertTo-Json -InputObject $r -Compress";
            var start=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8};foreach(var arg in new[]{"-NoProfile","-EncodedCommand",Convert.ToBase64String(Encoding.Unicode.GetBytes(code))})start.ArgumentList.Add(arg);
            using var process=Process.Start(start)!;var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(3));try{await process.WaitForExitAsync(timeout.Token);}catch{if(!process.HasExited)process.Kill(true);throw;}if(process.ExitCode!=0)throw new IOException(await error);
            using var json=JsonDocument.Parse(await output);var text=new StringBuilder();foreach(var row in json.RootElement.EnumerateArray())text.AppendLine($"{row.GetProperty("File").GetString()}: {row.GetProperty("Status").GetString()} — {row.GetProperty("Signer").GetString()}");
            var box = new TextBox {Text=text.ToString(),IsReadOnly=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(16),FontSize=15};
            box.SetResourceReference(Control.BackgroundProperty,"Panel2Brush");box.SetResourceReference(Control.ForegroundProperty,"TextBrush");
            var dialog = new Window {Owner=this,Title=App.T("Text_f326ad874498"),Width=760,Height=450,Content=box};dialog.SetResourceReference(Window.BackgroundProperty,"BgBrush");dialog.ShowDialog();
        }catch(Exception ex){MessageBox.Show(this,ex.Message);}finally{try{if(Directory.Exists(temp))Directory.Delete(temp,true);}catch{} SignatureButton.IsEnabled=true;EndResultAction();}
    }
    private async Task AddHistoricalReleasesAsync(string product)
    {
        try {
            var releases=await ReleaseCache.GetAsync("fido-catalog:"+product,()=>QueryFidoReleasesAsync(product,CancellationToken.None),CancellationToken.None);
            if(SelectedProduct()!=product)return;
            foreach(var release in releases)if(!VersionCombo.Items.Cast<ComboBoxItem>().Any(x=>x.Tag?.ToString()=="Fido:"+release))Add(VersionCombo,release+App.T("Text_dec1fe42ed26"),"Fido:"+release);
        }catch(Exception ex){App.WriteCrash("Fido release catalog: "+ex.Message);}
    }
}
