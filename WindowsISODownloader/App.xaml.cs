using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace WindowsISODownloader;

public partial class App : Application
{
    private bool _cliMode;
    internal static event Action? ThemeChanged;
    internal static event Action? LanguageChanged;
    internal static string Language { get => UiText.Language; private set => UiText.Language = value; }
    internal static string ThemeMode { get; private set; } = "System";
    private static readonly string ThemeFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WID", "theme.txt");

    private static readonly string LanguageFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WID", "language.txt");

    internal static string T(string key) => UiText.T(key);

    internal static void SetLanguage(string language)
    {
        if (language is not ("tr" or "en") || Language == language) return;
        Language = language;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LanguageFile)!);
            File.WriteAllText(LanguageFile, language);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        LanguageChanged?.Invoke();
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")] private static extern bool AttachConsole(uint processId);
    protected override async void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => WriteCrash(args.ExceptionObject.ToString() ?? "Bilinmeyen hata");
        bool cli=e.Args.Length>0;_cliMode=cli;
        if(cli){ShutdownMode=ShutdownMode.OnExplicitShutdown;try{AttachConsole(uint.MaxValue);Console.SetOut(new System.IO.StreamWriter(Console.OpenStandardOutput()){AutoFlush=true});}catch{}}
        if(!cli) StartupUri=new Uri("MainWindow.xaml",UriKind.Relative);
        base.OnStartup(e);
        if(cli){int code=await WindowsISODownloader.MainWindow.RunCliAsync(e.Args);Shutdown(code);return;}
        try
        {
            var stored = File.ReadAllText(ThemeFile).Trim();
            if (stored is "System" or "Dark" or "Light") ThemeMode = stored;
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        try { if (File.ReadAllText(LanguageFile).Trim() == "en") Language = "en"; }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        try { ApplyTheme(); }
        catch (Exception ex) { WriteCrash(ex.ToString()); }
        try { SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged; }
        catch (Exception ex) { WriteCrash(ex.ToString()); }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged; } catch { }
        base.OnExit(e);
    }

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteCrash(e.Exception.ToString());
        if(_cliMode){try{Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{success=false,error=e.Exception.Message}));}catch{}e.Handled=true;Shutdown(1);return;}
        (MainWindow as WindowsISODownloader.MainWindow)?.CancelActiveOperation();
        MessageBox.Show(App.T("Text_a9afd2388c79") + e.Exception.Message,
            "Windows ISO Downloader", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        if (MainWindow?.IsLoaded != true) Shutdown(1);
    }

    internal static void WriteCrash(string detail)
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WID", "hata.log");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {detail}\n\n");
        }
        catch { }
    }

    private static void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (ThemeMode == "System") Current.Dispatcher.BeginInvoke(ApplyTheme);
    }

    internal static void SetTheme(string mode)
    {
        if (mode is not ("System" or "Dark" or "Light")) return;
        ThemeMode = mode;
        ApplyTheme();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ThemeFile)!);
            File.WriteAllText(ThemeFile, mode);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void ApplyTheme()
    {
        bool dark = ThemeMode == "Dark" || (ThemeMode == "System" && SystemIsDark());
        Set("BgBrush", dark ? "#181B21" : "#F7F9FC");
        Set("PanelBrush", dark ? "#20242B" : "#FFFFFF");
        Set("Panel2Brush", dark ? "#282D35" : "#FFFFFF");
        Set("BorderBrush", dark ? "#424954" : "#CBD2DC");
        Set("TextBrush", dark ? "#F3F7FF" : "#151B25");
        Set("MutedBrush", dark ? "#B8C0CC" : "#586577");
        Set("AccentBrush", dark ? "#0785F5" : "#006FE8");
        Set("AccentTextBrush", "#FFFFFF");
        Set("PrimaryButtonBrush", dark ? "#0785F5" : "#006FE8");
        Set("PrimaryButtonTextBrush", dark ? "#FFFFFF" : "#FFFFFF");
        Set("PrimaryButtonBorderBrush", dark ? "#0785F5" : "#006FE8");
        Set("Accent2Brush", dark ? "#36A5FF" : "#155D9C");
        Set("HoverBrush", dark ? "#23527B" : "#D5E8F9");
        Set("SelectedBrush", dark ? "#1D4265" : "#C6E2F7");
        Set("BannerBrush", dark ? "#181D24" : "#EAF4FF");
        ThemeChanged?.Invoke();
    }

    internal static bool IsDark => ThemeMode == "Dark" || (ThemeMode == "System" && SystemIsDark());

    private static bool SystemIsDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return Convert.ToInt32(key?.GetValue("AppsUseLightTheme", 1)) == 0;
        }
        catch { return false; }
    }

    private static void Set(string key, string hex)
    {
        Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }
}
