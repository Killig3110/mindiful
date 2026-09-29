using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using static Minditful.App.Rendering.Ui;

namespace Minditful.App.Views.Panel;

/// <summary>Bảng màu sáng của bảng điều khiển — cùng tông với thẻ và dashboard của Milo.</summary>
internal static class P
{
    public const string Bg = "#F7F0E6", Side = "#EFE5D6", Card = "#FFFDF9", Line = "#EADCC7", Fill = "#F6EBDC";
    public const string Ink = "#3A2A1E", Ink2 = "#6B5646", Muted = "#9C8672";
    public const string Accent = "#E8772E", Amber = "#E8A33D", Good = "#5E8E3E", Warn = "#C07A2C", Bad = "#C0392B";
}

internal enum BtnKind { Primary, Soft, Ghost, Danger }

internal abstract partial class ControlShell
{
    protected static readonly FontFamily Icons = new("Segoe Fluent Icons, Segoe MDL2 Assets");

    // Ký tự icon (Segoe Fluent Icons / MDL2)
    protected const string IcHome = "", IcTimeline = "", IcTry = "", IcMilo = "", IcBrain = "",
        IcLink = "", IcPlay = "", IcPause = "", IcRestart = "", IcHelp = "";

    // ================= khối dựng trang =================
    protected static Border Card(UIElement body, string? title = null, string? subtitle = null, Thickness? padding = null)
    {
        var sp = new StackPanel();
        if (title is not null) sp.Children.Add(Text(title, 15, P.Ink, FontWeights.Bold));
        if (subtitle is not null)
        {
            var s = Text(subtitle, 12, P.Ink2);
            s.Margin = new Thickness(0, 3, 0, 0);
            sp.Children.Add(s);
        }
        if (title is not null || subtitle is not null)
        {
            if (body is FrameworkElement fe) fe.Margin = new Thickness(0, 12, 0, 0);
        }
        sp.Children.Add(body);
        return new Border
        {
            Background = Br(P.Card), BorderBrush = Br(P.Line), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(16),
            Padding = padding ?? new Thickness(18, 16, 18, 18), Margin = new Thickness(0, 0, 10, 14), Child = sp,
            Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 3, Direction = 270, Opacity = .06, Color = Rgb(P.Ink) },
        };
    }

    /// <summary>Tiêu đề trang + 1 câu giải thích trang này để làm gì.</summary>
    protected static StackPanel Page(string title, string intro, params UIElement[] children)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
        sp.Children.Add(Text(title, 22, P.Ink, FontWeights.Bold));
        var i = Text(intro, 13, P.Ink2);
        i.Margin = new Thickness(0, 4, 10, 16);
        i.MaxWidth = 720;
        i.HorizontalAlignment = HorizontalAlignment.Left;
        sp.Children.Add(i);
        foreach (var c in children) sp.Children.Add(c);
        return sp;
    }

    protected static TextBlock Label(string text, double size = 11.5, string color = P.Muted, bool bold = true)
    {
        var t = Text(text, size, color, bold ? FontWeights.SemiBold : null);
        return t;
    }

    protected static Button Btn(string text, Action click, BtnKind kind = BtnKind.Soft, string? icon = null)
    {
        var (bg, fg, border) = kind switch
        {
            BtnKind.Primary => (P.Accent, "#FFFFFF", (string?)null),
            BtnKind.Danger => ("#FBE3E0", P.Bad, null),
            BtnKind.Ghost => ("#00000000", P.Ink2, P.Line),
            _ => (P.Fill, P.Ink, null),
        };
        object content = text;
        if (icon is not null)
            content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { new TextBlock { Text = icon, FontFamily = Icons, FontSize = 13, Margin = new Thickness(0, 0, 7, 0), VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center } },
            };
        var b = new Button
        {
            Style = (Style)Application.Current.FindResource("Round"), Content = content, Background = Br(bg), Foreground = Br(fg),
            BorderBrush = border is null ? null : Br(border), BorderThickness = new Thickness(border is null ? 0 : 1.2),
            MinHeight = 34, Padding = new Thickness(14, 0, 14, 0), Margin = new Thickness(0, 0, 8, 8), HorizontalAlignment = HorizontalAlignment.Left,
        };
        System.Windows.Automation.AutomationProperties.SetName(b, text);
        b.Click += (_, _) => click();
        return b;
    }

    protected static WrapPanel Row(params UIElement[] items)
    {
        var w = new WrapPanel();
        foreach (var i in items) w.Children.Add(i);
        return w;
    }

    /// <summary>Ô số liệu: số to + nhãn nhỏ, số cập nhật theo engine.</summary>
    protected Border Tile(Func<string> big, string small, string? accent = null, Func<string>? note = null)
    {
        var b = Text("", 22, accent ?? P.Ink, FontWeights.Bold, false);
        var s = Text(small, 11.5, P.Ink2);
        var n = Text("", 11, P.Muted);
        Tick(() =>
        {
            b.Text = big();
            n.Text = note?.Invoke() ?? "";
            n.Visibility = n.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        });
        return new Border
        {
            Background = Br(P.Fill), CornerRadius = new CornerRadius(12), Padding = new Thickness(12, 9, 12, 10), Margin = new Thickness(0, 0, 8, 8),
            MinWidth = 140, Child = new StackPanel { Children = { b, s, n } },
        };
    }

    /// <summary>Công tắc bật/tắt kèm 1 câu "bật lên thì Milo sẽ…".</summary>
    protected Border Switch(string label, string effect, Func<bool> isOn, Action toggle)
    {
        var knob = new Ellipse { Width = 16, Height = 16, Fill = Brushes.White, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(2) };
        var track = new Border { Width = 38, Height = 22, CornerRadius = new CornerRadius(11), Child = knob, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 1, 12, 0) };
        var name = Text(label, 13, P.Ink, FontWeights.SemiBold);
        var desc = Text(effect, 11.5, P.Ink2);
        var box = new Border
        {
            Background = Br(P.Card), BorderBrush = Br(P.Line), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 8, 8), Cursor = Cursors.Hand, Width = 300,
            Child = Columns((track, Auto), (new StackPanel { Children = { name, desc } }, Star)), Focusable = true,
        };
        System.Windows.Automation.AutomationProperties.SetName(box, label);
        void Paint()
        {
            var on = isOn();
            track.Background = Br(on ? P.Accent : "#D9CBB8");
            knob.HorizontalAlignment = on ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            box.BorderBrush = Br(on ? P.Accent : P.Line);
        }
        box.MouseLeftButtonUp += (_, _) =>
        {
            toggle();
            Paint();
            RefreshMilo();
        };
        box.KeyDown += (_, e) =>
        {
            if (e.Key is not (Key.Space or Key.Enter)) return;
            toggle();
            Paint();
            RefreshMilo();
        };
        Tick(Paint);
        return box;
    }

    /// <summary>Nút bấm 1 lần dạng thẻ nhỏ (vd. "Rời cuộc họp").</summary>
    protected static Border ActionTile(string label, string effect, Action act, Func<bool>? enabled = null, string color = P.Amber)
    {
        var dot = Dot(color, 10);
        dot.Margin = new Thickness(0, 4, 10, 0);
        dot.VerticalAlignment = VerticalAlignment.Top;
        var name = Text(label, 13, P.Ink, FontWeights.SemiBold);
        var desc = Text(effect, 11.5, P.Ink2);
        var go = Text("Chạy ›", 12, P.Accent, FontWeights.Bold, false);
        go.VerticalAlignment = VerticalAlignment.Center;
        go.Margin = new Thickness(8, 0, 0, 0);
        var box = new Border
        {
            Background = Br(P.Card), BorderBrush = Br(P.Line), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 8, 8), Cursor = Cursors.Hand, Width = 300, Focusable = true,
            Child = Columns((dot, Auto), (new StackPanel { Children = { name, desc } }, Star), (go, Auto)),
        };
        System.Windows.Automation.AutomationProperties.SetName(box, label);
        box.MouseEnter += (_, _) => box.BorderBrush = Br(P.Accent);
        box.MouseLeave += (_, _) => box.BorderBrush = Br(P.Line);
        void Run()
        {
            if (enabled?.Invoke() == false) return;
            act();
            RefreshMilo();
        }
        box.MouseLeftButtonUp += (_, _) => Run();
        box.KeyDown += (_, e) =>
        {
            if (e.Key is Key.Space or Key.Enter) Run();
        };
        return box;
    }

    /// <summary>Dòng trạng thái kết nối: chấm màu + chữ.</summary>
    protected StackPanel Status(Func<(string Level, string Text)> read)
    {
        var dot = Dot(P.Muted, 10);
        dot.Margin = new Thickness(0, 4, 8, 0);
        dot.VerticalAlignment = VerticalAlignment.Top;
        var t = Text("", 12.5, P.Ink);
        Tick(() =>
        {
            var (lvl, text) = read();
            dot.Background = Br(lvl switch { "ok" => P.Good, "warn" => P.Amber, "bad" => P.Bad, _ => P.Muted });
            t.Text = text;
        });
        return new StackPanel { Orientation = Orientation.Horizontal, Children = { dot, new Border { MaxWidth = 620, Child = t } } };
    }

    /// <summary>Nút chọn 1 trong nhiều (tốc độ, góc neo…).</summary>
    protected Border Segmented((string Label, string Value)[] options, Func<string> current, Action<string> pick)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        var buttons = new List<(Button B, string V)>();
        foreach (var (label, value) in options)
        {
            var b = new Button
            {
                Style = (Style)Application.Current.FindResource("Round"), Content = label, MinHeight = 30, Padding = new Thickness(13, 0, 13, 0),
                Margin = new Thickness(0, 0, 2, 0),
            };
            b.Click += (_, _) =>
            {
                pick(value);
                foreach (var (x, v) in buttons) Paint(x, v == value);
                RefreshMilo();
            };
            buttons.Add((b, value));
            sp.Children.Add(b);
        }
        static void Paint(Button b, bool on)
        {
            b.Background = Br(on ? P.Card : "#00000000");
            b.Foreground = Br(on ? P.Ink : P.Ink2);
            b.Effect = on ? new DropShadowEffect { BlurRadius = 6, ShadowDepth = 1, Direction = 270, Opacity = .12 } : null;
        }
        Tick(() =>
        {
            var cur = current();
            foreach (var (b, v) in buttons) Paint(b, v == cur);
        });
        return new Border { Background = Br(P.Fill), CornerRadius = new CornerRadius(10), Padding = new Thickness(3), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 8, 8), Child = sp };
    }

    protected static Border Input(Control box, double width = 320)
    {
        box.BorderThickness = new Thickness(0);
        box.Background = Brushes.Transparent;
        box.Foreground = Br(P.Ink);
        box.VerticalContentAlignment = VerticalAlignment.Center;
        box.Padding = new Thickness(8, 0, 8, 0);
        box.Height = 32;
        return new Border
        {
            Background = Brushes.White, BorderBrush = Br(P.Line), BorderThickness = new Thickness(1.2), CornerRadius = new CornerRadius(8),
            Width = width, Margin = new Thickness(0, 0, 8, 8), Child = box, HorizontalAlignment = HorizontalAlignment.Left,
        };
    }

    /// <summary>Hộp gợi ý nền cam nhạt với icon bóng đèn.</summary>
    protected static Border TipBox(params string[] lines)
    {
        var sp = new StackPanel();
        foreach (var l in lines)
        {
            var bullet = Text("•", 13, P.Accent, FontWeights.Bold, false);
            bullet.Margin = new Thickness(0, 0, 8, 0);
            var t = Text(l, 12.5, P.Ink);
            var row = Columns((bullet, Auto), (t, Star));
            row.Margin = new Thickness(0, 0, 0, 5);
            sp.Children.Add(row);
        }
        return new Border { Background = Br("#FFF1DE"), CornerRadius = new CornerRadius(12), Padding = new Thickness(14, 11, 14, 7), Margin = new Thickness(0, 0, 10, 14), Child = sp };
    }

    protected static UniformGrid Grid2(params UIElement[] items)
    {
        var g = new UniformGrid { Columns = 2 };
        foreach (var i in items) g.Children.Add(i);
        return g;
    }
}
