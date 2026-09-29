using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using Minditful.App.Rendering;
using Minditful.App.Services;
using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Integrations;
using MClip = Minditful.Core.Engine.Clip;
using static Minditful.App.Rendering.Ui;

namespace Minditful.App.Controls;

/// <summary>
/// Góc phải dưới màn hình của Milo: chóp đuôi, Milo, thì thầm, thẻ, dashboard, chấm chờ, hiệu ứng.
/// Toạ độ giữ đúng prototype (sân khấu 700×560, taskbar 44px) — <see cref="BaseOffset"/> là chiều cao taskbar bên dưới.
/// </summary>
public sealed class MiloLayer : Grid
{
    private const double RegionW = 420, RegionH = 340;

    private readonly Canvas _region;
    private readonly Image _milo;
    private readonly ScaleTransform _scale = new();
    private readonly RotateTransform _rotate = new();
    private readonly TranslateTransform _translate = new();
    private readonly Button _miloHit, _tail, _dotPill;
    private readonly System.Windows.Shapes.Path _tailBody, _tailTip;
    private readonly ScaleTransform _tailFlip = new(1, 1, 17, 17);
    private readonly Canvas _tailArt;
    private readonly Ellipse _halo;
    private readonly TranslateTransform _tailDrag = new();
    private Point? _dragStart;
    private bool _dragging;
    private Corner _corner = Corner.BottomRight;
    private readonly Border _badge, _whisper;
    private readonly System.Windows.Controls.TextBlock _badgeText, _whisperText, _dotText;
    private readonly Ellipse _whisperDot;
    private readonly ContentControl _cardHost, _dashHost, _detailHost;
    private readonly Grid _fx;
    private readonly CardRenderer _cards;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _hoverTimer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    private readonly DispatcherTimer _leaveTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };

    private MiloEngine? _engine;
    private MClip _clip = MClip.Gone;
    private double _clipStart;
    private string? _cardKey, _dashKey, _fxKey;
    private int _cardEp = -1;
    private Phase? _cardPhase;
    private double _sat = -1;
    private readonly List<(FrameworkElement El, Func<double, (double Op, double Tx, double Ty, double Sx, double Sy)> Anim, double Delay)> _fxAnims = [];
    private readonly List<(TextBlock El, Func<double, string> Text)> _fxTexts = [];

    public double BaseOffset { get; set; } = 44;

    /// <summary>Tủ đồ: "auto", "none", hoặc các id món ngăn bởi dấu phẩy (<see cref="Wardrobe.Resolve"/>).</summary>
    internal string AccessoryChoice { get; set; } = Wardrobe.Auto;

    /// <summary>Tủ đồ đổi: CompanionWindow lưu lựa chọn. Bảng tủ đồ trên đầu Milo gọi vào đây.</summary>
    internal Action<string>? OutfitChanged { get; set; }

    internal AppEnvironment Env { get; set; }

    private string? Accessory(MiloEngine e, MClip clip)
    {
        var outfit = e.Snap.Wardrobe is null ? [] : Wardrobe.Resolve(AccessoryChoice, Wardrobe.Owned(e.Snap.Wardrobe, e.Day));
        // "Mọi thứ vẫn ổn…": Milo cần ly để nhấp; tay đang trống thì cầm tạm ly cà phê
        if (clip == MClip.ThisIsFine && !outfit.Any(id => Wardrobe.Find(id)?.Slots.Contains(Slot.Hand) == true))
            outfit = Wardrobe.Wear(outfit, "coffee", toggle: false);
        return Wardrobe.SkinKey(outfit);
    }

    /// <summary>Góc neo (§9.1). Góc trái lật Milo theo chiều ngang, góc trên thì Milo tụt xuống từ mép trên.</summary>
    internal Corner Corner
    {
        get => _corner;
        set
        {
            _corner = value;
            _fxKey = null;
            Render();
        }
    }

    private bool LeftSide => _corner is Corner.BottomLeft or Corner.TopLeft;
    private bool TopSide => _corner is Corner.TopRight or Corner.TopLeft;

    /// <summary>Người dùng kéo chóp đuôi rồi thả ở <c>Point</c> (toạ độ màn hình, pixel thiết bị).</summary>
    public event Action<Point>? TailDropped;

    /// <summary>Người dùng vừa tương tác (host có thể vẽ lại ngay, lưu trạng thái…).</summary>
    public event Action? Interacted;

    public MiloEngine? Engine
    {
        get => _engine;
        set
        {
            _engine = value;
            _cardKey = _dashKey = _fxKey = null;
        }
    }

    public MiloLayer()
    {
        ClipToBounds = false;

        _milo = new Image { Width = 150, Height = 150, IsHitTestVisible = false, RenderTransform = new TransformGroup { Children = { _scale, _rotate, _translate } } };
        RenderOptions.SetBitmapScalingMode(_milo, BitmapScalingMode.HighQuality);
        Canvas.SetLeft(_milo, RegionW - 40 - 150);
        Canvas.SetTop(_milo, RegionH - 150);
        _region = new Canvas { Width = RegionW, Height = RegionH, ClipToBounds = true, IsHitTestVisible = false, Children = { _milo } };
        Place(_region, 0, 0);

        _miloHit = new Button { Width = 150, Height = 150, Style = (Style)Application.Current.FindResource("Bare") };
        System.Windows.Automation.AutomationProperties.SetName(_miloHit, "Bấm vào Milo");
        _miloHit.Click += (_, _) => Act(e => e.MiloClick());
        _miloHit.ContextMenu = QuickMenu();
        Place(_miloHit, 40, 0);

        // Chóp đuôi (mục 9.1): quầng thở 5s + đuôi cáo, màu theo mood
        var halo = _halo = new Ellipse { Width = 40, Height = 40, Fill = Br("#FFE3C4"), RenderTransformOrigin = new Point(.5, .5), RenderTransform = new ScaleTransform(), IsHitTestVisible = false };
        StartHalo(halo);
        Canvas.SetLeft(halo, 3);
        Canvas.SetTop(halo, 40 - 40 + 14);
        _tailBody = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M6 34 C4 24 8 14 16 9 C20 6 24 4 27 5 C24 8 23 11 24 14 C26 11 29 10 31 11 C27 15 24 22 24 34 Z"),
            Stroke = Br("#45231F"), StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round,
        };
        _tailTip = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M16 9 C20 6 24 4 27 5 C24 8 23 11 24 14 C26 11 29 10 31 11 C28 14 26 17 25 20 C22 17 18 13 16 9 Z"),
            Stroke = Br("#45231F"), StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round,
        };
        var tailArt = _tailArt = new Canvas { Width = 34, Height = 34, Children = { _tailBody, _tailTip }, RenderTransform = _tailFlip };
        Canvas.SetLeft(tailArt, 6);
        Canvas.SetTop(tailArt, 6);
        _badgeText = new System.Windows.Controls.TextBlock { FontSize = 10.5, FontWeight = FontWeights.Bold, Foreground = Br("#2B211A"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _badge = new Border { MinWidth = 18, Height = 18, CornerRadius = new CornerRadius(9), Background = Br("#E8A33D"), BorderBrush = Brushes.White, BorderThickness = new Thickness(2), Padding = new Thickness(4, 0, 4, 0), Child = _badgeText, Visibility = Visibility.Collapsed };
        Canvas.SetLeft(_badge, 34);
        Canvas.SetTop(_badge, -4);
        var tailBox = new Canvas { Width = 46, Height = 40, Children = { halo, tailArt, _badge } };
        _tail = new Button { Width = 46, Height = 40, Content = tailBox, Style = (Style)Application.Current.FindResource("Bare") };
        System.Windows.Automation.AutomationProperties.SetName(_tail, "Chóp đuôi Milo: rê chuột để Milo ló đầu, bấm để mở dashboard");
        _tail.RenderTransform = _tailDrag;
        // Kéo chóp đuôi sang góc khác (§9.1); bấm không kéo thì vẫn là mở dashboard
        _tail.PreviewMouseLeftButtonDown += (_, e) =>
        {
            _dragStart = e.GetPosition(this);
            _dragging = false;
        };
        _tail.PreviewMouseMove += (_, e) =>
        {
            if (_dragStart is not { } p0 || e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;
            var p = e.GetPosition(this);
            if (!_dragging && (p - p0).Length < 8) return;
            _dragging = true;
            _hoverTimer.Stop();
            _tail.Cursor = System.Windows.Input.Cursors.SizeAll;
            _tailDrag.X = p.X - p0.X;
            _tailDrag.Y = p.Y - p0.Y;
        };
        _tail.PreviewMouseLeftButtonUp += (_, e) =>
        {
            _dragStart = null;
            _tail.Cursor = System.Windows.Input.Cursors.Hand;
            if (!_dragging) return;
            _dragging = false;
            e.Handled = true; // không tính là bấm
            _tail.ReleaseMouseCapture();
            _tailDrag.X = _tailDrag.Y = 0;
            TailDropped?.Invoke(PointToScreen(e.GetPosition(this)));
        };
        _tail.MouseEnter += (_, _) =>
        {
            _leaveTimer.Stop();
            _hoverTimer.Start();
        };
        _tail.MouseLeave += (_, _) =>
        {
            _hoverTimer.Stop();
            if (_engine?.S.Peek == true) _leaveTimer.Start();
        };
        _tail.Click += (_, _) =>
        {
            _hoverTimer.Stop();
            Act(e => e.TailClick());
        };
        _tail.ContextMenu = QuickMenu();
        _hoverTimer.Tick += (_, _) =>
        {
            _hoverTimer.Stop();
            Act(e => e.Hover());
        };
        _leaveTimer.Tick += (_, _) =>
        {
            _leaveTimer.Stop();
            Act(e => e.Unhover());
        };
        Place(_tail, 96, 0);

        _whisperDot = new Ellipse { Width = 14, Height = 14, StrokeThickness = 2, Stroke = Br("#E8A33D"), Margin = new Thickness(0, 0, 8, 0) };
        _whisperText = new System.Windows.Controls.TextBlock { FontSize = 12.5, Foreground = Br("#FBF3E7"), VerticalAlignment = VerticalAlignment.Center };
        _whisper = new Border
        {
            Background = Br("#3A2A1E"), CornerRadius = new CornerRadius(14), Padding = new Thickness(10, 9, 14, 9), IsHitTestVisible = false,
            Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 8, Direction = 270, Opacity = .25 },
            Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { _whisperDot, _whisperText } }, Visibility = Visibility.Collapsed,
        };
        Place(_whisper, 150, 68);

        _fx = new Grid { IsHitTestVisible = false };
        Children.Add(_fx);

        _cards = new CardRenderer((act, val) => Act(e => e.UserReply(act, val)));
        _cardHost = new ContentControl { Focusable = false };
        Place(_cardHost, 26, 162);
        _dashHost = new ContentControl { Focusable = false };
        Place(_dashHost, 0, 0); // vườn trái cây neo đúng góc, xếp vòng cung quanh đầu Milo
        _detailHost = new ContentControl { Focusable = false };
        Place(_detailHost, 16, 150); // bảng chi tiết nằm ngay trên đầu Milo, cỡ 1 thẻ

        var pulse = new Ellipse { Width = 10, Height = 10, Fill = Br("#E8A33D"), Margin = new Thickness(0, 0, 8, 0) };
        _dotText = new System.Windows.Controls.TextBlock { FontSize = 12, Foreground = Br("#3A2A1E") };
        _dotPill = new Button
        {
            Style = (Style)Application.Current.FindResource("Round"), Background = Br("#FBF3E7"), Padding = new Thickness(9, 7, 12, 7), Visibility = Visibility.Collapsed,
            Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { pulse, _dotText } },
            Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 6, Direction = 270, Opacity = .25 },
            ToolTip = "Bấm để mở lời nhắc đứng đầu ngay, kể cả khi đang họp",
        };
        _dotPill.Click += (_, _) => Act(e => e.DotPillClick());
        Place(_dotPill, 22, 10);
    }

    private void Place(FrameworkElement el, double right, double bottom)
    {
        el.HorizontalAlignment = HorizontalAlignment.Right;
        el.VerticalAlignment = VerticalAlignment.Bottom;
        el.Tag = (right, bottom);
        Children.Add(el);
    }

    /// <summary>Đặt phần tử cách góc neo (right, bottom) — toạ độ của prototype, lật theo góc đang neo.</summary>
    private void Anchor(FrameworkElement el, double right, double bottom)
    {
        el.HorizontalAlignment = LeftSide ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        el.VerticalAlignment = TopSide ? VerticalAlignment.Top : VerticalAlignment.Bottom;
        var v = bottom + BaseOffset;
        el.Margin = new Thickness(LeftSide ? right : 0, TopSide ? v : 0, LeftSide ? 0 : right, TopSide ? 0 : v);
    }

    private void Act(Action<MiloEngine> a)
    {
        if (_engine is null) return;
        a(_engine);
        Interacted?.Invoke();
        Render();
    }

    private static void StartHalo(Ellipse halo)
    {
        var ease = new SineEase { EasingMode = EasingMode.EaseInOut };
        var s = new DoubleAnimation(1, 1.18, TimeSpan.FromSeconds(2.5)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = ease };
        var o = new DoubleAnimation(.35, .8, TimeSpan.FromSeconds(2.5)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = ease };
        ((ScaleTransform)halo.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, s);
        ((ScaleTransform)halo.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, s);
        halo.BeginAnimation(OpacityProperty, o);
    }

    private double Now => _clock.Elapsed.TotalSeconds;

    /// <summary>Vẽ lại theo trạng thái engine. Gọi mỗi khung hình khi Milo đang hiện.</summary>
    public void Render()
    {
        var e = _engine;
        if (e is null) return;
        foreach (FrameworkElement el in Children)
            if (el.Tag is (double r, double b)) Anchor(el, r, b);
        Canvas.SetLeft(_milo, LeftSide ? 40 : RegionW - 40 - 150);
        Canvas.SetTop(_milo, TopSide ? 0 : RegionH - 150);
        _tailFlip.ScaleX = LeftSide ? -1 : 1;
        _tailFlip.ScaleY = TopSide ? -1 : 1;

        // ---- Milo ----
        var clip = Present.VisualClip(e);
        if (clip != _clip)
        {
            _clip = clip;
            _clipStart = Now;
        }
        var elapsed = Now - _clipStart;
        var tf = ClipAnimation.Evaluate(clip, elapsed);
        // Góc trái: lật ngang; góc trên: lật dọc (Milo thò xuống từ mép trên)
        if (LeftSide) tf = tf with { Tx = -tf.Tx, Rot = -tf.Rot, Sx = -tf.Sx, OriginX = 1 - tf.OriginX };
        if (TopSide) tf = tf with { Ty = -tf.Ty, Rot = -tf.Rot, Sy = -tf.Sy, OriginY = 1 - tf.OriginY };
        double cx = tf.OriginX * 150, cy = tf.OriginY * 150;
        _scale.CenterX = _rotate.CenterX = cx;
        _scale.CenterY = _rotate.CenterY = cy;
        _scale.ScaleX = tf.Sx;
        _scale.ScaleY = tf.Sy;
        _rotate.Angle = tf.Rot;
        _translate.X = tf.Tx;
        _translate.Y = tf.Ty;
        var sat = e.CurrentBand.Saturation;
        if (clip == MClip.Gone) _milo.Source = null;
        else
        {
            var tired = e.S.BandIdx >= 2;
            var rig = MiloRig.For(clip, tired);
            var blink = MiloRig.Blink(Now, tired);
            _milo.Source = MiloSkin.Frame(Present.PoseFor(e, clip), rig, MiloRig.FrameIndex(rig, elapsed), blink, sat, Accessory(e, clip));
        }

        // ---- chóp đuôi ----
        _tail.Visibility = Present.TailVisible(e) ? Visibility.Visible : Visibility.Collapsed;
        // Lúc ló đầu Milo đã hiện cả đuôi thật → ẩn hình chóp đuôi, nhưng giữ nút để chuột vẫn đang "rê" trên đuôi
        var peeking = e.Presence() == PresenceState.Peek;
        _tailArt.Visibility = peeking ? Visibility.Hidden : Visibility.Visible;
        // Đang im lặng (họp, tập trung, toàn màn hình…): chóp đuôi mờ, không quầng thở — Milo vẫn chạy nhưng không làm phiền
        var dimmed = Present.TailDimmed(e);
        _halo.Visibility = peeking || dimmed ? Visibility.Hidden : Visibility.Visible;
        _tailArt.Opacity = dimmed ? .45 : 1;
        if (Math.Abs(sat - _sat) > .001)
        {
            _sat = sat;
            _tailBody.Fill = MiloSkin.Saturated("#E8772E", sat);
            _tailTip.Fill = MiloSkin.Saturated("#FFF7EC", sat);
        }
        var badge = Present.TailBadge(e);
        _badge.Visibility = badge is null ? Visibility.Collapsed : Visibility.Visible;
        _badgeText.Text = badge ?? "";

        // ---- thì thầm ----
        var w = Present.Whisper(e);
        if (w is null) _whisper.Visibility = Visibility.Collapsed;
        else
        {
            if (_whisper.Visibility != Visibility.Visible) Pop(_whisper);
            _whisper.Visibility = Visibility.Visible;
            _whisperDot.Fill = Br(e.CurrentBand.Bg);
            _whisperText.Inlines.Clear();
            AddInlines(_whisperText.Inlines, w);
        }

        // ---- chấm chờ ----
        var dot = Present.DotPill(e);
        _dotPill.Visibility = dot is null ? Visibility.Collapsed : Visibility.Visible;
        _dotText.Text = dot ?? "";
        _miloHit.Visibility = Present.MiloClickable(e) ? Visibility.Visible : Visibility.Collapsed;

        // ---- thẻ ----
        var ep = e.S.Ep;
        var showDash = ep is { C: CaseId.Dashboard, Card: true };
        var cardKey = ep is { Card: true } && !showDash ? $"{ep.Id}:{ep.CardVer}" : null;
        if (cardKey != _cardKey)
        {
            _cardKey = cardKey;
            var model = cardKey is null ? CardModel.Empty : Present.Card(e);
            var el = _cards.Build(model);
            var focused = _cardHost.IsKeyboardFocusWithin;
            _cardHost.Content = el;
            _cardHost.Tag = model.Low ? (26.0, 16.0) : (26.0, 162.0);
            Anchor(_cardHost, 26, model.Low ? 16 : 162);
            if (el is not null && (ep!.Id != _cardEp || ep.Phase != _cardPhase)) Pop(el);
            if (focused && el is not null) FocusChat(el);
            _cardEp = ep?.Id ?? -1;
            _cardPhase = ep?.Phase;
        }
        if (_cards.Breathe is { } br && Present.Breathe(e) is { } bs) br.Update(Present.BreatheElapsed(e), bs.Phase, bs.Count, bs.Label);

        // ---- dashboard ----
        // ---- dashboard: vườn trái cây quanh Milo ----
        var dashKey = showDash ? $"{ep!.Id}:{ep.CardVer}:{ep.Page}:{ep.Detail}:{ep.Wardrobe}:{_corner}" : null;
        if (dashKey != _dashKey)
        {
            var wasDetail = _detailHost.Content is not null;
            _dashKey = dashKey;
            var detail = dashKey is null ? null : Present.Detail(e);
            var wardrobe = dashKey is not null && Present.WardrobeOpen(e);
            _dashHost.Content = dashKey is not null && detail is null && !wardrobe && Present.Fruits(e) is { } fruits
                ? new FruitDashboardView(fruits, _corner, act => Act(x => x.UserReply(act)))
                : null;
            FrameworkElement? view = detail is not null ? new DetailDashboardView(detail, act => Act(x => x.UserReply(act)))
                : wardrobe ? new WardrobeView(Env, () => e.Snap.Wardrobe, () => e.Day, () => AccessoryChoice, choice =>
                    {
                        AccessoryChoice = choice;
                        OutfitChanged?.Invoke(choice);
                        Render();
                    }, () => Act(x => x.UserReply("close")))
                : null;
            _detailHost.Content = view;
            if (view is not null && !wasDetail) Pop(view);
        }

        RenderFx(e, clip, elapsed);
    }

    /// <summary>Chuột phải Milo hoặc chóp đuôi: thay đồ, trò chuyện, dashboard — không cần mở bảng điều khiển.</summary>
    private ContextMenu QuickMenu()
    {
        MenuItem Item(string header, Action<MiloEngine> run)
        {
            var m = new MenuItem { Header = header };
            m.Click += (_, _) => Act(run);
            return m;
        }
        var personality = new MenuItem { Header = "Tính cách Milo" };
        foreach (var (value, name, hint) in Catalog.Personalities)
        {
            var m = new MenuItem { Header = name, ToolTip = hint, IsCheckable = true };
            m.Click += (_, _) => Act(e => PersonalitySetting.Set(Env, e, value));
            personality.Items.Add(m);
        }
        var menu = new ContextMenu
        {
            Items =
            {
                Item("Thay đồ cho Milo", e => e.OpenWardrobe()),
                Item("Trò chuyện với Milo", e => e.OpenTalk()),
                Item("Mở dashboard", e => { if (e.S.Ep is null) e.TailClick(); }),
                new Separator(),
                personality,
            },
        };
        menu.Opened += (_, _) =>
        {
            foreach (var (m, value) in personality.Items.OfType<MenuItem>().Zip(Catalog.Personalities.Select(p => p.Value)))
                m.IsChecked = _engine?.Cfg.Personality == value;
        };
        return menu;
    }

    private static void FocusChat(FrameworkElement card)
    {
        card.Dispatcher.BeginInvoke(() =>
        {
            var tb = FindChild<TextBox>(card);
            tb?.Focus();
        }, DispatcherPriority.Loaded);
    }

    private static T? FindChild<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var c = VisualTreeHelper.GetChild(root, i);
            if (c is T t) return t;
            if (FindChild<T>(c) is { } found) return found;
        }
        return null;
    }

    // pop: 0% mờ, lệch 14px, .85 → 70% rõ, 1.03 → 100% bình thường (.35s)
    private static void Pop(FrameworkElement el)
    {
        var sc = new ScaleTransform(1, 1);
        var tr = new TranslateTransform();
        el.RenderTransformOrigin = new Point(1, 1);
        el.RenderTransform = new TransformGroup { Children = { sc, tr } };
        var d = TimeSpan.FromSeconds(.35);
        var s = new DoubleAnimationUsingKeyFrames { Duration = d };
        s.KeyFrames.Add(new LinearDoubleKeyFrame(.85, KeyTime.FromPercent(0)));
        s.KeyFrames.Add(new EasingDoubleKeyFrame(1.03, KeyTime.FromPercent(.7), new CubicEase { EasingMode = EasingMode.EaseOut }));
        s.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromPercent(1)));
        sc.BeginAnimation(ScaleTransform.ScaleXProperty, s);
        sc.BeginAnimation(ScaleTransform.ScaleYProperty, s);
        tr.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(14, 0, TimeSpan.FromSeconds(.25)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        el.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(.25)));
    }

    // ================= hiệu ứng =================
    private void RenderFx(MiloEngine e, MClip clip, double elapsed)
    {
        var visible = clip != MClip.Gone;
        var paws = clip == MClip.HangPull && elapsed < 1.8;
        var key = $"{clip}:{paws}:{(e.S.BandIdx == 3 && visible)}:{_corner}";
        if (key != _fxKey)
        {
            _fxKey = key;
            _fx.Children.Clear();
            _fxAnims.Clear();
            _fxTexts.Clear();
            BuildFx(clip, paws, e.S.BandIdx == 3 && visible);
        }
        foreach (var (tb, text) in _fxTexts) tb.Text = text(elapsed);
        foreach (var (el, anim, delay) in _fxAnims)
        {
            var t = elapsed - delay;
            if (t < 0)
            {
                el.Opacity = 0;
                continue;
            }
            var (op, tx, ty, sx, sy) = anim(t);
            el.Opacity = op;
            if (el.RenderTransform is TransformGroup { Children: [ScaleTransform s, TranslateTransform tt] })
            {
                s.ScaleX = sx;
                s.ScaleY = sy;
                tt.X = tx;
                tt.Y = ty;
            }
        }
    }

    private void AddFx(FrameworkElement el, double right, double bottom, Func<double, (double, double, double, double, double)> anim, double delay = 0)
    {
        Anchor(el, right, bottom);
        el.RenderTransformOrigin = new Point(.5, .5);
        el.RenderTransform = new TransformGroup { Children = { new ScaleTransform(), new TranslateTransform() } };
        _fx.Children.Add(el);
        _fxAnims.Add((el, anim, delay));
    }

    private static double Seg(double t, double a, double b) => Math.Clamp((t - a) / (b - a), 0, 1);

    private void BuildFx(MClip clip, bool paws, bool zz)
    {
        if (paws)
        {
            foreach (var r in new[] { 76.0, 136.0 })
                AddFx(new Border { Width = 20, Height = 10, CornerRadius = new CornerRadius(6, 6, 3, 3), Background = Br("#D76C43"), BorderBrush = Br("#45231F"), BorderThickness = new Thickness(2) },
                    r, -4, t => (Seg(t, 0, .2), 0, 0, 1, 1));
        }
        if (clip == MClip.Hello)
        {
            var hello = new Border
            {
                Background = Br("#FBF3E7"), CornerRadius = new CornerRadius(18, 18, 4, 18), Padding = new Thickness(16, 9, 16, 9),
                Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 10, Direction = 270, Opacity = .2 },
                Child = new System.Windows.Controls.TextBlock { Text = "Hello!", FontFamily = Serif, FontSize = 24, Foreground = Br("#3A2A1E") },
            };
            AddFx(hello, 150, 170, t =>
            {
                var p = Seg(t, 0, .45);
                var sc = p < .7 ? .85 + (1.03 - .85) * (p / .7) : 1.03 - .03 * ((p - .7) / .3);
                return (Math.Min(1, p / .7), 0, 14 * (1 - Math.Min(1, p / .7)), sc, sc);
            });
        }
        if (clip is MClip.Hello or MClip.Celebrate or MClip.Thanks)
        {
            (double R, double B, string C, double D)[] sparks = [(62, 186, "#E8A33D", 0), (170, 156, "#7FA65A", .5), (120, 194, "#7261B0", .9)];
            foreach (var (r, b, c, d) in sparks)
                AddFx(Dot(c, 9), r, b, t =>
                {
                    var p = t % 1.4 / 1.4;
                    var op = p < .4 ? p / .4 : 1 - (p - .4) / .6;
                    var s = .4 + .7 * p;
                    return (op, 0, -40 * p, s, s);
                }, d);
        }
        if (clip == MClip.JumpIn)
        {
            foreach (var (r, d) in new[] { (150.0, 0.0), (66.0, .1) })
                AddFx(new Border { Width = 30, Height = 8, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Color.FromArgb(46, 58, 42, 30)) }, r, 0, t =>
                {
                    var p = Seg(t, 0, .6);
                    var op = p < .4 ? p / .4 : 1 - (p - .4) / .6;
                    return (op, 0, 0, .3 + 1.3 * p, 1);
                }, 1.0 + d);
        }
        if (clip == MClip.RunToCar)
        {
            var car = new Canvas { Width = 92, Height = 52 };
            var body = new System.Windows.Shapes.Path { Data = Geometry.Parse("M8 30 L16 16 C18 12 22 10 26 10 L52 10 C56 10 60 12 62 16 L70 30 Z"), Fill = Br("#5471B0") };
            var cars = new UIElement[]
            {
                body,
                new Rectangle { Width = 76, Height = 12, RadiusX = 5, RadiusY = 5, Fill = Br("#3F5A92") },
                new Rectangle { Width = 14, Height = 12, RadiusX = 2, RadiusY = 2, Fill = Br("#D6E2F4") },
                new Rectangle { Width = 14, Height = 12, RadiusX = 2, RadiusY = 2, Fill = Br("#D6E2F4") },
                new Ellipse { Width = 14, Height = 14, Fill = Br("#1F262D") },
                new Ellipse { Width = 14, Height = 14, Fill = Br("#1F262D") },
            };
            (double X, double Y)[] at = [(0, 0), (4, 28), (24, 14), (42, 14), (15, 34), (55, 34)];
            for (var i = 0; i < cars.Length; i++)
            {
                Canvas.SetLeft(cars[i], at[i].X);
                Canvas.SetTop(cars[i], at[i].Y);
                car.Children.Add(cars[i]);
            }
            var art = new Viewbox { Width = 92, Height = 52, Child = new Canvas { Width = 84, Height = 48, Children = { car } } };
            AddFx(art, 250, 2, t =>
            {
                var p = Seg(t, 1.12, 2.8); // 0–40% đứng yên rồi chạy (ease-in)
                return (1, -520 * p * p, 0, 1, 1);
            });
        }
        BuildMemeFx(clip);
        if (zz)
        {
            (double R, double B, double S, double D)[] zs = [(70, 174, 20, 0), (56, 184, 15, .8), (44, 194, 12, 1.6)];
            foreach (var (r, b, s, d) in zs)
                AddFx(new System.Windows.Controls.TextBlock { Text = "z", FontFamily = Serif, FontSize = s, Foreground = Br("#7A6455") }, r, b, t =>
                {
                    var p = t % 2.2 / 2.2;
                    var op = p < .3 ? p / .3 : 1 - (p - .3) / .7;
                    return (op, 14 * p, -30 * p, 1, 1);
                }, d);
        }
    }

    // ================= hiệu ứng clip hài =================
    /// <summary>Nhãn bo tròn kiểu sticker (vd. "slay", "NPC mode").</summary>
    private static Border Sticker(string text, string bg, string fg, double size = 15) => new()
    {
        Background = Br(bg), CornerRadius = new CornerRadius(12), Padding = new Thickness(10, 4, 10, 5),
        BorderBrush = Br("#45231F"), BorderThickness = new Thickness(2),
        Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 3, Direction = 270, Opacity = .25 },
        Child = new TextBlock { Text = text, FontSize = size, FontWeight = FontWeights.Black, Foreground = Br(fg) },
    };

    private static TextBlock Glyph(string text, double size, string color) =>
        new() { Text = text, FontSize = size, FontWeight = FontWeights.Bold, Foreground = Br(color), FontFamily = new FontFamily("Segoe UI Symbol, Segoe UI") };

    /// <summary>Nảy vào: mờ → rõ, .8 → 1.08 → 1.</summary>
    private static (double, double, double, double, double) PopIn(double t, double end = 1e9)
    {
        var p = Seg(t, 0, .35);
        var sc = p < .7 ? .8 + .28 * (p / .7) : 1.08 - .08 * ((p - .7) / .3);
        var op = Math.Min(1, p / .5) * (t > end ? 1 - Seg(t, end, end + .3) : 1);
        return (op, 0, 0, sc, sc);
    }

    private void BuildMemeFx(MClip clip)
    {
        switch (clip)
        {
            case MClip.Slay:
                AddFx(Sticker("slay", "#7261B0", "#FFFFFF", 17), 26, 128, t => PopIn(t), .15);
                foreach (var (r, b, d) in new[] { (40.0, 96.0, 0.0), (176.0, 108.0, .35), (160.0, 146.0, .7), (92.0, 158.0, 1.05) })
                    AddFx(Glyph("✦", 18, "#F2C94C"), r, b, t =>
                    {
                        var p = t % 1.1 / 1.1;
                        var s = .3 + Math.Sin(Math.PI * p);
                        return (Math.Sin(Math.PI * p), 0, 0, s, s);
                    }, d);
                break;
            case MClip.SideEye:
                AddFx(Sticker("hmm…", "#FFF9F1", "#3A2A1E", 14), 174, 104, t => PopIn(t), .6);
                break;
            case MClip.Confused:
                (string G, double R, double B, double D)[] math =
                    [("π", 56, 120, 0), ("∑", 90, 140, .5), ("√x", 132, 136, 1), ("x²", 170, 116, 1.5), ("∫", 186, 86, 2), ("?", 112, 152, 2.5), ("≠", 40, 92, 3)];
                foreach (var (g, r, b, d) in math)
                    AddFx(Glyph(g, 17, "#5B5FC7"), r, b, t =>
                    {
                        var p = t % 3.5 / 3.5;
                        var op = p < .2 ? p / .2 : p > .7 ? 1 - (p - .7) / .3 : 1;
                        return (op * .9, 6 * Math.Sin(p * 6.3), -18 * p, 1, 1);
                    }, d);
                break;
            case MClip.Faint:
                AddFx(Sticker("ơ kìa!", "#FFFFFF", "#D1242F", 16), 70, 158, t => PopIn(t, .9));
                // Sao quay trên đầu lúc nằm (đầu nằm về phía giữa màn hình)
                for (var i = 0; i < 3; i++)
                {
                    var k = i;
                    AddFx(Glyph("✦", 15, "#F2C94C"), 214, 98, t =>
                    {
                        if (t < 1.5 || t > 3.1) return (0, 0, 0, 1, 1);
                        var a = t * 5 + k * 2.1;
                        return (1, 18 * Math.Cos(a), 6 * Math.Sin(a), 1, 1);
                    });
                }
                break;
            case MClip.Vibe:
                AddFx(Sticker("TGIF", "#E8772E", "#FFFFFF", 15), 28, 132, t => PopIn(t), .3);
                foreach (var (g, r, b, d) in new[] { ("♪", 40.0, 90.0, 0.0), ("♫", 180.0, 100.0, .6), ("♪", 160.0, 130.0, 1.2), ("♫", 110.0, 146.0, 1.8) })
                    AddFx(Glyph(g, 20, "#7261B0"), r, b, t =>
                    {
                        var p = t % 2.4 / 2.4;
                        var op = p < .2 ? p / .2 : 1 - (p - .2) / .8;
                        return (op, 8 * Math.Sin(p * 9), -40 * p, 1, 1);
                    }, d);
                break;
            case MClip.Loading:
            {
                const double w = 120, full = 5.2;
                static double P(double t) => Math.Clamp(t / full, 0, 1);
                var label = new TextBlock { FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Br("#3A2A1E") };
                _fxTexts.Add((label, t => P(t) >= 1 ? "Tuần mới đã sẵn sàng!" : $"Đang tải tuần mới… {Math.Max(1, (int)(P(t) * 100))}%"));
                AddFx(label, 44, 146, t => (1, 0, 0, 1, 1));
                AddFx(new Border { Width = w, Height = 12, CornerRadius = new CornerRadius(6), Background = Br("#F3E9DA"), BorderBrush = Br("#45231F"), BorderThickness = new Thickness(2) },
                    44, 130, t => (1, 0, 0, 1, 1));
                var fill = new Border { Width = w - 6, Height = 6, CornerRadius = new CornerRadius(3), Background = Br("#7FA65A") };
                AddFx(fill, 47, 133, t => (1, 0, 0, Math.Max(.01, P(t)), 1));
                fill.RenderTransformOrigin = LeftSide ? new Point(1, .5) : new Point(0, .5);
                break;
            }
            case MClip.Cobweb:
                foreach (var (r, b, flip) in new[] { (156.0, 100.0, false), (40.0, 104.0, true) })
                {
                    var web = new System.Windows.Shapes.Path
                    {
                        Data = Geometry.Parse("M0,0 L40,0 M0,0 L0,40 M0,0 L34,20 M0,0 L20,34 M12,0 Q10,10 0,12 M24,0 Q20,20 0,24 M36,0 Q30,30 0,36"),
                        Stroke = Br("#9C8672"), StrokeThickness = 1.4, Width = 42, Height = 42, Stretch = Stretch.None,
                        LayoutTransform = flip ? new ScaleTransform(-1, 1) : Transform.Identity,
                    };
                    AddFx(web, r, b, t => (t < 3.9 ? .9 : 1 - Seg(t, 3.9, 4.4), 0, t < 3.9 ? 0 : -20 * Seg(t, 3.9, 4.4), 1, 1));
                }
                foreach (var (r, b, d) in new[] { (80.0, 40.0, 3.9), (150.0, 60.0, 4.0), (110.0, 20.0, 4.1), (60.0, 80.0, 4.0) })
                    AddFx(Dot("#C9B6A0", 7), r, b, t =>
                    {
                        var p = Seg(t, 0, .8);
                        return (1 - p, 20 * p * (r > 100 ? 1 : -1), -24 * p, 1, 1);
                    }, d);
                AddFx(Sticker("…vẫn đợi bạn", "#FFF9F1", "#7A6455", 13), 62, 150, t => PopIn(t, 3.7), .4);
                break;
            case MClip.ThisIsFine:
                AddFx(Sticker("mọi thứ vẫn ổn…", "#FFF9F1", "#3A2A1E", 14), 20, 138, t => PopIn(t), .4);
                foreach (var (r, b, d) in new[] { (30.0, 10.0, 0.0), (170.0, 20.0, .8), (60.0, 40.0, 1.6), (190.0, 60.0, 2.4), (20.0, 70.0, 3.2) })
                    AddFx(new Ellipse { Width = 26, Height = 22, Fill = new SolidColorBrush(Color.FromArgb(110, 140, 128, 118)) }, r, b, t =>
                    {
                        var p = t % 4 / 4;
                        var op = p < .2 ? p / .2 : 1 - (p - .2) / .8;
                        var s = .6 + 1.2 * p;
                        return (op * .8, 6 * Math.Sin(p * 5), -70 * p, s, s);
                    }, d);
                break;
            case MClip.Zombie:
                AddFx(Sticker("NPC mode", "#5A5A66", "#E8E8F0", 14), 28, 136, t => PopIn(t), .3);
                AddFx(Glyph("…", 22, "#5A5A66"), 172, 118, t => (Math.Floor(t * 1.5) % 2 == 0 ? 1 : .3, 0, 0, 1, 1), .6);
                break;
        }
    }
}
