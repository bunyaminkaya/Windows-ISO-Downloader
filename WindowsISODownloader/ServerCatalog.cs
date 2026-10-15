namespace WindowsISODownloader;

// Public Microsoft evaluation media. Languages are mapped to their own official media; no English fallback.
internal static class ServerCatalog
{
    public static readonly Uri Server2025Vhdx = new("https://aka.ms/WinServ2025vhd-enus");
    private static readonly Dictionary<string,Uri> VirtualDisks=new() {
        ["2025VHDX"]=Server2025Vhdx,
        ["2022VHD"]=new("https://go.microsoft.com/fwlink/p/?linkid=2195166&clcid=0x409&culture=en-us&country=us"),
        ["2019VHD"]=new("https://go.microsoft.com/fwlink/p/?linkid=2195334&clcid=0x409&culture=en-us&country=us")};
    public static bool IsVirtualDisk(string release)=>VirtualDisks.ContainsKey(release);
    public static Uri GetVirtualDisk(string release,string language) => language=="enus" && VirtualDisks.TryGetValue(release,out var link)?link:throw new ArgumentException(UiText.T("ServerVirtualError"));
    public static IEnumerable<Uri> AllLinks() => IsoLinks.Values.SelectMany(x=>x.Values).Select(x=>new Uri(x)).Concat(new[]{PosReady2009}.Concat(VirtualDisks.Values)).Concat(new[]{"2008R2VHD","2008VHD","2003R2VHD"}.SelectMany(GetBundle)).Distinct();
    public static readonly Uri PosReady2009 = new("https://download.microsoft.com/download/6/3/6/636e5b38-ea04-4f25-a059-ca6edf773f79/POSready2009_CD.iso");
    public static bool IsBundle(string release) => release is "2008R2VHD" or "2008VHD" or "2003R2VHD";
    private static readonly Dictionary<string, Dictionary<string, string>> IsoLinks = new()
    {
        ["2022"] = new()
        {
            ["enus"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195280&clcid=0x409&culture=en-us&country=US",
            ["zhcn"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195280&clcid=0x804&culture=zh-cn&country=CN",
            ["frfr"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195280&clcid=0x40c&culture=fr-fr&country=FR",
            ["dede"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195280&clcid=0x407&culture=de-de&country=DE",
            ["itit"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195280&clcid=0x410&culture=it-it&country=IT",
            ["jajp"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195280&clcid=0x411&culture=ja-jp&country=JP",
            ["ruru"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195280&clcid=0x419&culture=ru-ru&country=RU",
            ["eses"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195280&clcid=0x40a&culture=es-es&country=ES",
        },
        ["2019"] = new()
        {
            ["enus"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195167&clcid=0x409&culture=en-us&country=US",
            ["zhcn"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195167&clcid=0x804&culture=zh-cn&country=CN",
            ["frfr"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195167&clcid=0x40c&culture=fr-fr&country=FR",
            ["dede"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195167&clcid=0x407&culture=de-de&country=DE",
            ["itit"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195167&clcid=0x410&culture=it-it&country=IT",
            ["jajp"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195167&clcid=0x411&culture=ja-jp&country=JP",
            ["ruru"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195167&clcid=0x419&culture=ru-ru&country=RU",
            ["eses"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195167&clcid=0x40a&culture=es-es&country=ES",
        },
        ["2016"] = new()
        {
            ["enus"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195174&clcid=0x409&culture=en-us&country=US",
            ["zhcn"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195174&clcid=0x804&culture=zh-cn&country=CN",
            ["frfr"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195174&clcid=0x40c&culture=fr-fr&country=FR",
            ["dede"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195174&clcid=0x407&culture=de-de&country=DE",
            ["itit"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195174&clcid=0x410&culture=it-it&country=IT",
            ["jajp"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195174&clcid=0x411&culture=ja-jp&country=JP",
            ["ruru"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195174&clcid=0x419&culture=ru-ru&country=RU",
            ["eses"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195174&clcid=0x40a&culture=es-es&country=ES",
        },
        ["2012R2"] = new()
        {
            ["enus"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195443&clcid=0x409&culture=en-us&country=US",
            ["zhcn"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195443&clcid=0x804&culture=zh-cn&country=CN",
            ["frfr"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195443&clcid=0x40c&culture=fr-fr&country=FR",
            ["dede"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195443&clcid=0x407&culture=de-de&country=DE",
            ["itit"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195443&clcid=0x410&culture=it-it&country=IT",
            ["jajp"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195443&clcid=0x411&culture=ja-jp&country=JP",
            ["ruru"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195443&clcid=0x419&culture=ru-ru&country=RU",
            ["eses"] = "https://go.microsoft.com/fwlink/p/?LinkID=2195443&clcid=0x40a&culture=es-es&country=ES",
        },
        ["2012"] = new()
        {
            ["enus"] = "https://download.microsoft.com/download/6/D/A/6DAB58BA-F939-451D-9101-7DE07DC09C03/9200.16384.WIN8_RTM.120725-1247_X64FRE_SERVER_EVAL_EN-US-HRM_SSS_X64FREE_EN-US_DV5.ISO",
            ["zhcn"] = "https://download.microsoft.com/download/2/8/7/287E63DD-3A27-4469-9094-5F7B7C4BF828/9200.16384.WIN8_RTM.120725-1247_X64FRE_SERVER_EVAL_ZH-CN-HRM_SSS_X64FREE_ZH-CN_DV5.ISO",
            ["frfr"] = "https://download.microsoft.com/download/C/8/3/C8330E01-F91D-4232-9BC8-BBC75514B1D2/9200.16384.WIN8_RTM.120725-1247_X64FRE_SERVER_EVAL_FR-FR-HRM_SSS_X64FREE_FR-FR_DV5.ISO",
            ["dede"] = "https://download.microsoft.com/download/7/C/6/7C6A5A9A-321B-4EF8-95FD-0A483C1EBDC2/9200.16384.WIN8_RTM.120725-1247_X64FRE_SERVER_EVAL_DE-DE-HRM_SSS_X64FREE_DE-DE_DV5.ISO",
            ["itit"] = "https://download.microsoft.com/download/6/6/F/66F1747B-1F39-4D4A-846E-D4F75F019B80/9200.16384.WIN8_RTM.120725-1247_X64FRE_SERVER_EVAL_IT-IT-HRM_SSS_X64FREE_IT-IT_DV5.ISO",
            ["jajp"] = "https://download.microsoft.com/download/A/D/C/ADC0B6E5-26E6-4084-BAF4-38180C9C9721/9200.16384.WIN8_RTM.120725-1247_X64FRE_SERVER_EVAL_JA-JP-HRM_SSS_X64FREE_JA-JP_DV5.ISO",
            ["ruru"] = "https://download.microsoft.com/download/2/C/E/2CE2D1FC-D4F2-4F91-BF53-FCBB8960D2F1/9200.16384.WIN8_RTM.120725-1247_X64FRE_SERVER_EVAL_RU-RU-HRM_SSS_X64FREE_RU-RU_DV5.ISO",
            ["eses"] = "https://download.microsoft.com/download/9/7/2/97274F04-05B9-488E-A96F-5B29C14E4D31/9200.16384.WIN8_RTM.120725-1247_X64FRE_SERVER_EVAL_ES-ES-HRM_SSS_X64FREE_ES-ES_DV5.ISO",
        },
        ["2025"] = new()
        {
            ["enus"] = "https://aka.ms/WinServ2025iso-enus",
            ["zhcn"] = "https://aka.ms/WinServ2025iso-zhcn",
            ["frfr"] = "https://aka.ms/WinServ2025iso-frfr",
            ["dede"] = "https://aka.ms/WinServ2025iso-dede",
            ["itit"] = "https://aka.ms/WinServ2025iso-itit",
            ["jajp"] = "https://aka.ms/WinServ2025iso-jajp",
            ["ruru"] = "https://aka.ms/WinServ2025iso-ruru",
            ["eses"] = "https://aka.ms/WinServ2025iso-eses",
        },
    };
    public static IReadOnlyCollection<string> GetLanguages(string release) =>
        IsoLinks.TryGetValue(release, out var links) ? links.Keys.ToArray() :
        (IsBundle(release) || IsVirtualDisk(release)) ? new[] { "enus" } : throw new ArgumentException("Unknown Server release.");
    public static Uri GetIso(string release, string language)
    {
        if (!IsoLinks.TryGetValue(release, out var links) || !links.TryGetValue(language, out var link))
            throw new ArgumentException("Unsupported Server release or language.");
        return new Uri(link);
    }
    public static IReadOnlyList<Uri> GetBundle(string release)
    {
        var (root, prefix, count) = release switch
        {
            "2008R2VHD" => ("https://download.microsoft.com/download/5/4/c/54c15fa1-b3aa-4a8b-b26c-47c3ba7a20e0/", "WS2008R2Fullx64Ent", 3),
            "2008VHD" => ("https://download.microsoft.com/download/e/2/f/e2f0eb28-6223-4e70-b8ae-024f129cdcdb/", "TD01WS08SFR2SP1x64", 3),
            "2003R2VHD" => ("https://download.microsoft.com/download/8/d/9/8d912056-c2ad-4fbe-b84c-40a152c589c2/", "Win2k3R2EE", 4),
            _ => throw new ArgumentException("Unknown VHD release.")
        };
        return Enumerable.Range(1, count).Select(i => new Uri(root + prefix + $".part{i:00}" + (i == 1 ? ".exe" : ".rar"))).ToArray();
    }
    public static string GetSource(string release) => "https://www.microsoft.com/en-us/download/details.aspx?id=" + (release switch
    {
        "2008R2VHD" => "2227", "2008VHD" => "23163", "2003R2VHD" => "19727",
        _ => throw new ArgumentException("Unknown VHD release.")
    });
}
