using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using WindowsISODownloader;
internal static class DownloadTests
{
    static int count;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; }
    internal static async Task RunAsync()
    {
        var microsoft = new Uri("https://go.microsoft.com/fwlink/?linkid=1");
        foreach (var destination in new[] { "https://download.microsoft.com/test.iso", "https://software.download.prss.microsoft.com/test.iso" }) Check(DownloadSafety.IsAllowed(microsoft, new(destination)), destination);
        foreach (var destination in new[] { "http://download.microsoft.com/test.iso", "https://microsoft.com.evil.org/test.iso", "https://evil-microsoft.com/test.iso", "https://user:password@download.microsoft.com/test.iso", "https://download.microsoft.com:444/test.iso", "https://github.com/random/file" }) Check(!DownloadSafety.IsAllowed(microsoft, new(destination)), "Bad destination accepted");
        var sys = new Uri("https://download.sysinternals.com/files/SysinternalsSuite.zip");
        Check(DownloadSafety.IsAllowed(sys, sys), "Sysinternals rejected");
        var github = new Uri("https://github.com/microsoft/PowerToys/releases/download/v1/test.exe");
        Check(DownloadSafety.IsAllowed(github, new("https://release-assets.githubusercontent.com/test")), "PowerToys CDN rejected");
        Check(!DownloadSafety.IsAllowed(new("https://github.com/attacker/PowerToys/releases/download/v1/test.exe"), new("https://release-assets.githubusercontent.com/test")), "Wrong repository accepted");
        using var handler = new FakeHandler((uri, n) => n == 1 ? Redirect("https://evil.org/test.iso") : new(HttpStatusCode.OK));
        using var client = new HttpClient(handler);
        bool failed = false; try { using var response = await DownloadSafety.OpenAsync(client, microsoft, default); } catch (IOException) { failed = true; }
        Check(failed && handler.Calls == 1, "Untrusted redirect contacted");
        using var loop = new FakeHandler((uri, n) => Redirect("https://go.microsoft.com/loop"));
        using var loopClient = new HttpClient(loop);
        failed = false; try { using var response = await DownloadSafety.OpenAsync(loopClient, microsoft, default); } catch (IOException) { failed = true; }
        Check(failed && loop.Calls == 11, "Redirect loop not bounded");
        using var relative = new FakeHandler((uri, n) => n == 1 ? Redirect("/files/Suite.zip") : new(HttpStatusCode.OK));
        using var relativeClient = new HttpClient(relative);
        using (var response = await DownloadSafety.OpenAsync(relativeClient, sys, default)) Check(relative.Calls == 2 && response.StatusCode == HttpStatusCode.OK, "Relative redirect failed");
        DownloadSafety.CheckSpace(@"\\server\share\test.iso", 10_000_000_000L); count++;
        var hash = SHA256.HashData(new byte[] {1,2,3});
        Check(DownloadSafety.MatchesHash(hash, Convert.ToHexString(hash).ToLowerInvariant()), "Hash comparison failed");
        Check(!DownloadSafety.MatchesHash(hash, new string('0',64)), "Tampered hash accepted");
        Check(DownloadSafety.ParseSha256("sha256:" + Convert.ToHexString(hash)) == Convert.ToHexString(hash), "Digest prefix failed");
        failed = false; try { DownloadSafety.ParseSha256("sha256:bad"); } catch (IOException) { failed = true; } Check(failed,"Malformed hash accepted");
        var file=Path.GetTempFileName();
        try {
            using (var resource = typeof(RuntimeInfo).Assembly.GetManifestResourceStream("WindowsISODownloader.Fido.ps1")!) using (var output = File.Create(file)) resource.CopyTo(output);
            RuntimeInfo.VerifyFido(file); count++;
            File.AppendAllText(file, "tampered"); failed=false;try { RuntimeInfo.VerifyFido(file); } catch(IOException) { failed=true; } Check(failed,"Modified Fido accepted");
        } finally {File.Delete(file);}
        using var blocked = new BlockedStream();
        failed=false;try { await DownloadSafety.ReadChunkAsync(blocked,new byte[10],TimeSpan.FromMilliseconds(50),default); } catch(IOException) {failed=true;}Check(failed,"Inactivity timeout failed");
        using var canceled = new CancellationTokenSource();canceled.Cancel();failed=false;try {await DownloadSafety.ReadChunkAsync(blocked,new byte[10],TimeSpan.FromSeconds(5),canceled.Token);}catch(OperationCanceledException){failed=true;}Check(failed,"User cancellation misreported");
        UiText.Language = "tr";
        failed = false; try { DownloadSafety.ParseSha256("bad"); } catch (IOException ex) { failed = ex.Message.Contains("Yayıncının"); } Check(failed, "Turkish error text missing");
        UiText.Language = "en";
        failed = false; try { DownloadSafety.ParseSha256("bad"); } catch (IOException ex) { failed = ex.Message.Contains("publisher"); } Check(failed, "English error text missing");
        UiText.Language = "tr";
        var workspace = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        try {
            var config = Path.Combine(workspace, "office-configuration-current.xml");
            var extracted = Path.Combine(workspace, "OfficeDeploymentTool-current");
            var payload = Path.Combine(workspace, "Office");
            var existing = Path.Combine(workspace, "office-configuration-old.xml");
            Directory.CreateDirectory(extracted); Directory.CreateDirectory(payload);
            File.WriteAllText(config, "config"); File.WriteAllText(existing, "existing"); File.WriteAllText(Path.Combine(extracted,"setup.exe"), "stub");
            OfficeWorkspace.PrepareInstaller(config, Path.Combine(extracted, "setup.exe"));
            Check(File.Exists(Path.Combine(workspace,"setup.exe")) && File.Exists(Path.Combine(workspace,"Office-Kur.cmd")) && File.Exists(Path.Combine(workspace,"Office-Kur.ps1")), "Office installation entry points missing");
            var otherConfig = OfficeWorkspace.CreateConfiguration(workspace, "64", "tr-tr", "Office 2019");
            var thirdConfig = OfficeWorkspace.CreateConfiguration(workspace, "64", "en-us", "Office");
            Check(otherConfig != thirdConfig && Path.GetFileName(otherConfig) == "configuration.xml", "Office packages must be isolated and have a stable configuration filename");
            var xml = System.Xml.Linq.XDocument.Load(otherConfig);
            Check(xml.Root!.Element("Add")!.Element("Product")!.Attribute("ID")!.Value == "ProPlus2019Volume", "Wrong Office edition configuration");
            OfficeWorkspace.Cleanup(config, extracted);
            Check(File.Exists(config) && !Directory.Exists(extracted), "Office configuration must survive temporary cleanup");
            Check(File.Exists(existing) && Directory.Exists(payload), "Unrelated Office files deleted");
        } finally { Directory.Delete(workspace, true); }
        using var bodyHandler = new FakeHandler((uri, n) => new HttpResponseMessage(HttpStatusCode.OK) {Content = new StreamContent(new DelayedStream())});
        using var bodyClient = new HttpClient(bodyHandler) { Timeout = TimeSpan.FromMilliseconds(30) };
        using (var response = await DownloadSafety.OpenAsync(bodyClient, sys, default)) {
            using var body = await response.Content.ReadAsStreamAsync();
            var received = await DownloadSafety.ReadChunkAsync(body, new byte[10], TimeSpan.FromSeconds(2), default);
            Check(received == 3, "Header timeout incorrectly applied to body read");
        }
        var now = DateTime.UtcNow;
        var cache = new TimedMetadataCache(TimeSpan.FromMinutes(5), () => now);
        int fetches = 0;
        var gate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = cache.GetAsync("shared", () => {fetches++;return gate.Task;}, default);
        var second = cache.GetAsync("shared", () => {fetches++;return gate.Task;}, default);
        gate.SetResult(42);
        Check(await first == 42 && await second == 42 && fetches == 1, "Concurrent cache lookup was duplicated");
        Check(await cache.GetAsync("shared", () => Task.FromResult(99), default) == 42, "Cached result not reused");
        now = now.AddMinutes(6);
        Check(await cache.GetAsync("shared", () => Task.FromResult(99), default) == 99, "Expired metadata not refreshed");
        failed = false;
        try {await cache.GetAsync<int>("failure", () => Task.FromException<int>(new IOException("mock")), default);} catch(IOException) {failed=true;}
        Check(failed && await cache.GetAsync("failure", () => Task.FromResult(7), default) == 7, "Failed metadata was retained");
        using var waiter = new CancellationTokenSource();
        var pending = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceledLookup = cache.GetAsync("cancel", () => pending.Task, waiter.Token);
        waiter.Cancel(); failed = false; try {await canceledLookup;} catch(OperationCanceledException) {failed=true;}
        pending.SetResult(8);
        Check(failed && await cache.GetAsync("cancel", () => Task.FromResult(9), default) == 8, "One canceled waiter canceled shared metadata");
        var selection = new UserSelections("Windows 11", "ARM64", "Turkish", @"C:\İndirilenler");
        Check(System.Text.Json.JsonSerializer.Deserialize<UserSelections>(System.Text.Json.JsonSerializer.Serialize(selection)) == selection, "Selections serialization roundtrip failed");
        Console.WriteLine($"PASS: {count} download-security/integrity/timeout checks.");
    }
    static HttpResponseMessage Redirect(string to) {var response=new HttpResponseMessage(HttpStatusCode.Redirect);response.Headers.Location=new Uri(to,UriKind.RelativeOrAbsolute);return response;}
    sealed class FakeHandler(Func<Uri,int,HttpResponseMessage> action) : HttpMessageHandler
    {
        internal int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token) {var response=action(request.RequestUri!,++Calls);response.RequestMessage=request;return Task.FromResult(response);}
    }
    sealed class DelayedStream() : MemoryStream(new byte[] {1,2,3})
    {
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default) {await Task.Delay(100,token);return await base.ReadAsync(buffer, token);}
    }
    sealed class BlockedStream : MemoryStream
    {
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default) {await Task.Delay(Timeout.Infinite,token);return 0;}
    }
}
