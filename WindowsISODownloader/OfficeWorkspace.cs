using System.IO;
namespace WindowsISODownloader;
internal static class OfficeWorkspace
{
    internal static string CreateConfiguration(string parent, string architecture, string language, string product, OfficeOptions? options = null)
    {
        var folder = Path.Combine(parent, "Office-Paket-" + Guid.NewGuid().ToString("N"));
        options ??= new OfficeOptions(product == "Office 2019" ? "PerpetualVL2019" : "Current", new[]{language}, Array.Empty<string>(), false, false);
        options.Validate(product);
        Directory.CreateDirectory(folder);
        var xml = new System.Xml.Linq.XDocument(new System.Xml.Linq.XElement("Configuration",options.CreateAdd(folder,architecture,product)));
        var config = Path.Combine(folder, "configuration.xml");
        xml.Save(config);
        return config;
    }
    internal static void PrepareInstaller(string config, string extractedSetup)
    {
        var folder = Path.GetDirectoryName(config)!;
        File.Copy(extractedSetup, Path.Combine(folder, "setup.exe"), false);
        using var script = typeof(OfficeWorkspace).Assembly.GetManifestResourceStream("WindowsISODownloader.Office-Kur.template.ps1") ?? throw new IOException(UiText.T("Text_a6e5cf9f35c5"));
        using (var output = File.Create(Path.Combine(folder, "Office-Kur.ps1"))) script.CopyTo(output);
        File.WriteAllText(Path.Combine(folder, "Office-Kur.cmd"), "@echo off\r\ncd /d \"%~dp0\"\r\npowershell.exe -NoProfile -ExecutionPolicy Bypass -File \"%~dp0Office-Kur.ps1\"\r\nif errorlevel 1 pause\r\n", System.Text.Encoding.ASCII);
        File.WriteAllText(Path.Combine(folder, "KURULUM.txt"), "Office-Kur.cmd dosyasını çalıştırın. Microsoft imzası kontrol edilir ve yönetici onayı istenir. Office, setup.exe, configuration.xml ve betikler aynı klasörde kalmalıdır. Lisans/etkinleştirme hakkı ayrıca gerekir. / Run Office-Kur.cmd. Microsoft signature verification and administrator consent are required. Keep the complete folder together. A valid license is required.", new System.Text.UTF8Encoding(true));
    }
    internal static void Cleanup(string config, string extracted)
    {
        // Configuration is part of the durable installation package, not a temporary file.
        try { if (Directory.Exists(extracted)) Directory.Delete(extracted, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
