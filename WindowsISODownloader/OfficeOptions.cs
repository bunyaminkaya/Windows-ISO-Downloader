using System.Xml.Linq;
namespace WindowsISODownloader;
internal sealed record OfficeOptions(string Channel, string[] Languages, string[] ExcludedApps, bool Visio, bool Project)
{
    internal static readonly string[] LanguageIds = { "tr-tr", "en-us", "de-de", "fr-fr", "es-es", "it-it" };
    internal static readonly string[] AppIds = { "Access", "Publisher", "Excel", "Word", "PowerPoint", "Outlook", "OneNote", "OneDrive", "Teams" };
    internal void Validate(string product) {
        if (product is not ("Office" or "Office 2019")) throw new ArgumentException(UiText.T("Error_bedcf718604a"));
        if (product == "Office 2019" ? Channel != "PerpetualVL2019" : Channel is not ("Current" or "MonthlyEnterprise" or "SemiAnnual")) throw new ArgumentException(UiText.T("Error_1616b7cca539"));
        if (Languages.Length == 0 || Languages.Any(l => !LanguageIds.Contains(l)) || Languages.Distinct().Count()!=Languages.Length) throw new ArgumentException(UiText.T("Error_51435d3f8de3"));
        if (ExcludedApps.Any(a => !AppIds.Contains(a))) throw new ArgumentException(UiText.T("Error_193338a20be3"));
    }
    internal XElement CreateAdd(string folder, string architecture, string product) {
        Validate(product);
        if (architecture is not ("64" or "32")) throw new ArgumentException(UiText.T("Error_cc5f48ac8f87"));
        var add=new XElement("Add",new XAttribute("OfficeClientEdition",architecture),new XAttribute("Channel",Channel),new XAttribute("SourcePath",folder));
        string[] ids = {product=="Office 2019"?"ProPlus2019Volume":"O365ProPlusRetail"};
        if(Visio)ids=ids.Append(product=="Office 2019"?"VisioPro2019Volume":"VisioProRetail").ToArray();
        if(Project)ids=ids.Append(product=="Office 2019"?"ProjectPro2019Volume":"ProjectProRetail").ToArray();
        foreach(var id in ids) {var entry=new XElement("Product",new XAttribute("ID",id),Languages.Select(l=>new XElement("Language",new XAttribute("ID",l))));if(id==ids[0])foreach(var app in ExcludedApps.Distinct())entry.Add(new XElement("ExcludeApp",new XAttribute("ID",app)));add.Add(entry);}
        return add;
    }
}
