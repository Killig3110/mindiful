using Minditful.Integrations;
using Xunit;

namespace Minditful.Core.Tests;

public class DotEnvTests
{
    [Fact]
    public void Parses_comments_quotes_export_and_skips_empty_values()
    {
        var env = DotEnv.Parse(
        [
            "# chú thích",
            "ANTHROPIC_API_KEY=",
            "export MINDITFUL_SANDBOX_ADO_PAT=abc123",
            "MINDITFUL__Minditful__Sandbox__AzureDevOps__Project=\"Minditful Sandbox\"",
            "MINDITFUL__Minditful__Environment=Demo   # mở thẳng Demo",
            "khong co dau bang",
        ]);
        Assert.False(env.ContainsKey("ANTHROPIC_API_KEY")); // trống → dùng mặc định
        Assert.Equal("abc123", env["MINDITFUL_SANDBOX_ADO_PAT"]);
        Assert.Equal("Minditful Sandbox", env["MINDITFUL__Minditful__Sandbox__AzureDevOps__Project"]);
        Assert.Equal("Demo", env["MINDITFUL__Minditful__Environment"]);
        Assert.Equal(3, env.Count);
    }

    [Fact]
    public void Real_environment_variables_win_over_dotenv()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        var key = "MINDITFUL_TEST_" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllLines(Path.Combine(dir, ".env"), [$"{key}=from-file", $"{key}_2=from-file"]);
            Environment.SetEnvironmentVariable(key, "from-machine");
            Assert.Equal(Path.Combine(dir, ".env"), DotEnv.Load(DotEnv.DefaultCandidates(Path.Combine(dir, "bin", "x"), dir)));
            Assert.Equal("from-machine", Environment.GetEnvironmentVariable(key));
            Assert.Equal("from-file", Environment.GetEnvironmentVariable(key + "_2"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
            Environment.SetEnvironmentVariable(key + "_2", null);
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Sample_file_lists_every_secret_the_app_reads()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, ".env.sample"))) root = root.Parent;
        Assert.NotNull(root);
        var sample = File.ReadAllText(Path.Combine(root!.FullName, ".env.sample"));
        foreach (var k in new[] { "ANTHROPIC_API_KEY", "MINDITFUL_SANDBOX_ADO_PAT", "MINDITFUL_PROD_ADO_PAT", "MINDITFUL_ENV", "Production__AzureDevOps__Organization" })
            Assert.Contains(k, sample);
    }

    private static DirectoryInfo RepoRoot()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, ".env.sample"))) root = root.Parent;
        Assert.NotNull(root);
        return root!;
    }

    /// <summary>Mọi khoá cấu hình (a:b:c) có trong appsettings.json, không phân biệt hoa thường.</summary>
    private static HashSet<string> SettingKeys(DirectoryInfo root)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Walk(System.Text.Json.JsonElement e, string path)
        {
            if (path.Length > 0) keys.Add(path);
            if (e.ValueKind != System.Text.Json.JsonValueKind.Object) return;
            foreach (var p in e.EnumerateObject()) Walk(p.Value, path.Length == 0 ? p.Name : path + ":" + p.Name);
        }
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "src", "Minditful.App", "appsettings.json")));
        Walk(doc.RootElement, "");
        return keys;
    }

    [Fact]
    public void Every_config_variable_in_env_sample_and_readme_exists_in_appsettings()
    {
        var root = RepoRoot();
        var keys = SettingKeys(root);
        var sample = File.ReadAllText(Path.Combine(root.FullName, ".env.sample"));
        var names = System.Text.RegularExpressions.Regex.Matches(sample, @"^MINDITFUL__(Minditful__[A-Za-z_]+)=", System.Text.RegularExpressions.RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value.Replace("__", ":")).ToList();
        Assert.True(names.Count > 30);
        Assert.All(names, n => Assert.Contains(n, keys));

        // README viết tắt "…Wellbeing__FocusPlan" (… = MINDITFUL__Minditful__)
        var readme = File.ReadAllText(Path.Combine(root.FullName, "README.md"));
        var docNames = System.Text.RegularExpressions.Regex.Matches(readme, @"`(?:MINDITFUL__Minditful__|…)((?:WorkDay|Storage|Wellbeing|Llm)__[A-Za-z_]+)")
            .Select(m => "Minditful:" + m.Groups[1].Value.Replace("__", ":")).Distinct().ToList();
        Assert.Contains("Minditful:Wellbeing:MicroBreakEveryMinutes", docNames);
        Assert.All(docNames, n => Assert.Contains(n, keys));
    }
}
