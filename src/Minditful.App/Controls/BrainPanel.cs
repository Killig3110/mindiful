using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using static Minditful.App.Rendering.Ui;

namespace Minditful.App.Controls;

/// <summary>"Bộ não Milo" — trạng thái hiện diện, cổng im lặng, hàng đợi, Mood Engine, nhật ký quyết định.</summary>
public sealed class BrainPanel : Border
{
    private static readonly (PresenceState S, string L)[] States =
        [(PresenceState.Off, "Nghỉ làm"), (PresenceState.Hidden, "Ẩn"), (PresenceState.Peek, "Ló đầu"), (PresenceState.Visit, "Ghé ngang"), (PresenceState.Talk, "Đang nói"), (PresenceState.Silent, "Im lặng")];

    private static readonly (Gate G, string L)[] Gates =
        [(Gate.Meeting, "Đang họp"), (Gate.Fullscreen, "Toàn màn hình"), (Gate.Focus, "Giờ tập trung"), (Gate.Dnd, "Không làm phiền"), (Gate.Locked, "Khoá máy"), (Gate.Off, "Nghỉ làm")];

    private readonly UniformGrid _states = new() { Columns = 3 };
    private readonly WrapPanel _gates = new();
    private readonly StackPanel _queue = new();
    private readonly System.Windows.Controls.TextBlock _budget;
    private readonly System.Windows.Controls.TextBlock _score, _band;
    private readonly Border _bandBox;
    private readonly WrapPanel _pen = new();
    private readonly Grid _signals = new();
    private readonly StackPanel _log = new();
    private string? _logKey, _stateKey;

    public BrainPanel()
    {
        Style = (Style)Application.Current.FindResource("PanelBorder");
        var root = new StackPanel();
        root.Children.Add(Eyebrow("BỘ NÃO MILO · CHẠY THEO KỊCH BẢN HÀNH VI"));

        Section(root, "TRẠNG THÁI HIỆN DIỆN (MỤC 3)", _states);
        Section(root, "CỔNG IM LẶNG (MỤC 4)", _gates);
        _budget = new System.Windows.Controls.TextBlock { Style = (Style)Application.Current.FindResource("Hint"), Margin = new Thickness(0, 6, 0, 0) };
        Section(root, "HÀNG ĐỢI", new StackPanel { Children = { _queue, _budget } });

        _score = new System.Windows.Controls.TextBlock { FontFamily = Serif, FontSize = 40, Foreground = Br("#FBF3E7"), VerticalAlignment = VerticalAlignment.Center };
        _band = new System.Windows.Controls.TextBlock { FontSize = 11.5, FontWeight = FontWeights.Bold };
        _bandBox = new Border { CornerRadius = new CornerRadius(999), Padding = new Thickness(9, 3, 9, 3), Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Child = _band };
        _signals.ColumnDefinitions.Add(new ColumnDefinition { Width = Star });
        _signals.ColumnDefinitions.Add(new ColumnDefinition { Width = Auto });
        _pen.Margin = new Thickness(0, 8, 0, 8);
        Section(root, "MOOD ENGINE (MỤC 11)", new StackPanel { Children = { new StackPanel { Orientation = Orientation.Horizontal, Children = { _score, _bandBox } }, _pen, _signals } });
        Section(root, "NHẬT KÝ QUYẾT ĐỊNH", _log);

        Child = new ScrollViewer { Style = (Style)Application.Current.FindResource("ThinScroll"), Content = root };
    }

    private static System.Windows.Controls.TextBlock Eyebrow(string t) => new() { Text = t, Style = (Style)Application.Current.FindResource("Eyebrow") };

    private static void Section(Panel root, string title, UIElement body)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 14, 0, 0) };
        var e = Eyebrow(title);
        e.Margin = new Thickness(0, 0, 0, 8);
        sp.Children.Add(e);
        sp.Children.Add(body);
        root.Children.Add(sp);
    }

    private static Border Chip(string text, bool on, bool soft = false) => new()
    {
        CornerRadius = new CornerRadius(soft ? 999 : 9), Margin = new Thickness(0, 0, 5, 5), Padding = soft ? new Thickness(9, 3, 9, 3) : new Thickness(7, 6, 7, 6),
        Background = on ? Br("#E8A33D") : soft ? Brushes.Transparent : Br("#3A2D23"),
        BorderBrush = soft ? (on ? Br("#E8A33D") : Br("#4A3A2E")) : null, BorderThickness = new Thickness(soft ? 1 : 0),
        Child = new System.Windows.Controls.TextBlock
        {
            Text = text, FontSize = soft ? 11 : 11.5, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center,
            Foreground = on ? Br("#2B211A") : Br("#A8927B"),
        },
    };

    public void Render(MiloEngine e)
    {
        var s = e.S;
        var pres = e.Presence();
        var g = e.HardGate();
        e.SortQueue();

        var stateKey = $"{pres}|{g}|{s.Typing}|{s.T < s.SettleUntil}|{s.Away}|{string.Join(',', s.Queue.Select(q => q.C))}|{s.ParkedList.Count}";
        if (stateKey != _stateKey)
        {
            _stateKey = stateKey;
            _states.Children.Clear();
            foreach (var (st, l) in States) _states.Children.Add(Chip(l, st == pres));
            _gates.Children.Clear();
            foreach (var (gg, l) in Gates) _gates.Children.Add(Chip(l, g == gg, soft: true));
            _gates.Children.Add(Chip("Đang gõ", s.Typing, soft: true));
            _gates.Children.Add(Chip("Ổn định sau họp", s.T < s.SettleUntil, soft: true));
            _gates.Children.Add(Chip("Rời máy", s.Away, soft: true));

            _queue.Children.Clear();
            if (s.Queue.Count == 0)
                _queue.Children.Add(Text("Trống" + (s.ParkedList.Count > 0 ? $" · {s.ParkedList.Count} lời nhắc thu gọn trên đuôi" : ""), 12, "#A8927B"));
            foreach (var q in s.Queue)
            {
                var def = Catalog.Def(q.C);
                var row = Columns((Dot(def.Color, 10), Px(18)), (Text(def.Name, 12, "#F3E6D3", wrap: false), Star), (Text($"P{q.Pri} · từ {Tm.Hm(q.Enq)}", 10.5, "#A8927B", FontWeights.Bold, false), Auto));
                _queue.Children.Add(new Border { Background = Br("#3A2D23"), CornerRadius = new CornerRadius(8), Padding = new Thickness(9, 6, 9, 6), Margin = new Thickness(0, 0, 0, 5), Child = row });
            }
        }
        var hourCount = s.HourList.Count(x => x > s.T - 3600);
        _budget.Text = $"Ngân sách: hôm nay {s.DayCount}/{e.Cfg.PerDay} lần chủ động · giờ này {hourCount}/{e.Cfg.PerHour}"
                       + (s.LastProactive > 0 ? " · lần tới sớm nhất " + Tm.Hm(e.NextBudgetAt()) : "");

        _score.Text = s.Score.ToString();
        var band = e.CurrentBand;
        _band.Text = band.Label;
        _band.Foreground = Br(band.Fg);
        _bandBox.Background = Br(band.Bg);

        _pen.Children.Clear();
        foreach (var (k, v) in s.Pen)
            if (v >= .5) _pen.Children.Add(PenChip($"{Present.PenaltyLabels[k]} −{Tm.JsRound(v)}", false));
        if (s.Bonus > 0) _pen.Children.Add(PenChip($"Thưởng +{s.Bonus}", true));
        if (_pen.Children.Count == 0) _pen.Children.Add(PenChip("Chưa có điểm phạt", false));

        var n = e.NextMeeting();
        (string, string)[] kv =
        [
            ("Chuỗi làm liền", s.DayStarted ? Tm.Dur(e.Streak()) : "—"),
            ("Nghỉ hôm nay", $"{s.Rest} phút" + (s.BreakRun > 0 && !e.InCall() ? $" (+{s.BreakRun} đang nghỉ)" : "")),
            ("Đã họp", Tm.Dur(e.MeetingMin())),
            ("Họp kế tiếp", n is null ? "—" : $"{Tm.Hm(n.Start)} · còn {Tm.JsRound((n.Start - s.T) / 60)}'"),
            ("Chuyển việc/giờ", e.SwitchesHour().ToString()),
            ("Quá giờ", $"{s.OtMin} phút"),
            ("Đã nghỉ trưa", s.LunchTaken ? "Có" : "Chưa"),
        ];
        if (_signals.RowDefinitions.Count != kv.Length)
        {
            _signals.Children.Clear();
            _signals.RowDefinitions.Clear();
            for (var i = 0; i < kv.Length; i++)
            {
                _signals.RowDefinitions.Add(new RowDefinition { Height = Auto });
                var l = Text("", 12, "#A8927B", wrap: false);
                var r = Text("", 12, "#FBF3E7", wrap: false);
                r.HorizontalAlignment = HorizontalAlignment.Right;
                Grid.SetRow(l, i);
                Grid.SetRow(r, i);
                Grid.SetColumn(r, 1);
                _signals.Children.Add(l);
                _signals.Children.Add(r);
            }
        }
        for (var i = 0; i < kv.Length; i++)
        {
            ((System.Windows.Controls.TextBlock)_signals.Children[i * 2]).Text = kv[i].Item1;
            ((System.Windows.Controls.TextBlock)_signals.Children[i * 2 + 1]).Text = kv[i].Item2;
        }

        var logKey = s.Log.Count + ":" + (s.Log.Count > 0 ? s.Log[^1].T : 0);
        if (logKey != _logKey)
        {
            _logKey = logKey;
            _log.Children.Clear();
            foreach (var l in Enumerable.Reverse(s.Log).Take(90))
            {
                var color = l.Kind switch
                {
                    LogKind.Deliver => "#FBF3E7",
                    LogKind.Error => "#F09595",
                    LogKind.Action => "#9CC7F0",
                    _ => "#D6C5AF",
                };
                var text = Text(l.Text, 11.5, color);
                if (l.Kind == LogKind.Deliver)
                    foreach (var b in text.Inlines.OfType<System.Windows.Documents.Bold>()) b.Foreground = Br("#E8A33D");
                var row = Columns((Text(Tm.Hm(l.T), 11.5, "#A8927B", wrap: false), Px(40)), (text, Star));
                row.Margin = new Thickness(0, 0, 0, 4);
                _log.Children.Add(row);
            }
        }
    }

    private static Border PenChip(string text, bool plus) => new()
    {
        Background = plus ? Br("#33452A") : Br("#3A2D23"), CornerRadius = new CornerRadius(6), Padding = new Thickness(7, 2, 7, 2), Margin = new Thickness(0, 0, 5, 5),
        Child = new System.Windows.Controls.TextBlock { Text = text, FontSize = 11, Foreground = plus ? Br("#B9DB94") : Br("#D6C5AF") },
    };
}
