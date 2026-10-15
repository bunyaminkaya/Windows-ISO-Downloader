using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
namespace WindowsISODownloader;
public partial class MainWindow
{
    private OfficeOptions? _officeOptions;
    private string? _officeConfig;
    private string _officeOptionsProduct="";
    private void OfficeOptionsButton_Click(object sender, RoutedEventArgs e) {
        var product=SelectedProduct(); if(product is not ("Office" or "Office 2019") || _cancel!=null)return;
        var panel=new StackPanel {Margin=new Thickness(20)};
        panel.Children.Add(new TextBlock {Text=App.T("Text_1a85300df2ca"),FontSize=16});
        var channel=new ComboBox {Margin=new Thickness(0,8,0,12)};
        foreach(var id in product=="Office 2019"?new[]{"PerpetualVL2019"}:new[]{"Current","MonthlyEnterprise","SemiAnnual"})channel.Items.Add(new ComboBoxItem {Content=id,Tag=id});
        channel.SelectedIndex=0;if(_officeOptionsProduct==product && _officeOptions!=null)SelectTag(channel,_officeOptions.Channel);panel.Children.Add(channel);
        panel.Children.Add(new TextBlock {Text=App.T("Text_d19b293941c5"),FontSize=16});
        var languages=new List<CheckBox>();var languagePanel=new WrapPanel();
        foreach(var id in OfficeOptions.LanguageIds){var c=new CheckBox {Content=id,Tag=id,IsChecked=_officeOptionsProduct==product && _officeOptions!=null?_officeOptions.Languages.Contains(id):id==SelectedLanguage(),Margin=new Thickness(0,8,18,8)};c.SetResourceReference(Control.ForegroundProperty,"TextBrush");languages.Add(c);languagePanel.Children.Add(c);}panel.Children.Add(languagePanel);
        panel.Children.Add(new TextBlock {Text=App.T("Text_d3ca50807393"),FontSize=16,Margin=new Thickness(0,12,0,0)});
        var excluded=new List<CheckBox>();var excludePanel=new WrapPanel();foreach(var id in OfficeOptions.AppIds){var c=new CheckBox {Content=id,IsChecked=_officeOptionsProduct==product && _officeOptions?.ExcludedApps.Contains(id)==true,Margin=new Thickness(0,8,18,8)};c.SetResourceReference(Control.ForegroundProperty,"TextBrush");excluded.Add(c);excludePanel.Children.Add(c);}panel.Children.Add(excludePanel);
        var visio=new CheckBox {Content="Visio Professional",IsChecked=_officeOptionsProduct==product && _officeOptions?.Visio==true,Margin=new Thickness(0,12,0,8)};visio.SetResourceReference(Control.ForegroundProperty,"TextBrush");var project=new CheckBox {Content="Project Professional",IsChecked=_officeOptionsProduct==product && _officeOptions?.Project==true,Margin=new Thickness(0,0,0,12)};project.SetResourceReference(Control.ForegroundProperty,"TextBrush");panel.Children.Add(visio);panel.Children.Add(project);
        panel.Children.Add(new TextBlock {Text=App.T("Text_52438fa9dd5b"),TextWrapping=TextWrapping.Wrap});
        var dialog=new Window {Owner=this,Title=App.T("Text_e29b2717dcaf"),Width=660,Height=600,WindowStartupLocation=WindowStartupLocation.CenterOwner,Content=new ScrollViewer {Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}};dialog.SetResourceReference(Window.BackgroundProperty,"BgBrush");
        var save=new Button {Content=App.T("Text_3ff9272cc9e5"),Margin=new Thickness(0,18,0,0)};save.Click+=(_,_)=>{try{var value=new OfficeOptions(((ComboBoxItem)channel.SelectedItem).Tag.ToString()!,languages.Where(x=>x.IsChecked==true).Select(x=>x.Tag.ToString()!).ToArray(),excluded.Where(x=>x.IsChecked==true).Select(x=>x.Content.ToString()!).ToArray(),visio.IsChecked==true,project.IsChecked==true);value.Validate(product);_officeOptions=value;_officeOptionsProduct=product;DetailsText.Text=value.Channel+" • "+string.Join(", ",value.Languages)+(value.Visio?" • Visio":"")+(value.Project?" • Project":"");dialog.Close();}catch(Exception ex){MessageBox.Show(dialog,ex.Message);}};panel.Children.Add(save);dialog.ShowDialog();
    }
    private async void InstallOfficeButton_Click(object sender,RoutedEventArgs e) {
        if(_officeConfig==null || !File.Exists(_officeConfig))return;
        BeginResultAction();InstallOfficeButton.IsEnabled=false;
        try {
            var folder=Path.GetDirectoryName(_officeConfig)!;var setup=Path.Combine(folder,"setup.exe");using var fileLock=new FileStream(setup,FileMode.Open,FileAccess.Read,FileShare.Read);
            await VerifyMicrosoftSignatureAsync(setup,CancellationToken.None);
            using var resource=typeof(OfficeWorkspace).Assembly.GetManifestResourceStream("WindowsISODownloader.Office-Kur.template.ps1")!;using var reader=new StreamReader(resource);
            string code=(await reader.ReadToEndAsync()).Replace("$PSScriptRoot", "'"+folder.Replace("'","''")+"'");
            var psi=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,WorkingDirectory=folder};foreach(var arg in new[]{"-NoProfile","-EncodedCommand",Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(code))})psi.ArgumentList.Add(arg);
            Process.Start(psi);
        }catch(Exception ex){MessageBox.Show(this,ex.Message);}finally{InstallOfficeButton.IsEnabled=true;EndResultAction();}
    }
}
