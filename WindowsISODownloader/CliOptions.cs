namespace WindowsISODownloader;
internal sealed class CliOptions
{
    internal readonly Dictionary<string,string> Values=new(StringComparer.Ordinal);
    internal readonly HashSet<string> Flags=new(StringComparer.Ordinal);
    internal static readonly Dictionary<string,string> Products=new(StringComparer.OrdinalIgnoreCase) {
        ["win11"]="Windows 11",["win10"]="Windows 10",["server"]="Windows Server",["ltsc"]="Windows 11 LTSC",["posready"]="Windows Embedded POSReady 2009",["office"]="Office",["office2019"]="Office 2019",["office2016"]="Office 2016",["edge"]="Microsoft Edge",["powertoys"]="Microsoft PowerToys",["vscode"]="Visual Studio Code",["sysinternals"]="Sysinternals Suite",["vcredist"]="Visual C++ Redistributable",["terminal"]="Windows Terminal",["adk"]="Windows ADK",["winpe"]="Windows PE add-on",["sql"]="SQL Server",["vs-community"]="Visual Studio Community",["vs-buildtools"]="Visual Studio Build Tools",["dotnet-sdk"]=".NET SDK",["dotnet-runtime"]=".NET Runtime",["dotnet-desktop"]=".NET Desktop Runtime",["dotnet-aspnet"]=".NET ASP.NET Core Runtime"};
    internal string Get(string name,string fallback="")=>Values.GetValueOrDefault(name,fallback);
    internal static CliOptions Parse(string[] args) {
        var result=new CliOptions();var flags=new[]{"--json","--help","--list","--url-only","--force","--visio","--project"};var values=new[]{"--product","--release","--arch","--lang","--out","--channel","--exclude","--languages","--expected-sha256","--ui-lang"};
        for(int i=0;i<args.Length;i++) {
            var arg=args[i];if(result.Flags.Contains(arg)||result.Values.ContainsKey(arg))throw new ArgumentException(UiText.T("Error_19d23d726484")+arg);
            if(flags.Contains(arg)){result.Flags.Add(arg);continue;}
            if(!values.Contains(arg)||i+1==args.Length||args[i+1].StartsWith("--"))throw new ArgumentException(UiText.T("Error_0e5529944971")+arg);
            result.Values[arg]=args[++i];
        }
        if(!result.Flags.Contains("--help") && (!result.Values.TryGetValue("--product",out var p)||!Products.ContainsKey(p)))throw new ArgumentException(UiText.T("Error_26956722ea03"));
        if(result.Values.TryGetValue("--ui-lang",out var l)&&l is not ("tr" or "en"))throw new ArgumentException(UiText.T("Error_13df419af5c6"));
        if(result.Flags.Contains("--list")&&result.Flags.Contains("--url-only"))throw new ArgumentException(UiText.T("Error_a3c8fd732526"));
        if(result.Values.TryGetValue("--product",out var selected) && Products.TryGetValue(selected,out var product) && product is not ("Office" or "Office 2019") && (new[]{"--channel","--exclude","--languages"}.Any(result.Values.ContainsKey)||result.Flags.Contains("--visio")||result.Flags.Contains("--project")))throw new ArgumentException(UiText.T("Error_4869b84831be"));
        return result;
    }
}
