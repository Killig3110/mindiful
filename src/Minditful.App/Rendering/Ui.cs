using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Minditful.App.Rendering;

/// <summary>Tiện ích dựng UI bằng code cho thẻ, dashboard, bộ não (nội dung đổi liên tục theo engine).</summary>
internal static class Ui
{
    private static readonly Dictionary<string, SolidColorBrush> Brushes = [];

    public static SolidColorBrush Br(string hex)
    {
        if (Brushes.TryGetValue(hex, out var b)) return b;
        b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        b.Freeze();
        return Brushes[hex] = b;
    }

    public static Color Rgb(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    public static readonly FontFamily Sans = new("Segoe UI Variable Text, Segoe UI");
    public static readonly FontFamily Serif = new("Cambria, Georgia");

    /// <summary>TextBlock hiểu **đậm**.</summary>
    public static TextBlock Text(string text, double size = 13, string color = "#3A2A1E", FontWeight? weight = null, bool wrap = true)
    {
        var tb = new TextBlock
        {
            FontSize = size, Foreground = Br(color), FontFamily = Sans,
            FontWeight = weight ?? FontWeights.Normal, TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
            TextTrimming = wrap ? TextTrimming.None : TextTrimming.CharacterEllipsis,
        };
        AddInlines(tb.Inlines, text);
        return tb;
    }

    public static void AddInlines(InlineCollection inlines, string text)
    {
        var parts = text.Split("**");
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            var run = new Run(parts[i]);
            inlines.Add(i % 2 == 1 ? new Bold(run) : run);
        }
    }

    public static Border Pill(string text, string bg, string fg, double size = 11.5, UIElement? icon = null)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        if (icon is not null)
        {
            if (icon is FrameworkElement fe) fe.Margin = new Thickness(0, 0, 6, 0);
            sp.Children.Add(icon);
        }
        sp.Children.Add(Text(text, size, fg, FontWeights.Bold, wrap: false));
        return new Border { Background = Br(bg), CornerRadius = new CornerRadius(999), Padding = new Thickness(10, 5, 10, 5), Child = sp, HorizontalAlignment = HorizontalAlignment.Left };
    }

    public static Border Dot(string color, double size)
    {
        return new Border { Width = size, Height = size, CornerRadius = new CornerRadius(size / 2), Background = Br(color), VerticalAlignment = VerticalAlignment.Center };
    }

    public static Border Avatar(string text, string color, double size = 28, double font = 10.5, string? ring = null)
    {
        return new Border
        {
            Width = size, Height = size, CornerRadius = new CornerRadius(size / 2), Background = Br(color),
            BorderBrush = ring is null ? null : Br(ring), BorderThickness = new Thickness(ring is null ? 0 : 2),
            Child = new TextBlock
            {
                Text = text, FontSize = font, FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            },
        };
    }

    public static Grid Columns(params (UIElement El, GridLength Width)[] cols)
    {
        var g = new Grid();
        for (var i = 0; i < cols.Length; i++)
        {
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = cols[i].Width });
            Grid.SetColumn(cols[i].El, i);
            g.Children.Add(cols[i].El);
        }
        return g;
    }

    public static readonly GridLength Auto = GridLength.Auto;
    public static readonly GridLength Star = new(1, GridUnitType.Star);
    public static GridLength Px(double v) => new(v);

    public static System.Windows.Shapes.Path Icon(string data, string stroke, double size, double strokeWidth = 2.4, string? fill = null) => new()
    {
        Data = Geometry.Parse(data), Stroke = Br(stroke), StrokeThickness = strokeWidth, Width = size, Height = size,
        Stretch = Stretch.Uniform, Fill = fill is null ? null : Br(fill), VerticalAlignment = VerticalAlignment.Center,
        StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
    };

    public const string BellIcon = "M6 17V11a6 6 0 0 1 12 0v6l2 2H4l2-2Z";
    public const string ClockIcon = "M12 3a9 9 0 1 1 0 18a9 9 0 1 1 0-18Z M12 7v5l3 2";
}
