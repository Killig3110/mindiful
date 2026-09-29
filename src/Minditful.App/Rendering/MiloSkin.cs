using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace Minditful.App.Rendering;

/// <summary>
/// 8 tư thế SVG của Milo → DrawingImage. Mỗi mức mood có 1 bản giảm bão hoà tính sẵn
/// (thay cho CSS filter saturate: WPF không có sẵn hiệu ứng này).
/// Clip "Cần vẽ" (§12) được dựng thành chuỗi khung: mỗi khung là SVG gốc với transform trên nhóm tay/chân/đuôi/đầu/tai/mắt.
/// </summary>
internal static class MiloSkin
{
    private static readonly Dictionary<(Pose, double), DrawingImage> Cache = [];
    private static readonly Dictionary<Pose, DrawingGroup> Source = [];
    private static readonly Dictionary<Pose, XDocument> Templates = [];
    private static readonly Dictionary<(Pose, string, int, bool, string?), DrawingGroup> FrameSource = [];
    private static readonly Dictionary<(Pose, string, int, bool, double, string?), DrawingImage> FrameCache = [];

    /// <summary>
    /// Phụ kiện tủ đồ, vẽ trong hệ toạ độ SVG của Milo và gắn vào nhóm bộ phận để đi theo đầu/thân khi Milo cử động.
    /// Khăn quàng nằm trong "torso" (dưới cằm), kẹp hoa và mũ nồi nằm trong "head".
    /// </summary>
    private static readonly Dictionary<string, (string Group, string Svg)> Accessories = new()
    {
        ["scarf"] = ("torso", """
            <path d="M90,131 Q120,147 150,131 L152,143 Q120,161 88,143 Z" fill="#E0526B" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M100,137 L103,150 M112,141 L113,154 M128,141 L127,154 M140,137 L137,150" stroke="#FFF3E6" stroke-width="3" stroke-linecap="round"/>
            <path d="M131,147 L142,174 Q134,178 126,175 L122,150 Z" fill="#E0526B" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            """),
        ["flower"] = ("head", """
            <g fill="#FF9FB2" stroke="#45231F" stroke-width="2.5"><circle cx="152" cy="44" r="7"/><circle cx="162" cy="50" r="7"/>
            <circle cx="159" cy="61" r="7"/><circle cx="146" cy="61" r="7"/><circle cx="143" cy="50" r="7"/></g>
            <circle cx="152.5" cy="53" r="5" fill="#FFD166" stroke="#45231F" stroke-width="2.5"/>
            """),
        ["beret"] = ("head", """
            <path d="M86,50 C88,28 150,22 158,44 C160,54 146,56 120,56 C98,56 84,58 86,50 Z" fill="#7261B0" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M92,52 Q120,60 152,52" fill="none" stroke="#5A4A96" stroke-width="3"/>
            <path d="M122,27 q2,-8 8,-8" fill="none" stroke="#45231F" stroke-width="3.5" stroke-linecap="round"/>
            """),
    };

    /// <summary>Khung <paramref name="frame"/> của clip <paramref name="rig"/> ở tư thế <paramref name="pose"/>, kèm phụ kiện (id trong <see cref="Wardrobe"/>).</summary>
    public static DrawingImage Frame(Pose pose, Rig rig, int frame, bool blink, double saturation, string? accessory = null)
    {
        if (pose is Pose.Tired or Pose.Breathe or Pose.Greeting) blink = false; // các tư thế này không có mắt mở để chớp
        if (accessory is not null && !Accessories.ContainsKey(accessory)) accessory = null;
        var sat = Math.Round(saturation, 2);
        var key = (pose, rig.Name, frame, blink, sat, accessory);
        if (FrameCache.TryGetValue(key, out var img)) return img;
        var src = FrameDrawing(pose, rig, frame, blink, accessory);
        var drawing = sat >= 0.999 ? src : Desaturate(src.Clone(), sat);
        drawing.Freeze();
        img = new DrawingImage(drawing);
        img.Freeze();
        return FrameCache[key] = img;
    }

    private static DrawingGroup FrameDrawing(Pose pose, Rig rig, int frame, bool blink, string? accessory = null)
    {
        var key = (pose, rig.Name, frame, blink, accessory);
        if (FrameSource.TryGetValue(key, out var dg)) return dg;
        var doc = new XDocument(Template(pose));
        var groups = doc.Descendants().Where(e => e.Name.LocalName == "g" && e.Attribute("class") is not null)
            .ToLookup(e => e.Attribute("class")!.Value);
        if (accessory is not null && Accessories.TryGetValue(accessory, out var acc) && groups[acc.Group].FirstOrDefault() is { } host)
            host.Add(XElement.Parse($"<g xmlns=\"http://www.w3.org/2000/svg\" class=\"acc-{accessory}\">{acc.Svg}</g>"));
        foreach (var m in MiloRig.Frame(rig, frame, blink))
        {
            if (!MiloRig.Pivots.TryGetValue(m.Part, out var pv)) continue;
            var t = new StringBuilder();
            if (m.Dx != 0 || m.Dy != 0) t.Append(Inv($"translate({m.Dx} {m.Dy}) "));
            if (m.Angle != 0) t.Append(Inv($"rotate({m.Angle} {pv.X} {pv.Y}) "));
            if (m.ScaleY != 1) t.Append(Inv($"translate(0 {pv.Y}) scale(1 {m.ScaleY}) translate(0 {-pv.Y})"));
            if (t.Length == 0) continue;
            foreach (var g in groups[m.Part]) g.SetAttributeValue("transform", t.ToString().Trim());
        }
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(doc.ToString(SaveOptions.DisableFormatting)));
        using var reader = new FileSvgReader(new WpfDrawingSettings { IncludeRuntime = false, TextAsGeometry = true });
        dg = reader.Read(ms) ?? new DrawingGroup();
        dg.Freeze();
        return FrameSource[key] = dg;
    }

    private static string Inv(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);

    private static XDocument Template(Pose pose)
    {
        if (Templates.TryGetValue(pose, out var doc)) return doc;
        using var stream = Application.GetResourceStream(Uri(pose))!.Stream;
        return Templates[pose] = XDocument.Load(stream);
    }

    private static Uri Uri(Pose pose) => new($"pack://application:,,,/Assets/Milo/{pose.ToString().ToLowerInvariant()}.svg");

    /// <summary>Dựng sẵn các khung hay dùng lúc máy rảnh để lần đầu Milo hiện không bị giật.</summary>
    public static void Prewarm(Dispatcher ui)
    {
        var jobs = new Queue<(Pose, Rig, int)>();
        foreach (var clip in Enum.GetValues<Clip>())
        {
            if (clip == Clip.Gone) continue;
            var pose = Catalog.ClipPose.TryGetValue(clip, out var p) ? p : Pose.Idle;
            var rig = MiloRig.For(clip, tired: false);
            for (var f = 0; f < rig.Frames; f++) jobs.Enqueue((pose, rig, f));
        }
        void Next()
        {
            if (jobs.Count == 0) return;
            var (pose, rig, f) = jobs.Dequeue();
            FrameDrawing(pose, rig, f, blink: false);
            ui.BeginInvoke(Next, DispatcherPriority.ApplicationIdle);
        }
        ui.BeginInvoke(Next, DispatcherPriority.ApplicationIdle);
    }

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
        using var stream = Application.GetResourceStream(Uri(pose))!.Stream;
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
