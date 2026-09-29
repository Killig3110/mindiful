using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Minditful.App.Rendering;
using Minditful.App.Services;
using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using static Minditful.App.Rendering.Ui;

namespace Minditful.App.Controls;

/// <summary>
/// Bảng chi tiết nhỏ (≈ 290×380, cỡ 1 thẻ của Milo) mở từ nút "Chi tiết" của dashboard 4 quả.
/// Cùng tông kem – cam nhạt của thẻ, bo tròn, trái cây mini thay cho biểu đồ. Hôm nay và Tuần này dùng chung khung.
/// </summary>
internal sealed class DetailDashboardView : Border
{
    public const double W = 290, MaxH = 400;
    private const string Ink = "#3A2A1E", Soft = "#7A6455", Line = "#F1DFC6";

    private readonly Action<string> _onAct;

    public DetailDashboardView(DetailDashboard model, Action<string> onAct)
    {
        _onAct = onAct;
        Width = W;
        Background = Br("#FFF9F1");
        BorderBrush = Br(Line);
        BorderThickness = new Thickness(1.5);
        CornerRadius = new CornerRadius(20);
        Padding = new Thickness(14, 12, 14, 12);
        Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 8, Direction = 270, Opacity = .22, Color = Rgb("#3A2A1E") };

        var body = new StackPanel();
        body.Children.Add(Header(model.Page));
        if (model.Today is { } d) BuildToday(body, d);
        if (model.Week is { } w) BuildWeek(body, w);
        Child = new ScrollViewer
        {
            Style = (Style)Application.Current.FindResource("ThinScroll"), MaxHeight = MaxH - 24,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = body,
        };
        System.Windows.Automation.AutomationProperties.SetName(this, model.Page == DashPage.Today ? "Chi tiết hôm nay" : "Chi tiết tuần này");
    }

    // ================= khung chung =================
    private FrameworkElement Header(DashPage page)
    {
        var back = SmallButton("‹ 4 quả", "detail", "#FFFFFF", "#B85A34");
        System.Windows.Automation.AutomationProperties.SetName(back, "Quay lại 4 quả");
        var seg = new Border
        {
            Background = Br("#F3E9DA"), CornerRadius = new CornerRadius(9), Padding = new Thickness(2),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    SmallButton("Hôm nay", "today", page == DashPage.Today ? "#FFFFFF" : "#00000000", page == DashPage.Today ? Ink : Soft),
                    SmallButton("Tuần", "week", page == DashPage.Week ? "#FFFFFF" : "#00000000", page == DashPage.Week ? Ink : Soft),
                },
            },
        };
        var close = SmallButton("×", "close", "#EFE3D0", "#5B4A3C");
        close.Width = 24;
        close.Margin = new Thickness(6, 0, 0, 0);
        System.Windows.Automation.AutomationProperties.SetName(close, "Đóng dashboard");
        var row = Columns((back, Auto), (new Border(), Star), (seg, Auto), (close, Auto));
        row.Margin = new Thickness(0, 0, 0, 10);
        return row;
    }

    private Button SmallButton(string text, string act, string bg, string fg)
    {
        var b = new Button
        {
            Style = (Style)Application.Current.FindResource("Round"), Content = text, Background = Br(bg), Foreground = Br(fg),
            FontSize = 11.5, FontWeight = FontWeights.SemiBold, Height = 24, MinHeight = 24, Padding = new Thickness(9, 0, 9, 0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        b.Click += (_, _) => _onAct(act);
        return b;
    }

    private static TextBlock Section(string text)
    {
        var t = Text(text, 11, Soft, FontWeights.SemiBold, false);
        t.Margin = new Thickness(0, 12, 0, 6);
        return t;
    }

    /// <summary>Bong bóng lời Milo: nền tím nhạt, góc trái trên nhọn nhẹ như đang nói.</summary>
    private static Border Bubble(string text, string bg = "#F3EDFF", string fg = "#3B2E66") => new()
    {
        Background = Br(bg), CornerRadius = new CornerRadius(4, 14, 14, 14), Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0, 8, 0, 0),
        Child = Text(text, 11.5, fg),
    };

    private static FrameworkElement Mini(FrameworkElement art, double size)
    {
        var v = new Viewbox { Width = size, Height = size, Child = art, VerticalAlignment = VerticalAlignment.Center };
        return v;
    }

    private static Border Score(int score, string band, string? delta, string deltaColor)
    {
        var big = new TextBlock { Text = score.ToString(), FontFamily = Serif, FontSize = 30, FontWeight = FontWeights.SemiBold, Foreground = Br(Ink), VerticalAlignment = VerticalAlignment.Center };
        var pill = Pill(band, Catalog.Bands.FirstOrDefault(b => b.Label == band)?.Bg ?? "#A58FE0", Catalog.Bands.FirstOrDefault(b => b.Label == band)?.Fg ?? "#2B1B55", 10.5);
        pill.Padding = new Thickness(8, 2, 8, 2);
        pill.CornerRadius = new CornerRadius(8);
        var right = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), Children = { pill } };
        if (delta is not null)
        {
            var dt = Text(delta, 11, deltaColor, FontWeights.SemiBold, false);
            dt.Margin = new Thickness(2, 3, 0, 0);
            right.Children.Add(dt);
        }
        return new Border { Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { big, right } } };
    }

    // ================= hôm nay =================
    private void BuildToday(StackPanel body, DetailToday d)
    {
        var grape = Mini(FruitArt.Grape(d.Score), 46);
        Bob(grape);
        var delta = d.Delta is { } x ? (x >= 0 ? $"▲ {x} so với hôm qua" : $"▼ {-x} so với hôm qua") : null;
        var top = Columns((grape, Auto), (Score(d.Score, d.Band, delta, d.Delta >= 0 ? "#2B7A4B" : "#B8433A"), Star));
        body.Children.Add(top);
        body.Children.Add(Bubble(d.Insight ?? d.Phrase));

        body.Children.Add(Section("Hôm nay của bạn"));
        body.Children.Add(Timeline(d));

        body.Children.Add(Section("Office Vibe"));
        body.Children.Add(VibeRow("Tập trung", d.Vibe.F, "#F39A2B"));
        body.Children.Add(VibeRow("Năng lượng", d.Vibe.E, "#E8414F"));
        body.Children.Add(VibeRow("Căng thẳng", d.Vibe.S, "#8C5BB5"));

        if (d.Upcoming.Count > 0)
        {
            body.Children.Add(Section("Sắp tới"));
            foreach (var r in d.Upcoming) body.Children.Add(MeetingRow(r));
        }

        var chips = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
        foreach (var (big, small) in d.Chips) chips.Children.Add(Chip(big, small));
        body.Children.Add(chips);
        if (d.Note is { } note)
        {
            var n = Text(note, 10.5, "#9C8672");
            n.Margin = new Thickness(0, 8, 0, 0);
            body.Children.Add(n);
        }
    }

    private static FrameworkElement Timeline(DetailToday d)
    {
        const double w = 260, h = 18;
        var c = new Canvas { Width = w, Height = h + 16 };
        var track = new Rectangle { Width = w, Height = h, RadiusX = 9, RadiusY = 9, Fill = Br("#F3E9DA") };
        c.Children.Add(track);
        foreach (var s in d.Segments)
        {
            var (fill, edge) = s.Kind switch
            {
                "heavy" => ("#FFB38A", "#E08A5A"),
                "focus" => ("#BFDDFB", "#8DBBE8"),
                "break" => ("#CFE9BF", "#9CC98A"),
                _ => ("#FFD7A8", "#EDB77A"),
            };
            var r = new Rectangle
            {
                Width = Math.Max(5, (s.To - s.From) * w), Height = h - 6, RadiusX = 5, RadiusY = 5, Fill = Br(fill), Stroke = Br(edge), StrokeThickness = 1,
                ToolTip = s.Label,
            };
            Canvas.SetLeft(r, s.From * w);
            Canvas.SetTop(r, 3);
            c.Children.Add(r);
        }
        if (d.Now is { } now)
        {
            var line = new Rectangle { Width = 2, Height = h + 4, Fill = Br("#E8772E"), RadiusX = 1, RadiusY = 1 };
            Canvas.SetLeft(line, now * w - 1);
            Canvas.SetTop(line, -2);
            var dot = new Ellipse { Width = 9, Height = 9, Fill = Br("#E8772E"), Stroke = Brushes.White, StrokeThickness = 2 };
            Canvas.SetLeft(dot, now * w - 4.5);
            Canvas.SetTop(dot, -5);
            c.Children.Add(line);
            c.Children.Add(dot);
            Pulse(dot);
        }
        var a = Text(d.StartLabel, 10, Soft, null, false);
        var b = Text(d.EndLabel, 10, Soft, null, false);
        Canvas.SetTop(a, h + 2);
        Canvas.SetTop(b, h + 2);
        Canvas.SetRight(b, 0);
        c.Children.Add(a);
        c.Children.Add(b);
        var legend = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };
        foreach (var (col, label) in new[] { ("#FFD7A8", "họp"), ("#FFB38A", "họp nặng"), ("#BFDDFB", "tập trung"), ("#CFE9BF", "nghỉ") })
        {
            var dot = Dot(col, 8);
            dot.Margin = new Thickness(0, 0, 4, 0);
            var t = Text(label, 10, Soft, null, false);
            t.Margin = new Thickness(0, 0, 10, 0);
            legend.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { dot, t } });
        }
        return new StackPanel { Children = { c, legend } };
    }

    /// <summary>1 hàng Office Vibe: 5 chấm tròn mềm, chấm đầy = điểm.</summary>
    private static FrameworkElement VibeRow(string label, int value, string color)
    {
        var dots = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        for (var i = 1; i <= 5; i++)
        {
            var on = i <= value;
            var e = new Ellipse
            {
                Width = 13, Height = 13, Margin = new Thickness(0, 0, 4, 0),
                Fill = on ? Br(color) : Br("#F1E6D6"), Stroke = on ? new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)) : null, StrokeThickness = 1,
            };
            dots.Children.Add(e);
        }
        var name = Text(label, 11.5, Ink, null, false);
        name.VerticalAlignment = VerticalAlignment.Center;
        var num = Text($"{value}/5", 11, Soft, FontWeights.SemiBold, false);
        num.VerticalAlignment = VerticalAlignment.Center;
        var row = Columns((name, Px(82)), (dots, Star), (num, Auto));
        row.Margin = new Thickness(0, 0, 0, 5);
        return row;
    }

    private static FrameworkElement MeetingRow(DashRow r)
    {
        var time = Text(r.Time, 11.5, Ink, FontWeights.SemiBold, false);
        time.VerticalAlignment = VerticalAlignment.Center;
        var name = Text(r.Name, 11.5, Ink, null, false);
        name.VerticalAlignment = VerticalAlignment.Center;
        name.Margin = new Thickness(8, 0, 6, 0);
        var load = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (r.Load is { } l)
            for (var i = 1; i <= 5; i++)
                load.Children.Add(new Ellipse { Width = 6, Height = 6, Margin = new Thickness(1.5, 0, 0, 0), Fill = Br(i <= l ? (l >= 4 ? "#E8772E" : "#F0B070") : "#EADCC8") });
        else load.Children.Add(Pill(r.Tag, r.TagBg, r.TagFg, 9.5));
        return new Border
        {
            Background = Brushes.White, CornerRadius = new CornerRadius(10), Padding = new Thickness(9, 6, 9, 6), Margin = new Thickness(0, 0, 0, 4),
            ToolTip = r.Note, Child = Columns((time, Px(38)), (name, Star), (load, Auto)),
        };
    }

    private static Border Chip(string big, string small) => new()
    {
        Background = Brushes.White, CornerRadius = new CornerRadius(10), Padding = new Thickness(8, 4, 9, 4), Margin = new Thickness(0, 0, 5, 5),
        Child = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { Text(big, 12, Ink, FontWeights.Bold, false), Spacer(4), Text(small, 11, Soft, null, false) },
        },
    };

    private static FrameworkElement Spacer(double w) => new Border { Width = w };

    // ================= tuần này =================
    private void BuildWeek(StackPanel body, DetailWeek w)
    {
        var bunch = Mini(FruitArt.Bunch(w.Days), 46);
        Bob(bunch);
        var delta = w.Diff is { } x ? (x >= 0 ? $"▲ {x} so với tuần trước" : $"▼ {-x} so với tuần trước") : "Trung bình tuần";
        var band = Catalog.Bands.First(b => w.Avg >= b.Min).Label;
        body.Children.Add(Columns((bunch, Auto), (Score(w.Avg, band, delta, w.Diff is < 0 ? "#B8433A" : "#2B7A4B"), Star)));

        body.Children.Add(Section("7 ngày gần nhất"));
        body.Children.Add(GrapeRow(w.Days));

        body.Children.Add(Section("Cả tuần"));
        var stats = new UniformGrid { Columns = 2 };
        foreach (var (big, small) in w.Stats)
        {
            stats.Children.Add(new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(12), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 5, 5),
                Child = new StackPanel { Children = { Text(big, 15, Ink, FontWeights.Bold, false), Text(small, 10.5, Soft, null, false) } },
            });
        }
        body.Children.Add(stats);

        if (w.Replies.Count > 0)
        {
            var total = w.Replies.Sum(r => r.Count);
            body.Children.Add(Section($"Bạn trả lời Milo · {total} lời nhắc"));
            var bar = new Grid { Height = 16, ClipToBounds = true };
            var legend = new WrapPanel { Margin = new Thickness(0, 5, 0, 0) };
            for (var i = 0; i < w.Replies.Count; i++)
            {
                var (label, n, color) = w.Replies[i];
                bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(n, GridUnitType.Star) });
                var cell = new Border
                {
                    Background = Br(color), Margin = new Thickness(i == 0 ? 0 : 1.5, 0, 0, 0),
                    CornerRadius = new CornerRadius(i == 0 ? 8 : 0, i == w.Replies.Count - 1 ? 8 : 0, i == w.Replies.Count - 1 ? 8 : 0, i == 0 ? 8 : 0),
                };
                Grid.SetColumn(cell, i);
                bar.Children.Add(cell);
                var dot = Dot(color, 8);
                dot.Margin = new Thickness(0, 0, 4, 0);
                var t = Text($"{label} {n}", 10.5, Soft, null, false);
                t.Margin = new Thickness(0, 0, 10, 0);
                legend.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { dot, t } });
            }
            body.Children.Add(bar);
            body.Children.Add(legend);
        }
        if (w.Feel is { } feel)
        {
            var f = Text(feel, 11, Soft);
            f.Margin = new Thickness(0, 8, 0, 0);
            body.Children.Add(f);
        }
        if (w.Tip is { } tip) body.Children.Add(Bubble(tip, "#FFF1DE", "#6B4A1E"));
    }

    /// <summary>7 quả nho đứng thành hàng: quả càng to càng mọng, quả mờ là ngày chưa có dữ liệu, quả cuối (viền cam) là hôm nay.</summary>
    private static FrameworkElement GrapeRow(IReadOnlyList<DayScore> days)
    {
        var g = new UniformGrid { Rows = 1, Columns = Math.Max(1, days.Count) };
        for (var i = 0; i < days.Count; i++)
        {
            var d = days[i];
            var today = i == days.Count - 1;
            var size = d.Score < 0 ? 16 : 14 + d.Score / 100.0 * 14;
            var grape = new Ellipse
            {
                Width = size, Height = size, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom,
                Fill = d.Score < 0 ? Br("#F4EEE6") : FruitArt.GrapeBrush(d.Score),
                Stroke = today ? Br("#E8772E") : d.Score < 0 ? Br("#DCCFBE") : new SolidColorBrush(Color.FromArgb(70, 46, 26, 16)),
                StrokeThickness = today ? 2.5 : 1.2, StrokeDashArray = d.Score < 0 ? new DoubleCollection([2, 2]) : null,
                ToolTip = d.Score < 0 ? "Chưa có dữ liệu" : $"{d.Label}: {d.Score} điểm",
            };
            var score = Text(d.Score < 0 ? " " : d.Score.ToString(), 10, Soft, FontWeights.SemiBold, false);
            score.HorizontalAlignment = HorizontalAlignment.Center;
            var label = Text(today ? "Nay" : d.Score < 0 ? "·" : d.Label, 10.5, today ? "#E8772E" : Soft, today ? FontWeights.Bold : null, false);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            g.Children.Add(new StackPanel
            {
                Children = { score, new Grid { Height = 30, Children = { grape } }, label },
            });
        }
        return g;
    }

    // ================= hoạt ảnh nhẹ =================
    private static void Bob(FrameworkElement el)
    {
        var t = new TranslateTransform();
        el.RenderTransform = t;
        t.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, -2.5, TimeSpan.FromSeconds(1.6))
        {
            AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        });
    }

    private static void Pulse(FrameworkElement el)
    {
        el.BeginAnimation(OpacityProperty, new DoubleAnimation(1, .45, TimeSpan.FromSeconds(1.1))
        {
            AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        });
    }
}
