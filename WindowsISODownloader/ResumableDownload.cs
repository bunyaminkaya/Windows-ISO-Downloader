using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
namespace WindowsISODownloader;
internal sealed record TransferProgress(long Received, long? Total, double BytesPerSecond, int Attempt);
internal sealed record ResumeMetadata(string Identity, string? ETag, long? Total);
internal static class ResumableDownload
{
    internal static async Task<string> RunAsync(HttpClient client, Uri uri, string partial, string identity, string extension,
        long minimum, string? expectedHash, Action<TransferProgress> progress, CancellationToken token, TimeSpan? retryDelay = null)
    {
        string sidecar = partial + ".json";
        for (int attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                ResumeMetadata? old = null;
                try { old = JsonSerializer.Deserialize<ResumeMetadata>(await File.ReadAllTextAsync(sidecar, token)); } catch (Exception ex) when (ex is IOException or JsonException) { }
                long offset = File.Exists(partial) ? new FileInfo(partial).Length : 0;
                bool canResume = old?.Identity == identity && System.Net.Http.Headers.EntityTagHeaderValue.TryParse(old.ETag, out var validator) && !validator.IsWeak && validator.Tag != "*" && (!old.Total.HasValue || old.Total > offset) && offset > 0;
                if (!canResume) offset = 0;
                using var response = await DownloadSafety.OpenAsync(client, uri, token, offset, offset > 0 ? old!.ETag : null);
                if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable) { Delete(sidecar); throw new IOException(UiText.T("Text_0f7eecd29cde")); }
                response.EnsureSuccessStatusCode();
                var final = response.RequestMessage?.RequestUri ?? uri;
                if (!DownloadSafety.HasExpectedFormat(uri,final,response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName,extension)) throw new InvalidDataException(UiText.T("Text_e19876e427ee"));
                long? total = response.Content.Headers.ContentLength;
                string? etag = response.Headers.ETag is { IsWeak: false } tag ? tag.ToString() : null;
                if (response.StatusCode == HttpStatusCode.PartialContent)
                {
                    var range = response.Content.Headers.ContentRange;
                    if (offset == 0 || range?.From != offset || !range!.Length.HasValue || (old!.Total.HasValue && range.Length != old.Total) || etag != old.ETag || range.To < range.From || range.To >= range.Length || response.Content.Headers.ContentLength != range.To - range.From + 1)
                    { Delete(partial); Delete(sidecar); throw new InvalidDataException(UiText.T("Text_c37ccc46aee1")); }
                    total = range.Length;
                }
                else { offset = 0; }
                if (total.HasValue && total < minimum) throw new InvalidDataException(UiText.T("Text_fa100960595c"));
                DownloadSafety.CheckSpace(partial, total.HasValue ? total - offset : null);

                await using (var input = await response.Content.ReadAsStreamAsync(token))
                await using (var output = new FileStream(partial, offset > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, 1024 * 1024, true))
                {
                    await File.WriteAllTextAsync(sidecar, JsonSerializer.Serialize(new ResumeMetadata(identity, etag, total)), token);
                    var buffer = new byte[1024 * 1024]; long received = offset; long transferred = 0;
                    var watch = Stopwatch.StartNew(); var lastReport = TimeSpan.Zero;
                    while (true)
                    {
                        int count = await DownloadSafety.ReadChunkAsync(input, buffer, TimeSpan.FromSeconds(120), token);
                        if (count == 0) break;
                        await output.WriteAsync(buffer.AsMemory(0, count), token);
                        received += count; transferred += count;
                        if (total.HasValue && received > total.Value) { Delete(sidecar); throw new InvalidDataException(UiText.T("Text_1dd9f2981ef7")); }
                        if (watch.Elapsed - lastReport > TimeSpan.FromMilliseconds(250))
                        { progress(new(received, total, transferred / Math.Max(0.001, watch.Elapsed.TotalSeconds), attempt)); lastReport = watch.Elapsed; }
                    }
                    await output.FlushAsync(token);
                    if (received < minimum || total.HasValue && received != total.Value) throw new IOException(UiText.T("Text_a7b22e40b62f"));
                    progress(new(received, total, transferred / Math.Max(0.001, watch.Elapsed.TotalSeconds), attempt));
                }
                await using var file = File.OpenRead(partial);
                string hash = Convert.ToHexString(await SHA256.HashDataAsync(file, token));
                if (expectedHash != null && !hash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                { Delete(sidecar); throw new InvalidDataException(UiText.T("Text_4efe679cdde8")); }
                Delete(sidecar);
                return hash;
            }
            catch (InvalidDataException) { Delete(sidecar); Delete(partial); throw; }
            catch (Exception ex) when (!token.IsCancellationRequested && attempt < 3 && ex is IOException or HttpRequestException or TaskCanceledException)
            {
                progress(new(File.Exists(partial) ? new FileInfo(partial).Length : 0, null, 0, attempt + 1));
                await Task.Delay(retryDelay ?? TimeSpan.FromSeconds(2 << attempt), token);
            }
        }
        throw new IOException(UiText.T("Text_4a4e022d5374"));
    }
    private static void Delete(string path) { try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
}
