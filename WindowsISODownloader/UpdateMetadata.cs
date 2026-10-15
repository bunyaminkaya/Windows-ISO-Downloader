using System.Text.Json;
namespace WindowsISODownloader;
internal sealed record AvailableUpdate(string Tag,Version Version,Uri Page,Uri Package,long Size,string? Sha256,Uri? Checksums);
internal static class UpdateMetadata
{
    internal const string Repository="bunyaminkaya/Windows-ISO-Downloader";
    internal static AvailableUpdate? Parse(JsonElement release,string architecture,Version current) {
        if(release.GetProperty("draft").GetBoolean()||release.GetProperty("prerelease").GetBoolean())return null;
        var tag=release.GetProperty("tag_name").GetString()!;
        if(!Version.TryParse(tag.TrimStart('v','V'),out var version)||version<=current)return null;
        var page=new Uri(release.GetProperty("html_url").GetString()!);
        if(page.Scheme!="https"||page.Host!="github.com"||page.UserInfo.Length!=0||page.Port!=443||!page.AbsolutePath.Equals("/"+Repository+"/releases/tag/"+Uri.EscapeDataString(tag),StringComparison.Ordinal))throw new ArgumentException(UiText.T("Error_8957b3703ab6"));
        var assets=release.GetProperty("assets").EnumerateArray().ToArray();
        var names=new[]{"Windows-ISO-Downloader-win-"+architecture+"-IMZALI.zip","Windows-ISO-Downloader-V"+version.ToString(3)+"-Windows-"+architecture+"-IMZALI.zip","Windows-ISO-Downloader-win-"+architecture+".zip","Windows-ISO-Downloader-V"+version.ToString(3)+"-Windows-"+architecture+".zip"};
        var package=names.Select(name=>assets.FirstOrDefault(x=>x.GetProperty("name").GetString()!.Equals(name,StringComparison.OrdinalIgnoreCase))).FirstOrDefault(x=>x.ValueKind!=JsonValueKind.Undefined);
        if(package.ValueKind==JsonValueKind.Undefined)throw new ArgumentException(UiText.T("Error_4e27a202225e"));
        var url=new Uri(package.GetProperty("browser_download_url").GetString()!);
        if(!DownloadSafety.IsAllowed(url,url)||url.Host!="github.com"||!url.AbsolutePath.StartsWith("/"+Repository+"/releases/download/"+Uri.EscapeDataString(tag)+"/",StringComparison.Ordinal))throw new ArgumentException(UiText.T("Error_f17601483005"));
        long size=package.GetProperty("size").GetInt64();if(size<1_000_000||size>300_000_000)throw new ArgumentException(UiText.T("Error_3cc539f1879f"));
        string? digest=package.TryGetProperty("digest",out var d)?DownloadSafety.ParseSha256(d.GetString()):null;
        var checksum=assets.FirstOrDefault(x=>x.GetProperty("name").GetString()=="SHA256SUMS.txt");Uri? checksums=null;
        if(checksum.ValueKind!=JsonValueKind.Undefined){checksums=new Uri(checksum.GetProperty("browser_download_url").GetString()!);if(!DownloadSafety.IsAllowed(url,checksums))throw new ArgumentException(UiText.T("Error_2acee35b4512"));}
        return new AvailableUpdate(tag,version,page,url,size,digest,checksums);
    }
    internal static string ReadChecksum(string text,string file) {
        foreach(var line in text.Split('\n')) {var parts=line.Trim().Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);if(parts.Length==2&&parts[1].TrimStart('*')==file)return DownloadSafety.ParseSha256(parts[0])!;}
        throw new ArgumentException(UiText.T("Error_43051911cf9c"));
    }
}
