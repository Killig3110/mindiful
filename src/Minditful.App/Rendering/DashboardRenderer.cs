using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using static Minditful.App.Rendering.Ui;

namespace Minditful.App.Rendering;

/// <summary>Dashboard mở từ chóp đuôi (mục 9.6): "Hôm nay" với chùm nho 7 ngày, "Tuần này" với Office Vibe.</summary>
internal static class DashboardRenderer
{
    public static FrameworkElement Build(DashboardModel m, Action<string> onAct)
    {
        var sp = new StackPanel();
        void Add(UIElement el, double top = 10)
        {
            if (sp.Children.Count > 0 && el is FrameworkElement fe) fe.Margin = new Thickness(fe.Margin.Left, top, fe.Margin.Right, fe.Margin.Bottom);
            sp.Children.Add(el);
        }

        var close = new Button
        {
            Style = (Style)Application.Current.FindResource("Round"), Content = "×", Width = 26, Height = 26, MinHeight = 26, Padding = new Thickness(0),
            Background = Br("#EFE3D0"), Foreground = Br("#5B4A3C"), FontSize = 14, HorizontalContentAlignment = HorizontalAlignment.Center, ToolTip = "Đóng dashboard",
        };
        close.Click += (_, _) => onAct("close");

        if (m.Page == DashPage.Week)
        {
            var back = Link("← Hôm nay", () => onAct("today"));
            Add(Columns((back, Star), (close, Auto)));
            Add(Text("Tuần này", 15, "#3A2A1E", FontWeights.Bold));
            var days = new UniformGrid { Rows = 1, Columns = m.Days.Count };
            for (var i = 0; i < m.Days.Count; i++)
            {
                var w = m.Days[i];
                var ghost = w.Score < 0;
                var size = ghost ? 24 : 24 + w.Score / 100.0 * 18;
                var col = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };
                FrameworkElement grape = ghost
                    ? new System.Windows.Shapes.Ellipse
                    {
                        Width = size, Height = size, Fill = Br("#F4EEE6"), Stroke = Br("#CDBFAE"), StrokeThickness = 1.5,
                        StrokeDashArray = [2, 2], Margin = new Thickness(0, 0, 0, 4), ToolTip = "Chưa có dữ liệu",
                        HorizontalAlignment = HorizontalAlignment.Center,
                    }
                    : new Border
                    {
                        Width = size, Height = size, CornerRadius = new CornerRadius(size / 2), Background = GrapeBrush(w.Score),
                        BorderBrush = i == m.Days.Count - 1 ? Br("#F0A33D") : null, BorderThickness = new Thickness(i == m.Days.Count - 1 ? 3 : 0),
                        Margin = new Thickness(0, 0, 0, 4), ToolTip = $"{w.Score} điểm",
                    };
                col.Children.Add(grape);
                var lbl = Text(w.Label, 11, "#5B4A3C", wrap: false);
                lbl.HorizontalAlignment = HorizontalAlignment.Center;
                col.Children.Add(lbl);
                days.Children.Add(col);
            }
            Add(days);
            if (m.WeekSummary is { } ws)
            {
                Add(Text("THỐNG KÊ TUẦN (DỮ LIỆU TRÊN MÁY)", 10.5, "#9C8672", FontWeights.Bold));
                Add(new Border { Background = Brushes.White, CornerRadius = new CornerRadius(10), Padding = new Thickness(10, 7, 10, 7), Child = Text(ws, 12, "#3A2A1E") }, 6);
            }
            Add(Text("OFFICE VIBE", 10.5, "#9C8672", FontWeights.Bold));
            Add(VibeRow("Tập trung", m.Vibe.F, "#E8A33D"), 6);
            Add(VibeRow("Năng lượng", m.Vibe.E, "#D1495B"), 6);
            Add(VibeRow("Căng thẳng", m.Vibe.S, "#8C2F2F"), 6);
            if (m.Sprint is { } sp2)
            {
                Add(Text("AZURE BOARDS · " + sp2.Name.ToUpperInvariant(), 10.5, "#9C8672", FontWeights.Bold));
                Add(CardRenderer.Bar(sp2.Total > 0 ? sp2.Done / sp2.Total : 0, new Thickness(0)), 6);
                Add(Text($"Còn {sp2.DaysLeft} ngày · {sp2.Done:0.#}/{sp2.Total:0.#} điểm", 12, "#5B4A3C"), 6);
            }
            Add(Text("LỊCH HÔM NAY", 10.5, "#9C8672", FontWeights.Bold));
            foreach (var e in m.Events) Add(Row(e), 6);
            if (m.Events.Count == 0) Add(Text("Hôm nay không có cuộc họp nào.", 12, "#5B4A3C"), 6);
        }
        else
        {
            Add(Columns((Text("Hôm nay của bạn", 15, "#3A2A1E", FontWeights.Bold), Star), (close, Auto)));
            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            info.Children.Add(new System.Windows.Controls.TextBlock { Text = m.Score.ToString(CultureInfo.InvariantCulture), FontFamily = Serif, FontSize = 38, Foreground = Br("#3A2A1E") });
            var phrase = Text(m.Phrase, 12.5, "#3A2A1E", FontWeights.SemiBold);
            phrase.Margin = new Thickness(0, 4, 0, 0);
            info.Children.Add(phrase);
            var y = Text(m.YesterdayLine, 12, "#5B4A3C");
            y.Margin = new Thickness(0, 4, 0, 0);
            info.Children.Add(y);
            Add(new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(18), Padding = new Thickness(10, 8, 10, 8),
                Child = Columns((new GrapeCluster(m.Days) { Width = 118, Height = 131 }, Auto), (info, Star)),
            });
            Add(CardRenderer.Tiles(m.Tiles));
            if (m.Next is { } n) Add(Row(n));
            Add(Link("Xem cả tuần →", () => onAct("week")));
        }
        if (m.Insight is { } ins)
        {
            var box = new Border
            {
                Background = Br("#EFE7FA"), CornerRadius = new CornerRadius(10), Padding = new Thickness(10, 7, 10, 7),
                Child = Text(ins, 12, "#3B2E66"),
            };
            Add(box, 8);
        }
        if (m.StatusNote is { } note)
        {
            var t = Text(note, 11, "#9C8672");
            Add(t, 8);
        }

        return new Border
        {
            Width = 350, MaxHeight = 330, Background = Br("#FBF3E7"), CornerRadius = new CornerRadius(26), Padding = new Thickness(17, 15, 17, 15),
            Effect = new DropShadowEffect { BlurRadius = 50, ShadowDepth = 20, Direction = 270, Opacity = .3, Color = Rgb("#2B211A") },
            Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, Content = sp },
        };
    }

    private static Button Link(string text, Action click)
    {
        var b = new Button
        {
            Style = (Style)Application.Current.FindResource("Flat"), Content = text, Foreground = Br("#B85A34"), FontWeight = FontWeights.Bold,
            FontSize = 12.5, Padding = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Left, Background = Brushes.Transparent,
        };
        b.Click += (_, _) => click();
        return b;
    }

    private static FrameworkElement Row(DashRow r)
    {
        var tag = new Border { Background = Br(r.TagBg), CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 2, 8, 2), Child = Text(r.Tag, 10.5, r.TagFg, FontWeights.Bold, false), Margin = new Thickness(8, 0, 0, 0) };
        var name = Text(r.Name, 12, "#3A2A1E", wrap: false);
        name.VerticalAlignment = VerticalAlignment.Center;
        var time = Text(r.Time, 12, "#5B4A3C", wrap: false);
        time.VerticalAlignment = VerticalAlignment.Center;
        // Mức nặng cuộc họp 1–5 (luật hoặc Claude); rê chuột để xem nhận xét
        FrameworkElement load = new Border();
        if (r.Load is { } l)
        {
            var dots = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
            for (var i = 1; i <= 5; i++)
            {
                var d = Dot(l >= 4 ? "#D1495B" : l == 3 ? "#E8A33D" : "#7FA65A", 6);
                d.Opacity = i <= l ? 1 : .2;
                d.Margin = new Thickness(0, 0, 2, 0);
                dots.Children.Add(d);
            }
            load = dots;
        }
        return new Border
        {
            Background = Brushes.White, CornerRadius = new CornerRadius(10), Padding = new Thickness(9, 6, 9, 6), ToolTip = r.Note,
            Child = Columns((time, Px(48)), (name, Star), (load, Auto), (tag, Auto)),
        };
    }

    private static FrameworkElement VibeRow(string label, int v, string color)
    {
        var dots = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        for (var i = 1; i <= 5; i++)
        {
            var d = Dot(color, 13);
            d.Opacity = i <= v ? 1 : .2;
            d.Margin = new Thickness(0, 0, 4, 0);
            dots.Children.Add(d);
        }
        return Columns((Text(label, 12, "#3A2A1E", wrap: false), Px(78)), (dots, Star), (Text($"{v}/5", 12, "#3A2A1E", wrap: false), Px(38)));
    }

    public static RadialGradientBrush GrapeBrush(int score)
    {
        var c = Present.GrapeColors[Present.GrapeKey(score)];
        var b = new RadialGradientBrush { GradientOrigin = new Point(.36, .30), Center = new Point(.36, .30), RadiusX = .75, RadiusY = .75 };
        b.GradientStops.Add(new GradientStop(Rgb(c[0]), 0));
        b.GradientStops.Add(new GradientStop(Rgb(c[1]), .47));
        b.GradientStops.Add(new GradientStop(Rgb(c[2]), 1));
        b.Freeze();
        return b;
    }
}

/// <summary>Chùm nho 7 ngày: 6 ngày trước xếp 3-2-1, quả hôm nay nằm dưới cùng có vòng cam nét đứt.</summary>
internal sealed class GrapeCluster(IReadOnlyList<DayScore> days) : FrameworkElement
{
    private static readonly Point[] Pos = [new(66, 68), new(100, 64), new(134, 68), new(83, 102), new(117, 102), new(100, 136)];
    private static readonly Pen Outline = Freeze(new Pen(Br("#2E1A10"), 3));
    private static readonly Pen Stem = Freeze(new Pen(Br("#5A3A22"), 5.5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round });
    private static readonly Pen Ring = Freeze(new Pen(Br("#F0A33D"), 3.5) { DashStyle = new DashStyle([1.4, 1.4], 0) });
    private static readonly Pen GhostPen = Freeze(new Pen(Br("#CDBFAE"), 2) { DashStyle = new DashStyle([2, 2], 0) });

    private static Pen Freeze(Pen p)
    {
        p.Freeze();
        return p;
    }

    protected override void OnRender(DrawingContext dc)
    {
        var k = Math.Min(ActualWidth / 200, ActualHeight / 222);
        dc.PushTransform(new ScaleTransform(k, k));
        dc.DrawGeometry(null, Stem, Geometry.Parse("M100 8 C 95 20, 105 32, 100 46"));
        var leaf = new LinearGradientBrush(Rgb("#B2DC82"), Rgb("#5E9A3C"), 45);
        dc.DrawGeometry(leaf, new Pen(Br("#2E1A10"), 2.6), Geometry.Parse("M103 26 C 122 6, 154 10, 158 30 C 142 42, 116 42, 103 26 Z"));
        var shine = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255));
        var past = days.Take(days.Count - 1).TakeLast(6).ToList();
        for (var i = 0; i < past.Count; i++)
        {
            var p = Pos[i];
            if (past[i].Score < 0)
            {
                dc.DrawEllipse(Br("#F4EEE6"), GhostPen, p, 18, 18); // ngày chưa có dữ liệu
                continue;
            }
            dc.DrawEllipse(DashboardRenderer.GrapeBrush(past[i].Score), Outline, p, 20, 20);
            DrawShine(dc, shine, new Point(p.X - 6, p.Y - 7), 5.5, 3.5);
        }
        if (days.Count > 0)
        {
            var today = days[^1];
            dc.DrawEllipse(DashboardRenderer.GrapeBrush(today.Score), Outline, new Point(100, 176), 23, 23);
            dc.DrawEllipse(null, Ring, new Point(100, 176), 29, 29);
            DrawShine(dc, shine, new Point(92, 167), 7, 4.5);
        }
        dc.Pop();
    }

    private static void DrawShine(DrawingContext dc, Brush b, Point c, double rx, double ry)
    {
        dc.PushTransform(new RotateTransform(-30, c.X, c.Y));
        dc.DrawEllipse(b, null, c, rx, ry);
        dc.Pop();
    }

    protected override Size MeasureOverride(Size availableSize) => new(Width, Height);

    // Chạm một quả nho → hiện ngày và điểm của quả đó (mục 9.6)
    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        var k = Math.Min(ActualWidth / 200, ActualHeight / 222);
        var p = e.GetPosition(this);
        p = new Point(p.X / k, p.Y / k);
        var past = days.Take(days.Count - 1).TakeLast(6).ToList();
        string? tip = null;
        for (var i = 0; i < past.Count; i++)
            if ((Pos[i] - p).Length <= 20) tip = past[i].Score < 0 ? "Chưa có dữ liệu" : $"{past[i].Label}: {past[i].Score} điểm";
        if (days.Count > 0 && (new Point(100, 176) - p).Length <= 23) tip = $"Hôm nay: {days[^1].Score} điểm";
        ToolTip = tip;
    }

    protected override HitTestResult HitTestCore(PointHitTestParameters p) => new PointHitTestResult(this, p.HitPoint);
}
