using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
namespace WindowsISODownloader;
public partial class MainWindow
{
    private AvailableUpdate? _availableUpdate;
    private bool _checkingUpdate;
    private async Task CheckUpdateMetadataAsync(bool manual) {
        if(_checkingUpdate)return;_checkingUpdate=true;UpdateButton.IsEnabled=false;
        try {
            using var j=await AdditionalCatalog.GetJsonAsync(Client,"https://api.github.com/repos/"+UpdateMetadata.Repository+"/releases/latest",CancellationToken.None);
            string arch=RuntimeInformation.ProcessArchitecture==Architecture.Arm64?"arm64":"x64";
            _availableUpdate=UpdateMetadata.Parse(j.RootElement,arch,new Version(RuntimeInfo.Version));
            UpdateStatus.Text=_availableUpdate==null?(manual?App.T("Text_1a24cc9dedd3"):""):_availableUpdate.Tag+App.T("Text_3c6577b3d64d");
            if(_availableUpdate!=null)UpdateButton.Content=App.T("Text_628f63a67cbb");
        }catch(Exception ex){if(manual)UpdateStatus.Text=App.T("Text_3de184e0aee9")+ex.Message;else App.WriteCrash("Update check: "+ex.Message);}
        finally{_checkingUpdate=false;UpdateButton.IsEnabled=true;}
    }
    private async void UpdateButton_Click(object sender,RoutedEventArgs e) {
        if(_checkingUpdate || _cancel!=null || _resultActions>0)return;
        if(_availableUpdate==null){await CheckUpdateMetadataAsync(true);return;}
        _checkingUpdate=true;UpdateButton.IsEnabled=false;BeginResultAction();
        var temp=Path.Combine(Path.GetTempPath(),"wid-update-"+Guid.NewGuid().ToString("N"));
        try {
            var release=_availableUpdate;Directory.CreateDirectory(temp);UpdateStatus.Text=App.T("Text_6a418377458f");
            string expected=release.Sha256??await LoadUpdateChecksumAsync(release,CancellationToken.None);
            string zip=Path.Combine(temp,"package.zip");
            using(var response=await DownloadSafety.OpenAsync(DownloadClient,release.Package,CancellationToken.None)) {
                response.EnsureSuccessStatusCode();if(response.Content.Headers.ContentLength is long len && len!=release.Size)throw new IOException(UiText.T("Error_6935a47753c5"));
                await using var input=await response.Content.ReadAsStreamAsync();await using var output=File.Create(zip);var buffer=new byte[128*1024];long total=0;int read;
                while((read=await DownloadSafety.ReadChunkAsync(input,buffer,TimeSpan.FromSeconds(120),CancellationToken.None))>0){total+=read;if(total>release.Size)throw new IOException(UiText.T("Error_295c8c8b567d"));await output.WriteAsync(buffer.AsMemory(0,read));}
                if(total!=release.Size)throw new IOException(UiText.T("Error_8c645415fcff"));
            }
            await using(var input=File.OpenRead(zip))if(!DownloadSafety.MatchesHash(await System.Security.Cryptography.SHA256.HashDataAsync(input),expected))throw new IOException(App.T("Text_b20f17e588a2"));
            string exe=Path.Combine(temp,"Windows ISO Downloader.exe");
            using(var archive=ZipFile.OpenRead(zip)) {var candidates=archive.Entries.Where(x=>x.Name=="Windows ISO Downloader.exe").ToArray();if(candidates.Length!=1||candidates[0].Length>500_000_000)throw new IOException(UiText.T("Error_b2a1025f27fb"));candidates[0].ExtractToFile(exe);}
            using var lockFile=new FileStream(exe,FileMode.Open,FileAccess.Read,FileShare.Read);
            var current=Environment.ProcessPath??throw new IOException(UiText.T("Error_311ac428dad8"));
            string code="$ErrorActionPreference='Stop'; $a=Get-AuthenticodeSignature -LiteralPath '"+current.Replace("'","''")+"'; $b=Get-AuthenticodeSignature -LiteralPath '"+exe.Replace("'","''")+"'; if($a.Status -ne 'Valid' -or $b.Status -ne 'Valid' -or $null -eq $a.SignerCertificate -or $null -eq $b.SignerCertificate -or $a.SignerCertificate.Subject -ne $b.SignerCertificate.Subject){exit 1}; exit 0";
            var start=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true};foreach(var arg in new[]{"-NoProfile","-EncodedCommand",Convert.ToBase64String(Encoding.Unicode.GetBytes(code))})start.ArgumentList.Add(arg);
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(45));
            try{await RunToolAsync(start,timeout.Token);}catch(Exception ex){throw new IOException(App.T("Text_68b37f824c80"),ex);}
            if(!Version.TryParse(FileVersionInfo.GetVersionInfo(exe).ProductVersion?.Split('+')[0],out var actual)||actual!=release.Version)throw new IOException(UiText.T("Error_99caeca7e0f4"));
            UpdateStatus.Text=App.T("Text_8076ee4fa43d");
            MessageBox.Show(this,UpdateStatus.Text+"\n"+release.Page.AbsoluteUri,App.T("Text_ba30bd81759f"));
            // Only open the verified official release page. Never execute the update or install automatically.
            Process.Start(new ProcessStartInfo(release.Page.AbsoluteUri){UseShellExecute=true});
        }catch(Exception ex){UpdateStatus.Text=App.T("Text_724be8b2de24")+ex.Message;}
        finally{try{if(Directory.Exists(temp))Directory.Delete(temp,true);}catch{} _checkingUpdate=false;UpdateButton.IsEnabled=true;EndResultAction();}
    }
    private static async Task<string> LoadUpdateChecksumAsync(AvailableUpdate update,CancellationToken token) {
        if(update.Checksums==null)throw new IOException(UiText.T("Error_6c33c68865e7"));
        using var response=await DownloadSafety.OpenAsync(DownloadClient,update.Checksums,token);response.EnsureSuccessStatusCode();
        await using var input=await response.Content.ReadAsStreamAsync(token);using var output=new MemoryStream();var buffer=new byte[4096];int n;
        while((n=await DownloadSafety.ReadChunkAsync(input,buffer,TimeSpan.FromSeconds(45),token))>0){if(output.Length+n>65_536)throw new IOException(UiText.T("Error_0078def9d4cb"));output.Write(buffer,0,n);}
        return UpdateMetadata.ReadChecksum(Encoding.UTF8.GetString(output.ToArray()),Path.GetFileName(update.Package.AbsolutePath));
    }
}
