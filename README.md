# Minditful · Milo (WPF, Windows)

Ứng dụng desktop hiện thực hoá prototype **"Milo sống"** và tài liệu [Kịch bản hành vi Milo](docs/Kịch%20bản%20hành%20vi%20Milo.md).
Milo là chú cáo ẩn ở góc phải dưới màn hình (chỉ chừa chóp đuôi). Milo chỉ ló ra vào đúng lúc: không chen vào cuộc họp, và tối đa 1 lời nhắc chủ động mỗi 15 phút.

Một bộ não duy nhất (Rule Engine → Điều phối → Mood Engine) chạy ở **3 môi trường**:

| Môi trường | Dữ liệu | Milo hiện ở đâu | Dùng để |
| --- | --- | --- | --- |
| **Demo** | Ngày mẫu Thứ Năm 24/9 (y hệt prototype) | Cửa sổ mô phỏng 3 cột: điều khiển · màn hình giả · bộ não | Trình diễn kịch bản, nhảy 17 mốc, tua 60×/120×/300×, "Bạn thử làm", "Xem từng case" (bật ngay 1 trong 16 episode) |
| **Sandbox** | Microsoft account cá nhân + Azure DevOps org cá nhân (API thật) | Desktop thật (overlay trong suốt) + Bảng điều khiển | Thử tích hợp thật mà không đụng tenant công ty: tạo dữ liệu mẫu, giả lập tín hiệu, ép chạy từng case |
| **Production** | Tenant Bosch: Teams presence, Outlook, Azure Boards | Desktop thật, ẩn khỏi share màn hình | Dùng hằng ngày |

## Chạy nhanh

Yêu cầu: Windows 10 1809+ / Windows 11, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (hoặc Visual Studio 2022 17.8+).

```powershell
dotnet test                                                # 44 test của bộ não (chạy được cả trên macOS/Linux)
dotnet run --project src/Minditful.App                     # mở màn hình chọn môi trường
dotnet run --project src/Minditful.App -- --env Demo       # vào thẳng Demo (không cần tài khoản)
dotnet run --project src/Minditful.App -- --env Sandbox
dotnet run --project src/Minditful.App -- --env Production
```

Đóng gói 1 file exe: `dotnet publish src/Minditful.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`.

Ở màn hình chọn môi trường có ô "Nhớ lựa chọn". Muốn quay lại màn hình này thì giữ **Shift** khi mở app.
Sandbox/Production chạy nền và có biểu tượng chóp đuôi ở khay hệ thống. Chuột phải vào biểu tượng để mở dashboard, bảng điều khiển, đăng nhập hoặc thoát.

## Cấu hình

`src/Minditful.App/appsettings.json` là cấu hình mặc định. Giá trị riêng của bạn (ClientId, tenant, organization) để trong **`appsettings.local.json`** cạnh file exe; file này đã nằm trong `.gitignore`. Cũng có thể đặt file ở `%LOCALAPPDATA%\Minditful\appsettings.json` hoặc dùng biến môi trường `MINDITFUL__Minditful__...`.

```jsonc
// src/Minditful.App/appsettings.local.json
{
  "Minditful": {
    "Sandbox": {
      "Graph": { "ClientId": "00000000-0000-0000-0000-000000000000" },
      "AzureDevOps": { "Organization": "ten-org-ca-nhan", "Project": "Minditful-Sandbox" }
    },
    "Production": {
      "Graph": { "ClientId": "…", "TenantId": "…" },
      "AzureDevOps": { "Organization": "…", "Project": "…", "Team": "…" }
    }
  }
}
```

Khung giờ làm (`WorkDay.Start/End`), ngưỡng rời máy, ngưỡng phân mảnh và nhịp làm mới dữ liệu nằm trong mục `WorkDay`.

### Sandbox bằng tài khoản cá nhân

1. **App registration** ở [entra.microsoft.com](https://entra.microsoft.com), đăng nhập bằng Microsoft account cá nhân:
   - *Supported account types*: **Personal Microsoft accounts only**
   - *Platform*: **Mobile and desktop applications**, redirect URI `http://localhost`
   - *API permissions* (Delegated, Microsoft Graph): `User.Read`, `Calendars.ReadWrite`, `Mail.Read`, `Mail.Send`
   - Chép *Application (client) ID* vào `Sandbox.Graph.ClientId`. `TenantId` giữ là `consumers`.
2. **Azure DevOps**: tạo org miễn phí tại [dev.azure.com](https://dev.azure.com), tạo project `Minditful-Sandbox` (process **Agile**; nếu dùng Scrum thì đặt `SeedState` là `In Progress`, Basic thì `Doing`).
   Tạo **PAT** với scope *Work Items (Read & write)* và *Project and Team (Read)*.
3. Chạy `--env Sandbox`. Trong **Bảng điều khiển**, làm lần lượt: *Đăng nhập Microsoft* → dán PAT vào ô rồi bấm *Lưu PAT* (PAT được mã hoá DPAPI trên máy) → *Tạo dữ liệu mẫu*.
   Seeder dựng lại ngày mẫu và lệch theo giờ hiện tại:
   - 1 cuộc họp sau 7 phút, nên Sắp họp sẽ bật sau khoảng 2 phút
   - chuỗi 3 cuộc họp liền (Lịch kín → Họp liên tục)
   - 6 work item, trong đó 2 cái đã dở từ 3 ngày trở lên (Task kẹt)
   - 3 email có dấu "?" (Email chờ)
4. Dùng *Giả lập tín hiệu* để đè lên tín hiệu thật: gõ phím, rời máy, toàn màn hình, Không làm phiền. Dùng *Chạy thử 1 case* để ép một episode hiện ngay. Bấm nút trên thẻ của Milo sẽ gọi API thật, ví dụ Giữ chỗ tạo sự kiện "Nghỉ cùng Milo" trong Outlook cá nhân.

Microsoft account cá nhân **không có Teams presence**. Vì vậy trong Sandbox, cổng *Đang họp* được suy ra từ lịch, còn *Không làm phiền* được giả lập. Đây đúng là cột "Nếu thiếu" ở mục 14 của tài liệu.

### Production (tenant Bosch)

Cần IT tạo app registration trong tenant Bosch:

- *Single tenant*, *Allow public client flows* = Yes
- Redirect URI (Mobile and desktop):
  - `http://localhost`
  - `ms-appx-web://microsoft.aad.brokerplugin/{client-id}`, để đăng nhập một chạm bằng tài khoản Windows (WAM, `UseBroker: true`)
- Delegated permissions: `User.Read`, `Calendars.ReadWrite`, `Mail.Read`, `Presence.ReadWrite`. Có thể cần **admin consent**.
- Azure DevOps: dùng PAT (mặc định, theo spec), hoặc `"Auth": "Entra"` để dùng chung đăng nhập Microsoft.

Nếu tenant chỉ cấp một phần quyền, Milo tự hạ cấp đúng như mục 14:

| Thiếu quyền | Milo xử lý |
| --- | --- |
| `Calendars.ReadWrite` | Chỉ nhắc, không ghi vào lịch |
| `Mail.Read` | Tắt Email chờ |
| `Presence.ReadWrite` | Vẫn tạo sự kiện, nhắc bạn tự bật DND |
| Presence đọc lỗi | Cổng họp suy ra từ lịch |

Mất mạng hoặc token hết hạn thì Milo không bật popup, chỉ hiện một dòng nhỏ trong dashboard.

## Cấu trúc

```
src/Minditful.Core            Bộ não, không phụ thuộc UI/Windows — test được trên mọi OS
  Engine/                     Catalog (16 episode, clip, mức mood) · MiloEngine: tín hiệu, Rule Engine, Điều phối 8 bước,
                              episode Vào→Ở lại→Ra, phản hồi & chat, Mood Engine, ghé ngang
  Scenario/DemoScenario.cs    Ngày mẫu 24/9: lịch, email, task, kịch bản người dùng, tự trả lời, 17 mốc
  Presentation/               Nội dung thẻ/dashboard/chú thích + keyframes hoạt ảnh (chép từ CSS prototype)
src/Minditful.Integrations    MSAL, Graph (calendarView, messages, presence, events), Azure Boards (WIQL, iteration),
                              LiveWorkDataProvider, LiveActionSink, SandboxSeeder, lịch sử quả nho local
src/Minditful.App             WPF: Launcher · SimulatorWindow (Demo) · CompanionWindow (overlay) · ControlCenterWindow
                              MiloLayer (Milo, chóp đuôi, thì thầm, thẻ, dashboard, chấm chờ, hiệu ứng) · BrainPanel
                              WindowsActivityMonitor (khoá máy, idle, gõ phím, toàn màn hình, chuyển app) · LiveSession
tests/Minditful.Core.Tests    Ngày mẫu khớp mục 13 (08:58 chào sáng … 18:31 về thôi, 54 điểm), im lặng suốt họp, render mọi khung;
                              AllCasesTests: 16 episode tự bật đúng luật + mọi nút của mọi thẻ + chat, chấm chờ, chen ngang, cổng ngắt
```

Đối chiếu tài liệu → code:

| Tài liệu | Code |
| --- | --- |
| §2 Tín hiệu | `WindowsActivityMonitor` (idle < 3s = gõ, idle ≥ 5' = rời máy, SHQueryUserNotificationState + cửa sổ phủ màn hình = toàn màn hình, WinEvent foreground = chuyển việc), `PresenceWatcher`, `LiveWorkDataProvider` |
| §3 Trạng thái hiện diện | `MiloEngine.Presence()`; content protection bằng `SetWindowDisplayAffinity` |
| §4 Điều phối, cổng im lặng, ngân sách | `MiloEngine.Arbitrate()`, `HardGate()`, `BudgetOk()` |
| §5–9 Episode | `MiloEngine.Rules.cs`, `MiloEngine.Episodes.cs`, `Present.Card()` |
| §10 Phản hồi & chat | `Reply()`, `CareReply()`, `Chat()` (nhận diện ý định local) |
| §11 Mood Engine, lưu cuối ngày | `ComputeMood()`, `DayHistoryStore` (`%LOCALAPPDATA%\Minditful\<môi trường>\history.json`) |
| §12 Animation | `ClipAnimation` (keyframes) + `MiloSkin` (8 tư thế SVG, giảm bão hoà theo mood) |

## Giới hạn hiện tại

- Chưa có **Lớp 2 (LLM)** viết lại câu thoại. Câu thoại dùng template, chat nhận diện ý định bằng từ khoá như §10.
- Các clip "Cần vẽ" ở §12 đang dùng tư thế SVG có sẵn cộng transform, giống prototype.
- Chưa có kéo chóp đuôi sang góc khác (§9.1). Milo luôn neo ở góc phải dưới màn hình chính.
- Cá nhân hoá theo 7 ngày (§14, luật 2–3) chưa bật. Luật trong ngày (từ chối 2 lần thì giãn ×3) đã có.
