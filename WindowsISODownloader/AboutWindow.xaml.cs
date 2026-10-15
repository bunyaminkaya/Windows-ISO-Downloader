using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace WindowsISODownloader;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        Title = App.T("Text_740cb53032c2");
        AboutVersionText.Text = App.T("Text_92b2c95e99f0") + RuntimeInfo.Version;
        DeveloperText.Text = App.T("Text_ef7cd3929976");
        EmailLabel.Text = App.T("Text_620b685b667f");
        AboutNoticeText.Text = App.T("Text_9e2483cc592f");
        CloseButton.Content = App.T("Text_7d9eb7acb13e");
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(this, App.T("Text_76a113fb9d7b") + ex.Message, App.T("Text_4efca0d10c5f"), MessageBoxButton.OK, MessageBoxImage.Warning); }
        e.Handled = true;
    }
}
