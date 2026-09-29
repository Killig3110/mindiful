using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Minditful.Integrations.Storage;
using Xunit;

namespace Minditful.Core.Tests;

/// <summary>Phối đồ theo ô, mở khoá bằng thói quen tốt / theo mùa, và mở tủ đồ ngay trên Milo (không cần bảng điều khiển).</summary>
public sealed class WardrobeTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("milo-wardrobe-").FullName;
    private static readonly DateOnly Mid = new(2026, 9, 24); // sát Trung thu 2026 (25/9)

    public void Dispose() => Directory.Delete(_dir, true);

    private static IReadOnlyList<Accessory> All => Wardrobe.Items;

    [Fact]
    public void One_item_per_slot_and_a_set_frees_every_slot_it_uses()
    {
        IReadOnlyList<string> o = ["glasses", "scarf"];
        o = Wardrobe.Wear(o, "bowtie");                   // cùng ô cổ → thay khăn
        Assert.Equal(["glasses", "bowtie"], o);
        o = Wardrobe.Wear(o, "bosch");                    // đồng phục chiếm mũ + cổ → cởi nơ
        Assert.Equal(["glasses", "bosch"], o);
        o = Wardrobe.Wear(o, "beret");                    // mũ khác → cởi cả bộ đồng phục
        Assert.Equal(["glasses", "beret"], o);
        o = Wardrobe.Wear(o, "beret");                    // bấm lại = cởi
        Assert.Equal(["glasses"], o);
    }

    [Fact]
    public void Saved_choice_is_read_back_and_filtered_to_owned_items()
    {
        var owned = Wardrobe.Owned(new WardrobeInfo(3, 3), Mid);
        Assert.Equal(["glasses", "scarf"], Wardrobe.Resolve("glasses,beret,scarf", owned)); // chưa có mũ nồi → bỏ
        Assert.Equal(["scarf"], Wardrobe.Resolve(Wardrobe.Auto, owned));                   // tự chọn = món chuỗi khó nhất
        Assert.Empty(Wardrobe.Resolve(Wardrobe.None, owned));
        var all = Wardrobe.Owned(new WardrobeInfo(15, 15), Mid);
        Assert.Equal(["bosch"], Wardrobe.Resolve("bosch", all));                           // bản cũ lưu 1 id vẫn đọc được
        Assert.Equal("scarf+glasses", Wardrobe.SkinKey(["glasses", "scarf"]));             // khoá vẽ theo thứ tự ô, cổ vẽ trước
    }

    [Fact]
    public void Items_unlock_by_good_habits_never_by_overtime()
    {
        var none = Wardrobe.Owned(new WardrobeInfo(0, 0), new DateOnly(2026, 11, 15)).Select(i => i.Id).ToList();
        Assert.Equal(["glasses", "coffee"], none); // ai cũng có sẵn 2 món
        var habits = Wardrobe.Owned(new WardrobeInfo(0, 0, Breaks: 20, FocusMin: 300), new DateOnly(2026, 11, 15)).Select(i => i.Id).ToList();
        Assert.Contains("bubbletea", habits);
        Assert.Contains("sunglasses", habits);
        Assert.Contains("bowtie", habits);
        Assert.Contains("headphones", habits);
        Assert.All(All, i => Assert.NotEqual("", i.Condition));
        Assert.DoesNotContain(All, i => i.Condition.Contains("quá giờ"));
    }

    [Theory]
    [InlineData("trungthu", 2026, 9, 24, true)]
    [InlineData("trungthu", 2026, 10, 10, false)]
    [InlineData("tet", 2027, 2, 1, true)]
    [InlineData("tet", 2027, 3, 1, false)]
    [InlineData("halloween", 2026, 10, 31, true)]
    [InlineData("halloween", 2026, 10, 19, false)]
    [InlineData("noel", 2026, 12, 24, true)]
    [InlineData("noel", 2026, 12, 27, false)]
    public void Seasons_open_around_the_holiday(string season, int y, int m, int d, bool open) =>
        Assert.Equal(open, Wardrobe.InSeason(season, new DateOnly(y, m, d)));

    [Fact]
    public void Random_outfit_uses_owned_items_without_slot_clashes()
    {
        var owned = Wardrobe.Owned(new WardrobeInfo(15, 15, Breaks: 30, FocusMin: 400), Mid);
        var rnd = new Random(7);
        for (var i = 0; i < 200; i++)
        {
            var o = Wardrobe.Random(owned, rnd);
            Assert.All(o, id => Assert.Contains(owned, x => x.Id == id));
            var slots = o.SelectMany(id => Wardrobe.Find(id)!.Slots).ToList();
            Assert.Equal(slots.Count, slots.Distinct().Count());
        }
    }

    [Fact]
    public void Store_adds_breaks_and_focus_once_per_day_and_keeps_seasonal_items()
    {
        var s = new LocalStore(Path.Combine(_dir, "p.db"));
        var d = new DateOnly(2026, 9, 21);
        s.RecordDay(d, true, breaks: 6, focusMin: 60);
        s.RecordDay(d, true, breaks: 7, focusMin: 70);                 // tính lại cùng ngày: thay, không cộng dồn 2 lần
        var r = s.RecordDay(d.AddDays(1), true, breaks: 3, focusMin: 40);
        Assert.Equal("bubbletea", r.Unlocked!.Id);                     // 7 + 3 = 10 lần nghỉ
        var w = s.Wardrobe(Mid);
        Assert.Equal((10, 110), (w.Breaks, w.FocusMin));
        Assert.Contains("lantern", w.Kept!);                           // đang mùa Trung thu → giữ lồng đèn
        Assert.Contains(Wardrobe.Owned(s.Wardrobe(new DateOnly(2026, 11, 20)), new DateOnly(2026, 11, 20)), i => i.Id == "lantern");
        s.WipeAll();
        Assert.Equal(0, s.Wardrobe(new DateOnly(2026, 11, 20)).Breaks);
    }

    private static MiloEngine FreeEngine()
    {
        var e = DemoScenario.CreateEngine();
        e.RunTo(DemoTour.FreeMoment);
        return e;
    }

    private static void Settle(MiloEngine e)
    {
        for (var i = 0; i < 20 && e.S.Ep?.Phase != Phase.Show; i++) e.Advance(1, stopWhenBusy: false);
    }

    [Fact]
    public void Wardrobe_opens_on_milo_without_the_control_panel()
    {
        var e = FreeEngine();
        e.OpenWardrobe();
        Settle(e);
        Assert.True(Present.WardrobeOpen(e));
        Assert.Null(Present.Detail(e));
        e.UserReply("wardrobe");                                       // bấm "Tủ đồ" lần nữa → về 4 quả
        Assert.False(Present.WardrobeOpen(e));
        e.UserReply("wardrobe");
        e.UserReply("close");
        for (var i = 0; i < 20 && e.S.Ep is not null; i++) e.Advance(1, stopWhenBusy: false);
        Assert.Null(e.S.Ep);
    }

    [Fact]
    public void Wardrobe_does_not_interrupt_a_reminder()
    {
        var e = DemoScenario.CreateEngine();
        DemoTour.RunCase(e, CaseId.NoBreak);
        e.Advance(2, false);
        e.OpenWardrobe();
        Assert.Equal(CaseId.NoBreak, e.S.Ep!.C);
    }

    [Fact]
    public void Chat_about_clothes_offers_the_wardrobe_button()
    {
        var e = FreeEngine();
        e.OpenTalk();
        Settle(e);
        Assert.Equal("wardrobe", Talk.Suggest(e, "hôm nay Milo mặc gì đây, đổi đồ đi")[0]);
        e.UserReply("do", "wardrobe");
        Assert.True(e.S.Ep is { C: CaseId.Dashboard, Wardrobe: true });
    }

    [Fact]
    public void A_visiting_milo_can_be_clicked()
    {
        var e = FreeEngine();
        e.CallMilo(true);
        e.Advance(3, false);
        Assert.True(Present.MiloClickable(e));
    }

    [Fact]
    public void Morning_card_names_the_habit_behind_a_non_streak_unlock()
    {
        var demo = DemoScenario.Snapshot();
        var snap = new WorkSnapshot { Calendar = demo.Calendar, Week = demo.Week, Wardrobe = new WardrobeInfo(2, 2, "ly trà sữa", Breaks: 10) };
        var e = new MiloEngine(new EngineConfig(), snap, null, DemoScenario.Day, Tm.T("09:00"));
        e.SetAuto(false);
        e.SetLocked(false);
        for (var i = 0; i < 900 && e.S.Ep is not { C: CaseId.MorningHello, Phase: Phase.Show }; i++) e.Advance(1, stopWhenBusy: false);
        Assert.Contains(Present.Card(e).Blocks, b => b is ParagraphBlock p && p.Text.Contains("nghỉ cùng Milo 10 lần") && p.Text.Contains("ly trà sữa"));
    }
}
