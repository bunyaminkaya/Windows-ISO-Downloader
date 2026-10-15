using System.Reflection;
using System.IO;
using System.Security.Cryptography;
namespace WindowsISODownloader;
internal static class RuntimeInfo
{
    internal static string Version => typeof(RuntimeInfo).Assembly.GetName().Version?.ToString(3) ?? "unknown";
    internal static void VerifyFido(string path)
    {
        using var embedded = typeof(RuntimeInfo).Assembly.GetManifestResourceStream("WindowsISODownloader.Fido.ps1") ?? throw new IOException(UiText.T("Text_04cf48bf6082"));
        using var external = File.OpenRead(path);
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(embedded), SHA256.HashData(external)))
            throw new IOException(UiText.T("Text_19b2dd70dce6"));
    }
}
