using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ClipboardMonitorWinUI;

/// <summary>
/// Windows 剪贴板窗口查询接口。
/// </summary>
internal static class NativeMethods
{
    [DllImport("user32.dll")]
    internal static extern nint GetOpenClipboardWindow();

    [DllImport("user32.dll")]
    internal static extern nint GetClipboardOwner();

    [DllImport("user32.dll")]
    internal static extern nint GetClipboardViewer();

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetWindowText(nint hWnd, StringBuilder text, int count);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetClassName(nint hWnd, StringBuilder text, int count);
}

/// <summary>
/// 从窗口句柄解析出的进程和窗口信息。
/// </summary>
internal sealed class ClipboardWindowInfo
{
    internal required string Role { get; init; }
    internal nint Handle { get; init; }
    internal uint ProcessId { get; init; }
    internal string ProcessName { get; init; } = "无";
    internal string ExecutablePath { get; init; } = string.Empty;
    internal string WindowTitle { get; init; } = string.Empty;
    internal string WindowClass { get; init; } = string.Empty;

    internal bool Exists => Handle != nint.Zero;
    internal string Identity => $"{Role}|{Handle}|{ProcessId}|{ProcessName}";
    internal string DisplayWindow => FirstNonEmpty(WindowTitle, WindowClass, "—");
    internal string WindowTitleOrDash => FirstNonEmpty(WindowTitle, "—");
    internal string WindowClassOrDash => FirstNonEmpty(WindowClass, "—");
    internal string ExecutablePathOrDash => FirstNonEmpty(ExecutablePath, "—");

    private static string FirstNonEmpty(params string[] values)
    {
        return values.First(value => !string.IsNullOrWhiteSpace(value));
    }
}

/// <summary>
/// 将剪贴板窗口句柄映射为便于展示的进程信息。
/// </summary>
internal static class ClipboardInspector
{
    internal static ClipboardWindowInfo Inspect(string role, nint handle)
    {
        if (handle == nint.Zero)
        {
            return new ClipboardWindowInfo { Role = role, Handle = handle };
        }

        NativeMethods.GetWindowThreadProcessId(handle, out var processId);
        var title = new StringBuilder(1024);
        var className = new StringBuilder(256);
        NativeMethods.GetWindowText(handle, title, title.Capacity);
        NativeMethods.GetClassName(handle, className, className.Capacity);

        var processName = "进程已退出或无法访问";
        var executablePath = string.Empty;

        // 系统进程可能拒绝查询路径，但进程名和 PID 仍然可以正常展示。
        try
        {
            using var process = Process.GetProcessById((int)processId);
            processName = $"{process.ProcessName}.exe";
            try
            {
                executablePath = process.MainModule?.FileName ?? string.Empty;
            }
            catch
            {
                executablePath = "无法读取（权限受限）";
            }
        }
        catch
        {
            // 保留上面的明确状态说明，避免将异常误显示成“无”。
        }

        return new ClipboardWindowInfo
        {
            Role = role,
            Handle = handle,
            ProcessId = processId,
            ProcessName = processName,
            ExecutablePath = executablePath,
            WindowTitle = title.ToString(),
            WindowClass = className.ToString()
        };
    }
}
