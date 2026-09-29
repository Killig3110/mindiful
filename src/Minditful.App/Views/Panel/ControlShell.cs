using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Minditful.App.Rendering;
using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using static Minditful.App.Rendering.Ui;

namespace Minditful.App.Views.Panel;

/// <summary>
/// Khung chung của bảng điều khiển (Demo và Sandbox/Production): tông kem – cam như thẻ của Milo,
/// thanh bên trái chọn trang, dải trạng thái "Milo đang làm gì" luôn ở trên cùng.
/// Mỗi trang dựng 1 lần; phần số liệu đổi theo engine được cập nhật 2–3 lần/giây qua <see cref="Tick"/>.
/// </summary>
internal abstract partial class ControlShell : Window
{
    private sealed record PageDef(string Id, string Icon, string Label, string? Hint, Func<FrameworkElement> Build);

    private readonly List<PageDef> _pages = [];
    private readonly Dictionary<string, (FrameworkElement View, List<Action> Updaters)> _built = [];
    private readonly Dictionary<string, Button> _nav = [];
    private readonly StackPanel _navPanel = new();
    private readonly ContentControl _content = new();
    private readonly ScrollViewer _scroll;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private List<Action>? _building;
    private readonly List<Action> _headerUpdaters = [];
    private readonly Border _envPill;
    private readonly TextBlock _envPillText, _envNote;
    private string? _current;

    protected abstract MiloEngine Engine { get; }
    /// <summary>Dòng dưới đồng hồ, vd. "Thứ Năm · 24/9 · giờ kịch bản".</summary>
    protected abstract string ClockNote { get; }

    protected ControlShell(string title, string envLabel, string envColor, string envNote)
    {
        Title = title;
        Width = 1060;
        Height = 780;
        MinWidth = 880;
        MinHeight = 580;
        Background = Br(P.Bg);
        FontFamily = Sans;
        FontSize = 13;
        Foreground = Br(P.Ink);
        UseLayoutRounding = true;

        // ---- thanh bên ----
        var brand = new StackPanel { Margin = new Thickness(18, 20, 18, 18) };
        var avatar = new Image { Width = 46, Height = 46, Source = MiloSkin.Get(Pose.Greeting, 1), Margin = new Thickness(0, 0, 10, 0) };
        var names = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        names.Children.Add(Text("Milo", 17, P.Ink, FontWeights.Bold, false));
        names.Children.Add(Text("Minditful", 11.5, P.Muted, null, false));
        brand.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { avatar, names } });
        _envPillText = new TextBlock { FontSize = 11, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
        _envPill = new Border
        {
            CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 12, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left, Child = _envPillText,
        };
        brand.Children.Add(_envPill);
        _envNote = Text("", 11.5, P.Ink2);
        _envNote.Margin = new Thickness(0, 6, 0, 0);
        brand.Children.Add(_envNote);
        SetBadge(envLabel, envColor, envNote);

        _navPanel.Margin = new Thickness(10, 0, 10, 0);
        var foot = Text("Đóng cửa sổ này thì Milo vẫn chạy ở góc màn hình. Mở lại bằng biểu tượng chóp đuôi ở khay hệ thống.", 11, P.Muted);
        foot.Margin = new Thickness(18, 0, 18, 18);
        var side = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(brand, Dock.Top);
        DockPanel.SetDock(foot, Dock.Bottom);
        side.Children.Add(brand);
        side.Children.Add(foot);
        side.Children.Add(_navPanel);
        var sideBox = new Border { Background = Br(P.Side), BorderBrush = Br(P.Line), BorderThickness = new Thickness(0, 0, 1, 0), Child = side };

        // ---- nội dung ----
        _scroll = new ScrollViewer
        {
            Style = (Style)Application.Current.FindResource("ThinScroll"), Content = _content, Padding = new Thickness(0, 0, 0, 0),
        };
        var right = new Grid { Margin = new Thickness(24, 20, 14, 0) };
        right.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var header = BuildHeader();
        header.Margin = new Thickness(0, 0, 10, 16);
        right.Children.Add(header);
        Grid.SetRow(_scroll, 1);
        right.Children.Add(_scroll);

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.Children.Add(sideBox);
        Grid.SetColumn(right, 1);
        root.Children.Add(right);
        Content = root;

        _timer.Tick += (_, _) => Refresh();
        Loaded += (_, _) => _timer.Start();
        Closed += (_, _) => _timer.Stop();
    }

    /// <summary>Thêm 1 trang vào thanh bên. <paramref name="icon"/> là ký tự của font Segoe Fluent Icons / MDL2.</summary>
    protected void AddPage(string id, string icon, string label, Func<FrameworkElement> build, string? hint = null)
    {
        _pages.Add(new PageDef(id, icon, label, hint, build));
        var glyph = new TextBlock { Text = icon, FontFamily = Icons, FontSize = 16, Width = 26, VerticalAlignment = VerticalAlignment.Center };
        var name = new TextBlock { Text = label, FontSize = 13.5, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        var content = new StackPanel { Orientation = Orientation.Horizontal, Children = { glyph, name } };
        if (hint is not null)
        {
            var h = Text(hint, 10.5, P.Muted, null, false);
            h.Margin = new Thickness(26, 1, 0, 0);
            content = new StackPanel { Children = { content, h } };
        }
        var b = new Button
        {
            Style = (Style)Application.Current.FindResource("NavItem"), Content = content, Padding = new Thickness(12, 9, 12, 9),
            Margin = new Thickness(0, 0, 0, 3), HorizontalContentAlignment = HorizontalAlignment.Left,
        };
        System.Windows.Automation.AutomationProperties.SetName(b, label);
        b.Click += (_, _) => Show(id);
        _nav[id] = b;
        _navPanel.Children.Add(b);
    }

    /// <summary>Đổi nhãn môi trường ở thanh bên (vd. Sandbox chuyển giữa chế độ test và như Production).</summary>
    protected void SetBadge(string label, string color, string note)
    {
        _envPillText.Text = label;
        _envPill.Background = Br(color);
        _envNote.Text = note;
    }

    /// <summary>Ẩn/hiện 1 trang trên thanh bên. Đang mở trang bị ẩn thì quay về trang đầu.</summary>
    protected void SetPageVisible(string id, bool visible)
    {
        if (!_nav.TryGetValue(id, out var b)) return;
        b.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible && _current == id) Show(_pages[0].Id);
    }

    protected void Show(string id)
    {
        if (!_built.TryGetValue(id, out var page))
        {
            var def = _pages.First(p => p.Id == id);
            _building = [];
            var view = def.Build();
            page = (view, _building);
            _built[id] = page;
            _building = null;
        }
        _current = id;
        _content.Content = page.View;
        _scroll.ScrollToTop();
        foreach (var (k, b) in _nav)
        {
            var on = k == id;
            b.Background = on ? Br(P.Card) : Brushes.Transparent;
            b.Foreground = on ? Br(P.Accent) : Br(P.Ink2);
            b.Effect = on ? new DropShadowEffect { BlurRadius = 10, ShadowDepth = 2, Direction = 270, Opacity = .08 } : null;
        }
        Refresh();
    }

    /// <summary>Đăng ký 1 hàm cập nhật cho trang đang dựng (chạy khi trang đang mở).</summary>
    protected void Tick(Action update) => (_building ?? _headerUpdaters).Add(update); // chạy lần đầu ở Refresh() ngay sau khi trang hiện

    /// <summary>Vẽ lại ngay dải trạng thái và trang đang mở (sau khi người dùng bấm gì đó).</summary>
    protected void Refresh()
    {
        foreach (var u in _headerUpdaters) u();
        if (_current is not null && _built.TryGetValue(_current, out var p))
            foreach (var u in p.Updaters) u();
        OnRefresh();
    }

    protected virtual void OnRefresh() { }

    protected static void RefreshMilo() => ((App)Application.Current).Companion?.Refresh();

    // ================= dải trạng thái trên cùng =================
    private FrameworkElement BuildHeader()
    {
        var milo = new Image { Width = 70, Height = 70, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) };
        var status = Text("", 17, P.Ink, FontWeights.Bold);
        var detail = Text("", 12.5, P.Ink2);
        detail.Margin = new Thickness(0, 4, 0, 0);
        var mid = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { status, detail } };

        var clock = new TextBlock { FontFamily = Serif, FontSize = 34, Foreground = Br(P.Accent), HorizontalAlignment = HorizontalAlignment.Right };
        var clockNote = Text("", 11.5, P.Muted, null, false);
        clockNote.HorizontalAlignment = HorizontalAlignment.Right;
        var grape = new Viewbox { Width = 30, Height = 30, VerticalAlignment = VerticalAlignment.Center };
        var score = new TextBlock { FontSize = 15, FontWeight = FontWeights.Bold, Foreground = Br(P.Ink), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 6, 0) };
        var bandText = new TextBlock { FontSize = 11, FontWeight = FontWeights.Bold };
        var band = new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(7, 2, 7, 2), VerticalAlignment = VerticalAlignment.Center, Child = bandText };
        var mood = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 4, 0, 0), Children = { grape, score, band } };
        System.Windows.Automation.AutomationProperties.SetName(mood, "Điểm mood hôm nay");
        var clockBox = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 0, 0), Children = { clock, clockNote, mood } };

        var row = Ui.Columns((milo, Ui.Auto), (mid, Ui.Star), (clockBox, Ui.Auto));
        int? lastScore = null;
        Tick(() =>
        {
            var e = Engine;
            var (title, text, pose) = Describe(e);
            status.Text = title;
            detail.Text = text;
            milo.Source = MiloSkin.Get(pose, e.CurrentBand.Saturation);
            milo.Opacity = e.Presence() == PresenceState.Off ? .55 : 1;
            clock.Text = Tm.Hm(e.S.T);
            clockNote.Text = ClockNote;
            if (lastScore != e.S.Score)
            {
                lastScore = e.S.Score;
                grape.Child = FruitArt.Grape(e.S.Score);
            }
            score.Text = e.S.Score.ToString();
            bandText.Text = e.CurrentBand.Label;
            bandText.Foreground = Br(e.CurrentBand.Fg);
            band.Background = Br(e.CurrentBand.Bg);
        });
        return Card(row, padding: new Thickness(16, 12, 18, 12));
    }

    /// <summary>"Milo đang làm gì" bằng lời thường, không số mục tài liệu.</summary>
    protected static (string Title, string Text, Pose Pose) Describe(MiloEngine e)
    {
        var s = e.S;
        var tired = s.BandIdx >= 2 ? Pose.Tired : Pose.Idle;
        var gate = e.HardGate();
        switch (e.Presence())
        {
            case PresenceState.Talk when s.Ep is { } ep:
                var name = Catalog.Def(ep.C).Name;
                return ep.Phase switch
                {
                    Phase.Enter => ($"Milo đang xuất hiện · {name}", "Đang leo lên góc màn hình.", Pose.Greeting),
                    Phase.Exit => ("Milo đang rời đi", ep.Clip == Core.Engine.Clip.RunToCar ? "Chạy ra xe về nhà. Hẹn mai gặp!" : "Leo xuống, chỉ còn chóp đuôi ở góc.", Pose.Wave),
                    Phase.Breathe => ("Milo đang thở cùng bạn", "Hít vào 4 giây, giữ 4 giây, thở ra 4 giây.", Pose.Breathe),
                    _ => ($"Milo đang nói · {name}", ep.C == CaseId.Dashboard ? "Dashboard đang mở ở góc màn hình." : "Thẻ đang chờ bạn trả lời ở góc màn hình.", Pose.Wave),
                };
            case PresenceState.Off when gate == Gate.Presenting:
                return ("Milo đang trốn", "Bạn đang trình chiếu nên Milo ẩn hẳn, kể cả chóp đuôi.", Pose.Idle);
            case PresenceState.Off:
                return ("Milo đang nghỉ", !s.DayStarted ? "Chưa tới giờ làm hoặc máy đang khoá. Mở máy lần đầu trong ngày Milo sẽ chào." :
                    s.OffDuty ? "Đã tan tầm. Milo nghỉ tới sáng mai." : "Máy đang khoá.", Pose.Tired);
            case PresenceState.Silent:
                return ("Milo đang im lặng", $"Vì {Catalog.GateLabel[gate!.Value].ToLowerInvariant()}." +
                    (s.Queue.Count > 0 ? $" Có {s.Queue.Count} lời nhắc đang chờ ở góc (bấm chấm để xem ngay)." : " Không có lời nhắc nào đang chờ."), tired);
            case PresenceState.Peek:
                return ("Milo đang ló đầu", "Bạn đang rê chuột lên chóp đuôi. Bấm để mở dashboard.", Pose.Greeting);
            case PresenceState.Visit:
                return ("Milo ghé ngang", "Ló lên vài giây rồi đi, không nói gì.", tired);
            default:
                return ("Milo đang ẩn ở góc màn hình", s.Queue.Count > 0
                    ? $"{s.Queue.Count} lời nhắc đang chờ đúng lúc để nói."
                    : "Chỉ còn chóp đuôi. Rê chuột lên đuôi để Milo ló đầu, bấm để mở dashboard.", tired);
        }
    }
}
