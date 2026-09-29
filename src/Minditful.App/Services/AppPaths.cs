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

/// <summary>Bí mật lưu local (PAT Azure DevOps, API key Claude), mã hoá bằng DPAPI theo tài khoản Windows (spec: "PAT lưu local").</summary>
internal sealed class SecretStore(AppEnvironment env, string name = "ado-pat")
{
    private string File => Path.Combine(AppPaths.For(env), name + ".bin");

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

internal enum Corner { BottomRight, BottomLeft, TopRight, TopLeft }

/// <summary>Góc neo của Milo (§9.1: kéo đuôi sang góc khác), lưu riêng từng môi trường.</summary>
internal static class UiSettings
{
    private static string File(AppEnvironment env) => Path.Combine(AppPaths.For(env), "corner.txt");

    public static Corner LoadCorner(AppEnvironment env) =>
        System.IO.File.Exists(File(env)) && Enum.TryParse<Corner>(System.IO.File.ReadAllText(File(env)).Trim(), out var c) ? c : Corner.BottomRight;

    public static void SaveCorner(AppEnvironment env, Corner c) => System.IO.File.WriteAllText(File(env), c.ToString());

    private static string AccessoryFile(AppEnvironment env) => Path.Combine(AppPaths.For(env), "accessory.txt");

    /// <summary>Tủ đồ: "auto" (món khó mở nhất đang có), "none" hoặc các id món ngăn bởi dấu phẩy (xem <see cref="Core.Engine.Wardrobe.Resolve"/>).</summary>
    public static string LoadAccessory(AppEnvironment env) =>
        System.IO.File.Exists(AccessoryFile(env)) ? System.IO.File.ReadAllText(AccessoryFile(env)).Trim() : "auto";

    public static void SaveAccessory(AppEnvironment env, string choice) => System.IO.File.WriteAllText(AccessoryFile(env), choice);

    private static string OutfitSetsFile(AppEnvironment env) => Path.Combine(AppPaths.For(env), "outfits.txt");

    /// <summary>Bộ đồ đã lưu: mỗi dòng "tên[TAB]id,id,…".</summary>
    public static IReadOnlyList<(string Name, string Outfit)> LoadOutfitSets(AppEnvironment env)
    {
        try
        {
            return System.IO.File.Exists(OutfitSetsFile(env))
                ? System.IO.File.ReadAllLines(OutfitSetsFile(env)).Select(l => l.Split('\t')).Where(p => p.Length == 2).Select(p => (p[0], p[1])).ToList()
                : [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    public static void SaveOutfitSets(AppEnvironment env, IReadOnlyList<(string Name, string Outfit)> sets) =>
        System.IO.File.WriteAllLines(OutfitSetsFile(env), sets.Select(s => $"{s.Name}\t{s.Outfit}"));

    private static string TestModeFile(AppEnvironment env) => Path.Combine(AppPaths.For(env), "test-mode.txt");

    /// <summary>Sandbox: người dùng đã chọn chế độ test hay chạy như Production (null = chưa chọn, theo appsettings).</summary>
    public static bool? LoadTestMode(AppEnvironment env) =>
        System.IO.File.Exists(TestModeFile(env)) && bool.TryParse(System.IO.File.ReadAllText(TestModeFile(env)).Trim(), out var on) ? on : null;

    public static void SaveTestMode(AppEnvironment env, bool on) => System.IO.File.WriteAllText(TestModeFile(env), on.ToString());
}
