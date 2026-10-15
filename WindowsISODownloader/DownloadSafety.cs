using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
namespace WindowsISODownloader;
internal static class DownloadSafety
{
    internal static bool IsMicrosoft(Uri uri) => uri.Host.Equals("microsoft.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".microsoft.com", StringComparison.OrdinalIgnoreCase);
    internal static bool IsAllowed(Uri origin, Uri next)
    {
        if (next.Scheme != "https" || next.UserInfo.Length != 0 || next.Port != 443) return false;
        if (origin.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            return IsGitHubReleasePath(origin.AbsolutePath) &&
                (next.Host == "github.com" && IsGitHubReleasePath(next.AbsolutePath) || next.Host is "release-assets.githubusercontent.com" or "objects.githubusercontent.com");
        if (origin.Host == "download.sysinternals.com") return next.Host == "download.sysinternals.com" || IsMicrosoft(next);
        if (origin.Host == "update.code.visualstudio.com") return next.Host == origin.Host || IsMicrosoft(next);
        if (IsMicrosoft(origin) || origin.Host == "aka.ms") return IsMicrosoft(next) || next.Host == "aka.ms";
        return false;
    }
    internal static bool IsGitHubReleasePath(string path) => new[]{"/microsoft/PowerToys/releases/download/","/microsoft/terminal/releases/download/","/bunyaminkaya/Windows-ISO-Downloader/releases/download/"}.Any(p=>path.StartsWith(p,StringComparison.Ordinal));
    internal static bool HasExpectedFormat(Uri origin,Uri final,string? dispositionName,string extension) {
        if(!IsAllowed(origin,final))return false;
        if(final.AbsolutePath.EndsWith(extension,StringComparison.OrdinalIgnoreCase))return true;
        if(origin.Host!="github.com" || !IsGitHubReleasePath(origin.AbsolutePath) || final.Host is not ("release-assets.githubusercontent.com" or "objects.githubusercontent.com"))return false;
        string original=Uri.UnescapeDataString(Path.GetFileName(origin.AbsolutePath));string name=(dispositionName??"").Trim('"');
        return original.EndsWith(extension,StringComparison.OrdinalIgnoreCase) && name.IndexOfAny(new[]{'/','\\'})<0 && name.Equals(original,StringComparison.OrdinalIgnoreCase);
    }
    internal static async Task<HttpResponseMessage> OpenAsync(HttpClient client, Uri origin, CancellationToken token, long offset = 0, string? etag = null)
    {
        var current = origin;
        for (int hop = 0; hop <= 10; hop++)
        {
            if (!IsAllowed(origin, current)) throw new IOException(UiText.T("Text_86ff063c34b6") + current.Host);
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            if (offset > 0 && etag != null) {
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(offset, null);
                request.Headers.IfRange = new System.Net.Http.Headers.RangeConditionHeaderValue(new System.Net.Http.Headers.EntityTagHeaderValue(etag));
            }
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308)
            {
                var location = response.Headers.Location;
                response.Dispose();
                if (location == null) throw new IOException(UiText.T("Text_92d3e63f37ac"));
                current = location.IsAbsoluteUri ? location : new Uri(current, location);
                continue;
            }
            return response;
        }
        throw new IOException(UiText.T("Text_0fccf3569e46"));
    }
    internal static async Task<int> ReadChunkAsync(Stream input, byte[] buffer, TimeSpan timeout, CancellationToken token)
    {
        using var idle = CancellationTokenSource.CreateLinkedTokenSource(token);
        idle.CancelAfter(timeout);
        try { return await input.ReadAsync(buffer, idle.Token); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { throw new IOException(UiText.T("Text_6704bc43917d")); }
    }
    internal static void CheckSpace(string path, long? length)
    {
        if (!length.HasValue || path.StartsWith(@"\\", StringComparison.Ordinal) || Path.GetFullPath(path).StartsWith(@"\\", StringComparison.Ordinal)) return;
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))!);
            if (drive.AvailableFreeSpace < length.Value + 100_000_000) throw new IOException(UiText.T("Text_6ed45e24c780"));
        }
        catch (ArgumentException) { } // Non-drive paths: the write operation reports actual storage errors.
    }
    internal static bool MatchesHash(byte[] hash, string expected) => Convert.ToHexString(hash).Equals(expected, StringComparison.OrdinalIgnoreCase);
    internal static string? ParseSha256(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)) value = value[7..];
        if (value.Length != 64 || !value.All(Uri.IsHexDigit)) throw new IOException(UiText.T("Text_90297601967c"));
        return value;
    }
}
