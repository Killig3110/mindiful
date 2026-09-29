using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using static Minditful.App.Rendering.Ui;

namespace Minditful.App.Rendering;

/// <summary>
/// Trái cây vẽ tay cho dashboard quanh Milo (khớp bản xem trước "Vườn trái cây của Milo").
/// Mỗi hàm trả về Canvas 100×100 đơn vị, bọc trong Viewbox khi dùng. Hình đổi theo số liệu, có hoạt ảnh nhẹ.
/// </summary>
internal static class FruitArt
{
    private const string Outline = "#2E1A10";

    public static FrameworkElement Build(FruitItem f) => f.Kind switch
    {
        FruitKind.Grape => Grape(f.Score),
        FruitKind.Bunch => Bunch(f.Days ?? []),
        FruitKind.Orange => Orange(f.Total, f.Done),
        FruitKind.Cherries => Cherries(f.Available ? f.Count : 0),
        _ => Apple(f.Available ? f.Progress : 0),
    };

    private static Canvas Box(double h = 100) => new() { Width = 100, Height = h };

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    private static System.Windows.Shapes.Path P(string data, string? fill, string? stroke = null, double sw = 0, bool round = false) => new()
    {
        Data = Geometry.Parse(data), Fill = fill is null ? null : Br(fill), Stroke = stroke is null ? null : Br(stroke), StrokeThickness = sw,
        StrokeStartLineCap = round ? PenLineCap.Round : PenLineCap.Flat, StrokeEndLineCap = round ? PenLineCap.Round : PenLineCap.Flat,
        StrokeLineJoin = PenLineJoin.Round,
    };

    private static Ellipse Circle(Canvas c, double cx, double cy, double r, Brush? fill, string? stroke = null, double sw = 0)
    {
        var e = new Ellipse { Width = r * 2, Height = r * 2, Fill = fill, Stroke = stroke is null ? null : Br(stroke), StrokeThickness = sw };
        Canvas.SetLeft(e, cx - r);
        Canvas.SetTop(e, cy - r);
        c.Children.Add(e);
        return e;
    }

    private static void Shine(Canvas c, double cx, double cy, double rx, double ry)
    {
        var e = new Ellipse { Width = rx * 2, Height = ry * 2, Fill = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)), IsHitTestVisible = false,
            RenderTransform = new RotateTransform(-30, rx, ry) };
        Canvas.SetLeft(e, cx - rx);
        Canvas.SetTop(e, cy - ry);
        c.Children.Add(e);
    }

    private static void Leaf(Canvas c, string data) => c.Children.Add(P(data, "#8CC063", Outline, 2));

    public static RadialGradientBrush GrapeBrush(int score)
    {
        var col = Present.GrapeColors[Present.GrapeKey(score)];
        var b = new RadialGradientBrush { GradientOrigin = new Point(.36, .30), Center = new Point(.36, .30), RadiusX = .75, RadiusY = .75 };
        b.GradientStops.Add(new GradientStop(Rgb(col[0]), 0));
        b.GradientStops.Add(new GradientStop(Rgb(col[1]), .47));
        b.GradientStops.Add(new GradientStop(Rgb(col[2]), 1));
        b.Freeze();
        return b;
    }

    private static void Loop(Animatable target, DependencyProperty dp, double from, double to, double seconds, double delay = 0) =>
        target.BeginAnimation(dp, new DoubleAnimation(from, to, TimeSpan.FromSeconds(seconds))
        {
            AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, BeginTime = TimeSpan.FromSeconds(delay),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        });

    // ---------------- NHO: mood — căng mọng khi điểm cao, héo nhăn khi thấp ----------------
    public static Canvas Grape(int score)
    {
        var c = Box();
        var r = 20 + score / 100.0 * 10;
        c.Children.Add(P($"M50 {F(60 - r)} C 48 {F(50 - r)}, 52 {F(44 - r)}, 50 {F(38 - r)}", null, "#5A3A22", 4, true));
        Leaf(c, $"M51 {F(46 - r)} C 60 {F(36 - r)}, 76 {F(38 - r)}, 78 {F(48 - r)} C 70 {F(54 - r)}, 58 {F(54 - r)}, 51 {F(46 - r)} Z");
        var body = Box();
        Circle(body, 50, 60, r, GrapeBrush(score), Outline, 3);
        Shine(body, 50 - r * .35, 60 - r * .4, r * .28, r * .17);
        if (score < 45)
        {
            body.Children.Add(P($"M{F(50 - r * .5)} {F(60 - r * .1)} q6 -5 12 0 q6 5 12 0", null, "#592E1A10", 1.6));
            body.Children.Add(P($"M{F(50 - r * .35)} {F(60 + r * .35)} q5 -4 10 0 q5 4 10 0", null, "#4D2E1A10", 1.4));
        }
        var plump = new ScaleTransform(1, 1, 50, 60);
        body.RenderTransform = new TransformGroup { Children = { plump, new RotateTransform(score < 45 ? 8 : 0, 50, 60) } };
        Loop(plump, ScaleTransform.ScaleXProperty, 1, 1.04, 1.3);
        Loop(plump, ScaleTransform.ScaleYProperty, 1, .97, 1.3);
        c.Children.Add(body);
        if (score >= 80)
        {
            var spark = P("M80 24 l2 6 6 2 -6 2 -2 6 -2 -6 -6 -2 6 -2z", "#F0A33D");
            spark.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(.9)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
            c.Children.Add(spark);
        }
        return c;
    }

    // ---------------- CHÙM NHO 7 NGÀY: 3-2-1 + hôm nay; ngày chưa có dữ liệu là quả mờ ----------------
    private static readonly Point[] BunchPos = [new(26, 36), new(50, 34), new(74, 36), new(38, 56), new(62, 56), new(50, 74), new(50, 94)];

    public static Canvas Bunch(IReadOnlyList<DayScore> days)
    {
        var c = Box(112);
        var root = new Canvas { RenderTransform = new TranslateTransform(0, 6) };
        root.Children.Add(P("M50 0 C 47 10, 53 18, 50 24", null, "#5A3A22", 3.5, true));
        Leaf(root, "M52 12 C 62 0, 80 4, 82 14 C 72 20, 60 20, 52 12 Z");
        var list = days.TakeLast(7).ToList();
        for (var i = 0; i < list.Count && i < BunchPos.Length; i++)
        {
            var (p, d) = (BunchPos[i], list[i]);
            var today = i == list.Count - 1;
            var r = today ? 12 : 11;
            Ellipse e;
            if (d.Score < 0)
            {
                e = Circle(root, p.X, p.Y, r - 1, Br("#F4EEE6"), "#CDBFAE", 2);
                e.StrokeDashArray = [2, 2];
                e.ToolTip = "Chưa có dữ liệu";
            }
            else
            {
                e = Circle(root, p.X, p.Y, r, GrapeBrush(d.Score), Outline, 2.4);
                e.ToolTip = $"{(today ? "Hôm nay" : d.Label)}: {d.Score} điểm";
                Shine(root, p.X - 3.5, p.Y - 4, 3.4, 2);
            }
            e.Cursor = System.Windows.Input.Cursors.Hand;
            if (today)
            {
                var ring = Circle(root, p.X, p.Y, 16, null, "#F0A33D", 2.2);
                ring.StrokeDashArray = [1.4, 1.4];
                ring.IsHitTestVisible = false;
            }
        }
        c.Children.Add(root);
        return c;
    }

    // ---------------- CAM: cuộc họp — mỗi múi 1 cuộc, múi đã ăn = đã họp xong ----------------
    public static Canvas Orange(int total, int done)
    {
        var c = Box();
        Circle(c, 50, 50, 44, Br("#F39A2B"), Outline, 3);
        Circle(c, 50, 50, 38, Br("#FFF1D9"));
        var n = Math.Max(total, 1);
        for (var i = 0; i < n; i++)
        {
            double a0 = (double)i / n * Math.PI * 2 - Math.PI / 2 + .05, a1 = (double)(i + 1) / n * Math.PI * 2 - Math.PI / 2 - .05;
            Point Pt(double a, double r) => new(50 + Math.Cos(a) * r, 50 + Math.Sin(a) * r);
            var fig = new PathFigure { StartPoint = Pt(a0, 7), IsClosed = true };
            fig.Segments.Add(new LineSegment(Pt(a0, 34), true));
            fig.Segments.Add(new ArcSegment(Pt(a1, 34), new Size(34, 34), 0, n == 1, SweepDirection.Clockwise, true));
            fig.Segments.Add(new LineSegment(Pt(a1, 7), true));
            fig.Segments.Add(new ArcSegment(Pt(a0, 7), new Size(7, 7), 0, false, SweepDirection.Counterclockwise, true));
            var eaten = total > 0 && i < done;
            var wedge = new System.Windows.Shapes.Path
            {
                Data = new PathGeometry([fig]), Fill = Br(eaten ? "#FFF4E0" : total > 0 ? "#FFB347" : "#FFD9A3"),
                Stroke = Br(eaten ? "#E6C08F" : "#F08A1C"), StrokeThickness = eaten ? 1.4 : 1.2, Opacity = 0,
            };
            if (eaten) wedge.StrokeDashArray = [2.2, 1.5];
            wedge.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(.4)) { BeginTime = TimeSpan.FromMilliseconds(250 + i * 60) });
            c.Children.Add(wedge);
            if (!eaten)
            {
                var mid = (a0 + a1) / 2;
                var (p1, p2) = (Pt(mid, 14), Pt(mid, 28));
                c.Children.Add(new Line { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = Br("#FFE3B8"), StrokeThickness = 1.6, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, Opacity = .8 });
            }
        }
        Circle(c, 50, 50, 4, Br("#FFF1D9"));
        return c;
    }

    // ---------------- ANH ĐÀO: email chờ — mỗi quả 1 email ----------------
    public static Canvas Cherries(int count)
    {
        var c = Box();
        var n = Math.Min(count, 5);
        if (n == 0)
        {
            c.Children.Add(P("M50 12 C 40 30, 36 50, 34 66 M50 12 C 60 30, 64 50, 66 66", null, "#5A3A22", 3, true));
            Leaf(c, "M50 12 C 58 4, 72 6, 76 14 C 68 20, 56 20, 50 12 Z");
            Circle(c, 34, 70, 3, Br("#C9A77A"));
            Circle(c, 66, 70, 3, Br("#C9A77A"));
            return c;
        }
        double[][] xs = [[50], [34, 66], [26, 50, 74], [20, 40, 60, 80], [16, 33, 50, 67, 84]];
        double[][] ys = [[68], [68, 68], [66, 72, 66], [64, 70, 70, 64], [62, 68, 72, 68, 62]];
        var r = n > 3 ? 10 : 12;
        for (var i = 0; i < n; i++)
        {
            double x = xs[n - 1][i], y = ys[n - 1][i];
            c.Children.Add(P($"M50 12 C {F(50 + (x - 50) * .3)} 30, {F(x)} {F(y - 30)}, {F(x)} {F(y - r + 1)}", null, "#5A3A22", 2.6, true));
        }
        Leaf(c, "M50 12 C 58 4, 72 6, 76 14 C 68 20, 56 20, 50 12 Z");
        for (var i = 0; i < n; i++)
        {
            double x = xs[n - 1][i], y = ys[n - 1][i];
            var g = new Canvas();
            Circle(g, x, y, r, Br("#D1334A"), Outline, 2.4);
            Shine(g, x - 3.5, y - 4, 3.2, 2);
            var sway = new RotateTransform(0, 50, 12);
            g.RenderTransform = sway;
            Loop(sway, RotateTransform.AngleProperty, -4, 4, 1.6, i * .18);
            c.Children.Add(g);
        }
        return c;
    }

    // ---------------- TÁO CẮN DỞ: sprint — càng gần xong càng bị cắn nhiều, xong hẳn còn lõi ----------------
    private static readonly Point[] Bites = [new(86, 36), new(88, 52), new(82, 68), new(68, 84), new(48, 90), new(30, 82), new(15, 64), new(14, 44)];
    private const string AppleBody = "M50 26 C 38 16, 14 20, 14 46 C 14 72, 32 92, 50 86 C 68 92, 86 72, 86 46 C 86 20, 62 16, 50 26 Z";

    public static Canvas Apple(double progress)
    {
        var c = Box();
        var k = (int)Math.Round(Math.Clamp(progress, 0, 1) * Bites.Length);
        Geometry Holes(double r)
        {
            var g = new GeometryGroup { FillRule = FillRule.Nonzero };
            foreach (var p in Bites.Take(k)) g.Children.Add(new EllipseGeometry(p, r, r));
            return g;
        }
        var body = Geometry.Parse(AppleBody);
        var skin = k == 0 ? body : new CombinedGeometry(GeometryCombineMode.Exclude, body, Holes(15));
        var flesh = k == 0 ? body : new CombinedGeometry(GeometryCombineMode.Exclude, body, Holes(12));

        c.Children.Add(P("M50 26 C 50 18, 52 12, 56 8", null, "#5A3A22", 3.4, true));
        Leaf(c, "M54 14 C 62 4, 78 6, 80 14 C 72 20, 60 20, 54 14 Z");
        var fruit = new Canvas();
        fruit.Children.Add(new System.Windows.Shapes.Path { Data = flesh, Fill = Br("#FFF3D6"), Stroke = Br("#E8C98E"), StrokeThickness = 1.2 });
        fruit.Children.Add(new System.Windows.Shapes.Path { Data = skin, Fill = Br("#E24B4A"), Stroke = Br(Outline), StrokeThickness = 3, StrokeLineJoin = PenLineJoin.Round });
        var shine = new Canvas { Clip = skin };
        Shine(shine, 32, 40, 6, 3.6);
        fruit.Children.Add(shine);
        if (progress >= .75)
        {
            fruit.Children.Add(Seed(46, 56, -15));
            fruit.Children.Add(Seed(54, 56, 15));
        }
        // táo "rung" như vừa bị cắn khi hiện ra
        var wiggle = new RotateTransform(0, 50, 60);
        fruit.RenderTransform = wiggle;
        var a = new DoubleAnimationUsingKeyFrames { BeginTime = TimeSpan.FromSeconds(.35), Duration = TimeSpan.FromSeconds(.6) };
        a.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)));
        a.KeyFrames.Add(new LinearDoubleKeyFrame(-7, KeyTime.FromPercent(.25)));
        a.KeyFrames.Add(new LinearDoubleKeyFrame(5, KeyTime.FromPercent(.55)));
        a.KeyFrames.Add(new LinearDoubleKeyFrame(-2, KeyTime.FromPercent(.8)));
        a.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1)));
        wiggle.BeginAnimation(RotateTransform.AngleProperty, a);
        c.Children.Add(fruit);
        return c;
    }

    private static Ellipse Seed(double cx, double cy, double angle)
    {
        var e = new Ellipse { Width = 5.2, Height = 8, Fill = Br("#4A2A18"), RenderTransform = new RotateTransform(angle, 2.6, 4) };
        Canvas.SetLeft(e, cx - 2.6);
        Canvas.SetTop(e, cy - 4);
        return e;
    }
}
