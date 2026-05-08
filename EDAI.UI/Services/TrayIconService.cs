using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace EDAI.UI.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private bool _disposed;

    public TrayIconService()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "Elite Dangerous AI",
            Icon = SystemIcons.Application,
            Visible = false,
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Restore", null, (_, _) => Restore());
        contextMenu.Items.Add("Exit", null, (_, _) => ExitApplication());

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (_, _) => Restore();
    }

    public void Show() => _notifyIcon.Visible = true;
    public void Hide() => _notifyIcon.Visible = false;

    public void ShowBalloon(string title, string message)
    {
        if (!_notifyIcon.Visible) return;
        _notifyIcon.ShowBalloonTip(4000, title, message, ToolTipIcon.Info);
    }

    private static void Restore()
    {
        if (Application.Current.MainWindow is MainWindow mw)
        {
            mw.Show();
            mw.WindowState = WindowState.Normal;
            mw.Activate();
        }
    }

    private static void ExitApplication()
    {
        if (Application.Current.MainWindow is MainWindow mw)
            mw.RealClose();
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
