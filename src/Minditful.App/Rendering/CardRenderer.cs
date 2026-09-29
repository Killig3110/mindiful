using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Minditful.Core.Presentation;
using static Minditful.App.Rendering.Ui;

namespace Minditful.App.Rendering;

/// <summary>Dựng thẻ của Milo từ <see cref="CardModel"/> (tương ứng cardHTML của prototype).</summary>
internal sealed class CardRenderer(Action<string, string?> onAct)
{
    public BreatheView? Breathe { get; private set; }

    public FrameworkElement? Build(CardModel m)
    {
        Breathe = null;
        return m.Variant switch
        {
            CardVariant.None => null,
            CardVariant.Chip => Chip(m.SayText ?? ""),
            CardVariant.Say => Say(m.SayText ?? ""),
            CardVariant.Breathe => Breathe = new BreatheView(() => onAct("stop", null)),
            _ => Card(m),
        };
    }

    private static DropShadowEffect Shadow(double blur = 44, double depth = 18, double opacity = .28) =>
        new() { BlurRadius = blur, ShadowDepth = depth, Direction = 270, Opacity = opacity, Color = Ui.Rgb("#2B211A") };

    private FrameworkElement Chip(string text)
    {
        var b = new Button { Style = (Style)Application.Current.FindResource("Round"), Background = Br("#3A2A1E"), Foreground = Br("#FBF3E7"), Padding = new Thickness(14, 9, 14, 9) };
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        var dot = Dot("#E8A33D", 9);
        dot.Margin = new Thickness(0, 0, 8, 0);
        sp.Children.Add(dot);
        sp.Children.Add(new TextBlock { Text = text, FontSize = 12.5 });
        b.Content = sp;
        b.Click += (_, _) => onAct("expand", null);
        return b;
    }

    private static FrameworkElement Say(string text) => new Border
    {
        Background = Br("#3A2A1E"), CornerRadius = new CornerRadius(18), Padding = new Thickness(16, 12, 16, 12), MaxWidth = 280,
        Effect = Shadow(24, 10, .25),
        Child = new TextBlock { Text = text, Foreground = Br("#FBF3E7"), FontSize = 14, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, FontFamily = Sans },
    };

    private FrameworkElement Card(CardModel m)
    {
        var stack = new StackPanel();
        var first = true;
        foreach (var block in m.Blocks)
        {
            var el = Block(block);
            if (el is null) continue;
            if (!first) el.Margin = new Thickness(0, 9, 0, 0);
            first = false;
            stack.Children.Add(el);
        }
        return new Border
        {
            Background = Br("#FBF3E7"), CornerRadius = new CornerRadius(22), Padding = new Thickness(16, 15, 16, 15), Width = m.Width,
            Effect = Shadow(), Child = stack,
        };
    }

    private FrameworkElement? Block(CardBlock b) => b switch
    {
        TopBlock t => Top(t),
        EyebrowBlock e => Text(e.Text.ToUpperInvariant(), 10.5, "#9C8672", FontWeights.Bold),
        TitleBlock t => Text(t.Text, t.Size, "#3A2A1E", FontWeights.Bold),
        ParagraphBlock t => Text(t.Text, t.Small ? 12 : 13.5, t.Color ?? (t.Small ? "#5B4A3C" : "#3A2A1E")),
        LineBlock l => Line(l),
        ButtonsBlock bb => Buttons(bb),
        PeopleBlock p => People(p),
        ScheduleBlock s => Schedule(s),
        ProgressBlock p => Progress(p),
        TilesBlock t => Tiles(t.Tiles),
        StatusDotBlock s => StatusDot(s.Text),
        ChatBlock c => Chat(c),
        _ => null,
    };

    private static FrameworkElement Top(TopBlock t)
    {
        UIElement? icon = t.Icon switch
        {
            PillIcon.Bell => Icon(Ui.BellIcon, t.Fg, 12),
            PillIcon.Clock => Icon(Ui.ClockIcon, t.Fg, 11),
            _ => null,
        };
        var pill = Pill(t.Pill, t.Bg, t.Fg, icon: icon);
        FrameworkElement right = t.Stamp
            ? new Border { Background = Br("#7FA65A"), CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 3, 10, 3), Child = Text("Đã giữ", 11, "#FFFFFF", FontWeights.Bold, false) }
            : Text(t.Meta ?? "", 11.5, "#9C8672", wrap: false);
        right.HorizontalAlignment = HorizontalAlignment.Right;
        right.VerticalAlignment = VerticalAlignment.Center;
        return Columns((pill, Auto), (right, Star));
    }

    private static FrameworkElement Line(LineBlock l)
    {
        FrameworkElement lead = l.Lead switch
        {
            LeadKind.Square => new Border { Width = 18, Height = 18, CornerRadius = new CornerRadius(5), Background = Br(l.LeadColor) },
            LeadKind.Avatar => Avatar(l.LeadText, l.LeadColor),
            _ => Text(l.LeadText, 11, l.LeadColor, FontWeights.Bold, false),
        };
        lead.VerticalAlignment = VerticalAlignment.Center;
        lead.Margin = new Thickness(0, 0, 9, 0);
        var text = Text(l.Text, 12.5, "#3A2A1E");
        text.VerticalAlignment = VerticalAlignment.Center;
        FrameworkElement? tail = null;
        if (l.PillText is not null) tail = Pill(l.PillText, l.PillBg!, l.PillFg!, 11);
        else if (l.Source is not null) tail = Text(l.Source, 10.5, l.SourceAlert ? "#8A1F2B" : "#9C8672", l.SourceAlert ? FontWeights.Bold : FontWeights.Normal, false);
        if (tail is not null)
        {
            tail.Margin = new Thickness(8, 0, 0, 0);
            tail.VerticalAlignment = VerticalAlignment.Center;
        }
        var row = tail is null ? Columns((lead, Auto), (text, Star)) : Columns((lead, Auto), (text, Star), (tail, Auto));
        return new Border { Background = System.Windows.Media.Brushes.White, CornerRadius = new CornerRadius(11), Padding = new Thickness(10, 8, 10, 8), Child = row };
    }

    private FrameworkElement Buttons(ButtonsBlock bb)
    {
        var g = new UniformGrid { Rows = 1, Columns = bb.Buttons.Count };
        foreach (var cb in bb.Buttons)
        {
            var (bg, fg, border) = cb.Style switch
            {
                ButtonStyle.Amber => ("#E8A33D", "#3A2A1E", null as string),
                ButtonStyle.Dark => ("#3A2A1E", "#FBF3E7", null),
                ButtonStyle.Teams => ("#5B5FC7", "#FFFFFF", null),
                _ => ("#00000000", "#5B4A3C", "#E2CFB3"),
            };
            var btn = new Button
            {
                Style = (Style)Application.Current.FindResource("Round"),
                Background = Br(bg), Foreground = Br(fg), IsEnabled = cb.Enabled,
                FontWeight = cb.Style == ButtonStyle.Ghost ? FontWeights.SemiBold : FontWeights.Bold,
                Height = cb.Style == ButtonStyle.Ghost ? 34 : 38, Padding = new Thickness(10, 0, 10, 0),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                BorderBrush = border is null ? null : Br(border), BorderThickness = new Thickness(border is null ? 0 : 1.5),
                Margin = new Thickness(bb.Buttons.Count > 1 && cb != bb.Buttons[0] ? 3 : 0, 0, bb.Buttons.Count > 1 && cb != bb.Buttons[^1] ? 3 : 0, 0),
            };
            btn.Content = Marquee(btn, cb.Label);
            var (act, val) = (cb.Act, cb.Val);
            btn.Click += (_, _) => onAct(act, val);
            g.Children.Add(btn);
        }
        return g;
    }

    /// <summary>
    /// Chữ trên nút: vừa thì canh giữa; dài hơn nút thì hiện "…" và khi rê chuột chữ chạy ngang để đọc hết,
    /// dừng nửa giây ở 2 đầu rồi lặp lại cho tới khi rời chuột.
    /// </summary>
    private static FrameworkElement Marquee(Button btn, string text)
    {
        var tb = new TextBlock
        {
            Text = text, TextTrimming = TextTrimming.CharacterEllipsis, TextWrapping = TextWrapping.NoWrap,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
        };
        var move = new TranslateTransform();
        tb.RenderTransform = move;
        var host = new Grid { ClipToBounds = true, Children = { tb } };
        btn.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        System.Windows.Automation.AutomationProperties.SetName(btn, text);
        btn.ToolTip = null;
        btn.MouseEnter += (_, _) =>
        {
            tb.TextTrimming = TextTrimming.None;
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var full = tb.DesiredSize.Width;
            var over = full - host.ActualWidth;
            if (over <= 1)
            {
                tb.TextTrimming = TextTrimming.CharacterEllipsis;
                return;
            }
            tb.HorizontalAlignment = HorizontalAlignment.Left;
            tb.Width = full;
            var run = Math.Max(0.8, over / 40); // khoảng 40 px/giây, đọc kịp
            static KeyTime At(double s) => KeyTime.FromTimeSpan(TimeSpan.FromSeconds(s));
            var anim = new DoubleAnimationUsingKeyFrames { RepeatBehavior = RepeatBehavior.Forever };
            anim.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, At(0)));
            anim.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, At(0.5)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(-over, At(0.5 + run)));
            anim.KeyFrames.Add(new DiscreteDoubleKeyFrame(-over, At(1.3 + run)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(0, At(1.3 + run * 1.5)));
            move.BeginAnimation(TranslateTransform.XProperty, anim);
        };
        btn.MouseLeave += (_, _) =>
        {
            move.BeginAnimation(TranslateTransform.XProperty, null);
            move.X = 0;
            tb.Width = double.NaN;
            tb.HorizontalAlignment = HorizontalAlignment.Center;
            tb.TextTrimming = TextTrimming.CharacterEllipsis;
        };
        return host;
    }

    private static FrameworkElement People(PeopleBlock p)
    {
        var avs = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        for (var i = 0; i < p.People.Count; i++)
        {
            var a = Avatar(p.People[i].Initials, p.People[i].Color, 26, 10, "#FBF3E7");
            if (i > 0) a.Margin = new Thickness(-8, 0, 0, 0);
            avs.Children.Add(a);
        }
        var role = Pill(p.Role, p.RoleBg, p.RoleFg);
        role.Margin = new Thickness(10, 0, 0, 0);
        role.VerticalAlignment = VerticalAlignment.Center;
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(avs);
        sp.Children.Add(role);
        return sp;
    }

    private static FrameworkElement Schedule(ScheduleBlock s)
    {
        var axis = new Grid { Margin = new Thickness(0, 2, 10, 2) };
        axis.RowDefinitions.Add(new RowDefinition { Height = Auto });
        axis.RowDefinitions.Add(new RowDefinition { Height = Star });
        axis.RowDefinitions.Add(new RowDefinition { Height = Auto });
        var from = Text(s.From, 10.5, "#9C8672", wrap: false);
        var to = Text(s.To, 10.5, "#9C8672", wrap: false);
        Grid.SetRow(to, 2);
        axis.Children.Add(from);
        axis.Children.Add(to);
        var rows = new StackPanel();
        foreach (var r in s.Rows)
        {
            var el = r.IsSlot
                ? new Border
                {
                    Height = 18, CornerRadius = new CornerRadius(6), Background = Br("#CFE6BC"), BorderBrush = Br("#7FA65A"),
                    BorderThickness = new Thickness(1.5), Padding = new Thickness(8, 0, 8, 0), Margin = new Thickness(0, 0, 0, 3),
                    Child = Text(r.Text, 10.5, "#33501A", FontWeights.Bold, false),
                }
                : new Border
                {
                    Height = 26, CornerRadius = new CornerRadius(6), Background = Br("#D7DDF5"), Padding = new Thickness(8, 0, 8, 0),
                    Margin = new Thickness(0, 0, 0, 3), Child = Text(r.Text, 11, "#2E3A7A", wrap: false),
                };
            ((FrameworkElement)((Border)el).Child).VerticalAlignment = VerticalAlignment.Center;
            rows.Children.Add(el);
        }
        return new Border
        {
            Background = System.Windows.Media.Brushes.White, CornerRadius = new CornerRadius(14), Padding = new Thickness(10),
            Child = Columns((axis, Auto), (rows, Star)),
        };
    }

    private static FrameworkElement Progress(ProgressBlock p)
    {
        var sp = new StackPanel();
        var head = Columns((Text(p.Left, 12, "#5B4A3C", wrap: false), Star), (Text(p.Right, 12, "#5B4A3C", wrap: false), Auto));
        sp.Children.Add(head);
        sp.Children.Add(Bar(p.Fraction, new Thickness(0, 6, 0, 0)));
        return sp;
    }

    public static FrameworkElement Bar(double fraction, Thickness margin)
    {
        var track = new Grid { Height = 10, Margin = margin };
        track.Children.Add(new Border { CornerRadius = new CornerRadius(5), Background = Br("#EDE2D0") });
        track.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(5), Background = Br("#0078D4"), HorizontalAlignment = HorizontalAlignment.Left,
            Width = 0, Tag = Math.Clamp(fraction, 0, 1),
        });
        track.SizeChanged += (_, e) => ((Border)track.Children[1]).Width = e.NewSize.Width * (double)((Border)track.Children[1]).Tag;
        return track;
    }

    public static FrameworkElement Tiles(IReadOnlyList<(string Big, string Small)> tiles)
    {
        var g = new UniformGrid { Rows = 1, Columns = tiles.Count };
        foreach (var (big, small) in tiles)
        {
            var sp = new StackPanel();
            var b = Text(big, 15, "#3A2A1E", FontWeights.Bold, false);
            b.HorizontalAlignment = HorizontalAlignment.Center;
            var s = Text(small, 11, "#5B4A3C");
            s.TextAlignment = TextAlignment.Center;
            sp.Children.Add(b);
            sp.Children.Add(s);
            g.Children.Add(new Border { Background = System.Windows.Media.Brushes.White, CornerRadius = new CornerRadius(10), Padding = new Thickness(8, 7, 8, 7), Margin = new Thickness(3, 0, 3, 0), Child = sp });
        }
        return g;
    }

    private static FrameworkElement StatusDot(string text)
    {
        var dot = Dot("#C4314B", 9);
        dot.Margin = new Thickness(0, 0, 6, 0);
        return Columns((dot, Auto), (Text(text, 12, "#5B4A3C"), Star));
    }

    private FrameworkElement Chat(ChatBlock c)
    {
        var sp = new StackPanel();
        foreach (var line in c.Lines)
        {
            sp.Children.Add(Bubble(line.You, true));
            sp.Children.Add(Bubble(line.Milo, false));
        }
        if (c.Suggested is { Count: > 0 } sug)
        {
            // Tính năng Milo đề nghị: nút nhỏ ngay dưới câu trả lời, bấm là chạy
            var help = Text("Milo có thể giúp:", 11, "#9C8672", wrap: false);
            help.Margin = new Thickness(2, 0, 0, 4);
            sp.Children.Add(help);
            var row = Buttons(new ButtonsBlock(sug));
            row.Margin = new Thickness(0, 0, 0, 4);
            sp.Children.Add(row);
        }
        var input = new TextBox
        {
            BorderThickness = new Thickness(0), Background = System.Windows.Media.Brushes.Transparent, FontSize = 12.5, Height = 34,
            VerticalContentAlignment = VerticalAlignment.Center, Foreground = Br("#3A2A1E"),
        };
        var hint = Text(c.Hint ?? "Nói gì đó với Milo…", 12.5, "#9C8672", wrap: false);
        hint.IsHitTestVisible = false;
        hint.VerticalAlignment = VerticalAlignment.Center;
        hint.Margin = new Thickness(3, 0, 0, 0);
        if (c.Focus) input.Loaded += (_, _) => input.Focus();
        input.TextChanged += (_, _) => hint.Visibility = input.Text.Length > 0 ? Visibility.Collapsed : Visibility.Visible;
        var send = new Button
        {
            Style = (Style)Application.Current.FindResource("Flat"), Content = "→", Foreground = Br("#B85A34"), FontSize = 16,
            Width = 30, Height = 30, HorizontalContentAlignment = HorizontalAlignment.Center, ToolTip = "Gửi",
        };
        void Submit()
        {
            if (input.Text.Trim().Length > 0) onAct("chat", input.Text);
        }
        send.Click += (_, _) => Submit();
        input.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            Submit();
        };
        var box = new Grid();
        box.Children.Add(hint);
        box.Children.Add(input);
        sp.Children.Add(new Border
        {
            Background = System.Windows.Media.Brushes.White, CornerRadius = new CornerRadius(12), Padding = new Thickness(12, 0, 6, 0),
            Margin = new Thickness(0, c.Lines.Count > 0 ? 6 : 0, 0, 0), Child = Columns((box, Star), (send, Auto)),
        });
        return sp;
    }

    private static FrameworkElement Bubble(string text, bool you) => new Border
    {
        Background = Br(you ? "#3A2A1E" : "#FFFFFF"),
        CornerRadius = you ? new CornerRadius(12, 12, 3, 12) : new CornerRadius(12, 12, 12, 3),
        Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 5), MaxWidth = 240,
        HorizontalAlignment = you ? HorizontalAlignment.Right : HorizontalAlignment.Left,
        Child = Text(text, 12, you ? "#FBF3E7" : "#3A2A1E"),
    };
}

/// <summary>Vòng thở 4-4-4 (mục 8.3): phồng 4s, giữ 4s, xẹp 4s.</summary>
internal sealed class BreatheView : Border
{
    private readonly Ellipse _ring;
    private readonly TextBlock _phase, _count;
    private readonly Border _label;

    public BreatheView(Action stop)
    {
        Background = Br("#FBF3E7");
        CornerRadius = new CornerRadius(22);
        Padding = new Thickness(16, 15, 16, 15);
        Width = 280;
        Effect = new DropShadowEffect { BlurRadius = 44, ShadowDepth = 18, Direction = 270, Opacity = .28, Color = Ui.Rgb("#2B211A") };

        var sp = new StackPanel();
        _label = Pill("Thở 4-4-4", "#E3EFD6", "#3E5A22");
        sp.Children.Add(_label);

        var box = new Grid { Width = 168, Height = 168, Margin = new Thickness(0, 9, 0, 9), HorizontalAlignment = HorizontalAlignment.Center };
        box.Children.Add(new Ellipse { Stroke = Br("#CFE0BD"), StrokeThickness = 2, StrokeDashArray = [3, 3] });
        _ring = new Ellipse
        {
            Width = 144, Height = 144, Fill = Br("#D9EAC6"), RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = new ScaleTransform(.55, .55),
        };
        box.Children.Add(_ring);
        var center = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        _phase = new TextBlock { FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Br("#3E5A22"), HorizontalAlignment = HorizontalAlignment.Center, Text = "Hít vào" };
        _count = new TextBlock { FontSize = 26, FontFamily = Serif, Foreground = Br("#3E5A22"), HorizontalAlignment = HorizontalAlignment.Center };
        center.Children.Add(_phase);
        center.Children.Add(_count);
        box.Children.Add(center);
        sp.Children.Add(box);

        var hint = Text("Thở theo vòng tròn, không cần nhìn chữ", 12, "#5B4A3C");
        hint.TextAlignment = TextAlignment.Center;
        sp.Children.Add(hint);
        var btn = new Button
        {
            Style = (Style)Application.Current.FindResource("Round"), Content = "Dừng", Background = System.Windows.Media.Brushes.Transparent,
            Foreground = Br("#5B4A3C"), BorderBrush = Br("#E2CFB3"), BorderThickness = new Thickness(1.5), Height = 34,
            HorizontalContentAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 9, 0, 0),
        };
        btn.Click += (_, _) => stop();
        sp.Children.Add(btn);
        Child = sp;
    }

    /// <param name="elapsed">Giây trong chu kỳ 12s.</param>
    public void Update(double elapsed, string phase, int count, string label)
    {
        // keyframes ring: 0% .55 → 33% 1 → 66% 1 → 100% .55, ease-in-out
        double k = elapsed < 4 ? Ease(elapsed / 4) * .45 + .55 : elapsed < 8 ? 1 : 1 - Ease((elapsed - 8) / 4) * .45;
        var st = (ScaleTransform)_ring.RenderTransform;
        st.ScaleX = st.ScaleY = k;
        _phase.Text = phase;
        _count.Text = count.ToString();
        ((TextBlock)((StackPanel)_label.Child).Children[^1]).Text = label;
    }

    private static double Ease(double x) => x < .5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2;
}
