using Microsoft.Win32;
using Minditful.Integrations;

namespace Minditful.App.Services;

/// <summary>
/// Khởi động Milo cùng Windows: 1 mục trong HKCU\Software\Microsoft\Windows\CurrentVersion\Run (chỉ tài khoản hiện tại, không cần quyền admin).
/// Lệnh ghi kèm --env nên Milo mở thẳng đúng môi trường, không hiện màn hình chọn. Chỉ 1 môi trường được tự khởi động cùng lúc.
/// Tắt mặc định: người dùng tự bật ở menu khay hoặc bảng điều khiển → Milo của bạn.
/// </summary>
internal static class AutoStart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Minditful.Milo";

    /// <summary>Demo là ngày mẫu để trình diễn nên không tự khởi động.</summary>
    public static bool Supported(AppEnvironment env) => env != AppEnvironment.Demo && Environment.ProcessPath is not null;

    private static string Command(AppEnvironment env) => $"\"{Environment.ProcessPath}\" --env {env}";

    private static string? Current()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) as string;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Đang bật cho đúng môi trường này (mục Run trỏ tới --env này).</summary>
    public static bool IsEnabled(AppEnvironment env) =>
        Current() is { } cmd && cmd.EndsWith($"--env {env}", StringComparison.OrdinalIgnoreCase);

    /// <summary>Đang bật cho môi trường khác (vd. bật ở Sandbox rồi mở Production) — để giải thích trên giao diện.</summary>
    public static string? OtherEnvironment(AppEnvironment env) =>
        Current() is { } cmd && !IsEnabled(env) && cmd.LastIndexOf("--env ", StringComparison.OrdinalIgnoreCase) is var i and >= 0
            ? cmd[(i + 6)..].Trim()
            : null;

    /// <returns>null khi thành công, ngược lại là lý do lỗi để hiện cho người dùng.</returns>
    public static string? Set(AppEnvironment env, bool on)
    {
        if (!Supported(env)) return "Demo không tự khởi động cùng Windows.";
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
            if (on) key.SetValue(ValueName, Command(env));
            else if (IsEnabled(env)) key.DeleteValue(ValueName, throwOnMissingValue: false);
            return null;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or System.IO.IOException)
        {
            return "Không ghi được vào Registry: " + ex.Message;
        }
    }

    /// <summary>Đã bật mà app bị chuyển sang thư mục khác (giải nén bản mới) → cập nhật đường dẫn để lần sau vẫn mở được.</summary>
    public static void Repair(AppEnvironment env)
    {
        if (IsEnabled(env) && Current() != Command(env)) Set(env, true);
    }
}
