using System.Globalization;
using System.Resources;
namespace WindowsISODownloader;
internal static class UiText
{
    internal static string Language { get; set; } = "tr";
    private static readonly ResourceManager Resources = new("WindowsISODownloader.Strings",typeof(UiText).Assembly);
    internal static string T(string key) => Resources.GetString(key,CultureInfo.GetCultureInfo(Language)) ?? throw new MissingManifestResourceException("Missing UI resource: "+key);
}
