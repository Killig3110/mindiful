using Minditful.Core.Engine;
using Minditful.Integrations.Graph;
using Minditful.Integrations.Storage;

namespace Minditful.Integrations.Live;

/// <summary>Thực hiện <see cref="MiloAction"/> trên Teams/Outlook thật; thiếu quyền thì hạ cấp như cột "Nếu thiếu" của mục 14.</summary>
public sealed class LiveActionSink(
    ConnectionOptions conn, MicrosoftAuth auth, GraphClient? graph, LocalStore history,
    Func<DateOnly> day, Action<string, LogKind> log, Action<string> open)
{
    /// <summary>Milo đang giữ DND cho khối tập trung (để presence watcher không hiểu nhầm là người dùng tự bật).</summary>
    public bool OwnsDnd { get; private set; }

    private DateTime At(double t, int dayOffset = 0) => day().AddDays(dayOffset).ToDateTime(TimeOnly.MinValue).AddSeconds(t);

    /// <summary>Tủ đồ: vừa mở khoá phụ kiện mới (host báo lên overlay).</summary>
    public event Action<Accessory>? Unlocked;
    public bool WardrobeEnabled { get; set; } = true;
    private bool GraphReady => graph is not null && auth.IsConfigured;
    private bool UseGraphPresence => GraphReady && conn.PresenceMode.Equals("Graph", StringComparison.OrdinalIgnoreCase);

    /// <summary>Cập nhật chuỗi về đúng giờ cho 1 ngày (lúc "Về thôi" hoặc lúc qua ngày mà chưa bấm).</summary>
    public void RecordStreak(DayRecord r)
    {
        var (streak, _, item) = history.RecordDay(r.Date, Wardrobe.OnTime(r), r.AcceptedBreaks, (int)Math.Round(r.FocusMin));
        log(Wardrobe.OnTime(r) ? $"Chuỗi về đúng giờ: {streak} ngày" : "Quá giờ ≥ 15 phút → chuỗi về đúng giờ bắt đầu lại", LogKind.Action);
        if (item is not null)
        {
            log($"Mở khoá đồ mới cho Milo: {item.Name} (nhờ {item.Condition})", LogKind.Action);
            Unlocked?.Invoke(item);
        }
    }

    public async Task HandleAsync(MiloAction action)
    {
        try
        {
            switch (action)
            {
                case MiloAction.JoinMeeting { Event: var ev }:
                    if ((ev.JoinUrl ?? ev.WebLink) is { } url) open(url);
                    else log("Sự kiện không có joinUrl → mở Teams bằng tay giúp Milo nhé", LogKind.Action);
                    break;

                case MiloAction.OpenAttachment { Event: var ev }:
                    if (!GraphReady) break;
                    var target = await graph!.DownloadFirstAttachmentAsync(ev.Id) ?? ev.WebLink;
                    if (target is not null) open(target);
                    break;

                case MiloAction.HoldBreak { Start: var s, End: var e, Subject: var subject, DayOffset: var off }:
                    if (GraphReady && auth.Has("Calendars.ReadWrite"))
                    {
                        await graph!.CreateEventAsync(subject, At(s, off), At(e, off), "tentative");
                        log($"Đã tạo \"{subject}\" {(off == 1 ? "ngày mai " : "")}{Tm.Hm(s)}–{Tm.Hm(e)} trong lịch Outlook (tentative)", LogKind.Action);
                    }
                    else log("Thiếu Calendars.ReadWrite → Milo chỉ nhắc, không ghi vào lịch", LogKind.Action);
                    break;

                case MiloAction.HoldFocus { Start: var s, End: var e }:
                    if (GraphReady && auth.Has("Calendars.ReadWrite"))
                    {
                        await graph!.CreateEventAsync("Tập trung · Milo giữ chỗ", At(s), At(e), "busy");
                        log($"Đã giữ \"Tập trung\" {Tm.Hm(s)}–{Tm.Hm(e)} trong lịch Outlook (busy)", LogKind.Action);
                    }
                    else log("Thiếu Calendars.ReadWrite → không ghi vào lịch, tới giờ Milo vẫn bật tập trung", LogKind.Action);
                    break;

                case MiloAction.StartFocus { TaskId: var id, Start: var s, End: var e, CalendarHeld: var held }:
                    if (!held && GraphReady && auth.Has("Calendars.ReadWrite"))
                    {
                        await graph!.CreateEventAsync("Tập trung: #" + id, At(s), At(e), "busy");
                        log($"Đã chặn lịch \"Tập trung: #{id}\" tới {Tm.Hm(e)}", LogKind.Action);
                    }
                    if (UseGraphPresence && auth.Has("Presence.ReadWrite"))
                    {
                        await graph!.SetDoNotDisturbAsync(TimeSpan.FromSeconds(e - s));
                        OwnsDnd = true;
                        log($"Teams presence → Không làm phiền tới {Tm.Hm(e)}", LogKind.Action);
                    }
                    else
                    {
                        OwnsDnd = true;
                        log(UseGraphPresence
                            ? "Thiếu Presence.ReadWrite → bạn tự bật Không làm phiền trên Teams giúp Milo nhé"
                            : "Presence giả lập (tài khoản cá nhân không có Teams presence) → coi như đã bật Không làm phiền", LogKind.Action);
                    }
                    break;

                case MiloAction.EndFocus:
                    if (OwnsDnd && UseGraphPresence && auth.Has("Presence.ReadWrite"))
                    {
                        await graph!.ClearPreferredPresenceAsync();
                        log("Teams presence về lại tự động", LogKind.Action);
                    }
                    OwnsDnd = false;
                    break;

                case MiloAction.OpenMail { Mail: var m }:
                    if (m.WebLink is { } link) open(link);
                    break;

                case MiloAction.DayClosed { Record: var r }:
                    history.SaveDay(r);
                    log($"Đã lưu quả nho {r.Date:dd/MM}: {r.Score} điểm", LogKind.Action);
                    if (WardrobeEnabled) RecordStreak(r);
                    break;
            }
        }
        catch (Exception ex)
        {
            log($"Lỗi khi thực hiện {action.GetType().Name}: {LiveWorkDataProvider.Describe(ex)}", LogKind.Error);
        }
    }
}
