# Minditful · Milo (WPF, Windows)

Ứng dụng desktop hiện thực hoá prototype **"Milo sống"** và tài liệu [Kịch bản hành vi Milo](docs/Kịch%20bản%20hành%20vi%20Milo.md).
Milo là chú cáo ẩn ở góc phải dưới màn hình (chỉ chừa chóp đuôi). Milo chỉ ló ra vào đúng lúc: không chen vào cuộc họp, và tối đa 1 lời nhắc chủ động mỗi 15 phút.

Cả **3 môi trường đều là cùng một app**: Milo sống trên desktop Windows (overlay trong suốt ở góc màn hình, ngay trên taskbar), có biểu tượng ở khay hệ thống, dashboard, thẻ nhắc… Một bộ não duy nhất (Rule Engine → Điều phối → Mood Engine) chạy bên dưới. Ba môi trường chỉ khác **nguồn thời gian, dữ liệu và tín hiệu**:

| Môi trường | Dữ liệu | Milo hiện ở đâu | Dùng để |
| --- | --- | --- | --- |
| **Demo** | Ngày mẫu Thứ Năm 24/9 của prototype: giờ, lịch, email, task và thao tác người dùng theo kịch bản | **Desktop thật** (overlay trong suốt ở góc màn hình) + khay hệ thống + bảng điều khiển kịch bản | Chạy đủ 16 case của prototype trên app thật: tua 60×/120×/300×, nhảy 17 mốc, bật từng case, "Bạn thử làm" để bẻ kịch bản |
| **Sandbox** | Microsoft account cá nhân + Azure DevOps org cá nhân (API thật) | Desktop thật (overlay trong suốt) + khay hệ thống + Bảng điều khiển | Thử tích hợp thật mà không đụng tenant công ty: tạo dữ liệu mẫu, giả lập tín hiệu, ép chạy từng case |
| **Production** | Tenant Bosch: Teams presence, Outlook, Azure Boards | Desktop thật, ẩn khỏi share màn hình | Dùng hằng ngày |

## Chạy nhanh

Yêu cầu: Windows 10 1809+ / Windows 11, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (hoặc Visual Studio 2022 17.8+).

```powershell
dotnet test                                                # 64 test (chạy được cả trên macOS/Linux)
dotnet run --project src/Minditful.App                     # mở màn hình chọn môi trường
dotnet run --project src/Minditful.App -- --env Demo       # vào thẳng Demo (không cần tài khoản)
dotnet run --project src/Minditful.App -- --env Sandbox
dotnet run --project src/Minditful.App -- --env Production
```

Đóng gói 1 file exe: `dotnet publish src/Minditful.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`.

Ở màn hình chọn môi trường có ô "Nhớ lựa chọn". Muốn quay lại màn hình này thì giữ **Shift** khi mở app.
Sandbox/Production chạy nền và có biểu tượng chóp đuôi ở khay hệ thống. Chuột phải vào biểu tượng để mở dashboard, bảng điều khiển, đăng nhập hoặc thoát.

## Cấu hình

Bí mật và giá trị riêng của từng người (ClientId, tenant, organization, PAT, API key Claude) nằm trong file **`.env`** ở gốc repo:

```powershell
copy .env.sample .env    # rồi điền giá trị
```

- `.env` đã nằm trong `.gitignore`, **không commit**. `.env.sample` là bản mẫu được commit, liệt kê đủ các biến.
- App tìm `.env` ở cạnh file exe, rồi đi ngược lên các thư mục cha (khi chạy `dotnet run` từ repo), cuối cùng ở `%LOCALAPPDATA%\Minditful\.env`.
- Biến môi trường thật của máy luôn được ưu tiên hơn `.env`. Dòng để trống giá trị thì dùng mặc định trong appsettings.json.
- Cú pháp `MINDITFUL__Minditful__Sandbox__Graph__ClientId` tương ứng khoá `Minditful:Sandbox:Graph:ClientId` trong appsettings.json.

| Biến | Dùng cho |
| --- | --- |
| `ANTHROPIC_API_KEY` | Claude viết lời thoại. Để trống thì dùng câu mẫu |
| `MINDITFUL_SANDBOX_ADO_PAT` | PAT Azure DevOps của org cá nhân |
| `MINDITFUL_PROD_ADO_PAT` | PAT Azure DevOps của Bosch |
| `MINDITFUL__Minditful__Sandbox__…` / `…Production__…` | ClientId, TenantId, Organization, Project, Team |
| `MINDITFUL__Minditful__Environment` | Mở thẳng Demo / Sandbox / Production |

PAT và API key cũng có thể nhập trong Bảng điều khiển; khi đó chúng được lưu mã hoá DPAPI trên máy.

`src/Minditful.App/appsettings.json` giữ các giá trị mặc định không bí mật. Khung giờ làm (`WorkDay.Start/End`), ngưỡng rời máy, ngưỡng phân mảnh và nhịp làm mới dữ liệu nằm trong mục `WorkDay`.

### Sandbox (tenant `mindiful.onmicrosoft.com`)

Hướng dẫn đầy đủ: **[docs/KET-NOI-SANDBOX.md](docs/KET-NOI-SANDBOX.md)**. Tài liệu gồm app registration, quyền Graph, Teams/Outlook/Azure DevOps, cách kiểm tra bằng tay, checklist 14 bước test và các lỗi thường gặp.

TenantId, ClientId, org `mindiful-sandbox`, project `Milo-Sandbox` và team `Milo-Sandbox Team` đã có sẵn trong appsettings.json. Việc còn lại:

1. Điền `MINDITFUL_SANDBOX_ADO_PAT` vào `.env`. PAT do **thulu@** tạo, hoặc dán vào ô *Lưu PAT* trong Bảng điều khiển.
2. Chạy `--env Sandbox`, bấm *Đăng nhập Microsoft* rồi chọn **thulu@mindiful.onmicrosoft.com**.
3. Làm theo checklist ở mục 7 của hướng dẫn. Bảng điều khiển có sẵn các công cụ:
   - **Reset ngày**: chào sáng lại.
   - **Giờ về = bây giờ + 2'**: test Tan tầm ngay.
   - **Xoá PAT**.
   - **Tạo dữ liệu mẫu**: tự tạo Teams meeting và work item.
   - **Chạy thử 1 case**.

Sandbox dùng **ngưỡng rút gọn** (`BehaviorOverrides`) để test trong 1 buổi: task Active 0 ngày đã tính là kẹt, email chờ 0 ngày, làm liền 20 phút, 2 lời nhắc cách nhau 3 phút, ghé ngang 3–5 phút, chấm chờ giữ 5 phút. Production không có khối này nên dùng đúng ngưỡng của tài liệu.

Mỗi nguồn được đọc lại theo chu kỳ riêng: presence 30 giây, lịch 2 phút, mail 5 phút, Boards 3 phút. Khi mở khoá máy, đăng nhập hoặc bấm *Làm mới*, app đọc lại tất cả ngay.

Tên môi trường nhận cả `Scenario`/`Demo`, `Sandbox`, `Prod`/`Production`. Thứ tự chọn: `--env` > biến `MINDITFUL_ENV` > `Minditful:Environment`. Visual Studio có sẵn 3 launch profile.

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
src/Minditful.App             WPF: Launcher · CompanionWindow (overlay Milo trên desktop, chung cho cả 3 môi trường)
                              DemoSession (đồng hồ + dữ liệu kịch bản) / LiveSession (đồng hồ thật + Graph/Azure Boards)
                              DemoControlWindow (điều khiển kịch bản) · ControlCenterWindow (kết nối, công cụ Sandbox)
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
- **API key:** đặt `ANTHROPIC_API_KEY` trong `.env`, hoặc nhập trong Bảng điều khiển (lưu mã hoá DPAPI).
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
- Góc neo lưu riêng từng môi trường. Bảng điều khiển Sandbox/Production có 4 nút chọn góc; ở cả 3 môi trường đều kéo chóp đuôi được.

## Cá nhân hoá 7 ngày (§14)

Mọi phản hồi được ghi local vào `%LOCALAPPDATA%\Minditful\<môi trường>\outcomes.tsv`, giữ 30 ngày. Các loại phản hồi: hiện, đồng ý, để sau, không cần, bỏ qua, chat, bị cổng ngắt, bị gộp. Mỗi đầu ngày Milo tính lại:

| Luật | Điều kiện | Milo làm |
| --- | --- | --- |
| 1 · trong ngày | Bấm Không cần lần 2 | Thời gian chờ của case đó ×3 tới hết ngày |
| 2 · 7 ngày | Case hiện ≥ 5 lần, ≥ 60% là Không cần hoặc bị bỏ qua | Thời gian chờ ×2 và ngưỡng kích hoạt +15% (vd. Làm liền 120 → 138 phút) |
| 3 · 7 ngày | Case bị Để sau ≥ 60% | Giao case đó ở khoảng trống dài nhất kế tiếp thay vì khoảng trống đầu tiên |

Không case nào bị tắt hẳn, chỉ thưa đi. Bảng điều khiển hiện các điều chỉnh đang áp dụng.
