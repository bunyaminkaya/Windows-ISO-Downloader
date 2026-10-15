using System.IO;
using System.Text.Json;
namespace WindowsISODownloader;
internal sealed record UserSelections(string Product, string Architecture, string Language, string Folder)
{
    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WID", "selections.json");
    internal static UserSelections? Load() {try {return JsonSerializer.Deserialize<UserSelections>(File.ReadAllText(FilePath));} catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) {return null;} }
    internal void Save() {try {Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);File.WriteAllText(FilePath,JsonSerializer.Serialize(this));} catch(Exception ex) when (ex is IOException or UnauthorizedAccessException) { } }
}
