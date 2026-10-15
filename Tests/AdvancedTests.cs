using WindowsISODownloader;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Xml.Linq;
internal static class AdvancedTests
{
    private static int count;
    private static void Check(bool condition,string reason){count++;if(!condition)throw new Exception(reason);}
    private static void Reject(Action action,string reason){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,reason);}
    internal static async Task RunAsync() {
        var options=new OfficeOptions("MonthlyEnterprise",new[]{"tr-tr","en-us"},new[]{"Access","Publisher"},true,true);
        var xml=options.CreateAdd("C:\\Office & Install","64","Office");
        Check(xml.Attribute("Channel")!.Value=="MonthlyEnterprise","Office channel");Check(xml.Elements("Product").Count()==3,"Office + Visio + Project");Check(xml.Elements("Product").All(x=>x.Elements("Language").Count()==2),"Multiple language propagation");Check(xml.Elements("Product").First().Elements("ExcludeApp").Count()==2,"Office excluded apps");Check(xml.Elements("Product").Skip(1).All(x=>!x.Elements("ExcludeApp").Any()),"ExcludeApp must not exclude suite apps from Visio/Project");Check(XElement.Parse(xml.ToString()).Attribute("SourcePath")!.Value=="C:\\Office & Install","XML escaping");
        Reject(()=>options.Validate("Office 2019"),"Office 2019 cannot use M365 channels");Reject(()=>new OfficeOptions("Current",Array.Empty<string>(),Array.Empty<string>(),false,false).Validate("Office"),"At least one language");Reject(()=>new OfficeOptions("Current",new[]{"../evil"},Array.Empty<string>(),false,false).Validate("Office"),"No arbitrary language injection");Reject(()=>new OfficeOptions("Current",new[]{"en-us"},new[]{"Invalid"},false,false).Validate("Office"),"No unsupported app ID");Reject(()=>options.CreateAdd("folder","arm64","Office"),"ODT expects 64/32 architecture");
        var vl=new OfficeOptions("PerpetualVL2019",new[]{"de-de"},Array.Empty<string>(),true,true).CreateAdd("folder","32","Office 2019");Check(vl.Elements("Product").Select(x=>x.Attribute("ID")!.Value).SequenceEqual(new[]{"ProPlus2019Volume","VisioPro2019Volume","ProjectPro2019Volume"}),"2019 volume product IDs");
        var cli=CliOptions.Parse(new[]{"--product","win11","--lang","Turkish","--out","D:\\","--json"});Check(cli.Flags.Contains("--json")&&cli.Get("--lang")=="Turkish","CLI parsing");Reject(()=>CliOptions.Parse(new[]{"--product","win11","--product","win10"}),"Duplicate options");Reject(()=>CliOptions.Parse(new[]{"--product"}),"Missing value");Reject(()=>CliOptions.Parse(new[]{"--product","win11","--visio"}),"Office-only options");Reject(()=>CliOptions.Parse(new[]{"--product","win11","--unknown"}),"Unknown option");Reject(()=>CliOptions.Parse(new[]{"--product","win11","--list","--url-only"}),"Conflicting modes");Check(CliOptions.Parse(new[]{"--help"}).Flags.Contains("--help"),"Help without product");
        using var client=new HttpClient(new FixtureHandler());
        var net=await AdditionalCatalog.ReleasesAsync(client,".NET SDK",default);Check(net.Length==1&&net[0].Id=="10.0","Stable supported .NET channels only");
        var sdk=await AdditionalCatalog.ResolveAsync(client,".NET SDK","10.0","arm64",default);Check(sdk.Url.AbsolutePath.EndsWith("arm64.exe")&&sdk.Sha512?.Length==128,"ARM64 SDK with publisher SHA-512");
        var terminal=await AdditionalCatalog.ResolveAsync(client,"Windows Terminal","Latest","arm64",default);Check(terminal.Url.AbsolutePath.EndsWith("arm64.zip")&&terminal.Sha256?.Length==64,"Terminal architecture and digest");
        var sql=await AdditionalCatalog.ResolveAsync(client,"SQL Server","2022-Eval","x64",default);Check(sql.Url.Query.Contains("2215202"),"Official SQL eval link");
        bool badArch=false;try{await AdditionalCatalog.ResolveAsync(client,"SQL Server","2022-Eval","arm64",default);}catch(ArgumentException){badArch=true;}Check(badArch,"No fabricated SQL ARM64 installer");
        Check(DownloadSafety.IsAllowed(terminal.Url,new Uri("https://release-assets.githubusercontent.com/file")),"Trusted Terminal redirect");Check(!DownloadSafety.IsAllowed(new Uri("https://github.com/evil/terminal/releases/download/v1/file.zip"),new Uri("https://release-assets.githubusercontent.com/file")),"Reject other GitHub repos");
        var opaque=new Uri("https://release-assets.githubusercontent.com/github-production-release-asset/123/opaque");string fileName=Path.GetFileName(terminal.Url.AbsolutePath);
        Check(DownloadSafety.HasExpectedFormat(terminal.Url,opaque,fileName,".zip"),"GitHub opaque CDN URL with exact response filename");
        Check(!DownloadSafety.HasExpectedFormat(terminal.Url,opaque,"other.zip",".zip"),"Reject different response filename");
        Check(!DownloadSafety.HasExpectedFormat(terminal.Url,opaque,"../"+fileName,".zip"),"Reject response path traversal");
        Check(!DownloadSafety.HasExpectedFormat(terminal.Url,opaque,null,".zip"),"Reject opaque URL without expected filename");
        Check(!DownloadSafety.HasExpectedFormat(terminal.Url,new Uri("https://evil.example/"),fileName,".zip"),"Reject untrusted opaque target");
        Check(DownloadSafety.HasExpectedFormat(new Uri("https://github.com/microsoft/PowerToys/releases/download/v1/setup.exe"),opaque,"setup.exe",".exe"),"PowerToys GitHub CDN executable format");
        string updateJson=JsonSerializer.Serialize(new{draft=false,prerelease=false,tag_name="v3.7.0",html_url="https://github.com/bunyaminkaya/Windows-ISO-Downloader/releases/tag/v3.7.0",assets=new[]{new{name="Windows-ISO-Downloader-win-arm64.zip",browser_download_url="https://github.com/bunyaminkaya/Windows-ISO-Downloader/releases/download/v3.7.0/Windows-ISO-Downloader-win-arm64.zip",size=2_000_000,digest="sha256:"+new string('a',64)}}});using var j=JsonDocument.Parse(updateJson);var update=UpdateMetadata.Parse(j.RootElement,"arm64",new Version(3,6,0));Check(update!=null&&update.Version==new Version(3,7,0),"New release and architecture");Check(UpdateMetadata.Parse(j.RootElement,"arm64",new Version(3,7,0))==null,"No notification for same version");Reject(()=>UpdateMetadata.Parse(j.RootElement,"x64",new Version(3,6,0)),"No wrong-architecture update fallback");
        using var versioned=JsonDocument.Parse(updateJson.Replace("Windows-ISO-Downloader-win-arm64.zip","Windows-ISO-Downloader-V3.7.0-Windows-ARM64.zip"));Check(UpdateMetadata.Parse(versioned.RootElement,"arm64",new Version(3,6,0))!=null,"Manual versioned release package names");
        using var wrong=JsonDocument.Parse(updateJson.Replace("https://github.com/bunyaminkaya","https://evil.example/bunyaminkaya"));Reject(()=>UpdateMetadata.Parse(wrong.RootElement,"arm64",new Version(3,6,0)),"Untrusted update page");Check(UpdateMetadata.ReadChecksum(new string('b',64)+"  package.zip\n","package.zip")==new string('b',64),"Checksum manifest");Reject(()=>UpdateMetadata.ReadChecksum(new string('b',64)+"  other.zip","package.zip"),"Checksum must match exact asset name");
        Check(ServerCatalog.AllLinks().Count()==62,"All 48 ISOs + 10 VHD parts + POSReady + 3 virtual disks are exported");
        var temp=Path.Combine(Path.GetTempPath(),"wid-advanced-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
        try {
            var config=OfficeWorkspace.CreateConfiguration(temp,"64","tr-tr","Office",options);var saved=XDocument.Load(config);Check(saved.Root!.Element("Add")!.Elements("Product").Count()==3,"Persisted multilingual Office package");Check(File.Exists(config),"Durable Office configuration");
            var dataFile=Path.Combine(temp,"payload.bin");await File.WriteAllBytesAsync(dataFile,new byte[]{1,2,3,4});var expected=Convert.ToHexString(System.Security.Cryptography.SHA512.HashData(new byte[]{1,2,3,4}));await AdditionalCatalog.VerifySha512Async(dataFile,expected,default);count++;
            bool mismatch=false;try{await AdditionalCatalog.VerifySha512Async(dataFile,new string('0',128),default);}catch(IOException){mismatch=true;}Check(mismatch,"Reject bad publisher SHA-512");
            Check(ServerCatalog.GetLanguages("2022VHD").SequenceEqual(new[]{"enus"}),"VHD language availability");Reject(()=>ServerCatalog.GetVirtualDisk("2022VHD","trtr"),"No fake Turkish VHD fallback");
        } finally {Directory.Delete(temp,true);}
        using(var resource=typeof(UiText).Assembly.GetManifestResourceStream("WindowsISODownloader.Strings.resources")!)using(var reader=new System.Resources.ResourceReader(resource)) {
            var before=UiText.Language;foreach(System.Collections.DictionaryEntry entry in reader){string key=(string)entry.Key;UiText.Language="tr";Check(!string.IsNullOrWhiteSpace(UiText.T(key)),"Missing Turkish resource");UiText.Language="en";Check(!string.IsNullOrWhiteSpace(UiText.T(key)),"Missing English resource");}UiText.Language=before;
        }
        Console.WriteLine($"PASS: {count} Office, CLI, product metadata, ARM64 and update checks.");
    }
    private sealed class FixtureHandler:HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token) {
            string url=request.RequestUri!.AbsoluteUri;string data;
            if(url.EndsWith("releases-index.json"))data="{\"releases-index\":[{\"channel-version\":\"10.0\",\"release-type\":\"lts\",\"support-phase\":\"active\",\"latest-sdk\":\"10.0.402\",\"latest-runtime\":\"10.0.6\"},{\"channel-version\":\"11.0\",\"release-type\":\"sts\",\"support-phase\":\"preview\",\"latest-sdk\":\"11.0.100-preview\",\"latest-runtime\":\"11.0.0-preview\"}]}";
            else if(url.Contains("release-metadata/10.0"))data=JsonSerializer.Serialize(new{latest_sdk="unused"}).Replace("unused","unused");
            else data=JsonSerializer.Serialize(new{tag_name="v1.2.3",assets=new[]{new{name="Microsoft.WindowsTerminal_1.2.3_arm64.zip",browser_download_url="https://github.com/microsoft/terminal/releases/download/v1.2.3/Microsoft.WindowsTerminal_1.2.3_arm64.zip",digest="sha256:"+new string('c',64)}}});
            if(url.Contains("release-metadata/10.0"))data="{\"latest-sdk\":\"10.0.402\",\"latest-runtime\":\"10.0.6\",\"releases\":[{\"sdk\":{\"version\":\"10.0.402\",\"files\":[{\"rid\":\"win-arm64\",\"url\":\"https://builds.dotnet.microsoft.com/dotnet/sdk-arm64.exe\",\"hash\":\""+new string('d',128)+"\"}]}}]}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(data),RequestMessage=request});
        }
    }
}
