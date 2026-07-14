using System.IO;
using Microsoft.UI.Xaml;

namespace ClipboardMonitorWinUI;

/// <summary>
/// 管理应用启动、主窗口生命周期和未处理异常日志。
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// 初始化应用单例。
    /// </summary>
    public App()
    {
        InitializeComponent();
        UnhandledException += App_UnhandledException;
    }

    private static void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // WinUI 的 XAML 加载异常有时只表现为原生退出码，额外日志便于定位问题。
        var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "启动错误.log");
        File.AppendAllText(logPath,
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {e.Exception}\r\n{e.Message}\r\n\r\n");
    }

    /// <summary>
    /// 创建并激活主窗口。
    /// </summary>
    /// <param name="args">启动参数。</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
