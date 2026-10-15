using System.Windows.Controls;
using System.IO;
namespace WindowsISODownloader;
public partial class MainWindow
{
    private int _additionalGeneration;
    private async Task RefreshAdditionalProductAsync(string product) {
        int generation=++_additionalGeneration;
        try {
            var releases=await ReleaseCache.GetAsync("additional:"+product,()=>AdditionalCatalog.ReleasesAsync(Client,product,CancellationToken.None),CancellationToken.None);
            if(SelectedProduct()!=product || generation!=_additionalGeneration)return;
            VersionCombo.Items.Clear();foreach(var release in releases)Add(VersionCombo,release.Label,release.Id);VersionCombo.SelectedIndex=0;
            DownloadButton.IsEnabled=_cancel==null && _resultActions==0 && releases.Length>0;
        } catch(Exception ex) {if(SelectedProduct()==product && generation==_additionalGeneration){VersionCombo.Items.Clear();DownloadButton.IsEnabled=false;DetailsText.Text=ex.Message;}}
    }
}
