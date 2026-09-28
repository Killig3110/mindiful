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
dotnet test                                                # 61 test (chạy được cả trên macOS/Linux)
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
                              LimitationsTests: lời thoại/LLM (không lộ tiêu đề), khung clip, cá nhân hoá 7 ngày, log phản hồi
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
| §12 Animation | `ClipAnimation` (keyframes toàn thân) + `MiloRig` (khung theo bộ phận) + `MiloSkin` (dựng khung từ SVG, giảm bão hoà theo mood) |
| §14 Lời thoại, cá nhân hoá | `Lines` (template + ràng buộc), `ClaudeLineWriter` (Claude API), `Personalizer` + `OutcomeStore` |
| §9.1 Góc neo | `MiloLayer.Corner` + kéo chóp đuôi, `CompanionWindow.SetCorner` |

## Lớp 2 · Claude viết lời thoại (§14)

Khi một case vào hàng đợi, Milo gọi Claude ngay lúc đó để viết sẵn câu chính. Nhờ vậy khi thẻ hiện lên thì không phải chờ.

- **Claude nhận gì:** chỉ tên case, số liệu và câu mẫu, ví dụ "vừa họp liền 2h40, còn 12 phút tới cuộc họp sau". Claude **không bao giờ** nhận tiêu đề email, cuộc họp hay task; có test kiểm tra điều này.
- **Claude chỉ viết câu chính.** Nhãn, số liệu và nút bấm vẫn lấy từ template.
- **Ràng buộc:** tiếng Việt, dưới 25 từ, giọng ấm, 1 hành động cụ thể, Milo xưng "Milo"/"mình". Câu trả về sai ràng buộc thì bị loại.
- **Khi không dùng được Claude:** hết giờ (mặc định 2.5 giây), mất mạng, bị từ chối hoặc câu không đạt → dùng template. Mỗi case có 3–5 câu mẫu, xoay vòng để không lặp câu vừa dùng.
- **Chat tự do:** câu gõ không khớp từ khoá thì hỏi Claude (tối đa 2.5 giây); không có Claude thì trả lời bằng câu mặc định như §10.
- **Cấu hình:** mục `Llm` trong appsettings.json.
  - Mặc định `claude-opus-5`, `effort: low`. Muốn nhanh hơn có thể đổi `Model` sang `claude-haiku-4-5`.
  - Bật sẵn *server-side refusal fallback* (`fallbacks: "default"`). Tắt bằng `RefusalFallback: false`.
- **API key:** nhập trong Bảng điều khiển (lưu mã hoá DPAPI) hoặc đặt biến `ANTHROPIC_API_KEY`.
- **Demo:** mặc định dùng câu mẫu để giống prototype từng chữ. Đặt `Llm.UseInDemo: true` để bật Claude trong Demo.

## Clip hoạt ảnh (§12)

Các clip "Cần vẽ" được dựng thành chuỗi khung từ 8 tư thế SVG. Mỗi khung xoay hoặc dịch nhóm `armL/armR`, `legL/legR`, `tail`, `head`, `earL/earR`, `eyes/look` quanh khớp của nó:

| Clip | Khung · fps | Chuyển động |
| --- | --- | --- |
| Leo lên / leo xuống | 11 · 8 | hai tay với so le, co chân |
| Idle / IdleTired | 7 · 6 / 7 · 5 (ping-pong) | đuôi đung đưa; khi mệt thì đầu gục, tai cụp |
| Greet | 7 · 7 | vẫy tay phải |
| Reminder | 9 · 6 | nghiêng đầu, vẫy đuôi |
| Chỉ tay | 7 · 5 | tay trái chỉ về phía thẻ |
| Thở | 8 · 4 | ngẩng lên theo nhịp hít |
| Nhảy tưng mừng | 8 · 8 | hai tay giơ cao |
| Vươn vai | 6 · 5 | vươn vai |
| Chạy ra xe | 9 · 10 | nâng gối xen kẽ, đuôi bay |
| Đào bới | 9 · 8 | hai tay đào |
| Nhìn quanh | 8 · 2 | đảo mắt, quay đầu |
| Ngóc đầu | 3 · 10 | vểnh tai |

Milo chớp mắt 4.5 giây/lần, khi mệt thì nhắm lâu hơn (§9.4). Chuyển động toàn thân (leo, nhảy, tụt) vẫn theo keyframes của prototype. Khung hình được dựng sẵn lúc máy rảnh để lần hiện đầu không bị giật.

## Góc neo (§9.1)

Kéo chóp đuôi rồi thả ở đâu thì Milo neo vào **góc gần nhất** của màn hình đó (hỗ trợ nhiều màn hình). Bấm mà không kéo thì vẫn mở dashboard.

- Góc trái: Milo được lật ngang.
- Góc trên: Milo thò xuống từ mép trên; thẻ và dashboard mọc xuống dưới.
- Góc neo lưu riêng từng môi trường. Bảng điều khiển có 4 nút chọn góc; Demo cũng kéo được trong màn hình giả.

## Cá nhân hoá 7 ngày (§14)

Mọi phản hồi được ghi local vào `%LOCALAPPDATA%\Minditful\<môi trường>\outcomes.tsv`, giữ 30 ngày. Các loại phản hồi: hiện, đồng ý, để sau, không cần, bỏ qua, chat, bị cổng ngắt, bị gộp. Mỗi đầu ngày Milo tính lại:

| Luật | Điều kiện | Milo làm |
| --- | --- | --- |
| 1 · trong ngày | Bấm Không cần lần 2 | Thời gian chờ của case đó ×3 tới hết ngày |
| 2 · 7 ngày | Case hiện ≥ 5 lần, ≥ 60% là Không cần hoặc bị bỏ qua | Thời gian chờ ×2 và ngưỡng kích hoạt +15% (vd. Làm liền 120 → 138 phút) |
| 3 · 7 ngày | Case bị Để sau ≥ 60% | Giao case đó ở khoảng trống dài nhất kế tiếp thay vì khoảng trống đầu tiên |

Không case nào bị tắt hẳn, chỉ thưa đi. Bảng điều khiển hiện các điều chỉnh đang áp dụng.
