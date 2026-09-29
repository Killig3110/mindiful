using System.IO;
using Minditful.Core.Engine;
using Minditful.Integrations;

namespace Minditful.App.Services;

/// <summary>Tính cách Milo, nhớ theo từng môi trường. Mặc định Pha trộn (lâu lâu hài một chút).</summary>
internal static class PersonalitySetting
{
    private static string File(AppEnvironment env) => Path.Combine(AppPaths.For(env), "personality.txt");

    public static Personality Load(AppEnvironment env, WellbeingOptions? w)
    {
        try
        {
            if (System.IO.File.Exists(File(env)) && Enum.TryParse<Personality>(System.IO.File.ReadAllText(File(env)).Trim(), out var p)) return p;
        }
        catch (IOException) { }
        return Enum.TryParse<Personality>(w?.Personality, true, out var fromConfig) ? fromConfig : Personality.Mixed;
    }

    /// <summary>Đổi ngay trên engine đang chạy và nhớ cho lần sau.</summary>
    public static void Set(AppEnvironment env, MiloEngine engine, Personality p)
    {
        engine.Cfg.Personality = p;
        try { System.IO.File.WriteAllText(File(env), p.ToString()); } catch (IOException) { }
        engine.LogExternal($"Tính cách Milo: {Catalog.Personalities.First(x => x.Value == p).Name}", LogKind.User);
    }
}
