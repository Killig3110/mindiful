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
}
