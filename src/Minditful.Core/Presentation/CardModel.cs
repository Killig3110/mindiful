using Minditful.Core.Engine;

namespace Minditful.Core.Presentation;

public enum ButtonStyle { Amber, Dark, Teams, Ghost }
public enum PillIcon { None, Bell, Clock }
public enum LeadKind { Square, Avatar, IdTag }
public enum CardVariant { Card, Breathe, Say, Chip, None }

/// <summary>Khối nội dung của thẻ. Text có thể chứa **đậm**.</summary>
public abstract record CardBlock;
public sealed record TopBlock(string Pill, string Bg, string Fg, PillIcon Icon = PillIcon.None, string? Meta = null, bool Stamp = false) : CardBlock;
public sealed record EyebrowBlock(string Text) : CardBlock;
public sealed record TitleBlock(string Text, double Size = 17) : CardBlock;
public sealed record ParagraphBlock(string Text, bool Small = false, string? Color = null) : CardBlock;
public sealed record LineBlock(LeadKind Lead, string LeadText, string LeadColor, string Text, string? Source = null, bool SourceAlert = false,
    string? PillText = null, string? PillBg = null, string? PillFg = null) : CardBlock;
public sealed record CardButton(string Act, string Label, ButtonStyle Style, bool Enabled = true, string? Val = null);
public sealed record ButtonsBlock(IReadOnlyList<CardButton> Buttons) : CardBlock;
public sealed record PeopleBlock(IReadOnlyList<Person> People, string Role, string RoleBg, string RoleFg) : CardBlock;
public sealed record ScheduleRow(string Text, bool IsSlot);
public sealed record ScheduleBlock(string From, string To, IReadOnlyList<ScheduleRow> Rows) : CardBlock;
public sealed record ProgressBlock(string Left, string Right, double Fraction) : CardBlock;
public sealed record TilesBlock(IReadOnlyList<(string Big, string Small)> Tiles) : CardBlock;
public sealed record StatusDotBlock(string Text) : CardBlock;
/// <summary>Khung chat. <paramref name="Focus"/>: đặt con trỏ vào ô gõ ngay (khung Trò chuyện). <paramref name="Hint"/>: chữ mờ trong ô gõ.</summary>
public sealed record ChatBlock(IReadOnlyList<ChatLine> Lines, bool Focus = false, string? Hint = null) : CardBlock;

public sealed record CardModel(CardVariant Variant, IReadOnlyList<CardBlock> Blocks, double Width = 310, string? SayText = null, bool Low = false)
{
    public static readonly CardModel Empty = new(CardVariant.None, []);
}

/// <summary>Dòng lịch trong dashboard. <see cref="Load"/> = mức nặng 1–5 của cuộc họp (đánh giá bằng luật hoặc Claude).</summary>
public sealed record DashRow(string Time, string Name, string Tag, string TagBg, string TagFg, int? Load = null, string? Note = null);

public sealed record DashboardModel(
    DashPage Page, int Score, string Phrase, string YesterdayLine, IReadOnlyList<DayScore> Days,
    IReadOnlyList<(string Big, string Small)> Tiles, DashRow? Next,
    (int F, int E, int S) Vibe, SprintInfo? Sprint, IReadOnlyList<DashRow> Events, string? StatusNote, string? Insight = null, string? WeekSummary = null);

public sealed record Caption(string Tag, string Text, string Ref);

public enum FruitKind { Grape, Bunch, Orange, Cherries, Apple }

/// <summary>
/// Một quả trong "vườn trái cây" quanh Milo (dashboard mới). Hình quả đổi theo số liệu:
/// nho = mood · chùm nho = 7 ngày · cam = cuộc họp (múi đã ăn = đã họp) · anh đào = email chờ · táo cắn dở = sprint.
/// </summary>
public sealed record FruitItem(
    string Key, FruitKind Kind, string Big, string Small, string Badge, string BadgeColor, string TipTitle, IReadOnlyList<string> TipLines,
    int Score = 0, int Total = 0, int Done = 0, int Count = 0, double Progress = 0, IReadOnlyList<DayScore>? Days = null, bool Available = true);

public sealed record FruitDashboard(DashPage Page, string Title, IReadOnlyList<FruitItem> Items);

/// <summary>Đoạn trên thanh thời gian của bảng chi tiết. From/To: 0..1 trong khung giờ làm. Kind: meet · heavy · focus · break.</summary>
public sealed record TimelineSeg(double From, double To, string Kind, string Label);

/// <summary>Bảng chi tiết hôm nay (nhỏ, ngay cạnh Milo): điểm, dòng thời gian, Office Vibe, cuộc họp sắp tới.</summary>
public sealed record DetailToday(
    int Score, string Band, string Phrase, int? Delta, string? Insight, string StartLabel, string EndLabel,
    IReadOnlyList<TimelineSeg> Segments, double? Now, (int F, int E, int S) Vibe, IReadOnlyList<DashRow> Upcoming,
    IReadOnlyList<(string Big, string Small)> Chips, string? Note);

/// <summary>Bảng chi tiết tuần: 7 quả nho, thống kê, bạn trả lời Milo thế nào, bạn tự thấy thế nào.</summary>
public sealed record DetailWeek(
    int Avg, int? Diff, IReadOnlyList<DayScore> Days, IReadOnlyList<(string Big, string Small)> Stats,
    IReadOnlyList<(string Label, int Count, string Color)> Replies, string? Feel, string? Tip);

public sealed record DetailDashboard(DashPage Page, DetailToday? Today, DetailWeek? Week);
