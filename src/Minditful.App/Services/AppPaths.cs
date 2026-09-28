using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Minditful.Integrations;

namespace Minditful.App.Services;

internal static class AppPaths
{
    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Minditful");

    /// <summary>Mỗi môi trường một thư mục riêng: token cache, PAT, lịch sử quả nho không trộn lẫn.</summary>
    public static string For(AppEnvironment env)
    {
        var dir = Path.Combine(Root, env.ToString());
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string RememberedEnvFile => Path.Combine(Root, "last-environment.txt");
}

/// <summary>PAT Azure DevOps lưu local, mã hoá bằng DPAPI theo tài khoản Windows (spec: "PAT lưu local").</summary>
internal sealed class SecretStore(AppEnvironment env)
{
    private string File => Path.Combine(AppPaths.For(env), "ado-pat.bin");

    public string? Read()
    {
        try
        {
            if (!System.IO.File.Exists(File)) return null;
            var raw = ProtectedData.Unprotect(System.IO.File.ReadAllBytes(File), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(raw);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public void Write(string? pat)
    {
        if (string.IsNullOrWhiteSpace(pat))
        {
            if (System.IO.File.Exists(File)) System.IO.File.Delete(File);
            return;
        }
        System.IO.File.WriteAllBytes(File, ProtectedData.Protect(Encoding.UTF8.GetBytes(pat.Trim()), null, DataProtectionScope.CurrentUser));
    }
}

internal static class Shell
{
    public static void Open(string target)
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            System.Windows.MessageBox.Show("Không mở được: " + target + "\n" + ex.Message, "Minditful");
        }
    }
}
