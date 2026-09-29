using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Minditful.App.Rendering;
using Minditful.App.Services;
using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Integrations;
using static Minditful.App.Rendering.Ui;

namespace Minditful.App.Controls;

/// <summary>
/// Tủ đồ phối theo ô (mũ · kẹp tóc · kính · cổ · tay cầm). Bấm 1 món là Milo mặc ngay, bấm lại là cởi.
/// Dùng ở 2 nơi: bảng nhỏ ngay trên đầu Milo (mở từ chuột phải Milo, menu khay, dashboard, chat) và thẻ trong bảng điều khiển.
/// Món chưa mở khoá bị mờ, rê chuột để xem điều kiện. Lưu tối đa 4 bộ, phối ngẫu nhiên, tự chọn.
/// </summary>
internal sealed class WardrobeView : Border
{
    public const double W = 320, MaxH = 420;
    private const string Ink = "#3A2A1E", Soft = "#7A6455", Line = "#F1DFC6", Accent = "#E8772E";
    private const int MaxSets = 4;

    private readonly AppEnvironment _env;
    private readonly Func<WardrobeInfo?> _info;
    private readonly Func<DateOnly> _today;
    private readonly Func<string> _get;
    private readonly Action<string> _set;
    private readonly List<(Border Tile, Accessory Item)> _tiles = [];
    private readonly WrapPanel _sets = new();
    private readonly TextBlock _progress;
    private static readonly Random Rnd = new();

    /// <param name="onClose">Bảng nhỏ trên đầu Milo có nút đóng; thẻ trong bảng điều khiển thì null.</param>
    public WardrobeView(AppEnvironment env, Func<WardrobeInfo?> info, Func<DateOnly> today, Func<string> get, Action<string> set,
        Action? onClose = null)
    {
        _env = env;
        _info = info;
        _today = today;
        _get = get;
        _set = set;
        var popup = onClose is not null;
        Width = popup ? W : double.NaN;
        if (popup)
        {
            Background = Br("#FFF9F1");
            BorderBrush = Br(Line);
            BorderThickness = new Thickness(1.5);
            CornerRadius = new CornerRadius(20);
            Padding = new Thickness(14, 12, 14, 12);
            Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 8, Direction = 270, Opacity = .22, Color = Rgb(Ink) };
        }

        var body = new StackPanel();
        if (popup)
        {
            var title = Text("Tủ đồ của Milo", 13, Ink, FontWeights.Bold, false);
            title.VerticalAlignment = VerticalAlignment.Center;
            var close = Small("×", onClose!, "#EFE3D0", "#5B4A3C");
            close.Width = 24;
            System.Windows.Automation.AutomationProperties.SetName(close, "Đóng tủ đồ");
            var head = Columns((title, Star), (close, Auto));
            head.Margin = new Thickness(0, 0, 0, 6);
            body.Children.Add(head);
        }
        foreach (var (slot, name) in Wardrobe.SlotNames) body.Children.Add(Row(slot, name, popup));

        // Bộ đã lưu + phối nhanh
        var tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
        tools.Children.Add(Small("Ngẫu nhiên", () => Apply(Wardrobe.Format(Wardrobe.Random(Owned(), Rnd))), "#FFFFFF", "#B85A34"));
        tools.Children.Add(Small("Tự chọn", () => Apply(Wardrobe.Auto), "#FFFFFF", Soft, "Tự mặc món khó mở nhất đang có"));
        tools.Children.Add(Small("Bỏ hết", () => Apply(Wardrobe.None), "#FFFFFF", Soft));
        tools.Children.Add(Small("+ Lưu bộ này", SaveSet, "#FFF1DE", "#B85A34", $"Lưu tối đa {MaxSets} bộ. Chuột phải vào 1 bộ để xoá."));
        body.Children.Add(Label("Phối nhanh"));
        body.Children.Add(tools);
        body.Children.Add(_sets);

        _progress = Text("", 11, Soft);
        _progress.Margin = new Thickness(0, 8, 0, 0);
        body.Children.Add(_progress);

        Child = popup
            ? new ScrollViewer
            {
                Style = (Style)Application.Current.FindResource("ThinScroll"), MaxHeight = MaxH - 24, Content = body,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            }
            : body;
        System.Windows.Automation.AutomationProperties.SetName(this, "Tủ đồ của Milo");
        Refresh();
    }

    private IReadOnlyList<Accessory> Owned() => Wardrobe.Owned(_info(), _today());

    private IReadOnlyList<string> Current() => Wardrobe.Resolve(_get(), Owned());

    private void Apply(string choice)
    {
        _set(choice);
        Refresh();
    }

    /// <summary>Tô lại ô đang mặc, món bị khoá, bộ đã lưu và dòng tiến độ (gọi sau mỗi lần đổi, hoặc theo nhịp của bảng điều khiển).</summary>
    public void Refresh()
    {
        var owned = Owned();
        var wearing = Current();
        foreach (var (tile, item) in _tiles)
        {
            var has = owned.Contains(item);
            var on = wearing.Contains(item.Id);
            tile.Opacity = has ? 1 : .38;
            tile.BorderBrush = Br(on ? Accent : "#00000000");
            tile.Background = Br(on ? "#FFF1DE" : "#F7EEE2");
            tile.ToolTip = has ? $"{Cap(item.Name)} · {(on ? "bấm để cởi" : "bấm để mặc")}" : $"{Cap(item.Name)} · cần {item.Condition}";
            tile.Cursor = has ? Cursors.Hand : Cursors.Arrow;
        }
        _sets.Children.Clear();
        foreach (var (name, outfit) in UiSettings.LoadOutfitSets(_env))
        {
            var chip = Small(name, () => Apply(outfit), outfit == Wardrobe.Format(wearing) ? "#3A2A1E" : "#FFFFFF",
                outfit == Wardrobe.Format(wearing) ? "#FBF3E7" : Ink, Describe(outfit));
            var n = name;
            chip.MouseRightButtonUp += (_, e) =>
            {
                UiSettings.SaveOutfitSets(_env, UiSettings.LoadOutfitSets(_env).Where(s => s.Name != n).ToList());
                e.Handled = true;
                Refresh();
            };
            _sets.Children.Add(chip);
        }
        _progress.Text = Wardrobe.Progress(_info(), _today());
    }

    private static string Describe(string outfit) =>
        string.Join(" + ", outfit.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Wardrobe.Find).Where(i => i is not null).Select(i => i!.Name));

    private void SaveSet()
    {
        var wearing = Current();
        if (wearing.Count == 0) return;
        var sets = UiSettings.LoadOutfitSets(_env).ToList();
        var outfit = Wardrobe.Format(wearing);
        if (sets.Any(s => s.Outfit == outfit)) return;
        if (sets.Count >= MaxSets) sets.RemoveAt(0); // đầy thì thay bộ cũ nhất
        var n = Enumerable.Range(1, MaxSets + 1).First(i => sets.All(s => s.Name != $"Bộ {i}"));
        sets.Add(($"Bộ {n}", outfit));
        UiSettings.SaveOutfitSets(_env, sets);
        Refresh();
    }

    // ================= 1 hàng = 1 ô =================
    private FrameworkElement Row(Slot slot, string name, bool popup)
    {
        var label = Text(name, 11, Soft, FontWeights.SemiBold, false);
        label.Width = popup ? 46 : 70;
        label.VerticalAlignment = VerticalAlignment.Center;
        var wrap = new WrapPanel();
        // Món nhiều ô (đồng phục Bosch) nằm ở hàng của ô đầu tiên theo thứ tự hiện
        var order = Wardrobe.SlotNames.Select(s => s.Slot).ToList();
        foreach (var item in Wardrobe.Items.Where(i => i.Slots.OrderBy(order.IndexOf).First() == slot))
            wrap.Children.Add(Tile(item, popup ? 40 : 64));
        var row = Columns((label, Auto), (wrap, Star));
        row.Margin = new Thickness(0, 2, 0, 2);
        return row;
    }

    private Border Tile(Accessory item, double size)
    {
        var tile = new Border
        {
            Width = size + 6, Height = size + 6, Margin = new Thickness(0, 0, 5, 5), CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(2), ClipToBounds = true, Child = Thumb(item, size), Focusable = true,
        };
        System.Windows.Automation.AutomationProperties.SetName(tile, Cap(item.Name));
        ToolTipService.SetInitialShowDelay(tile, 200);
        void Pick()
        {
            if (!Owned().Contains(item)) return;
            Apply(Wardrobe.Format(Wardrobe.Wear(Current(), item.Id)));
        }
        tile.MouseLeftButtonUp += (_, _) => Pick();
        tile.KeyDown += (_, e) =>
        {
            if (e.Key is not (Key.Enter or Key.Space)) return;
            e.Handled = true;
            Pick();
        };
        _tiles.Add((tile, item));
        return tile;
    }

    /// <summary>Ảnh Milo mặc đúng món đó, phóng to vào chỗ món nằm (đầu, cổ hoặc tay).</summary>
    private static FrameworkElement Thumb(Accessory item, double size)
    {
        // Vùng cần xem trong hệ toạ độ SVG (viewBox -20 -40 290 290)
        var (x0, y0, span) = item.Slots.Min() switch
        {
            Slot.Neck when item.Slots.Length == 1 => (70.0, 95.0, 100.0),
            Slot.Hand => item.Id == "lantern" ? (122.0, 108.0, 92.0) : (122.0, 132.0, 70.0),
            _ => item.Id == "santa" ? (55.0, -10.0, 150.0) : (50.0, -15.0, 140.0),
        };
        var k = size / span;
        var img = new Image
        {
            Source = MiloSkin.Frame(Pose.Idle, MiloRig.Idle, 0, false, 1, item.Id), Width = 290 * k, Height = 290 * k, Stretch = Stretch.Fill,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(-(x0 + 20) * k, -(y0 + 40) * k, 0, 0),
        };
        var host = new Grid { Width = size, Height = size, ClipToBounds = true, Margin = new Thickness(1), Children = { img } };
        return host;
    }

    private static TextBlock Label(string text)
    {
        var t = Text(text, 11, Soft, FontWeights.SemiBold, false);
        t.Margin = new Thickness(0, 8, 0, 4);
        return t;
    }

    private static Button Small(string text, Action click, string bg, string fg, string? tip = null)
    {
        var b = new Button
        {
            Style = (Style)Application.Current.FindResource("Round"), Content = text, Background = Br(bg), Foreground = Br(fg),
            BorderBrush = Br(Line), BorderThickness = new Thickness(1), FontSize = 11.5, FontWeight = FontWeights.SemiBold,
            Height = 26, MinHeight = 26, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 0, 5, 5),
            HorizontalContentAlignment = HorizontalAlignment.Center, ToolTip = tip,
        };
        b.Click += (_, _) => click();
        return b;
    }

    private static string Cap(string s) => s.Length == 0 ? s : char.ToUpper(s[0]) + s[1..];
}
