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
    /// Khăn quàng, dây thẻ nằm trong "torso" (dưới cằm), kẹp hoa, mũ nồi, mũ Bosch nằm trong "head".
    /// </summary>
    /// <summary>
    /// Phụ kiện tủ đồ, vẽ trong hệ toạ độ SVG của Milo và gắn vào nhóm bộ phận để đi theo đầu/thân khi Milo cử động.
    /// Mỗi món có thể gồm nhiều phần: "torso" (khăn, dây thẻ nằm dưới cằm), "head" (mũ, kẹp hoa).
    /// </summary>
    private static readonly Dictionary<string, (string Group, string Svg)[]> Accessories = new()
    {
        ["scarf"] = [("torso", """
            <path d="M90,131 Q120,147 150,131 L152,143 Q120,161 88,143 Z" fill="#E0526B" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M100,137 L103,150 M112,141 L113,154 M128,141 L127,154 M140,137 L137,150" stroke="#FFF3E6" stroke-width="3" stroke-linecap="round"/>
            <path d="M131,147 L142,174 Q134,178 126,175 L122,150 Z" fill="#E0526B" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            """)],
        ["flower"] = [("head", """
            <g fill="#FF9FB2" stroke="#45231F" stroke-width="2.5"><circle cx="152" cy="44" r="7"/><circle cx="162" cy="50" r="7"/>
            <circle cx="159" cy="61" r="7"/><circle cx="146" cy="61" r="7"/><circle cx="143" cy="50" r="7"/></g>
            <circle cx="152.5" cy="53" r="5" fill="#FFD166" stroke="#45231F" stroke-width="2.5"/>
            """)],
        ["beret"] = [("head", """
            <path d="M86,50 C88,28 150,22 158,44 C160,54 146,56 120,56 C98,56 84,58 86,50 Z" fill="#7261B0" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M92,52 Q120,60 152,52" fill="none" stroke="#5A4A96" stroke-width="3"/>
            <path d="M122,27 q2,-8 8,-8" fill="none" stroke="#45231F" stroke-width="3.5" stroke-linecap="round"/>
            """)],
        // Đồng phục Bosch (3 màu đặc đỏ · xanh dương · xanh lá): dây thẻ xanh vòng qua cổ, móc kẹp, thẻ nhân viên; mũ lưỡi trai đỏ có băng 3 màu
        ["bosch"] =
        [
            ("torso", """
            <path d="M103,141 C107,150 114,156 120,159 C126,156 133,150 137,141" fill="none" stroke="#45231F" stroke-width="7" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M103,141 C107,150 114,156 120,159 C126,156 133,150 137,141" fill="none" stroke="#007BC0" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>
            <rect x="116" y="158" width="8" height="7" rx="2" fill="#C9CED6" stroke="#45231F" stroke-width="2"/>
            <rect x="107" y="164" width="26" height="30" rx="4" fill="#FFFFFF" stroke="#45231F" stroke-width="2.5"/>
            <path d="M107,168 a4,4 0 0 1 4,-4 h18 a4,4 0 0 1 4,4 v3 h-26 Z" fill="#E20015"/>
            <rect x="111" y="175" width="12" height="3" rx="1.5" fill="#C9B6A0"/><rect x="111" y="181" width="18" height="3" rx="1.5" fill="#C9B6A0"/>
            <rect x="111" y="187.5" width="6" height="3" fill="#E20015"/><rect x="117" y="187.5" width="6" height="3" fill="#007BC0"/><rect x="123" y="187.5" width="6" height="3" fill="#00884A"/>
            """),
            ("head", """
            <path d="M84,52 C84,26 156,20 160,46 L160,54 C140,50 104,50 84,56 Z" fill="#E20015" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M120,26 L120,51" stroke="#B80012" stroke-width="2"/>
            <circle cx="120" cy="25" r="4" fill="#E20015" stroke="#45231F" stroke-width="2.5"/>
            <path d="M87,52.5 C94,51 101,50.3 108,50" fill="none" stroke="#FFFFFF" stroke-width="4.5" stroke-linecap="round"/>
            <path d="M112,49.8 C119,49.6 127,49.6 134,49.8" fill="none" stroke="#007BC0" stroke-width="4.5" stroke-linecap="round"/>
            <path d="M138,50 C145,50.3 151,50.8 157,51.6" fill="none" stroke="#00884A" stroke-width="4.5" stroke-linecap="round"/>
            <path d="M150,50 C170,48 186,52 190,58 C176,60 162,58 152,56 Z" fill="#B80012" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            """),
        ],
        // Kính tròn: 2 tròng trong suốt đúng vị trí mắt (102,96) và (138,96)
        ["glasses"] = [("head", """
            <circle cx="102" cy="96" r="14" fill="#FFFFFF" fill-opacity=".18" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <circle cx="138" cy="96" r="14" fill="#FFFFFF" fill-opacity=".18" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M116,94 Q120,90 124,94" fill="none" stroke="#45231F" stroke-width="3" stroke-linecap="round"/>
            <path d="M88,93 L79,89 M152,93 L161,89" stroke="#45231F" stroke-width="3" stroke-linecap="round"/>
            """)],
        // Kính râm
        ["sunglasses"] = [("head", """
            <path d="M85,87 H118 V95 Q118,109 102,109 Q86,109 85,95 Z" fill="#2B211A" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M122,87 H155 V95 Q154,109 138,109 Q122,109 122,95 Z" fill="#2B211A" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M118,90 Q120,87 122,90" fill="none" stroke="#45231F" stroke-width="3"/>
            <path d="M90,92 L99,92 M127,92 L136,92" stroke="#8C7A6B" stroke-width="2.5" stroke-linecap="round"/>
            <path d="M85,89 L77,86 M155,89 L163,86" stroke="#45231F" stroke-width="3" stroke-linecap="round"/>
            """)],
        // Nơ cổ xanh ngọc ngay dưới cằm
        ["bowtie"] = [("torso", """
            <path d="M120,147 L103,138 Q100,147 103,156 Z" fill="#3E8E9E" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M120,147 L137,138 Q140,147 137,156 Z" fill="#3E8E9E" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M107,143 L112,146 M107,151 L112,148 M133,143 L128,146 M133,151 L128,148" stroke="#2A6A76" stroke-width="2" stroke-linecap="round"/>
            <rect x="115" y="142" width="10" height="10" rx="3" fill="#2F7A87" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            """)],
        // Tai nghe: quai vòng qua đỉnh đầu, 2 chụp tai ở 2 bên má
        ["headphones"] = [("head", """
            <path d="M72,90 C66,24 174,24 168,90" fill="none" stroke="#45231F" stroke-width="9" stroke-linecap="round"/>
            <path d="M72,90 C66,24 174,24 168,90" fill="none" stroke="#5B5FC7" stroke-width="4.5" stroke-linecap="round"/>
            <rect x="60" y="80" width="18" height="30" rx="8" fill="#5B5FC7" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <rect x="162" y="80" width="18" height="30" rx="8" fill="#5B5FC7" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <rect x="64" y="86" width="5" height="18" rx="2.5" fill="#8E91E0"/><rect x="171" y="86" width="5" height="18" rx="2.5" fill="#8E91E0"/>
            """)],
        // Đồ cầm tay nằm trong nhóm tay phải (armR) để đi theo khi Milo vẫy/chỉ
        ["coffee"] = [("armR", """
            <path d="M150,162 q-4,-6 0,-11 M157,162 q-4,-6 0,-11" fill="none" stroke="#C9B6A0" stroke-width="2.5" stroke-linecap="round"/>
            <path d="M163,174 q9,0 8,8 q-1,7 -9,6" fill="none" stroke="#45231F" stroke-width="3"/>
            <path d="M141,166 H165 L162,193 Q153,197 144,193 Z" fill="#FFFFFF" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M142.5,175 H163.8 L163,183 H143.5 Z" fill="#B85A34"/>
            <ellipse cx="153" cy="166" rx="12" ry="3.5" fill="#6B3A1E" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            """)],
        // Trà sữa trân châu
        ["bubbletea"] = [("armR", """
            <path d="M154,164 L161,138" stroke="#45231F" stroke-width="7" stroke-linecap="round"/>
            <path d="M154,164 L161,138" stroke="#E0526B" stroke-width="4" stroke-linecap="round"/>
            <path d="M141,164 H166 L162,196 H145 Z" fill="#F3DDBF" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <g fill="#3A2A1E"><circle cx="149" cy="190" r="2.6"/><circle cx="155" cy="191" r="2.6"/><circle cx="160" cy="189" r="2.6"/><circle cx="152" cy="185" r="2.6"/><circle cx="158" cy="184" r="2.6"/></g>
            <path d="M139,165 Q153.5,154 168,165 Z" fill="#FFFFFF" fill-opacity=".85" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            """)],
        // Tết: bao lì xì
        ["lixi"] = [("armR", """
            <rect x="140" y="163" width="22" height="30" rx="3" fill="#D1242F" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M140,166 L151,176 L162,166" fill="none" stroke="#9E1620" stroke-width="2.5" stroke-linejoin="round"/>
            <circle cx="151" cy="183" r="5" fill="#F2C94C" stroke="#9E1620" stroke-width="1.5"/>
            <path d="M143,190 H159" stroke="#F2C94C" stroke-width="2"/>
            """)],
        // Trung thu: lồng đèn ông sao cầm trên que
        ["lantern"] = [("armR", """
            <path d="M148,188 L170,146" stroke="#45231F" stroke-width="6" stroke-linecap="round"/>
            <path d="M148,188 L170,146" stroke="#B07A3E" stroke-width="3" stroke-linecap="round"/>
            <path d="M170,146 L180,142" stroke="#45231F" stroke-width="2"/>
            <path d="M186.0,113.0 L190.7,125.5 L204.1,126.1 L193.6,134.5 L197.2,147.4 L186.0,140.0 L174.8,147.4 L178.4,134.5 L167.9,126.1 L181.3,125.5 Z" fill="#E8403A" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M186.0,123.0 L188.4,128.8 L194.6,129.2 L189.8,133.2 L191.3,139.3 L186.0,136.0 L180.7,139.3 L182.2,133.2 L177.4,129.2 L183.6,128.8 Z" fill="#F2C94C"/>
            <path d="M186,151 V166" stroke="#F2C94C" stroke-width="3" stroke-linecap="round"/>
            """)],
        // Halloween: mũ phù thuỷ
        ["witch"] = [("head", """
            <path d="M96,48 L124,-4 Q131,-12 136,-2 L146,48 Z" fill="#4B3A86" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M99,41 Q121,47 144,41" fill="none" stroke="#E8A33D" stroke-width="6"/>
            <rect x="116" y="37" width="10" height="9" rx="2" fill="none" stroke="#F2C94C" stroke-width="2.5"/>
            <ellipse cx="121" cy="50" rx="50" ry="9" fill="#3B2E66" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            """)],
        // Noel: mũ Noel chóp rủ sang phải
        ["santa"] = [("head", """
            <path d="M86,50 C88,20 146,10 164,32 C174,44 184,60 190,78 L178,80 C172,66 164,56 156,50 Z" fill="#D1242F" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            <path d="M84,52 Q120,38 160,50" fill="none" stroke="#45231F" stroke-width="16" stroke-linecap="round"/>
            <path d="M84,52 Q120,38 160,50" fill="none" stroke="#FFFFFF" stroke-width="11" stroke-linecap="round"/>
            <circle cx="186" cy="82" r="9" fill="#FFFFFF" stroke="#45231F" stroke-width="3" stroke-linejoin="round"/>
            """)],
    };

    /// <summary>Bỏ id không có hình, giữ thứ tự vẽ. null = không mặc gì.</summary>
    private static string? Known(string? outfit)
    {
        if (string.IsNullOrEmpty(outfit)) return null;
        var ids = outfit.Split('+', StringSplitOptions.RemoveEmptyEntries).Where(Accessories.ContainsKey).ToList();
        return ids.Count == 0 ? null : string.Join("+", ids);
    }

    /// <summary>
    /// Khung <paramref name="frame"/> của clip <paramref name="rig"/> ở tư thế <paramref name="pose"/>, kèm bộ đồ
    /// (<paramref name="accessory"/> = các id trong <see cref="Wardrobe"/> nối bằng "+", xem <see cref="Wardrobe.SkinKey"/>).
    /// </summary>
    public static DrawingImage Frame(Pose pose, Rig rig, int frame, bool blink, double saturation, string? accessory = null)
    {
        if (pose is Pose.Tired or Pose.Breathe or Pose.Greeting) blink = false; // các tư thế này không có mắt mở để chớp
        accessory = Known(accessory);
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
        foreach (var id in accessory?.Split('+') ?? [])
            if (Accessories.TryGetValue(id, out var parts))
                foreach (var (group, svg) in parts)
                    if (groups[group].FirstOrDefault() is { } host)
                        host.Add(XElement.Parse($"<g xmlns=\"http://www.w3.org/2000/svg\" class=\"acc-{id}\">{svg}</g>"));
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
