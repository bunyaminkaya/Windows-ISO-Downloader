using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
namespace WindowsISODownloader;
internal static class AdaptiveWindow
{
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    public static void Attach(Window window)
    {
        HwndSource? source = null;
        Rect? lastArea = null;
        double lastDpiScale = 0;
        DispatcherOperation? pendingFit = null;
        void Fit(bool initial)
        {
            if (window.WindowState != WindowState.Normal) return;
            var area = SystemParameters.WorkArea;
            var handle = new WindowInteropHelper(window).Handle;
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (source?.CompositionTarget != null && GetMonitorInfo(MonitorFromWindow(handle, 2), ref info))
            {
                var transform = source.CompositionTarget.TransformFromDevice;
                var topLeft = transform.Transform(new Point(info.Work.Left, info.Work.Top));
                var bottomRight = transform.Transform(new Point(info.Work.Right, info.Work.Bottom));
                area = new Rect(topLeft, bottomRight);
            }
            // Work area excludes taskbars. All values below are WPF device-independent units.
            var availableWidth = Math.Max(1, area.Width - 24);
            var availableHeight = Math.Max(1, area.Height - 24);
            var dpiScale = source?.CompositionTarget?.TransformToDevice.M11 ?? 1;
            var displayChanged = lastArea != area || Math.Abs(lastDpiScale - dpiScale) > 0.001;
            var automatic = initial || displayChanged;
            var scale = Math.Min(1, Math.Min(availableWidth / 1300, availableHeight / 850));
            var targetWidth = automatic ? 1300 * scale : Math.Min(window.ActualWidth, availableWidth);
            var targetHeight = automatic ? 850 * scale : Math.Min(window.ActualHeight, availableHeight);
            window.MinWidth = Math.Min(320, targetWidth);
            window.MinHeight = Math.Min(220, targetHeight);
            window.Width = targetWidth;
            window.Height = targetHeight;
            lastArea = area;
            lastDpiScale = dpiScale;
            window.Left = automatic ? area.Left + (area.Width - window.Width) / 2 : Math.Clamp(window.Left, area.Left, area.Right - window.Width);
            window.Top = automatic ? area.Top + (area.Height - window.Height) / 2 : Math.Clamp(window.Top, area.Top, area.Bottom - window.Height);
        }
        IntPtr Hook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // DPI, monitor/work-area changes, and the end of a move/resize operation.
            if (message is 0x02E0 or 0x007E or 0x001A or 0x0232)
            {
                pendingFit?.Abort();
                pendingFit = window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => Fit(false)));
            }
            return IntPtr.Zero;
        }
        window.SourceInitialized += (_, _) =>
        {
            source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
            source?.AddHook(Hook);
        };
        window.Loaded += (_, _) => Fit(true);
        window.StateChanged += (_, _) => { if (window.WindowState == WindowState.Normal) window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => Fit(false))); };
        window.Closed += (_, _) => { pendingFit?.Abort(); source?.RemoveHook(Hook); };
    }
}
