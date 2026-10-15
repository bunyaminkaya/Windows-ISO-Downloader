using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;

namespace WindowsISODownloader;

public partial class MainWindow : Window
{
    private static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = true })
    {
        Timeout = TimeSpan.FromSeconds(45)
    };
    private CancellationTokenSource? _cancel;
    internal void CancelActiveOperation() => _cancel?.Cancel();
    private static readonly HttpClient DownloadClient = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(45) };
    private static readonly TimedMetadataCache ReleaseCache = new();

    public MainWindow()
    {
        InitializeComponent();
        AdaptiveWindow.Attach(this);
        VersionCombo.SelectionChanged += LtscVersionCombo_SelectionChanged;
        UiLanguageCombo.SelectedIndex = App.Language == "en" ? 1 : 0;
        App.LanguageChanged += ApplyLanguage;
        Closed += (_, _) => App.LanguageChanged -= ApplyLanguage;
        ApplyLanguage();
        ThemeCombo.SelectedIndex = App.ThemeMode switch { "Dark" => 1, "Light" => 2, _ => 0 };
        App.ThemeChanged += ApplyTitleBarTheme;
        SourceInitialized += (_, _) => ApplyTitleBarTheme();
        Closed += (_, _) => App.ThemeChanged -= ApplyTitleBarTheme;
        ApplyTitleBarTheme();
        InitializeFeatures();
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private void ApplyTitleBarTheme()
    {
        LogoLightLetters.Visibility = App.IsDark ? Visibility.Visible : Visibility.Collapsed;
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        int enabled = App.IsDark ? 1 : 0;
        try { _ = DwmSetWindowAttribute(hwnd, 20, ref enabled, sizeof(int)); }
        catch (Exception ex) { App.WriteCrash("Başlık çubuğu tema ayarı: " + ex.Message); }
    }

    private void UiLanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UiLanguageCombo?.SelectedItem is ComboBoxItem item && item.Tag is string language)
            App.SetLanguage(language);
    }

    private void ApplyLanguage()
    {
        if (UiLanguageCombo == null || ProductCombo == null || VersionCombo == null) return;
        LocalizeFeatures();
        SubtitleText.Text = App.T("Text_2d8a366eb0c0");
        ThemeLabel.Text = App.T("Text_efb52e7172b7");
        InterfaceLanguageLabel.Text = App.T("Text_8027f432a7fb");
        ((ComboBoxItem)ThemeCombo.Items[0]).Content = App.T("Text_6725e7bbcd28");
        ((ComboBoxItem)ThemeCombo.Items[1]).Content = App.T("Text_60acc53f13a5");
        ((ComboBoxItem)ThemeCombo.Items[2]).Content = App.T("Text_dbcd5e7bb7a0");
        SelectionHeading.Text = App.T("Text_85e83fbc5d62");
        ProductLabel.Text = App.T("Text_fb9ef894175c");
        VersionLabel.Text = App.T("Text_dd167905de0d");
        ArchitectureLabel.Text = App.T("Text_cd74053c5481");
        FileLanguageLabel.Text = App.T("Text_4c88961929c3");
        DownloadButtonText.Text = App.T("Text_9de4149fb971");
        CancelButton.Content = App.T("Text_858e4ba7a29f");
        DownloadHeading.Text = App.T("Text_728a3f41089b");
        AboutButton.Content = App.T("Text_4efca0d10c5f");
        FooterText.Text = "";
        PromotionText.Text = App.T("Text_039b3f90960c");
        VisitSiteButton.Content = App.T("Text_1836a03f7a4a");
        var release = SelectedRelease();
        var architecture = SelectedArchitecture();
        var fileLanguage = SelectedLanguage();
        if (_cancel == null)
        {
            ProductCombo_SelectionChanged(ProductCombo, new SelectionChangedEventArgs(Selector.SelectionChangedEvent, new List<object>(), new List<object>()));
            SelectTag(VersionCombo, release);
            SelectTag(ArchitectureCombo, architecture);
            SelectTag(LanguageCombo, fileLanguage);
        }
    }

    private static void SelectTag(ComboBox combo, string tag)
    {
        foreach (ComboBoxItem item in combo.Items)
            if (item.Tag?.ToString() == tag) { combo.SelectedItem = item; return; }
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is ComboBoxItem item && item.Tag is string mode)
            App.SetTheme(mode);
    }

    private void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ArchitectureCombo == null || VersionCombo == null || LanguageCombo == null || DownloadButton == null) return;
        var product = SelectedProduct();
        VersionCombo.Items.Clear();
        ArchitectureCombo.Items.Clear();
        LanguageCombo.Items.Clear();
        switch (product)
        {
            case "Windows 11":
                Add(VersionCombo, App.T("Text_ea38a28c2860"), "Latest");
                Add(ArchitectureCombo, "x64", "x64"); Add(ArchitectureCombo, "ARM64", "ARM64");
                AddCommonLanguages();
                ProductInfoText.Text = App.T("Text_689dcb008144");
                break;
            case "Windows 11 LTSC":
                Add(VersionCombo, "Enterprise LTSC 2024 Evaluation", "Enterprise");
                Add(VersionCombo, "IoT Enterprise LTSC 2024 Evaluation", "IoT");
                Add(ArchitectureCombo, "x64", "x64");
                Add(LanguageCombo, "English (US)", "en-us");
                ProductInfoText.Text = App.T("Text_ba36ec507c4d");
                break;
            case "Windows 10":
                Add(VersionCombo, App.T("Text_a7bd59bd7841"), "Latest");
                Add(ArchitectureCombo, "x64", "x64");
                AddCommonLanguages();
                ProductInfoText.Text = App.T("Text_6be464afdc14");
                break;
            case "Windows Server":
                Add(VersionCombo, App.T("Text_c83a971301f0"), "2025");
                foreach (var release in new[] { "2022", "2019", "2016", "2012R2", "2012" })
                    Add(VersionCombo, "Server " + release.Replace("R2", " R2") + " Evaluation (ISO)", release);
                Add(VersionCombo, "Server 2025 Evaluation (VHDX, English)", "2025VHDX");
                Add(VersionCombo, "Server 2022 Evaluation (VHD, English)", "2022VHD");
                Add(VersionCombo, "Server 2019 Evaluation (VHD, English)", "2019VHD");
                Add(VersionCombo, "Server 2008 R2 Enterprise x64 (VHD)", "2008R2VHD");
                Add(VersionCombo, "Server 2008 Standard x64 (VHD)", "2008VHD");
                Add(VersionCombo, "Server 2003 R2 Enterprise (VHD)", "2003R2VHD");
                _ = UpdateServerVersionAsync();
                Add(ArchitectureCombo, "x64", "x64");
                foreach (var (label, id) in ServerLanguages) Add(LanguageCombo, label, id);
                ProductInfoText.Text = App.T("Text_80e495730238");
                break;
            case "Windows Embedded POSReady 2009":
                Add(VersionCombo, "POSReady 2009 Evaluation (ISO)", "2009");
                Add(ArchitectureCombo, "x86", "x86");
                Add(LanguageCombo, "English", "enus");
                ProductInfoText.Text = App.T("Text_fe517d63a730");
                break;
            case "Microsoft Edge":
                Add(VersionCombo, App.T("Text_2d83ea2acfb0"), "Stable");
                Add(ArchitectureCombo, "x64", "x64"); Add(ArchitectureCombo, "ARM64", "arm64"); Add(ArchitectureCombo, "x86", "x86");
                Add(LanguageCombo, App.T("Text_36f4f3933670"), "multi");
                ProductInfoText.Text = App.T("Text_1335b4801d43");
                _ = UpdateEdgeVersionAsync();
                break;
            case "Microsoft PowerToys":
                Add(VersionCombo, App.T("Text_98354f359c8e"), "Latest");
                Add(ArchitectureCombo, "x64", "x64"); Add(ArchitectureCombo, "ARM64", "arm64");
                Add(LanguageCombo, App.T("Text_bb9755f32f23"), "multi");
                ProductInfoText.Text = App.T("Text_e89a97c91ead");
                _ = UpdatePowerToysVersionAsync();
                break;
            case "Visual Studio Code":
                Add(VersionCombo, App.T("Text_dfa893bf446c"), "Latest");
                Add(ArchitectureCombo, "x64", "x64"); Add(ArchitectureCombo, "ARM64", "arm64");
                Add(LanguageCombo, App.T("Text_bb9755f32f23"), "multi");
                ProductInfoText.Text = App.T("Text_2fc122b8eb2d");
                break;
            case "Sysinternals Suite":
                Add(VersionCombo, App.T("Text_bf9e126de78c"), "Latest");
                Add(ArchitectureCombo, "x64 / ARM64", "multi");
                Add(LanguageCombo, App.T("Text_bb9755f32f23"), "multi");
                ProductInfoText.Text = App.T("Text_793cda129d8c");
                break;
            case "Office 2019":
                Add(VersionCombo, "Office Professional Plus 2019 (Volume)", "2019");
                Add(ArchitectureCombo, "x64", "64"); Add(ArchitectureCombo, "x86", "32");
                AddOfficeLanguages();
                ProductInfoText.Text = App.T("Text_064b9a994007");
                break;
            case "Office 2016":
                Add(VersionCombo, "Office 2016 Professional Plus Retail", "ProPlusRetail");
                Add(VersionCombo, "Office 2016 Home & Business Retail", "HomeBusinessRetail");
                Add(VersionCombo, "Office 2016 Home & Student Retail", "HomeStudentRetail");
                Add(ArchitectureCombo, App.T("Text_cae9d8e32c71"), "multi");
                AddOfficeLanguages();
                ProductInfoText.Text = App.T("Text_13e0aceef568");
                break;
            default:
                if(AdditionalCatalog.Supports(product)) {
                    Add(VersionCombo,App.T("Text_6ddbf8cd2a7b"),"Loading");
                    foreach(var arch in AdditionalCatalog.Architectures(product))Add(ArchitectureCombo,arch=="auto"?App.T("Text_4be503939e49"):arch.ToUpperInvariant(),arch);
                    Add(LanguageCombo,App.T("Text_e115ea135e29"),"multi");
                    ProductInfoText.Text=App.T("Text_ac9852af71ab");
                    ProductInfoText.Text=product=="Windows Terminal"?App.T("AdditionalTerminal"):product=="SQL Server"?App.T("AdditionalSql"):product.StartsWith(".NET",StringComparison.Ordinal)?App.T("AdditionalNet"):App.T("AdditionalGeneral");
                    _=RefreshAdditionalProductAsync(product);
                }
                break;
            case "Office":
                Add(VersionCombo, "Microsoft 365 Apps (ODT)", "M365");
                Add(ArchitectureCombo, "x64 / ARM64 (Microsoft 365)", "64"); Add(ArchitectureCombo, "x86", "32");
                AddOfficeLanguages();
                ProductInfoText.Text = App.T("Text_3c52000302e4");
                _ = UpdateOfficeVersionAsync();
                break;
        }
        if(OfficeOptionsButton!=null) OfficeOptionsButton.Visibility=product is "Office" or "Office 2019"?Visibility.Visible:Visibility.Collapsed;
        VersionCombo.SelectedIndex = 0;
        ArchitectureCombo.SelectedIndex = 0;
        if (AdditionalCatalog.Supports(product)) SelectTag(ArchitectureCombo, System.Runtime.InteropServices.RuntimeInformation.OSArchitecture==Architecture.Arm64?"arm64":"x64");
        if (product is "Windows 11" or "Microsoft Edge" or "Microsoft PowerToys" or "Visual Studio Code") SelectTag(ArchitectureCombo, _savedSelections?.Architecture ?? (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "ARM64" : "x64"));
        LanguageCombo.SelectedIndex = 0;
        DownloadButton.IsEnabled = _cancel == null && _resultActions == 0;
        if (product is "Windows 11" or "Windows 10") { _ = RefreshWindowsReleaseAsync(); _ = AddHistoricalReleasesAsync(product); }
        if (_cancel == null)
        {
            StatusText.Text = App.T("Text_ce313ea4ba99");
            DetailsText.Text = product switch
            {
                "Office" or "Office 2019" => App.T("Text_37a5701905d4"),
                "Office 2016" => App.T("Text_97a72f1fc34a"),
                "Windows 11 LTSC" => App.T("Text_d33569ce0348"),
                "Windows Server" => ServerCatalog.IsBundle(SelectedRelease())||ServerCatalog.IsVirtualDisk(SelectedRelease())?ProductInfoText.Text:App.T("ServerCore"),
                "Microsoft Edge" => App.T("Text_fadc4f73fa74"),
                "Microsoft PowerToys" or "Visual Studio Code" => App.T("Text_99254275a6b4"),
                "Sysinternals Suite" => App.T("Text_65fd4fc0f852"),
                _ => AdditionalCatalog.Supports(product)?ProductInfoText.Text:App.T("Text_a9cfda34d193")
            };
        }
    }

    private void LtscVersionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ArchitectureCombo == null || LanguageCombo == null) return;
        if (SelectedProduct() == "Windows Server")
        {
            if (VersionCombo.SelectedItem == null) return;
            var release = SelectedRelease();
            var language = SelectedLanguage();
            ArchitectureCombo.Items.Clear();
            // Microsoft does not explicitly identify the guest architecture of the 2003 R2 VHD.
            Add(ArchitectureCombo, release == "2003R2VHD" ? App.T("Text_629f75d4a6aa") : "x64", release == "2003R2VHD" ? "Package" : "x64");
            ArchitectureCombo.SelectedIndex = 0;
            LanguageCombo.Items.Clear();
            var availableLanguages = ServerCatalog.GetLanguages(release);
            foreach (var (label, id) in ServerLanguages)
                if (availableLanguages.Contains(id)) Add(LanguageCombo, label, id);
            SelectTag(LanguageCombo, language);
            if (LanguageCombo.SelectedIndex < 0) LanguageCombo.SelectedIndex = 0;
            ProductInfoText.Text = ServerCatalog.IsVirtualDisk(release)?App.T("Text_ab275fdd73ae"):ServerCatalog.IsBundle(release)
                ? App.T("Text_15b25e830f69")
                : App.T("Text_72023bce815a");
            return;
        }
        if (SelectedProduct() != "Windows 11 LTSC") return;
        var prior = SelectedArchitecture();
        ArchitectureCombo.Items.Clear();
        Add(ArchitectureCombo, "x64", "x64");
        if (SelectedRelease() == "IoT") Add(ArchitectureCombo, "ARM64", "ARM64");
        ArchitectureCombo.SelectedIndex = prior == "ARM64" && ArchitectureCombo.Items.Count > 1 ? 1 : 0;
    }

    private void VisitSiteButton_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://bunyaminkaya.com/") { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, App.T("Text_fadfdd9e5130")); }
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e) => new AboutWindow { Owner = this }.ShowDialog();

    private void ArchitectureCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VersionCombo != null && ProductCombo != null && SelectedProduct() == "Windows 11")
            _ = RefreshWindowsReleaseAsync();
    }

    private sealed record WindowsRelease(string Version, int EditionId);

    private static Task<WindowsRelease> GetOfficialWindowsReleaseAsync(string product, string arch, CancellationToken token) => ReleaseCache.GetAsync("windows:" + product + ":" + arch, () => FetchGetOfficialWindowsReleaseAsync(product, arch, CancellationToken.None), token);

    private static async Task<WindowsRelease> FetchGetOfficialWindowsReleaseAsync(string product, string arch, CancellationToken token)
    {
        string page = product == "Windows 11"
            ? (arch == "ARM64" ? "windows11arm64" : "windows11")
            : "windows10iso";
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.microsoft.com/en-us/software-download/" + page);
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) WindowsISODownloader/" + RuntimeInfo.Version + "");
        request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(40));
        string html;
        try
        {
            using var response = await Client.SendAsync(request, timeout.Token);
            response.EnsureSuccessStatusCode();
            html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(timeout.Token));
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            throw new IOException(App.T("Text_c9d41df82dec"));
        }
        var versionMatch = Regex.Match(html, @"Version\s+(\d{2}H[12])", RegexOptions.IgnoreCase);
        if (!versionMatch.Success) throw new IOException(App.T("Text_a56ef00cb485"));
        var optionPattern = product == "Windows 10"
            ? @"<option\s+value=""(\d+)""[^>]*>Windows 10 \(multi-edition ISO\)"
            : arch == "ARM64"
                ? @"<option\s+value=""(\d+)""[^>]*>Windows 11 \(multi-edition ISO for Arm64\)"
                : @"<option\s+value=""(\d+)""[^>]*>Windows 11 \(multi-edition ISO for x64 devices\)";
        var option = Regex.Match(html, optionPattern, RegexOptions.IgnoreCase);
        if (!option.Success || !int.TryParse(option.Groups[1].Value, out var editionId))
            throw new IOException(App.T("Text_918c4a7d9a47"));
        return new WindowsRelease(versionMatch.Groups[1].Value.ToUpperInvariant(), editionId);
    }

    private async Task RefreshWindowsReleaseAsync()
    {
        var product = SelectedProduct();
        if (product is not ("Windows 11" or "Windows 10") || VersionCombo.Items.Count == 0) return;
        var arch = SelectedArchitecture();
        try
        {
            var release = await GetOfficialWindowsReleaseAsync(product, arch, CancellationToken.None);
            if (SelectedProduct() != product || SelectedArchitecture() != arch || VersionCombo.Items.Count == 0) return;
            (VersionCombo.Items[0] as ComboBoxItem)!.Content = product + " " + release.Version + " (Microsoft ISO)";
            (VersionCombo.Items[0] as ComboBoxItem)!.Tag = release.EditionId.ToString();
        }
        catch (Exception ex)
        {
            if (SelectedProduct() == product && SelectedArchitecture() == arch && VersionCombo.Items.Count > 0)
            {
                (VersionCombo.Items[0] as ComboBoxItem)!.Content = App.T("Text_0210456b4705");
                (VersionCombo.Items[0] as ComboBoxItem)!.Tag = "Latest";
                DetailsText.Text = ex.Message + App.T("Text_8f3c3876c216");
            }
        }
    }

    private static readonly (string Label, string Id)[] ServerLanguages =
    {
        ("English", "enus"), ("简体中文", "zhcn"), ("Français", "frfr"), ("Deutsch", "dede"),
        ("Italiano", "itit"), ("日本語", "jajp"), ("Русский", "ruru"), ("Español", "eses")
    };

    private static void Add(ComboBox box, string label, string id) => box.Items.Add(new ComboBoxItem { Content = label, Tag = id });
    private void AddCommonLanguages()
    {
        Add(LanguageCombo, App.T("Text_6e7aac65db01"), "Turkish"); Add(LanguageCombo, "English", "English");
        Add(LanguageCombo, "English International", "English International"); Add(LanguageCombo, "Deutsch", "German");
        Add(LanguageCombo, "Français", "French"); Add(LanguageCombo, "Español", "Spanish"); Add(LanguageCombo, "Italiano", "Italian");
    }
    private void AddOfficeLanguages()
    {
        Add(LanguageCombo, App.T("Text_6e7aac65db01"), "tr-tr"); Add(LanguageCombo, "English", "en-us");
        Add(LanguageCombo, "Deutsch", "de-de"); Add(LanguageCombo, "Français", "fr-fr");
        Add(LanguageCombo, "Español", "es-es"); Add(LanguageCombo, "Italiano", "it-it");
    }
    private async Task UpdateEdgeVersionAsync()
    {
        try
        {
            var release = await GetEdgeReleaseAsync("x64", CancellationToken.None);
            if (SelectedProduct() == "Microsoft Edge" && VersionCombo.Items.Count > 0)
                (VersionCombo.Items[0] as ComboBoxItem)!.Content = "Stable " + release.Version;
        }
        catch (Exception ex) { if (SelectedProduct() == "Microsoft Edge") DetailsText.Text = App.T("Text_843055c2a695") + ex.Message; }
    }
    private async Task UpdateServerVersionAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, "https://aka.ms/WinServ2025iso-enus");
            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var path = response.RequestMessage?.RequestUri?.AbsolutePath ?? "";
            var match = Regex.Match(path, @"/(\d+\.\d+)\.");
            if (SelectedProduct() == "Windows Server" && VersionCombo.Items.Count > 0 && (VersionCombo.Items[0] as ComboBoxItem)?.Tag?.ToString() == "2025")
                (VersionCombo.Items[0] as ComboBoxItem)!.Content = match.Success ? App.T("Text_1d7ecdebf191") + match.Groups[1].Value + ")" : "Server 2025 Evaluation";
        }
        catch (Exception ex) { if (SelectedProduct() == "Windows Server") DetailsText.Text = App.T("Text_c06398d11400") + ex.Message; }
    }

    private async Task UpdateOfficeVersionAsync()
    {
        try
        {
            var url = await GetOfficeToolUrlAsync(CancellationToken.None);
            var match = Regex.Match(url.AbsolutePath, @"officedeploymenttool_(\d+-\d+)", RegexOptions.IgnoreCase);
            if (SelectedProduct() == "Office" && match.Success)
                (VersionCombo.Items[0] as ComboBoxItem)!.Content = "Microsoft 365 Apps (ODT " + match.Groups[1].Value.Replace('-', '.') + ")";
        }
        catch (Exception ex) { if (SelectedProduct() == "Office") DetailsText.Text = App.T("Text_aef9627fbabf") + ex.Message; }
    }

    private async Task UpdatePowerToysVersionAsync()
    {
        try
        {
            var release = await GetPowerToysReleaseAsync(SelectedArchitecture(), CancellationToken.None);
            if (SelectedProduct() == "Microsoft PowerToys" && VersionCombo.Items.Count > 0)
                (VersionCombo.Items[0] as ComboBoxItem)!.Content = "PowerToys " + release.Version;
        }
        catch (Exception ex) { if (SelectedProduct() == "Microsoft PowerToys") DetailsText.Text = App.T("Text_694e0cba819f") + ex.Message; }
    }

    private string SelectedProduct() => (ProductCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Windows 11";
    private string SelectedRelease() => (VersionCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Latest";
    private string SelectedArchitecture() => (ArchitectureCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "x64";
    private string SelectedLanguage() => (LanguageCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Turkish";

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_cancel != null || _resultActions > 0 || VersionCombo.SelectedItem==null || SelectedRelease()=="Loading") return;
        var selectedProduct = SelectedProduct();
        var selectedRelease = SelectedRelease();
        var isBundle = selectedProduct == "Windows Server" && ServerCatalog.IsBundle(selectedRelease);
        var selectedArch = SelectedArchitecture();
        var selectedLanguage = SelectedLanguage();
        if (selectedProduct == "Windows 10" && selectedArch == "ARM64")
        {
            StatusText.Text = App.T("Text_fa0ed02a755b");
            return;
        }

        var extension = selectedProduct switch { "Office" or "Office 2019" or "Microsoft PowerToys" or "Visual Studio Code" => ".exe", "Microsoft Edge" => ".msi", "Sysinternals Suite" => ".zip", "Office 2016" => ".img", _ => ".iso" };
        if (AdditionalCatalog.Supports(selectedProduct))extension=AdditionalCatalog.Extension(selectedProduct);
        if(selectedProduct=="Windows Server" && ServerCatalog.IsVirtualDisk(selectedRelease))extension=selectedRelease=="2025VHDX"?".vhdx":".vhd";
        if (isBundle) extension = ".exe";
        var displayVersion = (VersionCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Latest";
        var versionMatch = Regex.Match(displayVersion, @"\d{2}H[12]");
        var safeVersion = versionMatch.Success ? versionMatch.Value : selectedProduct switch { "Windows Server" => selectedRelease, "Windows Embedded POSReady 2009" => "2009", "Microsoft Edge" => "Stable", "Office" or "Office 2019" => "ODT", "Windows 11 LTSC" => "2024", "Office 2016" => SelectedRelease(), _ => "Latest" };
        string target;
        if (isBundle)
        {
            var folder = new OpenFolderDialog { Title = App.T("Text_6fafcf6b261a") };
            if (folder.ShowDialog(this) != true) return;
            target = Path.Combine(folder.FolderName, "WindowsServer_" + selectedRelease + ".bundle");
        }
        else
        {
        var save = new SaveFileDialog
        {
            Title = App.T("Text_2a6f7e3ba15d"),
            InitialDirectory = Directory.Exists(_lastFolder) ? _lastFolder : "",
            Filter = extension switch { ".exe" => App.T("Text_b8966881fb0c"), ".msi" => "Windows Installer (*.msi)|*.msi", ".zip" => App.T("Text_a53ddca30276"), ".img" => App.T("Text_1cd89461cec7"), ".vhdx" => "Hyper-V disk (*.vhdx)|*.vhdx", ".vhd" => "Virtual disk (*.vhd)|*.vhd", _ => App.T("Text_db40405f2f37") },
            FileName = $"{selectedProduct.Replace(" ", "")}_{safeVersion}_{selectedLanguage.Replace(" ", "")}_{selectedArch}{extension}",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (save.ShowDialog(this) != true) return;
        target = save.FileName;
        }
        _lastFolder = Path.GetDirectoryName(target)!;
        PersistSelections();
        var partial = target + ".part";
        string? bundlePath = null;
        string? officePackage = null;
        bool savedThisAttempt = false;
        string? expectedHash = null;
        _cancel = new CancellationTokenSource();
        ResultPanel.Visibility = Visibility.Collapsed;
        _officeConfig=null;InstallOfficeButton.Visibility=Visibility.Collapsed;OfficeOptionsButton.IsEnabled=false;
        DownloadButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        ProductCombo.IsEnabled = VersionCombo.IsEnabled = ArchitectureCombo.IsEnabled = LanguageCombo.IsEnabled = UiLanguageCombo.IsEnabled = false;
        TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
        DownloadProgress.Value = 0;
        DownloadProgress.IsIndeterminate = true;
        try
        {
            StatusText.Text = App.T("Text_6355bb362165");
            DetailsText.Text = App.T("Text_7bc73f632810");
            if (isBundle)
            {
                bundlePath = await DownloadServerBundleAsync(selectedRelease, Path.GetDirectoryName(target)!, _cancel.Token);
            }
            else
            {
            var edge = selectedProduct == "Microsoft Edge" ? await GetEdgeReleaseAsync(selectedArch, _cancel.Token) : null;
            var powerToys = selectedProduct == "Microsoft PowerToys" ? await GetPowerToysReleaseAsync(selectedArch, _cancel.Token) : null;
            expectedHash = edge?.Sha256 ?? powerToys?.Sha256;
            var additional=AdditionalCatalog.Supports(selectedProduct)?await AdditionalCatalog.ResolveAsync(Client,selectedProduct,selectedRelease,selectedArch,_cancel.Token):null;
            expectedHash ??= additional?.Sha256;
            Uri url = additional?.Url ?? (selectedProduct switch
            {
                "Windows 11" or "Windows 10" => selectedRelease.StartsWith("Fido:", StringComparison.Ordinal) ? await ResolveHistoricalUrlAsync(selectedProduct, selectedRelease[5..], selectedArch, selectedLanguage, _cancel.Token) : await ResolveLatestWindowsUrlAsync(selectedProduct, selectedArch, selectedLanguage, _cancel.Token),
                "Windows Server" => ServerCatalog.IsVirtualDisk(selectedRelease)?ServerCatalog.GetVirtualDisk(selectedRelease,selectedLanguage):ServerCatalog.GetIso(selectedRelease, selectedLanguage),
                "Windows Embedded POSReady 2009" => ServerCatalog.PosReady2009,
                "Microsoft Edge" => edge!.Url,
                "Office" or "Office 2019" => await GetOfficeToolUrlAsync(_cancel.Token),
                "Windows 11 LTSC" => GetLtscUrl(selectedRelease, selectedArch),
                "Office 2016" => GetOffice2016Url(selectedRelease, selectedLanguage),
                "Microsoft PowerToys" => powerToys!.Url,
                "Visual Studio Code" => new Uri("https://update.code.visualstudio.com/latest/win32-" + selectedArch + "-user/stable"),
                "Sysinternals Suite" => new Uri("https://download.sysinternals.com/files/SysinternalsSuite.zip"),
                _ => throw new IOException(App.T("Text_f9787ca016c4"))
            });
            _cancel.Token.ThrowIfCancellationRequested();
            StatusText.Text = App.T("Text_15b3b491cd03");
            await DownloadFileAsync(url, partial, extension, _cancel.Token, expectedHash);
            try { await AdditionalCatalog.VerifySha512Async(partial,additional?.Sha512,_cancel.Token); }
            catch (IOException) {TryDelete(partial);throw;}
            File.Move(partial, target, true);
            savedThisAttempt = true;

            }
            if (selectedProduct is "Office" or "Office 2019")
            {
                var config = OfficeWorkspace.CreateConfiguration(Path.GetDirectoryName(target)!, selectedArch,selectedLanguage,selectedProduct,_officeOptionsProduct==selectedProduct?_officeOptions:null);
                officePackage = Path.GetDirectoryName(config);
                StatusText.Text = App.T("Text_f0b79bd33747");
                DownloadProgress.IsIndeterminate = true;
                await DownloadOfficePayloadAsync(target, config, _cancel.Token);
                _officeConfig=config;InstallOfficeButton.Visibility=Visibility.Visible;
            }
            if (savedThisAttempt && _lastHash != null) ShowCompletedFile(target, _lastHash);
            DownloadProgress.IsIndeterminate = false;
            DownloadProgress.Value = 100;
            StatusText.Text = App.T("Text_581ccff7f438");
            DetailsText.Text = selectedProduct is "Office" or "Office 2019" ? App.T("Text_be7e41a930f4") + officePackage + App.T("Text_7834e39f29bd") : bundlePath ?? target;
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = _cancel?.IsCancellationRequested == true ? App.T("Text_ec9841a5309a") : App.T("Text_e7a50435a351");
            DetailsText.Text = _cancel?.IsCancellationRequested == true ? (File.Exists(partial) || isBundle ? App.T("Text_73ac3b393fb5") : App.T("Text_b0af9a3b0566")) : App.T("Text_32487620277f");
            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
        }
        catch (Exception ex)
        {
            DownloadProgress.IsIndeterminate = false;
            StatusText.Text = (selectedProduct is "Office" or "Office 2019") && savedThisAttempt ? App.T("Text_40e6c9f09501") : App.T("Text_22edbe1fbf15");
            DetailsText.Text = ex.Message;
            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
        }
        finally
        {
            OfficeOptionsButton.IsEnabled=true;
            _cancel.Dispose();
            _cancel = null;
            DownloadButton.IsEnabled = _resultActions == 0;
            CancelButton.IsEnabled = false;
            ProductCombo.IsEnabled = VersionCombo.IsEnabled = ArchitectureCombo.IsEnabled = LanguageCombo.IsEnabled = UiLanguageCombo.IsEnabled = true;
            DownloadProgress.IsIndeterminate = false;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => _cancel?.Cancel();

    private async Task<Uri> ResolveLatestWindowsUrlAsync(string product, string arch, string language, CancellationToken token)
    {
        var release = await GetOfficialWindowsReleaseAsync(product, arch, token);
        if (SelectedProduct() == product && SelectedArchitecture() == arch && VersionCombo.Items.Count > 0)
        {
            (VersionCombo.Items[0] as ComboBoxItem)!.Content = product + " " + release.Version + " (Microsoft ISO)";
            (VersionCombo.Items[0] as ComboBoxItem)!.Tag = release.EditionId.ToString();
        }
        return await ResolveUrlAsync(product, release.EditionId, arch, language, token);
    }

    private static async Task<Uri> ResolveUrlAsync(string product, int editionId, string arch, string language, CancellationToken token, string release = "Latest")
    {
        var script = Path.Combine(AppContext.BaseDirectory, "Fido.ps1");
        if (!File.Exists(script)) throw new FileNotFoundException(App.T("Text_89528da3a485"));
        using var scriptLock = new FileStream(script, FileMode.Open, FileAccess.Read, FileShare.Read);
        RuntimeInfo.VerifyFido(script);
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var arg in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script,
                     "-Win", product == "Windows 11" ? "11" : "10", "-Rel", release,
                     "-Lang", language,
                     "-Arch", arch, "-PlatformArch", arch, "-GetUrl" })
            start.ArgumentList.Add(arg);
        if (editionId > 0) { start.ArgumentList.Add("-ProductEditionId"); start.ArgumentList.Add(editionId.ToString()); }
        using var process = Process.Start(start) ?? throw new IOException(App.T("Text_be296fc83a75"));
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        using var resolverTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        resolverTimeout.CancelAfter(TimeSpan.FromMinutes(3));
        try { await process.WaitForExitAsync(resolverTimeout.Token); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(true);
            throw;
        }
        var output = await outputTask;
        var error = await errorTask;
        var line = output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault(s => Uri.TryCreate(s, UriKind.Absolute, out var u) && IsMicrosoftIsoUrl(u));
        if (process.ExitCode != 0 || line == null || !Uri.TryCreate(line, UriKind.Absolute, out var uri))
        {
            var message = string.Join(" ", (output + " " + error).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            throw new IOException(App.T("Text_8448a5a88cec") + (message.Length > 400 ? message[..400] : message));
        }
        return uri;
    }

    private static bool IsMicrosoftIsoUrl(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps &&
        (uri.Host.Equals("microsoft.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".microsoft.com", StringComparison.OrdinalIgnoreCase)) &&
        uri.AbsolutePath.EndsWith(".iso", StringComparison.OrdinalIgnoreCase);

    private async Task<string> DownloadServerBundleAsync(string release, string parent, CancellationToken token)
    {
        var finalDirectory = Path.Combine(parent, "WindowsServer_" + release.Replace("VHD", "") + "_VHD");
        if (Directory.Exists(finalDirectory) || File.Exists(finalDirectory))
            throw new IOException(App.T("Text_c5debbade126") + finalDirectory);
        var staging = finalDirectory + ".download";
        var marker = Path.Combine(staging, ".wid-source");
        if (Directory.Exists(staging) && (!File.Exists(marker) || File.ReadAllText(marker) != release))
            throw new IOException(App.T("Text_78c3e7d67e3a"));
        Directory.CreateDirectory(staging);
        await File.WriteAllTextAsync(marker, release, token);
        try
        {
            var files = ServerCatalog.GetBundle(release);
            for (var index = 0; index < files.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                var uri = files[index];
                var name = Path.GetFileName(uri.AbsolutePath);
                var destination = Path.Combine(staging, name);
                StatusText.Text = App.T("Text_594e3032965c") + $"{index + 1}/{files.Count} — {name}";
                await DownloadFileAsync(uri, destination + ".part", Path.GetExtension(name), token);
                File.Move(destination + ".part", destination, true);
            }
            await File.WriteAllTextAsync(Path.Combine(staging, "README.txt"),
                "Bu paket ISO değildir / This package is not an ISO.\n" +
                "Tüm parçaları aynı klasörde tutun / Keep all parts in the same folder.\n" +
                "part01.exe kendiliğinden açılan arşivdir; uygulama bunu otomatik çalıştırmaz.\n" +
                "part01.exe is a self-extracting archive; the downloader does not execute it.\n" +
                "Çıkarılan VHD dosyasını uyumlu sanallaştırma yazılımında kullanın.\n" +
                "Use the extracted VHD in compatible virtualization software.\n" +
                "Kaynak / Source: " + ServerCatalog.GetSource(release), token);
            token.ThrowIfCancellationRequested();
            Directory.Move(staging, finalDirectory);
            return finalDirectory;
        }
        catch
        {
            // Keep staged parts for a later retry; completed files are revalidated by redownloading.
            throw;
        }
    }

    private async Task DownloadFileAsync(Uri uri, string partial, string extension, CancellationToken token, string? expectedHash = null)
    {
        var identity = string.Join("|", SelectedProduct(), SelectedRelease(), SelectedArchitecture(), SelectedLanguage(), uri.GetLeftPart(UriPartial.Path));
        _lastHash = await ResumableDownload.RunAsync(DownloadClient, uri, partial, identity, extension,
            extension is ".iso" or ".img" or ".vhd" or ".vhdx" ? 100_000_000 : 1_000_000, expectedHash, update => {
                DownloadProgress.IsIndeterminate = !update.Total.HasValue;
                TaskbarItemInfo.ProgressState = update.Total.HasValue ? System.Windows.Shell.TaskbarItemProgressState.Normal : System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                if (update.Total is > 0) { DownloadProgress.Value = Math.Min(100, update.Received * 100.0 / update.Total.Value); TaskbarItemInfo.ProgressValue = DownloadProgress.Value / 100; }
                var speed = update.BytesPerSecond / 1048576;
                string eta = update.Total.HasValue && update.BytesPerSecond > 0 ? TimeSpan.FromSeconds(Math.Max(0, update.Total.Value - update.Received) / update.BytesPerSecond).ToString(@"hh\:mm\:ss") : "—";
                DetailsText.Text = $"{update.Received / 1048576.0:F0} MB / {(update.Total.HasValue ? (update.Total.Value / 1048576.0).ToString("F0") : "?")} MB • {speed:F1} MB/s • " + App.T("Text_767791c52c04") + eta + (update.Attempt > 0 ? App.T("Text_28a3860ee475") + update.Attempt : "");
            }, token);
    }
    private static async Task<Uri> ResolveHistoricalUrlAsync(string product, string release, string architecture, string language, CancellationToken token)
    {
        var uri = await ResolveUrlAsync(product, 0, architecture, language, token, release);
        var expected = Regex.Match(release, @"\d{2}H[12]", RegexOptions.IgnoreCase).Value;
        if (expected.Length == 0 || !Regex.Match(Path.GetFileName(uri.AbsolutePath), @"\d{2}H[12]", RegexOptions.IgnoreCase).Value.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new IOException(App.T("Text_2f46ad6dde36"));
        return uri;
    }

    private sealed record PowerToysRelease(Uri Url, string Version, string? Sha256);

    private static Task<PowerToysRelease> GetPowerToysReleaseAsync(string architecture, CancellationToken token) => ReleaseCache.GetAsync("powertoys:" + architecture, () => FetchGetPowerToysReleaseAsync(architecture, CancellationToken.None), token);

    private static async Task<PowerToysRelease> FetchGetPowerToysReleaseAsync(string architecture, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/microsoft/PowerToys/releases/latest");
        request.Headers.UserAgent.ParseAdd("WindowsISODownloader/" + RuntimeInfo.Version);
        using var response = await Client.SendAsync(request, token);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        var version = document.RootElement.GetProperty("tag_name").GetString() ?? "Latest";
        foreach (var asset in document.RootElement.GetProperty("assets").EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            if (!Regex.IsMatch(name, @"^PowerToysUserSetup-.*-" + Regex.Escape(architecture) + @"\.exe$", RegexOptions.IgnoreCase)) continue;
            if (!Uri.TryCreate(asset.GetProperty("browser_download_url").GetString(), UriKind.Absolute, out var url) ||
                url.Scheme != Uri.UriSchemeHttps || url.Host != "github.com" ||
                !url.AbsolutePath.StartsWith("/microsoft/PowerToys/releases/download/", StringComparison.OrdinalIgnoreCase)) continue;
            return new PowerToysRelease(url, version, DownloadSafety.ParseSha256(asset.TryGetProperty("digest", out var digest) && digest.ValueKind == JsonValueKind.String ? digest.GetString() : null));
        }
        throw new IOException(App.T("Text_f20c2c2589ea"));
    }

    private sealed record EdgeRelease(Uri Url, string Version, string? Sha256);

    private static Task<EdgeRelease> GetEdgeReleaseAsync(string architecture, CancellationToken token) => ReleaseCache.GetAsync("edge:" + architecture, () => FetchGetEdgeReleaseAsync(architecture, CancellationToken.None), token);

    private static async Task<EdgeRelease> FetchGetEdgeReleaseAsync(string architecture, CancellationToken token)
    {
        using var response = await Client.GetAsync("https://edgeupdates.microsoft.com/api/products?view=enterprise", token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: token);
        EdgeRelease? best = null;
        foreach (var product in document.RootElement.EnumerateArray())
        {
            if (product.GetProperty("Product").GetString() != "Stable") continue;
            foreach (var release in product.GetProperty("Releases").EnumerateArray())
            {
                if (release.GetProperty("Platform").GetString() != "Windows" ||
                    !string.Equals(release.GetProperty("Architecture").GetString(), architecture, StringComparison.OrdinalIgnoreCase)) continue;
                var version = release.GetProperty("ProductVersion").GetString() ?? "";
                if (!Version.TryParse(version, out var parsed)) continue;
                foreach (var artifact in release.GetProperty("Artifacts").EnumerateArray())
                {
                    if (!string.Equals(artifact.GetProperty("ArtifactName").GetString(), "msi", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!Uri.TryCreate(artifact.GetProperty("Location").GetString(), UriKind.Absolute, out var url) ||
                        url.Scheme != Uri.UriSchemeHttps || !url.Host.EndsWith(".microsoft.com", StringComparison.OrdinalIgnoreCase) ||
                        !url.AbsolutePath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)) continue;
                    if (best == null || Version.Parse(best.Version).CompareTo(parsed) < 0) best = new EdgeRelease(url, version, artifact.TryGetProperty("HashAlgorithm", out var algorithm) && algorithm.GetString()?.Replace("-", "").Equals("SHA256", StringComparison.OrdinalIgnoreCase) == true && artifact.TryGetProperty("Hash", out var hashValue) ? DownloadSafety.ParseSha256(hashValue.GetString()) : null);
                }
            }
        }
        return best ?? throw new IOException(App.T("Text_4ea81d47189c"));
    }

    private static async Task<Uri> GetOfficeToolUrlAsync(CancellationToken token)
    {
        using var response = await Client.GetAsync("https://www.microsoft.com/en-us/download/details.aspx?id=49117", token);
        response.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(token));
        var match = Regex.Match(html, @"https://download\.microsoft\.com/download/[^""'\s<>]+/officedeploymenttool_[\d-]+\.exe", RegexOptions.IgnoreCase);
        if (!match.Success || !Uri.TryCreate(match.Value, UriKind.Absolute, out var url))
            throw new IOException(App.T("Text_107ac8a4fd6d"));
        return url;
    }

    private static Uri GetOffice2016Url(string edition, string language)
    {
        if (edition is not ("ProPlusRetail" or "HomeBusinessRetail" or "HomeStudentRetail") ||
            language is not ("tr-tr" or "en-us" or "de-de" or "fr-fr" or "es-es" or "it-it"))
            throw new IOException(App.T("Text_59750f262f24"));
        return new Uri("https://officecdn.microsoft.com/db/492350F6-3A01-4F97-B9C0-C7C6DDF67D60/media/" + language + "/" + edition + ".img");
    }

    private static Uri GetLtscUrl(string edition, string architecture)
    {
        // Public Microsoft Evaluation Center download links, not ISO mirrors.
        var id = (edition, architecture) switch
        {
            ("Enterprise", "x64") => "2289029",
            ("IoT", "x64") => "2270353",
            ("IoT", "ARM64") => "2269595",
            _ => throw new IOException(App.T("Text_1808b9297e37"))
        };
        return new Uri("https://go.microsoft.com/fwlink/?linkid=" + id);
    }

    private static string WriteOfficeConfiguration(string installer, string architecture, string language, string product) =>
        OfficeWorkspace.CreateConfiguration(Path.GetDirectoryName(installer)!, architecture, language, product);

    private static async Task DownloadOfficePayloadAsync(string installer, string config, CancellationToken token)
    {
        var folder = Path.GetDirectoryName(config)!;
        var extracted = Path.Combine(folder, "OfficeDeploymentTool-" + Guid.NewGuid().ToString("N"));
        try
        {
        Directory.CreateDirectory(extracted);
        using var installerLock = new FileStream(installer, FileMode.Open, FileAccess.Read, FileShare.Read);
        await VerifyMicrosoftSignatureAsync(installer, token);
        var extract = new ProcessStartInfo(installer)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = folder
        };
        extract.ArgumentList.Add("/quiet");
        extract.ArgumentList.Add("/extract:" + extracted);
        await RunToolAsync(extract, token);
        var setup = Path.Combine(extracted, "setup.exe");
        if (!File.Exists(setup)) throw new IOException(App.T("Text_73dc3851baf4"));
        using var setupLock = new FileStream(setup, FileMode.Open, FileAccess.Read, FileShare.Read);
        await VerifyMicrosoftSignatureAsync(setup, token);
        var download = new ProcessStartInfo(setup)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = folder
        };
        download.ArgumentList.Add("/download");
        download.ArgumentList.Add(config);
        await RunToolAsync(download, token);
        if (!Directory.Exists(Path.Combine(folder, "Office")))
            throw new IOException(App.T("Text_c7505809432c"));
        OfficeWorkspace.PrepareInstaller(config, setup);
        }
        finally
        {
            OfficeWorkspace.Cleanup(config, extracted);
        }

    }

    private static async Task VerifyMicrosoftSignatureAsync(string path, CancellationToken token)
    {
        var code = "$s=Get-AuthenticodeSignature -LiteralPath '" + path.Replace("'", "''") + "'; if ($s.Status -ne 'Valid' -or $null -eq $s.SignerCertificate -or $s.SignerCertificate.GetNameInfo([System.Security.Cryptography.X509Certificates.X509NameType]::SimpleName,$false) -ne 'Microsoft Corporation') { exit 1 }; exit 0";
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
        foreach (var arg in new[] { "-NoProfile", "-EncodedCommand", Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(code)) }) start.ArgumentList.Add(arg);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        try { await RunToolAsync(start, timeout.Token); }
        catch (Exception ex) when (ex is not OperationCanceledException) { throw new IOException(App.T("Text_9f147df4c4b5"), ex); }
    }

    private static async Task RunToolAsync(ProcessStartInfo start, CancellationToken token)
    {
        start.RedirectStandardOutput=true;start.RedirectStandardError=true;
        using var process = Process.Start(start) ?? throw new IOException(App.T("Text_c2f5d78dd12f"));
        var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(token); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(true);
            throw;
        }
        await output;var detail=await error;
        if (process.ExitCode != 0) throw new IOException(App.T("Text_2c74511abb68") + process.ExitCode + ". " + (detail.Length>400?detail[..400]:detail));
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
