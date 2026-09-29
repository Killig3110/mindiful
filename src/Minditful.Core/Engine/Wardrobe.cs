namespace Minditful.Core.Engine;

/// <summary>Phụ kiện của Milo, mở khoá theo chuỗi ngày về đúng giờ (quá giờ dưới 15 phút).</summary>
public sealed record Accessory(string Id, string Name, int Streak);

public static class Wardrobe
{
    /// <summary>Quá giờ dưới ngưỡng này vẫn tính là về đúng giờ.</summary>
    public const double OnTimeGraceMin = 15;

    public static readonly Accessory[] Items =
    [
        new("scarf", "khăn quàng", 3),
        new("flower", "kẹp hoa", 5),
        new("beret", "mũ nồi", 10),
        // Phần thưởng cao nhất: mũ lưỡi trai đỏ + thẻ nhân viên Bosch
        new("bosch", "đồng phục Bosch", 15),
    ];

    public static IEnumerable<Accessory> Unlocked(int best) => Items.Where(i => best >= i.Streak);

    /// <summary>Món mới mở khoá khi chuỗi dài nhất đi từ <paramref name="before"/> lên <paramref name="after"/>.</summary>
    public static Accessory? NewlyUnlocked(int before, int after) => Items.LastOrDefault(i => before < i.Streak && after >= i.Streak);

    public static Accessory? Find(string? id) => Items.FirstOrDefault(i => i.Id == id);

    public static bool OnTime(DayRecord r) => r.OvertimeMin < OnTimeGraceMin;
}
