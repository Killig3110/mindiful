namespace Minditful.Integrations;

/// <summary>
/// Đọc file <c>.env</c> (KEY=VALUE, # chú thích, cho phép "export " và dấu nháy) vào biến môi trường.
/// Biến môi trường thật luôn được ưu tiên; dòng để trống giá trị bị bỏ qua để không đè cấu hình mặc định.
/// </summary>
public static class DotEnv
{
    public static IReadOnlyDictionary<string, string> Parse(IEnumerable<string> lines)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("export ", StringComparison.Ordinal)) line = line[7..].TrimStart();
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();
            if (value.Length >= 2 && (value[0] == '"' && value[^1] == '"' || value[0] == '\'' && value[^1] == '\''))
                value = value[1..^1];
            else if (value.IndexOf(" #", StringComparison.Ordinal) is var hash and >= 0)
                value = value[..hash].TrimEnd(); // chú thích cuối dòng
            if (value.Length == 0) continue;
            result[key] = value;
        }
        return result;
    }

    /// <summary>Nạp file đầu tiên tìm thấy trong <paramref name="candidates"/>. Trả về đường dẫn đã nạp (null nếu không có).</summary>
    public static string? Load(IEnumerable<string> candidates)
    {
        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            foreach (var (k, v) in Parse(File.ReadAllLines(path)))
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(k)))
                    Environment.SetEnvironmentVariable(k, v);
            return path;
        }
        return null;
    }

    /// <summary>
    /// Nơi tìm <c>.env</c>: cạnh file exe, rồi đi ngược lên các thư mục cha (khi chạy <c>dotnet run</c> từ repo),
    /// cuối cùng là <c>%LOCALAPPDATA%\Minditful\.env</c>.
    /// </summary>
    public static IEnumerable<string> DefaultCandidates(string baseDir, string appDataDir)
    {
        for (var dir = new DirectoryInfo(baseDir); dir is not null; dir = dir.Parent)
            yield return Path.Combine(dir.FullName, ".env");
        yield return Path.Combine(appDataDir, ".env");
    }
}
