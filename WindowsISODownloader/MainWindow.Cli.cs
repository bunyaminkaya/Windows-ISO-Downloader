using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
namespace WindowsISODownloader;
public partial class MainWindow
{
    internal static async Task<int> RunCliAsync(string[] args) {
        bool json=args.Contains("--json");using var cancel=new CancellationTokenSource();ConsoleCancelEventHandler handler=(_,e)=>{e.Cancel=true;cancel.Cancel();};Console.CancelKeyPress+=handler;
        try {
            var o=CliOptions.Parse(args);if(o.Values.ContainsKey("--ui-lang"))UiText.Language=o.Get("--ui-lang");
            if(o.Flags.Contains("--help")){Console.WriteLine("Windows ISO Downloader "+RuntimeInfo.Version+"\n--product <"+string.Join('|',CliOptions.Products.Keys)+"> --release <id> --arch x64|arm64|x86 --lang Turkish --out <folder> --json\n--list | --url-only | --force | --expected-sha256 <hex>\nOffice: --channel Current|MonthlyEnterprise|SemiAnnual --languages tr-tr,en-us --exclude Access,Publisher --visio --project\nNo automatic installation. Exit codes: 0 success, 1 failure, 2 canceled. Use start /wait for the Windows GUI-subsystem executable.");return 0;}
            var product=CliOptions.Products[o.Get("--product")];var token=cancel.Token;
            string arch=o.Get("--arch",RuntimeInformation.OSArchitecture==Architecture.Arm64?"arm64":"x64").ToLowerInvariant();
            if(!o.Values.ContainsKey("--arch") && product is "Windows 10" or "Windows Server" or "Office 2019")arch="x64";
            string language=o.Get("--lang",product is "Windows 11" or "Windows 10"?"Turkish":product is "Office" or "Office 2019" or "Office 2016"?"tr-tr":"enus");
            string release=o.Get("--release","Latest");DownloadFile file;string? configuration=null;OfficeOptions? officeOptions=null;
            if(product is "Office" or "Office 2019") {officeOptions=new OfficeOptions(o.Get("--channel",product=="Office 2019"?"PerpetualVL2019":"Current"),o.Get("--languages",language).Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries),o.Get("--exclude").Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries),o.Flags.Contains("--visio"),o.Flags.Contains("--project"));officeOptions.Validate(product);}
            if(product is "Windows 11" or "Windows 10") {
                if(o.Flags.Contains("--list")){var catalog=await QueryFidoReleasesAsync(product,token);Emit(new{product,releases=new[]{"Latest"}.Concat(catalog).ToArray(),architectures=product=="Windows 11"?new[]{"x64","arm64"}:new[]{"x64"}},json);return 0;}
                if(release!="Latest"){var catalog=await QueryFidoReleasesAsync(product,token);var matches=catalog.Where(x=>x.Equals(release,StringComparison.OrdinalIgnoreCase)||x.StartsWith(release+" ",StringComparison.OrdinalIgnoreCase)).ToArray();if(matches.Length!=1)throw new ArgumentException(UiText.T("CliReleaseError"));release=matches[0];}
            }
            if(product=="Windows Server" && ServerCatalog.IsBundle(release) && !o.Flags.Contains("--list")) {
                if(arch!="x64")throw new ArgumentException(UiText.T("Error_4f8ad51bf2b1"));
                var links=ServerCatalog.GetBundle(release);if(o.Flags.Contains("--url-only")){Emit(new{product,release,urls=links.Select(x=>x.AbsoluteUri)},json);return 0;}
                var bundle=Path.Combine(Path.GetFullPath(o.Get("--out",Environment.CurrentDirectory)),"WindowsServer_"+release);
                Directory.CreateDirectory(bundle);var results=new List<object>();
                foreach(var link in links){string path=Path.Combine(bundle,Path.GetFileName(link.AbsolutePath));if(File.Exists(path)&&!o.Flags.Contains("--force"))throw new IOException(UiText.T("Error_ef26b1d417cb"));var partHash=await ResumableDownload.RunAsync(DownloadClient,link,path+".part",release+"|"+link.AbsoluteUri,Path.GetExtension(path),1_000_000,null,_=>{},token);File.Move(path+".part",path,true);results.Add(new{file=path,sha256=partHash});}
                Emit(new{success=true,product,release,files=results},json);return 0;
            }
            if(AdditionalCatalog.Supports(product)) {
                var releases=await AdditionalCatalog.ReleasesAsync(Client,product,token);
                if(o.Flags.Contains("--list")){Emit(new{product,releases,architectures=AdditionalCatalog.Architectures(product)},json);return 0;}
                if(!o.Values.ContainsKey("--arch")&&AdditionalCatalog.Architectures(product).SequenceEqual(new[]{"auto"}))arch="auto";
                if(!o.Values.ContainsKey("--arch")&&product=="SQL Server")arch="x64";
                if(release=="Latest")release=releases.First().Id;
                file=await AdditionalCatalog.ResolveAsync(Client,product,release,arch,token);
            } else {
                if(o.Flags.Contains("--list")){Emit(new{product,releases=product=="Windows Server"?new[]{"2025","2022","2019","2016","2012R2","2012","2025VHDX","2022VHD","2019VHD","2008R2VHD","2008VHD","2003R2VHD"}:product=="Windows 11 LTSC"?new[]{"Enterprise","IoT"}:product=="Office 2016"?new[]{"ProPlusRetail","HomeBusinessRetail","HomeStudentRetail"}:new[]{"Latest"},note="Windows historical releases: use the exact bundled Fido catalog name; unsupported releases fail."},json);return 0;}
                file=await ResolveCliFileAsync(product,release,arch,language,token);
            }
            if(o.Flags.Contains("--url-only")){Emit(new{product,release,architecture=arch,language,url=file.Url.AbsoluteUri,extension=file.Extension,sha256=file.Sha256,sha512=file.Sha512},json);return 0;}
            string folder=Path.GetFullPath(o.Get("--out",Environment.CurrentDirectory));Directory.CreateDirectory(folder);
            string name=o.Get("--product")+"_"+System.Text.RegularExpressions.Regex.Replace(release,"[^A-Za-z0-9.-]","_")+"_"+arch+file.Extension;string target=Path.Combine(folder,name);
            if(File.Exists(target)&&!o.Flags.Contains("--force"))throw new IOException(UiText.T("Error_f936a3dc3103"));

            string? expected=file.Sha256;var userHash=DownloadSafety.ParseSha256(o.Get("--expected-sha256"));if(userHash!=null&&expected!=null&&!userHash.Equals(expected,StringComparison.OrdinalIgnoreCase))throw new IOException(UiText.T("Error_377c66bd2076"));expected??=userHash;
            string hash=await ResumableDownload.RunAsync(DownloadClient,file.Url,target+".part",product+"|"+release+"|"+arch+"|"+language+"|"+file.Url.GetLeftPart(UriPartial.Path),file.Extension,file.Extension is ".iso" or ".img" or ".vhdx" or ".vhd"?100_000_000:1_000_000,expected,_=>{},token);
            try{await AdditionalCatalog.VerifySha512Async(target+".part",file.Sha512,token);}catch(IOException){TryDelete(target+".part");throw;}
            File.Move(target+".part",target,true);
            if(product is "Office" or "Office 2019") {
                configuration=OfficeWorkspace.CreateConfiguration(folder,arch=="x86"?"32":"64",language,product,officeOptions);await DownloadOfficePayloadAsync(target,configuration,token);
            }
            Emit(new{success=true,product,release,architecture=arch,language,file=target,sha256=hash,publisherHashVerified=expected!=null||file.Sha512!=null,officeConfiguration=configuration},json);return 0;
        }catch(OperationCanceledException){Emit(new{success=false,error="Canceled; resumable partial files retained."},json);return 2;}catch(Exception ex){Emit(new{success=false,error=ex.Message},json);return 1;}finally{Console.CancelKeyPress-=handler;}
    }
    private static async Task<string[]> QueryFidoReleasesAsync(string product,CancellationToken token) {
        string script=Path.Combine(AppContext.BaseDirectory,"Fido.ps1");using var fileLock=File.OpenRead(script);RuntimeInfo.VerifyFido(script);
        var start=new System.Diagnostics.ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var arg in new[]{"-NoProfile","-ExecutionPolicy","Bypass","-File",script,"-Win",product=="Windows 11"?"11":"10","-Rel","List"})start.ArgumentList.Add(arg);
        using var process=System.Diagnostics.Process.Start(start)!;var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromSeconds(30));
        try{await process.WaitForExitAsync(timeout.Token);}catch{if(!process.HasExited)process.Kill(true);throw;}
        var releases=(await output).Split('\n').Select(x=>x.Trim()).Where(x=>x.StartsWith("- ",StringComparison.Ordinal)).Select(x=>x[2..].Trim()).ToArray();
        if(releases.Length==0)throw new IOException(UiText.T("CliFidoError")+await error);return releases;
    }
    private static void Emit(object value,bool json)=>Console.WriteLine(JsonSerializer.Serialize(value,new JsonSerializerOptions {WriteIndented=!json}));
    private static async Task<DownloadFile> ResolveCliFileAsync(string product,string release,string arch,string language,CancellationToken token) {
        if(product is "Windows 11" or "Windows 10") {
            if(arch is not ("x64" or "arm64") || product=="Windows 10"&&arch!="x64")throw new ArgumentException(UiText.T("Error_3c7685b92c84"));
            var a=arch=="arm64"?"ARM64":"x64";
            if(release=="Latest"){var r=await GetOfficialWindowsReleaseAsync(product,a,token);return new(await ResolveUrlAsync(product,r.EditionId,a,language,token),".iso");}
            return new(await ResolveHistoricalUrlAsync(product,release,a,language,token),".iso");
        }
        if(product=="Windows Server"){if(arch!="x64")throw new ArgumentException(UiText.T("Error_4f8ad51bf2b1"));if(release=="Latest")release="2025";return new(ServerCatalog.IsVirtualDisk(release)?ServerCatalog.GetVirtualDisk(release,language):ServerCatalog.GetIso(release,language),ServerCatalog.IsVirtualDisk(release)?(release=="2025VHDX"?".vhdx":".vhd"):".iso");}
        if(product=="Windows 11 LTSC")return new(GetLtscUrl(release=="Latest"?"Enterprise":release,arch=="arm64"?"ARM64":arch),".iso");
        if(product=="Windows Embedded POSReady 2009")return new(ServerCatalog.PosReady2009,".iso");
        if(product=="Microsoft Edge"){var r=await GetEdgeReleaseAsync(arch,token);return new(r.Url,".msi",r.Sha256);}
        if(product=="Microsoft PowerToys"){var r=await GetPowerToysReleaseAsync(arch,token);return new(r.Url,".exe",r.Sha256);}
        if(product is "Office" or "Office 2019") {if(arch is not ("x64" or "x86" or "arm64"))throw new ArgumentException(UiText.T("Error_cc5f48ac8f87"));if(product=="Office 2019"&&arch=="arm64")throw new ArgumentException(UiText.T("Error_971129683dea"));return new(await GetOfficeToolUrlAsync(token),".exe");}
        if(product=="Office 2016")return new(GetOffice2016Url(release=="Latest"?"ProPlusRetail":release,language),".img");
        if(product=="Visual Studio Code") {if(arch is not ("x64" or "arm64"))throw new ArgumentException(UiText.T("Error_f00bd03bffdb"));return new(new Uri("https://update.code.visualstudio.com/latest/win32-"+arch+"-user/stable"),".exe");}
        if(product=="Sysinternals Suite")return new(new Uri("https://download.sysinternals.com/files/SysinternalsSuite.zip"),".zip");
        throw new ArgumentException(UiText.T("Error_bed9f72719ca"));
    }
}
