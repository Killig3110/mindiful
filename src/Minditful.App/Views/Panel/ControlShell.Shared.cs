using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Minditful.App.Controls;
using Minditful.App.Rendering;
using Minditful.App.Services;
using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Integrations;
using static Minditful.App.Rendering.Ui;

namespace Minditful.App.Views.Panel;

/// <summary>Các khối dùng chung cho cả Demo và Sandbox/Production.</summary>
internal abstract partial class ControlShell
{
    /// <summary>Mỗi case: nhóm + 1 câu "Milo sẽ làm gì" bằng lời thường.</summary>
    protected static readonly (string Group, string Hint, (CaseId C, string What)[] Cases)[] CaseCatalog =
    [
        ("Chào hỏi", "Milo xuất hiện đầu ngày, cuối ngày và thỉnh thoảng hỏi thăm.",
        [
            (CaseId.MorningHello, "Leo lên chào + bản tin: mấy cuộc họp, email chưa đọc, task đang dở"),
            (CaseId.CheckIn, "Hỏi thăm khi bạn đang làm tốt, 1 nút \"Cảm ơn Milo\""),
            (CaseId.EodWrapup, "Tổng kết ngày, hỏi hôm nay thấy sao, rủ về"),
            (CaseId.EodNudge, "Nhắc lại sau 30 phút làm thêm"),
        ]),
        ("Giúp việc", "Nhắc đúng lúc về lịch họp, email và task.",
        [
            (CaseId.MeetingSoon, "5 phút trước họp Teams: mở slide, bấm vào họp"),
            (CaseId.CalendarPacked, "Thấy họp liền nhau, đề nghị giữ 10 phút nghỉ trong lịch"),
            (CaseId.EmailWaiting, "Email hỏi thẳng bạn mà chưa trả lời"),
            (CaseId.StuckTask, "Task dở nhiều ngày → khoá giờ tập trung + Không làm phiền"),
            (CaseId.TaskDone, "Nhảy tưng ăn mừng khi 1 task sang Done"),
            (CaseId.FocusDone, "Chúc mừng khi hết khối tập trung"),
        ]),
        ("Chăm sóc", "Khi bạn làm quá sức. Mỗi lần chỉ nhắc 1 chuyện.",
        [
            (CaseId.MeetingOverload, "Họp liền mấy tiếng → rủ thở 4-4-4"),
            (CaseId.NoBreak, "Ngồi liền lâu không nghỉ → thở 1 phút"),
            (CaseId.LowRest, "Cả ngày nghỉ quá ít → nghỉ hẳn 15 phút"),
            (CaseId.LunchMissed, "Quá trưa chưa rời máy → rủ đi ăn"),
            (CaseId.HighFragmentation, "Nhảy việc liên tục → gom việc 30 phút"),
            (CaseId.Overtime, "Quá giờ về → nhắc chốt việc rồi về"),
        ]),
        ("Mới thêm", "Tính năng mở rộng ngoài kịch bản gốc.",
        [
            (CaseId.FocusPlan, "Đề nghị giữ khoảng trống dài nhất trong ngày để tập trung"),
            (CaseId.WeekReport, "Sáng thứ Hai: tóm tắt tuần trước + 1 mẹo"),
            (CaseId.MicroBreak, "Ló lên 5 giây nhắc nghỉ ngắn: uống nước, vươn vai"),
            (CaseId.Dashboard, "Mở 4 quả quanh Milo (nho, cam, anh đào, táo)"),
        ]),
    ];

    /// <summary>Danh sách case chia nhóm, bấm là Milo làm ngay trên desktop.</summary>
    protected StackPanel CaseList(Action<CaseId> run)
    {
        var sp = new StackPanel();
        foreach (var (group, hint, cases) in CaseCatalog)
        {
            var head = Text(group, 14, P.Ink, FontWeights.Bold);
            var h = Text(hint, 11.5, P.Ink2);
            h.Margin = new Thickness(0, 2, 0, 8);
            var wrap = new WrapPanel();
            foreach (var (c, what) in cases)
            {
                var cc = c;
                wrap.Children.Add(ActionTile(Catalog.Def(c).Name, what, () => run(cc), color: Catalog.Def(c).Color));
            }
            sp.Children.Add(new StackPanel { Margin = new Thickness(0, 0, 0, 10), Children = { head, h, wrap } });
        }
        return sp;
    }

    /// <summary>Tủ đồ: 5 lựa chọn, món chưa mở khoá bị mờ. Mỗi món có ảnh Milo đang mặc.</summary>
    protected Border WardrobeCard(AppEnvironment env, Func<int> best, Func<string> progress)
    {
        (string Id, string Name, int Need)[] items = [("auto", "Tự chọn món mới nhất", 0), ("none", "Không mặc", 0),
            .. Wardrobe.Items.Select(i => (i.Id, $"{char.ToUpper(i.Name[0])}{i.Name[1..]}", i.Streak))];
        var wrap = new WrapPanel();
        var boxes = new List<(Border Box, string Id, int Need, TextBlock Lock)>();
        foreach (var (id, name, need) in items)
        {
            var acc = Wardrobe.Find(id)?.Id;
            var img = new Image
            {
                Width = 74, Height = 74, HorizontalAlignment = HorizontalAlignment.Center,
                Source = id == "none" || id == "auto" ? MiloSkin.Get(Pose.Idle, 1) : MiloSkin.Frame(Pose.Idle, MiloRig.Idle, 0, false, 1, acc),
            };
            var title = Text(name, 12, P.Ink, FontWeights.SemiBold);
            title.TextAlignment = TextAlignment.Center;
            var lockText = Text(need > 0 ? $"{need} ngày về đúng giờ" : id == "auto" ? "mặc định" : "", 10.5, P.Muted);
            lockText.TextAlignment = TextAlignment.Center;
            var box = new Border
            {
                Width = 128, Padding = new Thickness(8, 8, 8, 10), Margin = new Thickness(0, 0, 8, 8), CornerRadius = new CornerRadius(14),
                Background = Br(P.Fill), BorderThickness = new Thickness(2), Cursor = System.Windows.Input.Cursors.Hand,
                Child = new StackPanel { Children = { img, title, lockText } },
            };
            System.Windows.Automation.AutomationProperties.SetName(box, name);
            var chosen = id;
            box.MouseLeftButtonUp += (_, _) =>
            {
                if (need > best()) return;
                ((App)Application.Current).Companion?.SetAccessory(chosen);
                Refresh();
            };
            boxes.Add((box, id, need, lockText));
            wrap.Children.Add(box);
        }
        var prog = Text("", 12.5, P.Ink2);
        prog.Margin = new Thickness(0, 4, 0, 0);
        Tick(() =>
        {
            var b = best();
            var cur = UiSettings.LoadAccessory(env);
            foreach (var (box, id, need, lk) in boxes)
            {
                var open = need <= b;
                box.Opacity = open ? 1 : .45;
                box.BorderBrush = Br(id == cur ? P.Accent : "#00000000");
                box.Background = Br(id == cur ? "#FFF1DE" : P.Fill);
                lk.Text = need == 0 ? (id == "auto" ? "mặc định" : "") : open ? "đã mở khoá" : $"cần {need} ngày về đúng giờ";
            }
            prog.Text = progress();
        });
        return Card(new StackPanel { Children = { wrap, prog } }, "Tủ đồ của Milo",
            "Về đúng giờ (quá giờ dưới 15 phút) nhiều ngày liền để mở khoá. Bấm 1 món để Milo mặc ngay.");
    }

    protected Border CornerCard(AppEnvironment env) =>
        Card(Segmented(
            [("↖ Trên trái", nameof(Corner.TopLeft)), ("↗ Trên phải", nameof(Corner.TopRight)), ("↙ Dưới trái", nameof(Corner.BottomLeft)), ("↘ Dưới phải", nameof(Corner.BottomRight))],
            () => UiSettings.LoadCorner(env).ToString(),
            v => ((App)Application.Current).Companion?.SetCorner(Enum.Parse<Corner>(v))),
            "Milo ở góc nào", "Cũng có thể kéo chóp đuôi Milo sang góc khác ngay trên desktop.");

    /// <summary>Trang nâng cao: trạng thái, lý do im lặng, hàng đợi, cách tính mood, nhật ký quyết định.</summary>
    protected FrameworkElement BrainPage()
    {
        var brain = new BrainPanel { Margin = new Thickness(0, 0, 10, 14) };
        Tick(() => brain.Render(Engine));
        return Page("Bộ não Milo",
            "Dành cho người muốn xem bên trong: Milo đang ở trạng thái nào, vì sao đang im lặng, lời nhắc nào đang chờ, điểm mood được tính ra sao và nhật ký từng quyết định.",
            brain);
    }
}
