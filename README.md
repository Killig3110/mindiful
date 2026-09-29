# Minditful · Milo (WPF, Windows)

Ứng dụng desktop hiện thực hoá prototype **"Milo sống"** và tài liệu [Kịch bản hành vi Milo](docs/Kịch%20bản%20hành%20vi%20Milo.md).
Milo là chú cáo ẩn ở góc phải dưới màn hình (chỉ chừa chóp đuôi). Milo chỉ ló ra vào đúng lúc: không chen vào cuộc họp, và tối đa 1 lời nhắc chủ động mỗi 15 phút.

Cả **3 môi trường đều là cùng một app**: Milo sống trên desktop Windows (overlay trong suốt ở góc màn hình, ngay trên taskbar), có biểu tượng ở khay hệ thống, dashboard, thẻ nhắc… Một bộ não duy nhất (Rule Engine → Điều phối → Mood Engine) chạy bên dưới. Ba môi trường chỉ khác **nguồn thời gian, dữ liệu và tín hiệu**:

| Môi trường | Dữ liệu | Milo hiện ở đâu | Dùng để |
| --- | --- | --- | --- |
| **Demo** | Ngày mẫu Thứ Năm 24/9 của prototype: giờ, lịch, email, task và thao tác người dùng theo kịch bản | **Desktop thật** (overlay trong suốt ở góc màn hình) + khay hệ thống + bảng điều khiển kịch bản | Chạy đủ 16 case của prototype trên app thật: tua 60×/120×/300×, nhảy 17 mốc, bật từng case, "Bạn thử làm" để bẻ kịch bản |
| **Sandbox** | Tenant thử `mindiful.onmicrosoft.com` (Teams, Outlook) + Azure DevOps `mindiful-sandbox` — API thật | Desktop thật (overlay trong suốt) + khay hệ thống + Bảng điều khiển có công cụ test | Thử tích hợp thật mà không đụng tenant Bosch; ngưỡng hành vi rút gọn để test trong 1 buổi |
| **Production** | Tenant Bosch: Teams presence, Outlook, Azure Boards | Desktop thật, ẩn khỏi share màn hình | Dùng hằng ngày |

## Cài đặt từ đầu

Làm theo thứ tự dưới đây trên **máy Windows 10 (1809+) hoặc Windows 11**. App là WPF nên chỉ **chạy** được trên Windows. macOS/Linux build được và chạy được `dotnet test`, nhưng không mở được app.

### Bước 1. Cài công cụ

| Công cụ | Bắt buộc? | Cài | Kiểm tra |
| --- | --- | --- | --- |
| **Git** | Có | [git-scm.com/download/win](https://git-scm.com/download/win) hoặc `winget install Git.Git` | `git --version` |
| **.NET 8 SDK** (x64) | Có | [dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0) hoặc `winget install Microsoft.DotNet.SDK.8` | `dotnet --list-sdks` có dòng `8.0.x` |
| Visual Studio 2022 (17.8+), workload **.NET desktop development** | Không (tiện để debug) | [visualstudio.microsoft.com](https://visualstudio.microsoft.com/) | Mở được `Minditful.sln` |
| VS Code + extension **C# Dev Kit** | Không (thay cho VS) | [code.visualstudio.com](https://code.visualstudio.com/) | — |
| Microsoft Teams (desktop hoặc web) | Chỉ Sandbox/Prod | — | Cần đang mở thì mới có presence và DND |

Cài xong .NET SDK, **mở lại** cửa sổ PowerShell/Terminal để nhận lệnh `dotnet`.

### Bước 2. Lấy code và build

```powershell
git clone https://github.com/Killig3110/mindiful.git
cd mindiful
dotnet restore          # tải package NuGet (lần đầu ~1–2 phút)
dotnet build            # phải ra: Build succeeded · 0 Warning(s) · 0 Error(s)
dotnet test             # phải ra: Passed! - Failed: 0, Passed: 87
```

Cấu trúc thư mục sau khi clone:

```
mindiful/
├─ .env.sample            ← mẫu biến môi trường (được commit)
├─ Minditful.sln
├─ docs/                  ← KIEN-TRUC.md, KET-NOI-SANDBOX.md, Kịch bản hành vi Milo.md
├─ src/Minditful.Core/    ← bộ não (không phụ thuộc Windows)
├─ src/Minditful.Integrations/  ← Graph, Azure DevOps, Claude, SQLite
├─ src/Minditful.App/     ← app WPF + appsettings.json
└─ tests/Minditful.Core.Tests/
```

### Bước 3. Tạo file `.env`

```powershell
Copy-Item .env.sample .env      # PowerShell  (cmd: copy .env.sample .env)
notepad .env                    # hoặc mở bằng VS Code
```

- `.env` nằm ở **gốc repo**, cạnh `.env.sample`. Git đã bỏ qua file này (`.gitignore`), nên **không bao giờ commit** nó.
- App tự tìm `.env` khi chạy bằng `dotnet run` từ repo.
- Nếu chạy file exe đã đóng gói: đặt `.env` cạnh `Minditful.exe`, hoặc ở `%LOCALAPPDATA%\Minditful\.env`.
- Dòng để trống = dùng giá trị mặc định trong `src/Minditful.App/appsettings.json`. Vì vậy chỉ cần điền những dòng liên quan tới môi trường bạn dùng.

**Cần điền gì cho từng môi trường:**

| Môi trường | Điền trong `.env` | Ghi chú |
| --- | --- | --- |
| **Demo** | *Không cần gì* | Chạy được ngay, không cần mạng |
| **Sandbox** | `MINDITFUL_SANDBOX_ADO_PAT=<PAT>` | TenantId, ClientId, org `mindiful-sandbox`, project, team **đã có sẵn** trong appsettings.json. PAT do **thulu@** tạo ở `https://dev.azure.com/mindiful-sandbox` → *User settings → Personal access tokens*, scope *Work Items (Read & write)* + *Project and Team (Read)*. Chi tiết: [docs/KET-NOI-SANDBOX.md](docs/KET-NOI-SANDBOX.md) |
| **Production** | `MINDITFUL__Minditful__Production__AzureDevOps__Organization=…`<br>`…__Project=…` · `…__Team=…`<br>`MINDITFUL_PROD_ADO_PAT=<PAT>` | TenantId/ClientId Bosch đã có trong appsettings.json; IT cần duyệt quyền (xem mục *Production* bên dưới) |
| Tuỳ chọn · Claude | `ANTHROPIC_API_KEY=sk-ant-…` rồi bật từng tính năng `…Llm__Features__Mood=Hybrid`, `…Features__Meetings=Llm` | Không có key thì app dùng luật + câu mẫu, vẫn chạy đủ |
| Tuỳ chọn · mở thẳng môi trường | `MINDITFUL_ENV=Scenario` / `Sandbox` / `Prod` | Để trống = hiện màn hình chọn |
| Tuỳ chọn · lưu trữ | `…Storage__RetentionPeriod=Week` hoặc `Month` | Mặc định tự xoá dữ liệu cá nhân theo tuần |

Ví dụ `.env` tối thiểu để chạy Sandbox:

```ini
MINDITFUL_SANDBOX_ADO_PAT=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

PAT và API key cũng có thể dán vào ô **Lưu PAT / Lưu key** trong Bảng điều khiển khi app đang chạy. Khi đó chúng được lưu mã hoá DPAPI trên máy, không cần ghi vào `.env`.

### Bước 4. Chạy

**Dòng lệnh** (từ thư mục gốc repo):

```powershell
dotnet run --project src/Minditful.App                       # màn hình chọn môi trường
dotnet run --project src/Minditful.App -- --env Scenario     # Demo
dotnet run --project src/Minditful.App -- --env Sandbox
dotnet run --project src/Minditful.App -- --env Prod
```

**Visual Studio:** mở `Minditful.sln` → chọn startup project **Minditful.App** → chọn launch profile ở thanh công cụ: `Scenario (Demo)`, `Sandbox`, `Prod`, hoặc `Chọn môi trường` → F5.

**File exe để gửi người khác** (không cần cài .NET):

```powershell
dotnet publish src/Minditful.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
# kết quả: src/Minditful.App/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/Minditful.exe (+ appsettings.json)
```

Gửi kèm `appsettings.json`. Người nhận tự tạo `.env` của họ cạnh file exe; **không gửi `.env` của bạn**, vì trong đó có PAT/API key.

**Lần đầu chạy Sandbox/Prod:**
- Trình duyệt mở trang đăng nhập Microsoft. Chọn đúng tài khoản: Sandbox là **thulu@mindiful.onmicrosoft.com**, Prod là tài khoản Bosch.
- Bảng điều khiển phải hiện *Đã đăng nhập …* và Azure Boards *N work item đang làm*.
- Những lần sau app đăng nhập im lặng, không hỏi lại.

Khi chạy, Milo ở **góc phải dưới màn hình** và có biểu tượng chóp đuôi ở **khay hệ thống**; chuột phải vào biểu tượng để mở menu, *Thoát Milo* để tắt. Nếu lần trước đã tick "Nhớ lựa chọn", giữ **Shift** khi mở app để chọn lại môi trường.

### Bước 5. Cập nhật code mới

```powershell
git pull
dotnet build
dotnet test
```

`.env` và dữ liệu local (`%LOCALAPPDATA%\Minditful`) không bị ảnh hưởng. Khi `.env.sample` có dòng mới, chép những dòng đó sang `.env`.

### Lỗi hay gặp khi cài

| Hiện tượng | Nguyên nhân | Cách sửa |
| --- | --- | --- |
| `'dotnet' is not recognized` | Chưa cài SDK hoặc chưa mở lại terminal | Cài .NET 8 SDK, mở terminal mới |
| `NETSDK1045` / không tìm thấy SDK 8 | Chỉ có runtime, hoặc SDK cũ | Cài **SDK** 8.0 (không phải Runtime) |
| `dotnet restore` báo lỗi mạng / 407 trên mạng Bosch | Proxy công ty chặn nuget.org | Đặt `HTTPS_PROXY` hoặc cấu hình proxy trong `%APPDATA%\NuGet\NuGet.Config`, hoặc restore ngoài mạng công ty |
| App không mở trên macOS/Linux | WPF chỉ chạy trên Windows | Dùng máy Windows; trên Mac chỉ chạy được `dotnet build` và `dotnet test` |
| Chạy exe bị Windows SmartScreen chặn | Exe chưa ký số | *More info → Run anyway*; bản phát hành chính thức nên ký số |
| Sandbox báo "Chưa kết nối Azure Boards (chưa có PAT)" | `.env` thiếu PAT hoặc đặt sai chỗ | Kiểm tra `.env` ở gốc repo (hoặc cạnh exe), đúng tên `MINDITFUL_SANDBOX_ADO_PAT` |
| Đăng nhập báo lỗi AADSTS… | Cấu hình app registration | Bảng điều khiển ghi lý do bằng tiếng Việt; xem [KET-NOI-SANDBOX.md](docs/KET-NOI-SANDBOX.md) mục 8 |

## Hướng dẫn test 3 môi trường (dành cho người mới và khi present)

> Tài liệu kiến trúc chi tiết (flow, sequence, dữ liệu, API): **[docs/KIEN-TRUC.md](docs/KIEN-TRUC.md)**.
> Kết nối tenant sandbox: **[docs/KET-NOI-SANDBOX.md](docs/KET-NOI-SANDBOX.md)**. Hành vi gốc của Milo: **[docs/Kịch bản hành vi Milo.md](docs/Kịch%20bản%20hành%20vi%20Milo.md)**.

### 0. Chuẩn bị

Làm xong mục **[Cài đặt từ đầu](#cài-đặt-từ-đầu)** ở trên: `dotnet test` ra `Passed! … 87`, và `.env` đã điền cho môi trường cần test.

Mở thẳng một môi trường: `--env Scenario` (= Demo), `--env Sandbox`, `--env Prod`. Nếu đã tick "Nhớ lựa chọn", **giữ Shift** khi mở app để hiện lại màn hình chọn.

Cả 3 môi trường đều là **cùng một app**:
- Milo sống ở **góc phải dưới màn hình thật**, ngay trên taskbar.
- Khay hệ thống có biểu tượng **chóp đuôi cáo**; chuột phải vào để mở menu.
- Có một **bảng điều khiển** riêng: Demo là "Điều khiển kịch bản", Sandbox/Prod là "Bảng điều khiển".

### 1. Demo (Scenario): không cần tài khoản, không cần mạng

Dùng để present và để kiểm tra đủ 16 case của prototype. Giờ, lịch, email, task và thao tác của người dùng đều theo **ngày mẫu Thứ Năm 24/9**.

**1a. Để ngày mẫu tự chạy** (tick "Người dùng trong kịch bản tự trả lời", tốc độ 120×):

| Giờ kịch bản | Nhìn vào góc màn hình | Nhìn vào bảng điều khiển |
| --- | --- | --- |
| 08:50 → 08:58 | Không có gì (máy "đang khoá") | Trạng thái *Nghỉ làm*, đồng hồ tua nhanh |
| 08:58 | Hai bàn chân bám mép → Milo leo lên → chữ **Hello!** → thẻ **Chào buổi sáng** (4 họp, 7 email, 6 task) → tự bấm *Đã rõ* → Milo leo xuống | Nhật ký: *Giao Chào sáng (P1, miễn ngân sách)* |
| 09:25 | Milo chỉ tay, thẻ **Teams · còn 5 phút · Sprint Planning**, vai *Trình bày* → *Mở slide* → *Tham gia* → Milo thụt nhanh | |
| 09:30–10:30 | Milo **im lặng**, cả chóp đuôi cũng ẩn; góc màn hình hiện chấm **"2 lời nhắc đang chờ"** | Cổng *Đang họp* sáng |
| 10:37 | Milo **nhảy vòng cung** từ mép phải (JumpIn) → thẻ **Task kẹt #4821** → *Khoá 90 phút* → Milo thụt xuống | Cổng *Giờ tập trung* sáng tới 12:07 |
| 12:07 | Bóng thoại "90 phút sâu xong rồi!" → thẻ **Nghỉ quá ít** → *Để sau* | |
| 12:55 | Thẻ **Lịch kín** + lịch mini → *Giữ chỗ* → khối xanh "Nghỉ 10'" trượt vào, dấu **Đã giữ** | |
| 13:10 | Thẻ **Email chờ** (3 email) → *Mở Outlook* | |
| 13:14–13:15 | Milo ló đầu thì thầm "Hôm nay mọng 80" → mở **dashboard trái cây** quanh Milo → *Tuần này →* → đóng | |
| 13:30–16:10 | Im lặng suốt 3 cuộc họp liền | Điểm tụt xuống 57 → **Mệt dần**, Milo nhạt màu |
| 16:12 | JumpIn → **Họp liên tục · 2h40** → *Đồng ý* → **vòng thở 4-4-4** × 3 → "Cảm ơn…" → leo xuống | |
| 17:45 | Milo nhảy tưng "Xong #4821 rồi!", điểm hồi lại | Mức về *Cân bằng* |
| 18:00 → 18:31 | Thẻ **Tan tầm** (tổng kết ngày) → *Thêm 30 phút* → 18:31 **Nhắc lại tan tầm** → *Về thôi* → Milo **chạy ra xe** | Điểm cuối ngày **54**, trạng thái *Nghỉ làm* |

**1b. Tự bấm:** bỏ tick "tự trả lời", rồi bấm các nút trên thẻ của Milo ngay trên desktop. Nên thử:
- *Để sau* 2 lần: lần thứ 3 thẻ không còn nút Để sau.
- *Không cần*: chờ lâu hơn, bấm lần 2 thì giãn ×3.
- Gõ chat "mệt quá": Milo vào vòng thở. Gõ "đang bận": tương đương Để sau.

**1c. Chạy từng case:** mục **Chạy từng case · 16 episode**, bấm một dòng là Milo giao ngay. Checklist:

| Nhóm | Case | Cần thấy |
| --- | --- | --- |
| Xã giao | Chào sáng · Chào hỏi · Tan tầm · Nhắc lại tan tầm | Hello! + bản tin · thẻ 1 nút "Cảm ơn Milo" · tổng kết 3 ô · 1 nút "Về thôi" |
| Hỗ trợ | Sắp họp · Lịch kín · Email chờ · Task kẹt · Task xong · Hết giờ tập trung | Thẻ Teams · lịch mini · 3 email · thanh sprint · bóng thoại 3s · bóng thoại 3s |
| Chăm sóc | Họp liên tục · Quá giờ · Chưa nghỉ trưa · Làm liền · Nghỉ quá ít · Phân mảnh | Nhãn màu riêng từng case, nút Đồng ý / Để sau (Np) / Không cần, ô chat |
| Người dùng | Dashboard | 4 quả quanh Milo: nho (mood) · cam (cuộc họp, múi đã ăn = đã họp) · anh đào (email chờ) · táo cắn dở (sprint); rê chuột lên từng quả xem chi tiết. Bấm **Chi tiết** trên thanh tiêu đề: bảng nhỏ cỡ 1 thẻ ngay trên đầu Milo (dòng thời gian, Office Vibe, cuộc họp sắp tới; trang Tuần có 7 quả nho, thống kê, bạn trả lời Milo thế nào) |
| Mở rộng | Giữ giờ tập trung · Báo cáo tuần · Uống nước · nhìn xa | Thẻ "khoảng trống dài nhất 16:15–17:45" → *Giữ 1h30* · thẻ "Tuần trước của bạn" + 1 mẹo → *Xem chùm nho* mở dashboard tuần · bóng thoại 5 giây, không nút |

**1d. Bẻ kịch bản** (mục *Bạn thử làm*):
- *Đang gõ phím*: lời nhắc bị hoãn; gõ liên tục 5' thì chỉ hiện nhãn gọn "Milo có lời nhắn".
- *Toàn màn hình* hoặc *Không làm phiền*: Milo im lặng và hiện chấm chờ; **bấm chấm chờ** thì thẻ bung ra ngay kể cả đang họp.
- *Nhảy việc 12 lần/giờ*: case Phân mảnh.
- *Giả lập ngày căng*: Milo đổi dáng mệt, có chữ z bay.
- *Teams: đang trình chiếu*: Milo trốn hẳn, kể cả chóp đuôi và chấm chờ; bấm lại thì hiện lại.
- *Tủ đồ: đổi phụ kiện*: xoay vòng tự chọn → khăn quàng → kẹp hoa → mũ nồi → không mặc (ngày mẫu có sẵn chuỗi 10 ngày về đúng giờ).
- Ở thẻ **Tan tầm** (18:00): bấm *Vui / Bình thường / Mệt* để thấy điểm đổi (+3 / 0 / −6), bấm *Giữ 10' nghỉ lúc 15:30* cho chuỗi họp ngày mai.

**1e. Tương tác chung** (cả 3 môi trường):
- Rê chuột lên chóp đuôi **0,6 giây** thì Milo ló đầu; bấm vào đuôi thì mở dashboard; Esc để đóng.
- **Kéo chóp đuôi** sang góc khác thì Milo neo góc đó; góc trái lật ngang, góc trên thò xuống.
- Menu khay có: Mở dashboard · Điều khiển kịch bản · Phát/tạm dừng · Làm lại ngày mẫu · Thoát.

### 2. Sandbox: dữ liệu thật của tenant `mindiful.onmicrosoft.com`

Điều kiện: đã setup theo [docs/KET-NOI-SANDBOX.md](docs/KET-NOI-SANDBOX.md) mục 1, và đã điền `MINDITFUL_SANDBOX_ADO_PAT` trong `.env`.

1. `dotnet run --project src/Minditful.App -- --env Sandbox`. **Lần đầu** trình duyệt mở màn hình đăng nhập: chọn **thulu@mindiful.onmicrosoft.com**, rồi đồng ý quyền nếu được hỏi.
2. Bảng điều khiển phải hiện:
   - *Đã đăng nhập thulu@… · quyền: …*, không có chữ "THIẾU".
   - Azure Boards: *N work item đang làm · Sprint 1 x/y*.
   - Dòng *Ngưỡng rút gọn: task kẹt ≥ 0 ngày · làm liền 20' …*.
3. Chạy **checklist 14 bước** ở mục 7 của KET-NOI-SANDBOX.md. Công cụ trong Bảng điều khiển giúp làm nhanh:

| Muốn test | Dùng |
| --- | --- |
| Chào sáng lại (bước 1) | **Reset ngày (chào sáng lại)** |
| Sắp họp, Lịch kín (bước 2, 4) mà không cần đồng nghiệp | **Tạo dữ liệu mẫu**: Teams meeting sau 7' + chuỗi 3 cuộc sau 52' + 6 work item |
| Tan tầm ngay (bước 12) | **Giờ về = bây giờ + 2'** |
| Xoá PAT (bước 14) | **Xoá PAT**: dashboard phải có dòng "Chưa kết nối Azure Boards" |
| Một case bất kỳ ngay lập tức | **Chạy thử 1 case** (bỏ qua điều kiện và ngân sách) |
| Giả lập gõ phím / rời máy / toàn màn hình / DND | Các nút *Giả lập tín hiệu* (đè lên tín hiệu thật, bấm lần nữa để trả lại) |

4. Kiểm tra hành động thật:
   - *Giữ chỗ*: Outlook của thulu@ có sự kiện "Nghỉ cùng Milo" (tentative, category **Milo**).
   - *Khoá 90 phút*: có sự kiện "Tập trung: #id" (busy), và Teams chuyển **Do not disturb** nếu Teams đang mở.
   - Share màn hình trong Teams: người xem **không thấy** Milo.
5. Kiểm tra dữ liệu local: dashboard → *Tuần này →* → rê chuột lên **chùm nho** để xem **Thống kê tuần**. Bảng điều khiển → *Dữ liệu cá nhân trên máy* ghi chính sách xoá và có nút **Xoá toàn bộ dữ liệu thống kê ngay**.
6. (Có API key) điền `ANTHROPIC_API_KEY`, đặt `…Features__Mood=Hybrid` và `…Features__Meetings=Llm`, mở lại app:
   - Bảng Bộ não có "Nguồn: luật X + Claude ±Y" và câu nhận xét.
   - Dashboard có mức nặng cuộc họp nguồn *Claude*.
   - Nhật ký có dòng *Claude viết sẵn câu cho …*.

Dọn dẹp sau khi test: trong Outlook, xoá các sự kiện category **Milo**; bấm *Xoá toàn bộ dữ liệu thống kê ngay* nếu cần.

### 3. Production: tenant Bosch

1. Đảm bảo IT đã duyệt app registration (TenantId/ClientId đã có trong appsettings.json). Điền org/project Azure DevOps và `MINDITFUL_PROD_ADO_PAT` trong `.env`.
2. `--env Prod`: lần đầu đăng nhập bằng tài khoản Bosch. Prod mặc định chỉ có 3 quyền đọc, nên Bảng điều khiển (mở từ khay) có thể ghi *THIẾU: Calendars.ReadWrite, Mail.Read, Presence.ReadWrite*. **Đó là đúng**, app đang tự hạ cấp:
   - Nút *Giữ chỗ trong lịch* đổi thành **Nhắc tôi lúc đó**, không ghi vào lịch.
   - Không có thẻ Email chờ; bản tin sáng bỏ dòng email.
   - Khoá tập trung chỉ nhắc bạn tự bật DND.
3. Kiểm tra:
   - Tham gia một cuộc gọi Teams: Milo và chóp đuôi ẩn trong vòng ≤ 30s, lời nhắc dồn thành chấm chờ.
   - Share màn hình: người xem không thấy Milo.
   - Khoá máy rồi mở lại: Milo tiếp tục đúng trạng thái.
4. Prod dùng **ngưỡng chuẩn** của tài liệu: làm liền 120', task kẹt ≥ 3 ngày, 2 lời nhắc cách nhau ≥ 15'. Vì vậy trong 1 buổi sẽ thấy ít lời nhắc hơn Sandbox. Đó là thiết kế, không phải lỗi.

### 4. Kịch bản present ~10 phút

| Phút | Làm gì | Nói gì |
| --- | --- | --- |
| 0–1 | Mở app → màn hình chọn 3 môi trường | "Một app, 3 môi trường: Demo chạy kịch bản, Sandbox là tenant thử, Prod là Bosch" |
| 1–3 | Chọn **Demo**, tốc độ **300×**; nhảy mốc **08:58** | Milo sống ở góc màn hình thật; chào sáng + bản tin |
| 3–4 | Mốc **09:25** → **09:30** | Nhắc họp đúng lúc; vào họp thì Milo **im lặng**, lời nhắc dồn thành chấm chờ |
| 4–5 | Mốc **10:37** | JumpIn sau họp, Task kẹt → khoá 90' tập trung (tạo lịch + Teams DND) |
| 5–6 | Mốc **16:12**, bỏ tick tự trả lời, tự bấm *Đồng ý* | Vòng thở 4-4-4 ngay trong thẻ |
| 6–7 | Rê chuột lên đuôi → bấm → rê lên từng quả → *Tuần này →* | Dashboard trái cây: nho mood, cam họp, anh đào email, táo sprint; tuần là chùm nho 7 ngày + thống kê tuần |
| 7–8 | Bảng **Bộ não Milo** (cột phải) | Mọi quyết định có lý do: cổng im lặng, ngân sách 15', hàng đợi ưu tiên; mood tính minh bạch |
| 8–9 | (Tuỳ chọn) chuyển sang **Sandbox** đã đăng nhập sẵn: Tạo dữ liệu mẫu → 2' sau Milo nhắc Teams meeting thật | Cùng bộ não, dữ liệu thật từ Graph + Azure Boards |
| 9–10 | Kéo đuôi sang góc khác; nhắc riêng tư | Chỉ gửi số liệu cho LLM, dữ liệu cá nhân tự xoá mỗi tuần, ẩn khi share màn hình |

Mẹo:
- Để bảng điều khiển ở màn hình thứ hai, màn hình chính chỉ có Milo.
- Trước giờ present chạy thử 1 lượt, rồi bấm *Làm lại ngày mẫu* trong menu khay.
- Present Sandbox thì đăng nhập trước, để lần đầu không phải chờ trình duyệt.

### 5. Sự cố hay gặp khi test

| Hiện tượng | Cách xử lý |
| --- | --- |
| Không thấy Milo | Nhìn đúng góc đã neo (mặc định phải dưới); nếu trạng thái là *Nghỉ làm*/*Im lặng* thì đó là đúng. Demo: kiểm tra đồng hồ kịch bản đang chạy |
| Mở app vào thẳng một môi trường không mong muốn | Giữ Shift khi mở, hoặc xoá `MINDITFUL_ENV` trong `.env` |
| Đăng nhập báo lỗi | Bảng điều khiển ghi lý do bằng tiếng Việt; xem thêm bảng lỗi AADSTS ở KET-NOI-SANDBOX.md mục 8 |
| Azure Boards 0 task | PAT phải do đúng người được giao task tạo; project/team đúng tên |
| App lỗi | Xem `%LOCALAPPDATA%\Minditful\crash.log` |

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

`src/Minditful.App/appsettings.json` giữ các giá trị mặc định không bí mật. Giờ làm (`WorkDay`: mặc định **Flexible** kiểu Bosch, bắt đầu = lần mở máy đầu ngày trong 08:00–10:00, làm 9 tiếng → 8→17, 9→18, 10→19; đặt `Mode=Fixed` để dùng `Start/End` cố định), ngưỡng rời máy, ngưỡng phân mảnh và nhịp làm mới dữ liệu nằm trong mục `WorkDay`.

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

### Đánh giá cảm xúc và đánh giá cuộc họp: 2 hướng, luật hoặc Claude

| Tính năng | `Rules` (mặc định, không cần key) | Claude |
| --- | --- | --- |
| **Mood** (`Features.Mood`) | Mood Engine theo §11 | `Hybrid`: luật làm nền, Claude chỉnh **±10 điểm** và viết 1 câu nhận xét. `Llm`: Claude chấm điểm 0–100, quá 2 chu kỳ không có nhận xét mới thì về điểm luật. Cả hai đều đặt lại 3 chỉ số Office Vibe (Tập trung / Năng lượng / Căng thẳng) |
| **Cuộc họp** (`Features.Meetings`) | Mức nặng 1–5 tính từ độ dài, số người, trình bày, vị trí trong chuỗi, ngoài giờ/đè trưa | `Llm`: Claude đánh giá mức nặng, loại họp và số phút nên nghỉ sau đó |

- **Claude nhận gì:** Mood chỉ nhận số liệu cả ngày (điểm luật, phút họp, làm liền, nghỉ, quá giờ, task, email…). Cuộc họp chỉ nhận độ dài, số người, vai trò, vị trí trong chuỗi. **Không bao giờ gửi tiêu đề**, có test kiểm tra. `IncludeChatInMood: true` thì gửi kèm tối đa 5 câu bạn tự gõ cho Milo để Claude đọc cảm xúc; mặc định tắt.
- **Kết quả hiện ở đâu:**
  - Mức nặng cuộc họp là 5 chấm trong dashboard (tab *Tuần này*); rê chuột vào dòng để xem nhận xét.
  - Câu nhận xét mood nằm ở tab *Hôm nay*.
  - Bảng Bộ não ghi rõ nguồn điểm, ví dụ "luật 72 + Claude −4".
- **Cách gọi Claude:** dùng structured output (JSON theo schema), timeout riêng `InsightTimeoutMs` (mặc định 20 giây) vì chạy nền. Mood hỏi mỗi `MoodIntervalMinutes` phút (mặc định 30).

**Bật tắt** trong `.env` (hoặc mục `Llm` của appsettings.json):

```ini
ANTHROPIC_API_KEY=sk-ant-...
MINDITFUL__Minditful__Llm__Features__Mood=Hybrid      # Rules | Hybrid | Llm
MINDITFUL__Minditful__Llm__Features__Meetings=Llm     # Rules | Llm
MINDITFUL__Minditful__Llm__Features__Lines=true       # câu thoại
MINDITFUL__Minditful__Llm__Features__Chat=true        # chat tự do
MINDITFUL__Minditful__Llm__Enabled=false              # công tắc tổng
```

Chưa có key, hoặc `Enabled=false`, thì mọi tính năng tự chạy bằng luật và câu mẫu. Nhập key trong Bảng điều khiển thì các chế độ Claude bật ngay, không cần mở lại app.

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

## Tính năng chăm sóc mở rộng (mục `Wellbeing`)

7 tính năng thêm ngoài prototype. Sandbox/Production bật theo `Wellbeing` trong `appsettings.json` hoặc `.env`. Demo tắt sẵn để ngày mẫu giữ đúng các mốc của tài liệu; bật thử từng cái ở mục **Mở rộng** của bảng điều khiển kịch bản.

| Tính năng | Khi nào | Milo làm gì | Cấu hình |
| --- | --- | --- | --- |
| Giữ giờ tập trung | 1 lần/ngày, sau lần mở máy đầu 20 phút, trước 15:00, khi còn khoảng trống ≥ 60 phút | Đề nghị giữ khoảng trống dài nhất (tối đa 90 phút) trong lịch (busy). Tới giờ tự bật Không làm phiền, hết giờ bóng thoại "… phút sâu xong rồi!". Không có `Calendars.ReadWrite` thì chỉ nhắc | `FocusPlan`, `FocusPlanMinMinutes` |
| Báo cáo tuần | Sáng thứ Hai, ngay sau Chào sáng | Điểm TB, giờ họp, số lần nghỉ, ngày tốt/mệt nhất của tuần trước + 1 mẹo chọn theo điểm yếu nhất. *Xem chùm nho* mở dashboard tuần | `WeekReport` |
| Hôm nay thấy sao? | Thẻ Tan tầm (và Nhắc lại tan tầm nếu chưa trả lời) | 3 nút Vui / Bình thường / Mệt. Chỉ lưu trên máy; "Mệt" trừ 6 điểm, "Vui" cộng 3; gửi cho Claude khi bật Mood Hybrid/Llm; thống kê tuần có "Bạn tự thấy" | `EveningCheck` |
| Nghỉ giữa chuỗi họp ngày mai | Thẻ Tan tầm, khi mai có ≥ 3 cuộc họp liền | *Giữ 10' nghỉ lúc HH:mm* tạo sự kiện tentative trong lịch ngày mai | `EveningCheck` |
| Uống nước · 20-20-20 | Mỗi 50 phút ngồi máy liên tục (không tính giờ họp), tối đa 6 lần/ngày | Ló lên 5 giây với 1 bóng thoại, không nút, không tính ngân sách lời nhắc. Rời máy ≥ 5 phút thì đếm lại | `MicroBreakEveryMinutes` (0 = tắt), `MicroBreakMaxPerDay` |
| Trốn khi trình chiếu | Teams presence = Presenting | Trốn hẳn, kể cả chóp đuôi và chấm chờ; thẻ đang mở thu lại | `HideWhenPresenting` |
| Tủ đồ của Milo | Về đúng giờ (quá giờ < 15 phút) 3 / 5 / 10 ngày liền | Mở khoá khăn quàng / kẹp hoa / mũ nồi, sáng hôm sau thẻ Chào sáng báo. Chọn món ở Bảng điều khiển (mặc định: món mới nhất) | `Wardrobe` |

Test nhanh trên Sandbox: chọn *Giữ giờ tập trung*, *Báo cáo tuần* hoặc *Uống nước · nhìn xa* ở ô **Chạy thử 1 case**; bấm *Đang trình chiếu* ở mục giả lập tín hiệu; bấm *Giờ về = bây giờ + 2'* để thấy thẻ Tan tầm có 3 nút cảm xúc. Báo cáo tuần cần dữ liệu tuần trước trên máy (chạy app ít nhất 1 ngày tuần trước).

## Góc neo (§9.1)

Kéo chóp đuôi rồi thả ở đâu thì Milo neo vào **góc gần nhất** của màn hình đó (hỗ trợ nhiều màn hình). Bấm mà không kéo thì vẫn mở dashboard.

- Góc trái: Milo được lật ngang.
- Góc trên: Milo thò xuống từ mép trên; thẻ và dashboard mọc xuống dưới.
- Góc neo lưu riêng từng môi trường. Bảng điều khiển Sandbox/Production có 4 nút chọn góc; ở cả 3 môi trường đều kéo chóp đuôi được.

## Dữ liệu cá nhân trên máy (SQLite, tự xoá)

Mỗi môi trường có 1 file `%LOCALAPPDATA%\Minditful\<môi trường>\minditful.db`, gồm các bảng:
- `day_record`: điểm, phút họp, nghỉ, tập trung, câu trả lời "Hôm nay thấy sao?"… mỗi ngày.
- `day_start`: giờ mở máy đầu ngày (giờ làm linh hoạt).
- `streak`: 1 dòng duy nhất cho tủ đồ (chuỗi về đúng giờ hiện tại, dài nhất, món vừa mở). Không tự xoá theo tuần vì chỉ là bộ đếm; nút xoá toàn bộ vẫn xoá.
- `mood_sample`: điểm mood mỗi 15 phút.
- `outcome_event`: log phản hồi từng lời nhắc.
- `meeting_assessment`: mức nặng cuộc họp. Id sự kiện được **băm**, không lưu tiêu đề.

App **không lưu tiêu đề hay nội dung** email, cuộc họp hay task.

| Cấu hình (`Storage`) | Mặc định | Ý nghĩa |
| --- | --- | --- |
| `RetentionPeriod` | `Week` | `Week`: sang thứ Hai tự xoá. `Month`: sang ngày 1 tự xoá |
| `KeepPreviousPeriod` | `true` | Giữ thêm 1 kỳ trước (chùm nho 7 ngày, cá nhân hoá 7 ngày, "so với tuần trước"). `false` = chỉ giữ kỳ hiện tại |
| `MoodSampleMinutes` | `15` | Nhịp lưu mẫu mood và bản ghi ngày đang chạy |

Việc dọn chạy lúc mở app và mỗi lần sang ngày mới, xoá xong thì `VACUUM` để dữ liệu không còn trong file. Nút **Xoá toàn bộ dữ liệu thống kê ngay** nằm trong Bảng điều khiển. Dữ liệu bản cũ (`history.json`, `outcomes.tsv`) được tự chuyển sang SQLite ở lần chạy đầu.

## Cá nhân hoá 7 ngày (§14)

Mọi phản hồi được ghi vào bảng `outcome_event` của SQLite local, tự xoá theo tuần/tháng như mục trên. Các loại phản hồi: hiện, đồng ý, để sau, không cần, bỏ qua, chat, bị cổng ngắt, bị gộp. Mỗi đầu ngày Milo tính lại:

| Luật | Điều kiện | Milo làm |
| --- | --- | --- |
| 1 · trong ngày | Bấm Không cần lần 2 | Thời gian chờ của case đó ×3 tới hết ngày |
| 2 · 7 ngày | Case hiện ≥ 5 lần, ≥ 60% là Không cần hoặc bị bỏ qua | Thời gian chờ ×2 và ngưỡng kích hoạt +15% (vd. Làm liền 120 → 138 phút) |
| 3 · 7 ngày | Case bị Để sau ≥ 60% | Giao case đó ở khoảng trống dài nhất kế tiếp thay vì khoảng trống đầu tiên |

Không case nào bị tắt hẳn, chỉ thưa đi. Bảng điều khiển hiện các điều chỉnh đang áp dụng.
