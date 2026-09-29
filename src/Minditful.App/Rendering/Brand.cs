using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Minditful.App.Rendering;

/// <summary>
/// Nhận diện của app: logo Milo đội mũ Bosch (Assets/Brand) và 3 màu Bosch đỏ · xanh dương · xanh lá
/// (màu đặc, không chuyển màu) làm dải nhấn trên các cửa sổ.
/// </summary>
internal static class Brand
{
    private const string Ico = "pack://application:,,,/Assets/Brand/milo.ico";
    private const string Png = "pack://application:,,,/Assets/Brand/logo.png";

    public static readonly string[] Colors = ["#E20015", "#007BC0", "#00884A"];

    /// <summary>Icon cửa sổ (thanh tiêu đề, Alt+Tab, taskbar).</summary>
    public static ImageSource WindowIcon() => BitmapFrame.Create(new Uri(Ico));

    /// <summary>Logo 256px cho giao diện (thanh bên bảng điều khiển, màn hình chọn môi trường).</summary>
    public static ImageSource Logo() => new BitmapImage(new Uri(Png));

    /// <summary>Dải 3 màu Bosch nằm ngang, chia đều, ranh giới sắc nét (mỗi màu 2 điểm dừng trùng nhau).</summary>
    public static LinearGradientBrush Stripe()
    {
        var b = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        for (var i = 0; i < Colors.Length; i++)
        {
            b.GradientStops.Add(new GradientStop(Ui.Rgb(Colors[i]), i / (double)Colors.Length));
            b.GradientStops.Add(new GradientStop(Ui.Rgb(Colors[i]), (i + 1) / (double)Colors.Length));
        }
        b.Freeze();
        return b;
    }

    /// <summary>Icon khay hệ thống từ milo.ico (cỡ nhỏ của Windows), lỗi thì dùng chóp đuôi vẽ tay.</summary>
    public static System.Drawing.Icon TrayIcon()
    {
        try
        {
            using var s = Application.GetResourceStream(new Uri(Ico))!.Stream;
            return new System.Drawing.Icon(s, System.Windows.Forms.SystemInformation.SmallIconSize);
        }
        catch (Exception ex) when (ex is System.IO.IOException or ArgumentException or NullReferenceException)
        {
            return App.TailIcon();
        }
    }
}
