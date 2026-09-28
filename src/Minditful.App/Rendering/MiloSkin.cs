using System.Windows;
using System.Windows.Media;
using Minditful.Core.Engine;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace Minditful.App.Rendering;

/// <summary>
/// 8 tư thế SVG của Milo → DrawingImage. Mỗi mức mood có 1 bản giảm bão hoà tính sẵn
/// (thay cho CSS filter saturate: WPF không có sẵn hiệu ứng này).
/// </summary>
internal static class MiloSkin
{
    private static readonly Dictionary<(Pose, double), DrawingImage> Cache = [];
    private static readonly Dictionary<Pose, DrawingGroup> Source = [];

    public static DrawingImage Get(Pose pose, double saturation)
    {
        var key = (pose, Math.Round(saturation, 2));
        if (Cache.TryGetValue(key, out var img)) return img;
        var src = Load(pose);
        var drawing = saturation >= 0.999 ? src : Desaturate(src.Clone(), saturation);
        drawing.Freeze();
        img = new DrawingImage(drawing);
        img.Freeze();
        return Cache[key] = img;
    }

    private static DrawingGroup Load(Pose pose)
    {
        if (Source.TryGetValue(pose, out var dg)) return dg;
        var uri = new Uri($"pack://application:,,,/Assets/Milo/{pose.ToString().ToLowerInvariant()}.svg");
        using var stream = Application.GetResourceStream(uri)!.Stream;
        using var reader = new FileSvgReader(new WpfDrawingSettings { IncludeRuntime = false, TextAsGeometry = true });
        dg = reader.Read(stream) ?? new DrawingGroup();
        dg.Freeze();
        return Source[pose] = dg;
    }

    private static DrawingGroup Desaturate(DrawingGroup g, double s)
    {
        foreach (var d in g.Children) Walk(d, s);
        if (g.OpacityMask is not null) Tint(g.OpacityMask, s);
        return g;
    }

    private static void Walk(Drawing d, double s)
    {
        switch (d)
        {
            case DrawingGroup dg:
                foreach (var c in dg.Children) Walk(c, s);
                break;
            case GeometryDrawing gd:
                if (gd.Brush is not null) Tint(gd.Brush, s);
                if (gd.Pen?.Brush is not null) Tint(gd.Pen.Brush, s);
                break;
        }
    }

    private static void Tint(Brush b, double s)
    {
        if (b.IsFrozen) return;
        switch (b)
        {
            case SolidColorBrush sc: sc.Color = Saturate(sc.Color, s); break;
            case GradientBrush gb:
                foreach (var st in gb.GradientStops) st.Color = Saturate(st.Color, s);
                break;
        }
    }

    /// <summary>Ma trận saturate() của CSS Filter Effects.</summary>
    public static Color Saturate(Color c, double s)
    {
        double r = c.R, g = c.G, b = c.B;
        var nr = (0.213 + 0.787 * s) * r + (0.715 - 0.715 * s) * g + (0.072 - 0.072 * s) * b;
        var ng = (0.213 - 0.213 * s) * r + (0.715 + 0.285 * s) * g + (0.072 - 0.072 * s) * b;
        var nb = (0.213 - 0.213 * s) * r + (0.715 - 0.715 * s) * g + (0.072 + 0.928 * s) * b;
        static byte B(double v) => (byte)Math.Clamp(Math.Round(v), 0, 255);
        return System.Windows.Media.Color.FromArgb(c.A, B(nr), B(ng), B(nb));
    }

    public static SolidColorBrush Saturated(string hex, double s)
    {
        var b = new SolidColorBrush(Saturate(Ui.Rgb(hex), s));
        b.Freeze();
        return b;
    }
}
