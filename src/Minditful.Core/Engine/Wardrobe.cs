namespace Minditful.Core.Engine;

/// <summary>Ô phối đồ. Thứ tự enum cũng là thứ tự vẽ (cổ, tay dưới cùng; mũ trên cùng).</summary>
public enum Slot { Neck, Hand, Eyes, Hair, Hat }

/// <summary>Cách mở khoá: miễn phí, chuỗi ngày về đúng giờ, tổng số lần nghỉ cùng Milo, tổng phút tập trung sâu, hoặc theo mùa.</summary>
public enum UnlockKind { Free, Streak, Breaks, FocusMin, Season }

/// <summary>
/// 1 món đồ của Milo. Một món có thể chiếm nhiều ô (đồng phục Bosch = mũ + thẻ đeo cổ): mặc món khác vào 1 trong các ô đó thì cả bộ được cởi.
/// Chỉ thưởng thói quen tốt (về đúng giờ, nghỉ, tập trung), không bao giờ thưởng cho làm thêm giờ.
/// </summary>
public sealed record Accessory(string Id, string Name, Slot[] Slots, UnlockKind Kind, int Need = 0, string? Season = null)
{
    /// <summary>Số ngày về đúng giờ cần có (0 nếu món không mở bằng chuỗi).</summary>
    public int Streak => Kind == UnlockKind.Streak ? Need : 0;

    /// <summary>Điều kiện mở khoá, viết cho người dùng đọc.</summary>
    public string Condition => Kind switch
    {
        UnlockKind.Free => "có sẵn",
        UnlockKind.Streak => $"về đúng giờ {Need} ngày liền",
        UnlockKind.Breaks => $"nghỉ cùng Milo {Need} lần",
        UnlockKind.FocusMin => $"tập trung sâu tổng {Tm.Dur(Need)}",
        _ => $"mở trong mùa {Wardrobe.SeasonName(Season)}",
    };
}

public static class Wardrobe
{
    /// <summary>Quá giờ dưới ngưỡng này vẫn tính là về đúng giờ.</summary>
    public const double OnTimeGraceMin = 15;

    public const string Auto = "auto", None = "none";

    public static readonly Accessory[] Items =
    [
        // Có sẵn để ai cũng phối được ngay từ ngày đầu
        new("glasses", "kính tròn", [Slot.Eyes], UnlockKind.Free),
        new("coffee", "ly cà phê", [Slot.Hand], UnlockKind.Free),
        // Về đúng giờ nhiều ngày liền (như bản trước)
        new("scarf", "khăn quàng", [Slot.Neck], UnlockKind.Streak, 3),
        new("flower", "kẹp hoa", [Slot.Hair], UnlockKind.Streak, 5),
        new("beret", "mũ nồi", [Slot.Hat], UnlockKind.Streak, 10),
        // Phần thưởng cao nhất: mũ lưỡi trai đỏ + thẻ nhân viên Bosch
        new("bosch", "đồng phục Bosch", [Slot.Hat, Slot.Neck], UnlockKind.Streak, 15),
        // Nghỉ cùng Milo
        new("bubbletea", "ly trà sữa", [Slot.Hand], UnlockKind.Breaks, 10),
        new("sunglasses", "kính râm", [Slot.Eyes], UnlockKind.Breaks, 20),
        // Tập trung sâu
        new("bowtie", "nơ cổ", [Slot.Neck], UnlockKind.FocusMin, 120),
        new("headphones", "tai nghe", [Slot.Hat], UnlockKind.FocusMin, 300),
        // Theo mùa: mở khi tới mùa, giữ luôn sau đó
        new("lixi", "bao lì xì", [Slot.Hand], UnlockKind.Season, Season: "tet"),
        new("lantern", "lồng đèn ông sao", [Slot.Hand], UnlockKind.Season, Season: "trungthu"),
        new("witch", "mũ phù thuỷ", [Slot.Hat], UnlockKind.Season, Season: "halloween"),
        new("santa", "mũ Noel", [Slot.Hat], UnlockKind.Season, Season: "noel"),
    ];

    /// <summary>Tên ô, theo thứ tự hiện trên giao diện.</summary>
    public static readonly (Slot Slot, string Name)[] SlotNames =
        [(Slot.Hat, "Mũ"), (Slot.Hair, "Kẹp tóc"), (Slot.Eyes, "Kính"), (Slot.Neck, "Cổ"), (Slot.Hand, "Tay cầm")];

    // Tết và Trung thu theo âm lịch: ghi sẵn ngày dương của 5 năm tới
    private static readonly (string Id, string Name, DateOnly[] Peaks, int Before, int After)[] Seasons =
    [
        ("tet", "Tết", [new(2026, 2, 17), new(2027, 2, 6), new(2028, 1, 26), new(2029, 2, 13), new(2030, 2, 3)], 14, 10),
        ("trungthu", "Trung thu", [new(2026, 9, 25), new(2027, 9, 15), new(2028, 10, 3), new(2029, 9, 22), new(2030, 9, 12)], 14, 3),
    ];

    public static string SeasonName(string? id) => id switch
    {
        "tet" => "Tết",
        "trungthu" => "Trung thu",
        "halloween" => "Halloween",
        "noel" => "Noel",
        _ => id ?? "",
    };

    /// <summary>Hôm nay có đang trong mùa không (Tết, Trung thu theo bảng ngày; Halloween 20–31/10; Noel 10–26/12).</summary>
    public static bool InSeason(string? season, DateOnly today) => season switch
    {
        "halloween" => today.Month == 10 && today.Day >= 20,
        "noel" => today.Month == 12 && today.Day is >= 10 and <= 26,
        _ => Seasons.FirstOrDefault(s => s.Id == season) is { Peaks: not null } s
            && s.Peaks.Any(p => today >= p.AddDays(-s.Before) && today <= p.AddDays(s.After)),
    };

    /// <summary>Món theo mùa đang mở hôm nay (để lưu lại, giữ luôn sau mùa).</summary>
    public static IEnumerable<Accessory> InSeasonToday(DateOnly today) =>
        Items.Where(i => i.Kind == UnlockKind.Season && InSeason(i.Season, today));

    /// <summary>Món đã mở khoá bằng các con số (không tính mùa).</summary>
    public static bool Earned(Accessory i, int best, int breaks, int focusMin) => i.Kind switch
    {
        UnlockKind.Free => true,
        UnlockKind.Streak => best >= i.Need,
        UnlockKind.Breaks => breaks >= i.Need,
        UnlockKind.FocusMin => focusMin >= i.Need,
        _ => false,
    };

    /// <summary>Mọi món người dùng đang có.</summary>
    public static IReadOnlyList<Accessory> Owned(WardrobeInfo? w, DateOnly today) => w is null ? [] :
        Items.Where(i => Earned(i, w.Best, w.Breaks, w.FocusMin)
            || i.Kind == UnlockKind.Season && (InSeason(i.Season, today) || (w.Kept?.Contains(i.Id) ?? false))).ToList();

    /// <summary>Món mở bằng chuỗi về đúng giờ (giữ cho mã cũ và test).</summary>
    public static IEnumerable<Accessory> Unlocked(int best) => Items.Where(i => i.Kind == UnlockKind.Streak && best >= i.Streak);

    /// <summary>Món mở khoá mới khi các con số đi từ trước lên sau. Nhiều món cùng lúc thì lấy món khó nhất (đứng sau trong danh sách).</summary>
    public static Accessory? NewlyUnlocked(int bestBefore, int bestAfter, int breaksBefore = 0, int breaksAfter = 0, int focusBefore = 0, int focusAfter = 0) =>
        Items.LastOrDefault(i => i.Kind is UnlockKind.Streak or UnlockKind.Breaks or UnlockKind.FocusMin
            && !Earned(i, bestBefore, breaksBefore, focusBefore) && Earned(i, bestAfter, breaksAfter, focusAfter));

    public static Accessory? Find(string? id) => Items.FirstOrDefault(i => i.Id == id);

    /// <summary>1 dòng tiến độ: chuỗi, số lần nghỉ, giờ tập trung, số món đã có và món gần mở nhất.</summary>
    public static string Progress(WardrobeInfo? w, DateOnly today)
    {
        if (w is null) return "Tủ đồ đang tắt.";
        var owned = Owned(w, today);
        // Món chưa có gần mở nhất (tỉ lệ đã đạt cao nhất)
        var next = Items.Where(i => i.Kind is UnlockKind.Streak or UnlockKind.Breaks or UnlockKind.FocusMin && !owned.Contains(i))
            .OrderByDescending(i => (double)(i.Kind switch { UnlockKind.Streak => w.Best, UnlockKind.Breaks => w.Breaks, _ => w.FocusMin }) / i.Need)
            .FirstOrDefault();
        return $"Về đúng giờ {w.Streak} ngày liền (dài nhất {w.Best}) · nghỉ cùng Milo {w.Breaks} lần · tập trung {Tm.Dur(w.FocusMin)} · có {owned.Count}/{Items.Length} món"
            + (next is null ? "" : $" · sắp mở: {next.Name} ({next.Condition})");
    }

    public static bool OnTime(DayRecord r) => r.OvertimeMin < OnTimeGraceMin;

    // ================= phối đồ =================
    /// <summary>
    /// Đọc lựa chọn đã lưu: "auto" (món khó nhất đã có), "none", hoặc danh sách id ngăn bởi dấu phẩy (bản cũ lưu 1 id cũng đọc được).
    /// Bỏ món chưa có, món trùng ô thì giữ món sau.
    /// </summary>
    public static IReadOnlyList<string> Resolve(string? choice, IReadOnlyList<Accessory> owned)
    {
        if (string.IsNullOrWhiteSpace(choice) || choice == Auto)
            return owned.Where(i => i.Kind == UnlockKind.Streak).OrderBy(i => i.Need).LastOrDefault() is { } top ? [top.Id] : [];
        if (choice == None) return [];
        var list = new List<string>();
        foreach (var id in choice.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (owned.Any(o => o.Id == id)) list = Wear(list, id, toggle: false).ToList();
        return list;
    }

    /// <summary>Mặc thêm 1 món: cởi món đang chiếm cùng ô. <paramref name="toggle"/>: bấm lại món đang mặc thì cởi ra.</summary>
    public static IReadOnlyList<string> Wear(IReadOnlyList<string> outfit, string id, bool toggle = true)
    {
        if (Find(id) is not { } item) return outfit;
        if (toggle && outfit.Contains(id)) return outfit.Where(x => x != id).ToList();
        return outfit.Where(x => x != id && Find(x) is { } o && !o.Slots.Intersect(item.Slots).Any()).Append(id).ToList();
    }

    /// <summary>Lưu lựa chọn thành chuỗi (rỗng = "none").</summary>
    public static string Format(IReadOnlyList<string> outfit) => outfit.Count == 0 ? None : string.Join(",", outfit);

    /// <summary>Khoá vẽ cho MiloSkin: các id theo thứ tự vẽ (ô thấp nhất trước), nối bằng "+". null = không mặc gì.</summary>
    public static string? SkinKey(IReadOnlyList<string> outfit) => outfit.Count == 0 ? null :
        string.Join("+", outfit.Select(Find).Where(i => i is not null).OrderBy(i => i!.Slots.Min()).ThenBy(i => i!.Id).Select(i => i!.Id));

    /// <summary>Phối ngẫu nhiên từ món đang có: mỗi ô 50% có món.</summary>
    public static IReadOnlyList<string> Random(IReadOnlyList<Accessory> owned, Random rnd)
    {
        IReadOnlyList<string> outfit = [];
        foreach (var (slot, _) in SlotNames.OrderBy(_ => rnd.Next()))
        {
            var pick = owned.Where(i => i.Slots.Contains(slot)).ToList();
            if (pick.Count > 0 && rnd.NextDouble() < 0.6) outfit = Wear(outfit, pick[rnd.Next(pick.Count)].Id, toggle: false);
        }
        return outfit;
    }
}
