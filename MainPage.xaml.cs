using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ClipboardMonitorWinUI;

/// <summary>
/// 剪贴板活动监视器主页面。
/// </summary>
public sealed partial class MainPage : Page
{
    private const int MaximumHistoryCount = 500;
    private readonly DispatcherTimer _timer = new();
    private string _lastOpenIdentity = string.Empty;
    private string _lastOwnerIdentity = string.Empty;

    public ObservableCollection<HistoryEntry> HistoryEntries { get; } = [];

    public MainPage()
    {
        InitializeComponent();

        _timer.Interval = TimeSpan.FromMilliseconds(250);
        _timer.Tick += (_, _) => RefreshClipboardState(forceHistory: false);
        Loaded += MainPage_Loaded;
        Unloaded += (_, _) => _timer.Stop();
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshClipboardState(forceHistory: true);
        if (AutoRefreshToggle.IsOn)
        {
            _timer.Start();
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshClipboardState(forceHistory: true);
    }

    private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        HistoryEntries.Clear();
        _lastOpenIdentity = string.Empty;
        _lastOwnerIdentity = string.Empty;
        UpdateEmptyHistoryState();
    }

    private void AutoRefreshToggle_Toggled(object sender, RoutedEventArgs e)
    {
        // XAML 初始化 IsOn 时也会触发事件，页面加载完成前不访问其他命名控件。
        if (!IsLoaded || sender is not ToggleSwitch toggle)
        {
            return;
        }

        if (toggle.IsOn)
        {
            _timer.Start();
            RefreshClipboardState(forceHistory: false);
        }
        else
        {
            _timer.Stop();
        }
    }

    private void IntervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectedIndex 在 XAML 构造阶段会先变化一次，默认间隔已经在构造函数中设置。
        if (!IsLoaded || sender is not ComboBox comboBox || comboBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        if (double.TryParse(item.Tag?.ToString(), out var milliseconds))
        {
            _timer.Interval = TimeSpan.FromMilliseconds(milliseconds);
        }
    }

    private void RefreshClipboardState(bool forceHistory)
    {
        // 这里只读取剪贴板相关窗口的元数据，不读取或修改剪贴板内容。
        var open = ClipboardInspector.Inspect("正在打开", NativeMethods.GetOpenClipboardWindow());
        var owner = ClipboardInspector.Inspect("内容所有者", NativeMethods.GetClipboardOwner());
        var viewer = ClipboardInspector.Inspect("旧式查看器", NativeMethods.GetClipboardViewer());

        UpdateCard(open, OpenCard, OpenProcessName, OpenPidHandle, OpenWindowInfo, OpenPathInfo);
        UpdateCard(owner, OwnerCard, OwnerProcessName, OwnerPidHandle, OwnerWindowInfo, OwnerPathInfo);
        UpdateCard(viewer, ViewerCard, ViewerProcessName, ViewerPidHandle, ViewerWindowInfo, ViewerPathInfo);

        if (open.Exists)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Warning;
            StatusInfoBar.Title = $"检测到 {open.ProcessName} 正在打开剪贴板";
            StatusInfoBar.Message = "这可能是正常的瞬时访问；请结合下方变化历史判断是否持续占用。";
            OpenProcessName.Foreground = GetThemeBrush("SystemFillColorCriticalBrush", Colors.Firebrick);
        }
        else
        {
            StatusInfoBar.Severity = InfoBarSeverity.Success;
            StatusInfoBar.Title = "当前没有程序持续打开剪贴板";
            StatusInfoBar.Message = "自动刷新会继续捕获短暂出现的访问。";
            OpenProcessName.Foreground = GetThemeBrush("TextFillColorPrimaryBrush", Colors.Black);
        }

        LastRefreshText.Text = $"刷新于 {DateTime.Now:HH:mm:ss.fff}";

        if (forceHistory || open.Identity != _lastOpenIdentity)
        {
            AddHistory(open);
            _lastOpenIdentity = open.Identity;
        }

        if (forceHistory || owner.Identity != _lastOwnerIdentity)
        {
            AddHistory(owner);
            _lastOwnerIdentity = owner.Identity;
        }

        UpdateEmptyHistoryState();
    }

    private static void UpdateCard(
        ClipboardWindowInfo info,
        FrameworkElement card,
        TextBlock processName,
        TextBlock pidHandle,
        TextBlock windowInfo,
        TextBlock pathInfo)
    {
        processName.Text = info.ProcessName;
        pidHandle.Text = $"PID：{(info.ProcessId == 0 ? "—" : info.ProcessId)}    句柄：0x{info.Handle.ToInt64():X}";
        windowInfo.Text = $"窗口：{info.DisplayWindow}";
        pathInfo.Text = $"路径：{info.ExecutablePathOrDash}";

        // 悬停卡片时提供完整信息，便于复制前人工核对长路径。
        ToolTipService.SetToolTip(card,
            $"进程：{info.ProcessName}\n" +
            $"PID：{(info.ProcessId == 0 ? "—" : info.ProcessId)}\n" +
            $"窗口标题：{info.WindowTitleOrDash}\n" +
            $"窗口类名：{info.WindowClassOrDash}\n" +
            $"程序路径：{info.ExecutablePathOrDash}");
    }

    private void AddHistory(ClipboardWindowInfo info)
    {
        var foreground = info.Role == "正在打开" && info.Exists
            ? GetThemeBrush("SystemFillColorCriticalBrush", Colors.Firebrick)
            : GetThemeBrush("TextFillColorPrimaryBrush", Colors.Black);

        HistoryEntries.Insert(0, new HistoryEntry(
            DateTime.Now.ToString("HH:mm:ss.fff"),
            info.Role,
            info.ProcessName,
            info.ProcessId == 0 ? "—" : info.ProcessId.ToString(),
            info.DisplayWindow,
            foreground));

        while (HistoryEntries.Count > MaximumHistoryCount)
        {
            HistoryEntries.RemoveAt(HistoryEntries.Count - 1);
        }
    }

    private void UpdateEmptyHistoryState()
    {
        EmptyHistoryPanel.Visibility = HistoryEntries.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static Brush GetThemeBrush(string resourceName, Windows.UI.Color fallback)
    {
        return Application.Current.Resources.TryGetValue(resourceName, out var value) && value is Brush brush
            ? brush
            : new SolidColorBrush(fallback);
    }
}

/// <summary>
/// 历史列表中的单条状态变化。
/// </summary>
public sealed class HistoryEntry
{
    public HistoryEntry(
        string time,
        string role,
        string processName,
        string processId,
        string window,
        Brush foreground)
    {
        Time = time;
        Role = role;
        ProcessName = processName;
        ProcessId = processId;
        Window = window;
        Foreground = foreground;
    }

    // WinUI 的 XAML 类型系统需要可写属性来生成绑定访问器。
    public string Time { get; set; }
    public string Role { get; set; }
    public string ProcessName { get; set; }
    public string ProcessId { get; set; }
    public string Window { get; set; }
    public Brush Foreground { get; set; }
}
