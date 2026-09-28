using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Minditful.Core.Engine;
using static Minditful.App.Rendering.Ui;

namespace Minditful.App.Controls;

/// <summary>Màn hình giả 700×560 của ngày mẫu: VS Code, cuộc họp Teams, trình chiếu, màn hình khoá, taskbar.</summary>
public sealed class DesktopScene : Grid
{
    private readonly Border _focusBand, _meet, _lock, _fullscreen, _presDot;
    private readonly System.Windows.Controls.TextBlock _focusText, _lockTime, _lockSub, _tbClock, _meetTitle;
    private readonly Grid _meetGrid = new();
    private readonly StackPanel _meetRec = new() { Orientation = Orientation.Horizontal };
    private string? _meetKey;
    private Color _bg;

    public MiloLayer Layer { get; } = new() { BaseOffset = 44 };

    public DesktopScene()
    {
        Width = 700;
        Height = 560;
        ClipToBounds = true;
        Background = new SolidColorBrush(Rgb("#D6DEE4"));

        // Cửa sổ VS Code
        var code = new StackPanel { Margin = new Thickness(20, 18, 20, 18) };
        foreach (var w in new[] { 44, 90, 76, 84, 62, 70, 55, 88, 40, 66 })
            code.Children.Add(new Border { Height = 8, CornerRadius = new CornerRadius(4), Background = Br("#E1E6EA"), Width = 400 * w / 100.0, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 10) });
        var bar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        for (var i = 0; i < 3; i++) bar.Children.Add(new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(5), Background = Br("#C2CBD2"), Margin = new Thickness(0, 0, 6, 0) });
        bar.Children.Add(Text("login-flow.ts — Visual Studio Code", 11.5, "#56616B", wrap: false));
        _focusText = Text("", 11.5, "#0B4F8A", FontWeights.Bold, false);
        _focusText.VerticalAlignment = VerticalAlignment.Center;
        _focusBand = new Border { Height = 26, Background = Br("#DDEBF7"), Padding = new Thickness(12, 0, 12, 0), Child = _focusText, Visibility = Visibility.Collapsed, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 32, 0, 0) };
        var win = new Border
        {
            Width = 440, Height = 360, CornerRadius = new CornerRadius(12), Background = Br("#F4F6F8"), ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(28, 26, 0, 0),
            Effect = new DropShadowEffect { BlurRadius = 30, ShadowDepth = 10, Direction = 270, Opacity = .15 },
            Child = new Grid
            {
                Children =
                {
                    new DockPanel { Children = { Dock(new Border { Height = 32, Background = Br("#E3E8EC"), Child = bar }, System.Windows.Controls.Dock.Top), new Border { Margin = new Thickness(0, 0, 0, 0), Child = code } } },
                    _focusBand,
                },
            },
        };
        Children.Add(win);

        // Cuộc họp Teams
        _meetTitle = Text("", 12, "#AEB9C2", wrap: false);
        var hd = Columns((_meetTitle, Star), (_meetRec, Auto));
        hd.Margin = new Thickness(14, 0, 14, 0);
        hd.Height = 36;
        _meetGrid.Margin = new Thickness(8);
        for (var i = 0; i < 2; i++)
        {
            _meetGrid.ColumnDefinitions.Add(new ColumnDefinition());
            _meetGrid.RowDefinitions.Add(new RowDefinition());
        }
        _meet = new Border
        {
            Width = 664, Height = 452, CornerRadius = new CornerRadius(12), Background = Br("#1B2229"), ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(18, 18, 0, 0),
            Visibility = Visibility.Collapsed,
            Child = new DockPanel { Children = { Dock(hd, System.Windows.Controls.Dock.Top), _meetGrid } },
        };
        Children.Add(_meet);

        Children.Add(Layer);

        // Taskbar + chấm presence Teams
        _presDot = new Border { Width = 9, Height = 9, CornerRadius = new CornerRadius(5), BorderBrush = Br("#1F262D"), BorderThickness = new Thickness(2), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, -4, -4) };
        var icons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        foreach (var c in new[] { "#4A5560", "#5B5FC7", "#0F6CBD", "#0078D4", "#2B7A4B" })
        {
            var ic = new Grid { Width = 18, Height = 18, Margin = new Thickness(0, 0, 12, 0) };
            ic.Children.Add(new Border { CornerRadius = new CornerRadius(4), Background = Br(c) });
            if (c == "#5B5FC7") ic.Children.Add(_presDot);
            icons.Children.Add(ic);
        }
        _tbClock = Text("08:50", 12, "#C9D3DA", wrap: false);
        _tbClock.VerticalAlignment = VerticalAlignment.Center;
        var taskbar = Columns((icons, Star), (_tbClock, Auto));
        Children.Add(new Border { Height = 44, Background = Br("#1F262D"), VerticalAlignment = VerticalAlignment.Bottom, Padding = new Thickness(16, 0, 16, 0), Child = taskbar });

        // Toàn màn hình (trình chiếu)
        var slide = new StackPanel { Width = 520, Height = 300, Background = Br("#1E2A36"), VerticalAlignment = VerticalAlignment.Center };
        slide.Children.Add(new System.Windows.Controls.TextBlock { Text = "Q3 Review · trình chiếu", FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Br("#DCE3EA"), Margin = new Thickness(40, 70, 40, 14) });
        foreach (var w in new[] { .8, .64, .72 })
            slide.Children.Add(new Border { Height = 10, CornerRadius = new CornerRadius(5), Background = Br("#34475A"), Width = 440 * w, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(40, 0, 0, 14) });
        var fsNote = Text("Ứng dụng toàn màn hình · Milo tự im", 12, "#DCE3EA", wrap: false);
        fsNote.Opacity = .6;
        fsNote.HorizontalAlignment = HorizontalAlignment.Center;
        fsNote.Margin = new Thickness(0, 6, 0, 0);
        _fullscreen = Overlay(Br("#0F1419"), new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { slide, fsNote } });
        Children.Add(_fullscreen);

        // Màn hình khoá
        _lockTime = new System.Windows.Controls.TextBlock { FontFamily = Serif, FontSize = 72, Foreground = Br("#E7EEF6"), HorizontalAlignment = HorizontalAlignment.Center };
        var date = Text("Thứ Năm, 24 tháng 9", 14, "#E7EEF6", wrap: false);
        date.Opacity = .8;
        date.HorizontalAlignment = HorizontalAlignment.Center;
        _lockSub = Text("Máy đang khoá", 12, "#E7EEF6", wrap: false);
        _lockSub.Opacity = .6;
        _lockSub.HorizontalAlignment = HorizontalAlignment.Center;
        _lockSub.Margin = new Thickness(0, 18, 0, 0);
        _lock = Overlay(new LinearGradientBrush(Rgb("#27405E"), Rgb("#141E2B"), 70), new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { _lockTime, date, _lockSub } });
        Children.Add(_lock);
    }

    private static T Dock<T>(T el, System.Windows.Controls.Dock d) where T : UIElement
    {
        DockPanel.SetDock(el, d);
        return el;
    }

    private static Border Overlay(Brush bg, UIElement child) => new() { Background = bg, Child = child, Visibility = Visibility.Collapsed };

    public void SetDateLabel(string text) => ((System.Windows.Controls.TextBlock)((StackPanel)_lock.Child).Children[1]).Text = text;

    public void Render(MiloEngine e)
    {
        var s = e.S;
        var t = s.T;
        var g = e.HardGate();
        _tbClock.Text = _lockTime.Text = Tm.Hm(t);

        var bg = Rgb(t < Tm.T("11:30") ? "#D6DEE4" : t < Tm.T("16:00") ? "#C9D3DA" : t < Tm.T("18:00") ? "#C3CCD3" : "#B9BDD4");
        if (bg != _bg)
        {
            _bg = bg;
            ((SolidColorBrush)Background).BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(bg, TimeSpan.FromSeconds(1.5)));
        }

        _lock.Visibility = s.Locked ? Visibility.Visible : Visibility.Collapsed;
        _lockSub.Text = s.DayStarted ? (s.OffDuty ? "Hẹn gặp lại ngày mai" : "Máy đang khoá") : $"Máy đang khoá · khung giờ {Tm.Hm(e.Cfg.Start)}–{Tm.Hm(e.Cfg.End)}";
        _fullscreen.Visibility = s.Fullscreen && !s.Locked ? Visibility.Visible : Visibility.Collapsed;
        _presDot.Background = Br(g is Gate.Meeting or Gate.Focus or Gate.Dnd ? "#C4314B" : s.Away ? "#F8D22A" : "#6BB700");

        var focus = e.FocusActive();
        _focusBand.Visibility = focus ? Visibility.Visible : Visibility.Collapsed;
        if (focus) _focusText.Text = $"Tập trung: #{s.FocusTask?.Id} · Không làm phiền đến {Tm.Hm(s.FocusUntil!.Value)}";

        var ev = e.InCall() ? e.Ongoing() : null;
        if (ev is null)
        {
            _meet.Visibility = Visibility.Collapsed;
            _meetKey = null;
        }
        else
        {
            if (_meetKey != ev.Id)
            {
                _meetKey = ev.Id;
                _meetGrid.Children.Clear();
                var people = ev.People.Append(new Person("Bạn", "#3A2A1E")).Take(4).ToList();
                for (var i = 0; i < people.Count; i++)
                {
                    var tile = new Border { Background = Br("#2C3640"), CornerRadius = new CornerRadius(8), Margin = new Thickness(4), Child = Avatar(people[i].Initials, people[i].Color, 56, 19) };
                    ((FrameworkElement)tile.Child).HorizontalAlignment = HorizontalAlignment.Center;
                    SetColumn(tile, i % 2);
                    SetRow(tile, i / 2);
                    _meetGrid.Children.Add(tile);
                }
                _meetRec.Children.Clear();
                if (ev.Role == "Trình bày")
                {
                    var d = Dot("#E24B4A", 8);
                    d.Margin = new Thickness(0, 0, 6, 0);
                    _meetRec.Children.Add(d);
                    _meetRec.Children.Add(Text("Đang chia sẻ màn hình", 11.5, "#F09595", wrap: false));
                }
                else _meetRec.Children.Add(Text("Teams", 12, "#AEB9C2", wrap: false));
                _meetRec.VerticalAlignment = VerticalAlignment.Center;
                _meetTitle.VerticalAlignment = VerticalAlignment.Center;
            }
            _meet.Visibility = Visibility.Visible;
            var el = t - ev.Start;
            _meetTitle.Text = $"{ev.Subject} · {(int)(el / 60):00}:{(int)(el % 60):00}";
        }

        Layer.Render();
    }
}
