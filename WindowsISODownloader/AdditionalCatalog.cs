using System.Net.Http;
using System.Text.Json;
using System.Security.Cryptography;
using System.IO;
namespace WindowsISODownloader;
internal sealed record ProductRelease(string Label, string Id);
internal sealed record DownloadFile(Uri Url, string Extension, string? Sha256=null, string? Sha512=null);
internal static class AdditionalCatalog
{
    internal static readonly string[] Products={"Visual C++ Redistributable","Windows Terminal","Windows ADK","Windows PE add-on","SQL Server","Visual Studio Community","Visual Studio Build Tools",".NET SDK",".NET Runtime",".NET Desktop Runtime",".NET ASP.NET Core Runtime"};
    internal static bool Supports(string product)=>Products.Contains(product);
    internal static string[] Architectures(string product)=>product switch {
        "SQL Server"=>new[]{"x64"},"Windows ADK" or "Windows PE add-on" or "Visual Studio Community" or "Visual Studio Build Tools"=>new[]{"auto"},
        "Visual C++ Redistributable"=>new[]{"x64","arm64","x86"}, _=>new[]{"x64","arm64"}};
    internal static string Extension(string product)=>product=="Windows Terminal"?".zip":".exe";
    internal static async Task<ProductRelease[]> ReleasesAsync(HttpClient client,string product,CancellationToken token) {
        if(product.StartsWith(".NET",StringComparison.Ordinal)) {
            using var j=await GetJsonAsync(client,"https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json",token);
            return j.RootElement.GetProperty("releases-index").EnumerateArray().Where(x=> x.GetProperty("release-type").GetString() is "lts" or "sts" && x.GetProperty("support-phase").GetString() is "active" or "maintenance")
                .Select(x=>new ProductRelease(".NET "+x.GetProperty("channel-version").GetString()+" — "+x.GetProperty(product==".NET SDK"?"latest-sdk":"latest-runtime").GetString(),x.GetProperty("channel-version").GetString()!)).ToArray();
        }
        if(product=="Windows Terminal") {using var j=await GetJsonAsync(client,"https://api.github.com/repos/microsoft/terminal/releases/latest",token);return new[]{new ProductRelease(j.RootElement.GetProperty("tag_name").GetString()!,"Latest")};}
        return product switch {
            "Visual C++ Redistributable"=>new[]{new ProductRelease("Visual C++ v14 (latest)","Latest")},
            "Windows ADK" or "Windows PE add-on"=>new[]{new ProductRelease("10.1.26100.9457 — Windows 11 24H2/25H2/26H2, Server 2025/2022","26100"),new ProductRelease("10.1.28000.1 — Windows 11 26H1 ARM64","28000")},
            "SQL Server"=>new[]{new ProductRelease("SQL Server 2025 Standard Developer (bootstrapper)","2025-StdDev"),new ProductRelease("SQL Server 2025 Enterprise Developer (bootstrapper)","2025-EntDev"),new ProductRelease("SQL Server 2022 Evaluation (bootstrapper)","2022-Eval")},
            "Visual Studio Community" or "Visual Studio Build Tools"=>new[]{new ProductRelease("Visual Studio 2026 Stable (bootstrapper)","stable"),new ProductRelease("Visual Studio 2022 (bootstrapper)","17")},
            _=>throw new ArgumentException(UiText.T("Error_bed9f72719ca"))};
    }
    internal static async Task<DownloadFile> ResolveAsync(HttpClient client,string product,string release,string architecture,CancellationToken token) {
        if(!Supports(product)||!Architectures(product).Contains(architecture))throw new ArgumentException(UiText.T("Error_4d549603d106"));
        if(product.StartsWith(".NET",StringComparison.Ordinal)) {
            var releases=await ReleasesAsync(client,product,token);if(!releases.Any(x=>x.Id==release))throw new ArgumentException(UiText.T("Error_3c8b9f25fa13"));
            using var j=await GetJsonAsync(client,$"https://builds.dotnet.microsoft.com/dotnet/release-metadata/{release}/releases.json",token);
            var latest=j.RootElement.GetProperty(product==".NET SDK"?"latest-sdk":"latest-runtime").GetString();
            var component=product switch {".NET SDK"=>"sdk",".NET Runtime"=>"runtime",".NET Desktop Runtime"=>"windowsdesktop",_=>"aspnetcore-runtime"};
            foreach(var r in j.RootElement.GetProperty("releases").EnumerateArray()) {
                if(!r.TryGetProperty(component,out var c) || c.GetProperty("version").GetString()!=latest)continue;
                var file=c.GetProperty("files").EnumerateArray().FirstOrDefault(f=>f.GetProperty("rid").GetString()=="win-"+architecture && f.GetProperty("url").GetString()!.EndsWith(".exe",StringComparison.OrdinalIgnoreCase));
                if(file.ValueKind==JsonValueKind.Undefined)throw new IOException(UiText.T("Error_4e1d69341dd1"));
                var url=new Uri(file.GetProperty("url").GetString()!);if(!DownloadSafety.IsAllowed(url,url))throw new IOException(UiText.T("Error_d813f4caca79"));
                var hash=file.GetProperty("hash").GetString();if(hash?.Length!=128 || !hash.All(Uri.IsHexDigit))throw new IOException(UiText.T("Error_07a8308e3dd4"));
                return new DownloadFile(url,".exe",Sha512:hash);
            }
            throw new IOException(UiText.T("Error_30f3d0eb2f5b"));
        }
        if(product=="Windows Terminal") {
            if(release!="Latest")throw new ArgumentException(UiText.T("Error_b9b0c0a413fb"));
            using var j=await GetJsonAsync(client,"https://api.github.com/repos/microsoft/terminal/releases/latest",token);
            var files=j.RootElement.GetProperty("assets").EnumerateArray().Where(x=>x.GetProperty("name").GetString()!.EndsWith("_"+architecture+".zip",StringComparison.OrdinalIgnoreCase)).ToArray();
            var file=files.FirstOrDefault(x=>x.GetProperty("name").GetString()!.StartsWith("Microsoft.WindowsTerminal_",StringComparison.OrdinalIgnoreCase));
            if(file.ValueKind==JsonValueKind.Undefined)throw new IOException(UiText.T("Error_65d833230c2e"));
            var url=new Uri(file.GetProperty("browser_download_url").GetString()!);if(!DownloadSafety.IsAllowed(url,url))throw new IOException(UiText.T("Error_9c060f77f11a"));
            return new DownloadFile(url,".zip",file.TryGetProperty("digest",out var digest)?DownloadSafety.ParseSha256(digest.GetString()):null);
        }
        if(!(await ReleasesAsync(client,product,token)).Any(x=>x.Id==release))throw new ArgumentException(UiText.T("Error_7a766708206e"));
        var link=product switch {
            "Visual C++ Redistributable"=>$"https://aka.ms/vc14/vc_redist.{architecture}.exe",
            "Windows ADK"=>"https://go.microsoft.com/fwlink/?linkid="+(release=="26100"?"2289980":"2337875"),
            "Windows PE add-on"=>"https://go.microsoft.com/fwlink/?linkid="+(release=="26100"?"2289981":"2337681"),
            "SQL Server"=>"https://go.microsoft.com/fwlink/?linkid="+(release=="2025-StdDev"?"2344626":release=="2025-EntDev"?"2344711":"2215202"),
            "Visual Studio Community"=>$"https://aka.ms/vs/{(release=="stable"?"stable":"17/release")}/vs_community.exe",
            "Visual Studio Build Tools"=>$"https://aka.ms/vs/{(release=="stable"?"stable":"17/release")}/vs_BuildTools.exe",
            _=>throw new ArgumentException(UiText.T("Error_bed9f72719ca"))};
        return new DownloadFile(new Uri(link),Extension(product));
    }
    internal static async Task<JsonDocument> GetJsonAsync(HttpClient client,string url,CancellationToken token) {
        using var req=new HttpRequestMessage(HttpMethod.Get,url);req.Headers.UserAgent.ParseAdd("WindowsISODownloader/"+RuntimeInfo.Version);
        using var response=await client.SendAsync(req,token);response.EnsureSuccessStatusCode();return JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
    }
    internal static async Task VerifySha512Async(string path,string? expected,CancellationToken token) {
        if(expected==null)return;await using var input=File.OpenRead(path);if(!Convert.ToHexString(await SHA512.HashDataAsync(input,token)).Equals(expected,StringComparison.OrdinalIgnoreCase))throw new IOException(UiText.T("Error_19177e50e227"));
    }
}
