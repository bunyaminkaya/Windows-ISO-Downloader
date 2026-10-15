using WindowsISODownloader;
if(args.Length==2 && args[0]=="--export-links"){System.IO.File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(ServerCatalog.AllLinks().Select(x=>x.AbsoluteUri)));return;}
if(args.Length==2 && args[0]=="--resolve-products") {
    using var client=new System.Net.Http.HttpClient(){Timeout=TimeSpan.FromSeconds(45)};var resolved=new List<object>();
    foreach(var product in AdditionalCatalog.Products) {
        var releases=await AdditionalCatalog.ReleasesAsync(client,product,default);
        foreach(var release in releases)foreach(var arch in AdditionalCatalog.Architectures(product)) {
            var file=await AdditionalCatalog.ResolveAsync(client,product,release.Id,arch,default);resolved.Add(new{product,release=release.Label,architecture=arch,url=file.Url.AbsoluteUri,extension=file.Extension,sha256=file.Sha256,sha512=file.Sha512});
        }
    }
    System.IO.File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(resolved,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"PASS: {resolved.Count} official product selections resolved from live metadata; no installer downloaded.");return;
}
static void Check(bool value, string message) { if (!value) throw new Exception(message); }
foreach (var release in new[] { "2025", "2022", "2019", "2016", "2012R2", "2012" })
{
    Check(ServerCatalog.GetLanguages(release).Count == 8, "Missing Server media language");
    foreach (var language in ServerCatalog.GetLanguages(release))
    {
    var uri = ServerCatalog.GetIso(release, language);
    Check(uri.Scheme == "https", "Insecure source");
    Check(uri.Host == "aka.ms" || uri.Host.EndsWith(".microsoft.com"), "Untrusted ISO host");
    }
    Check(ServerCatalog.GetLanguages(release).Select(l => ServerCatalog.GetIso(release, l)).Distinct().Count() == 8, "Language selection must change the download address");
}
foreach (var (release, count) in new[] { ("2008R2VHD", 3), ("2008VHD", 3), ("2003R2VHD", 4) })
{
    var files = ServerCatalog.GetBundle(release);
    Check(files.Count == count && files.Distinct().Count() == count, "Missing or duplicate VHD part");
    Check(files[0].AbsolutePath.EndsWith(".part01.exe"), "Missing extraction entry point");
    Check(files.Skip(1).All(u => u.AbsolutePath.EndsWith(".rar")), "Wrong continuation part format");
    Check(files.All(u => u.Host == "download.microsoft.com" && u.Scheme == "https"), "Untrusted VHD source");
}
foreach (var (release, language) in new[] { ("2022", "trtr"), ("2025", "trtr"), ("invalid", "enus") })
{
    bool rejected = false;
    try { ServerCatalog.GetIso(release, language); } catch (ArgumentException) { rejected = true; }
    Check(rejected, "Unsupported release/language must fail rather than return a different ISO");
}
Check(!ServerCatalog.IsBundle("2012R2") && ServerCatalog.IsBundle("2003R2VHD"), "Wrong media type");
Console.WriteLine("PASS: ISO selection, all VHD parts, HTTPS sources, invalid language/release rejection.");

await DownloadTests.RunAsync();

await ResumeTests.RunAsync();
await AdvancedTests.RunAsync();
