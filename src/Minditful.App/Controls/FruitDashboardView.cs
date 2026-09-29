using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
/// Dashboard dạng "vườn trái cây": 4 bong bóng xếp vòng cung sát quanh đầu Milo, chiếm chỗ không hơn khung dashboard cũ.
/// Hôm nay: nho (mood) · cam (cuộc họp) · anh đào (email chờ) · táo cắn dở (sprint). Tuần này: nho thành chùm 7 ngày.
/// Mọi chi tiết của dashboard cũ nằm trong thẻ khi rê chuột lên từng quả.
/// </summary>
internal sealed class FruitDashboardView : Canvas
{
    public const double W = 340, H = 360;
    // Tâm vòng cung ≈ đầu Milo (Milo 150×150 cách mép 40px) tính từ góc neo
    private const double HeadFromSide = 115, HeadFromBottom = 112, Radius = 160;
    private static readonly double[] Angles = [172, 139, 106, 74];

    public FruitDashboardView(FruitDashboard model, Corner corner, Action<string> onAct)
    {
        Width = W;
        Height = H;
        var left = corner is Corner.BottomLeft or Corner.TopLeft;
        var top = corner is Corner.TopLeft or Corner.TopRight;
        // Toạ độ tính cho góc phải dưới rồi lật theo góc neo
        Point Map(double x, double y) => new(left ? W - x : x, top ? H - y : y);
        var head = Map(W - HeadFromSide, H - HeadFromBottom);

        for (var i = 0; i < model.Items.Count && i < Angles.Length; i++)
        {
            var a = Angles[i] * Math.PI / 180;
            var p = Map(W - HeadFromSide + Math.Cos(a) * Radius, H - HeadFromBottom - Math.Sin(a) * Radius);
            AddBubble(model.Items[i], p, head, i);
        }

        // Thanh tiêu đề nhỏ: tên trang · chuyển trang · đóng
        var toggle = LinkButton(model.Page == DashPage.Today ? "Tuần này →" : "← Hôm nay", () => onAct(model.Page == DashPage.Today ? "week" : "today"));
        var close = new Button
        {
            Style = (Style)Application.Current.FindResource("Round"), Content = "×", Width = 22, Height = 22, MinHeight = 22, Padding = new Thickness(0),
            Background = Br("#EFE3D0"), Foreground = Br("#5B4A3C"), FontSize = 12, HorizontalContentAlignment = HorizontalAlignment.Center, Margin = new Thickness(8, 0, 0, 0),
        };
        System.Windows.Automation.AutomationProperties.SetName(close, "Đóng dashboard");
        close.Click += (_, _) => onAct("close");
        var title = Text(model.Title, 12.5, "#3A2A1E", FontWeights.Bold, false);
        title.VerticalAlignment = VerticalAlignment.Center;
        title.Margin = new Thickness(0, 0, 6, 0);
        toggle.VerticalAlignment = VerticalAlignment.Center;
        var detail = LinkButton("Chi tiết", () => onAct("detail"));
        detail.VerticalAlignment = VerticalAlignment.Center;
        System.Windows.Automation.AutomationProperties.SetName(detail, "Mở bảng chi tiết");
        var chip = new Border
        {
            Background = Br("#FFF9F1"), BorderBrush = Br("#F1DFC6"), BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(14), Padding = new Thickness(12, 4, 6, 4),
            Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 5, Direction = 270, Opacity = .18, Color = Rgb("#2B211A") },
            Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { title, toggle, Sep(), detail, close } },
        };
        chip.Loaded += (_, _) =>
        {
            SetLeft(chip, left ? 6 : W - chip.ActualWidth - 6);
            SetTop(chip, top ? H - chip.ActualHeight - 4 : 4);
        };
        chip.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(.3)));
        Children.Add(chip);
    }

    private static Border Sep() => new() { Width = 1, Height = 12, Background = Br("#E8D6BD"), Margin = new Thickness(2, 0, 2, 0), VerticalAlignment = VerticalAlignment.Center };

    /// <summary>Viền pastel riêng cho từng quả: nho tím, cam đào, anh đào hồng, táo xanh lá.</summary>
    private static (string Ring, string Glow) Tint(FruitKind k) => k switch
    {
        FruitKind.Grape or FruitKind.Bunch => ("#E4DAFA", "#F6F1FF"),
        FruitKind.Orange => ("#FFDDBA", "#FFF5EA"),
        FruitKind.Cherries => ("#FFD2DB", "#FFF3F5"),
        _ => ("#D5EDC9", "#F3FAEF"),
    };

    private static Button LinkButton(string text, Action click)
    {
        var b = new Button
        {
            Style = (Style)Application.Current.FindResource("SoftLink"), Content = text, Foreground = Br("#B85A34"), FontWeight = FontWeights.Bold,
            FontSize = 12, Padding = new Thickness(4, 2, 4, 2), Background = Brushes.Transparent,
        };
        b.Click += (_, _) => click();
        return b;
    }

    private void AddBubble(FruitItem f, Point p, Point head, int index)
    {
        var size = f.Kind == FruitKind.Bunch ? 100.0 : 84.0;
        var (ring, glow) = Tint(f.Kind);
        var bg = new RadialGradientBrush { GradientOrigin = new Point(.4, .3), Center = new Point(.4, .3), RadiusX = .8, RadiusY = .8 };
        bg.GradientStops.Add(new GradientStop(Rgb("#FFFFFF"), 0));
        bg.GradientStops.Add(new GradientStop(Rgb(glow), .75));
        bg.GradientStops.Add(new GradientStop(Rgb(ring), 1));
        var disc = new Ellipse
        {
            Fill = bg, Stroke = Br(ring), StrokeThickness = 3,
            Effect = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 6, Direction = 270, Opacity = .16, Color = Rgb("#3A2A1E") },
        };

        var art = new Viewbox { Width = f.Kind == FruitKind.Bunch ? 58 : 40, Height = f.Kind == FruitKind.Bunch ? 64 : 40, Child = FruitArt.Build(f) };
        var big = new System.Windows.Controls.TextBlock
        {
            Text = f.Big, FontFamily = Serif, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Br("#3A2A1E"),
            HorizontalAlignment = HorizontalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = size - 22,
        };
        var small = new System.Windows.Controls.TextBlock
        {
            Text = f.Small, FontSize = 8.5, Foreground = Br("#5B4A3C"), HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = size - 22,
        };
        var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 4, 0, 0), Children = { art, big, small } };
        var badge = new Border
        {
            Width = 17, Height = 17, CornerRadius = new CornerRadius(5), Background = Br(f.BadgeColor), HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 2, 4, 0),
            Child = new System.Windows.Controls.TextBlock { Text = f.Badge, FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
        };

        var hover = new ScaleTransform(1, 1, size / 2, size / 2);
        var popScale = new ScaleTransform(1, 1, size / 2, size / 2);
        var popMove = new TranslateTransform();
        var bob = new TranslateTransform();
        var bubble = new Grid
        {
            Width = size, Height = size, Children = { disc, content, badge }, Cursor = Cursors.Hand, Opacity = 0,
            RenderTransform = new TransformGroup { Children = { hover, popScale, popMove, bob } },
            ToolTip = Tip(f),
        };
        if (!f.Available) content.Opacity = .55;
        System.Windows.Automation.AutomationProperties.SetName(bubble, $"{f.TipTitle}. {string.Join(". ", f.TipLines)}");
        ToolTipService.SetInitialShowDelay(bubble, 150);
        ToolTipService.SetShowDuration(bubble, 20000);
        ToolTipService.SetPlacement(bubble, System.Windows.Controls.Primitives.PlacementMode.Top);
        bubble.MouseEnter += (_, _) => Hover(hover, 1.07);
        bubble.MouseLeave += (_, _) => Hover(hover, 1);
        SetLeft(bubble, p.X - size / 2);
        SetTop(bubble, p.Y - size / 2);
        Children.Add(bubble);

        // Bung ra từ chỗ Milo, lần lượt từng quả
        var begin = TimeSpan.FromMilliseconds(index * 90);
        var dur = TimeSpan.FromSeconds(.4);
        var ease = new BackEase { Amplitude = .35, EasingMode = EasingMode.EaseOut };
        popScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(.2, 1, dur) { BeginTime = begin, EasingFunction = ease });
        popScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(.2, 1, dur) { BeginTime = begin, EasingFunction = ease });
        popMove.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(head.X - p.X, 0, dur) { BeginTime = begin, EasingFunction = ease });
        popMove.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(head.Y - p.Y, 0, dur) { BeginTime = begin, EasingFunction = ease });
        bubble.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(.25)) { BeginTime = begin });
        // Sau khi bung ra, mỗi quả nhấp nhô nhẹ lệch nhịp nhau như đang lơ lửng
        bob.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, -3, TimeSpan.FromSeconds(1.8 + index * .15))
        {
            BeginTime = begin + dur, AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        });
    }

    private static void Hover(ScaleTransform t, double to)
    {
        var a = new DoubleAnimation(to, TimeSpan.FromSeconds(.18)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        t.BeginAnimation(ScaleTransform.ScaleXProperty, a);
        t.BeginAnimation(ScaleTransform.ScaleYProperty, a);
    }

    /// <summary>Thẻ chi tiết khi rê chuột: tiêu đề + các dòng (lịch họp, email, Office Vibe, thống kê tuần…).</summary>
    private static object Tip(FruitItem f)
    {
        var sp = new StackPanel { MaxWidth = 280 };
        sp.Children.Add(Text(f.TipTitle, 12.5, "#E8A33D", FontWeights.SemiBold));
        foreach (var line in f.TipLines)
        {
            var t = Text(line, 11.5, "#FBF3E7");
            t.Margin = new Thickness(0, 4, 0, 0);
            sp.Children.Add(t);
        }
        return sp;
    }
}
