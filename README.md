# Minditful · Milo (WPF, Windows)

<img src="src/Minditful.App/Assets/Brand/logo.png" width="96" alt="Logo Minditful: Milo đội mũ Bosch, đáy 3 dải màu đỏ, xanh dương, xanh lá">


Ứng dụng desktop hiện thực hoá prototype **"Milo sống"** và tài liệu [Kịch bản hành vi Milo](docs/Kịch%20bản%20hành%20vi%20Milo.md).
Milo là chú cáo ẩn ở góc phải dưới màn hình (chỉ chừa chóp đuôi). Milo chỉ ló ra vào đúng lúc: không chen vào cuộc họp, và tối đa 1 lời nhắc chủ động mỗi 15 phút.

Cả **3 môi trường đều là cùng một app**: Milo sống trên desktop Windows (overlay trong suốt ở góc màn hình, ngay trên taskbar), có biểu tượng ở khay hệ thống, dashboard, thẻ nhắc… Một bộ não duy nhất (Rule Engine → Điều phối → Mood Engine) chạy bên dưới. Ba môi trường chỉ khác **nguồn thời gian, dữ liệu và tín hiệu**:

| Môi trường | Dữ liệu | Milo hiện ở đâu | Dùng để |
| --- | --- | --- | --- |
| **Demo** | Ngày mẫu Thứ Năm 24/9 của prototype: giờ, lịch, email, task và thao tác người dùng theo kịch bản | **Desktop thật** (overlay trong suốt ở góc màn hình) + khay hệ thống + bảng điều khiển kịch bản | Chạy đủ 16 case của prototype (+ 3 case mở rộng) trên app thật: tua 60×/120×/300×, nhảy 17 mốc, bật từng case, "Giả vờ bạn đang…" để bẻ kịch bản |
| **Sandbox** | Tenant thử `mindiful.onmicrosoft.com` (Teams, Outlook) + Azure DevOps `mindiful-sandbox` — API thật | Desktop thật (overlay trong suốt) + khay hệ thống + Bảng điều khiển có công cụ test | Thử tích hợp thật mà không đụng tenant Bosch; ngưỡng hành vi rút gọn để test trong 1 buổi |
| **Production** | Tenant Bosch: Teams presence, Outlook, Azure Boards | Desktop thật, ẩn khỏi share màn hình | Dùng hằng ngày |

**Điểm mood lấy từ đâu?** Mô hình Job Demands–Resources, mỗi khoản dựa trên nghiên cứu công bố từ 2021 tới nay, có bộ test chứng minh công thức đúng chiều nghiên cứu với 20.000 bộ số liệu ngẫu nhiên, và trang *Kiểm chứng điểm* (WHO-5) để đo với người thật: **[docs/CO-SO-KHOA-HOC.md](docs/CO-SO-KHOA-HOC.md)**.

**Đi present?** Kịch bản từng bước: **[Demo](docs/KICH-BAN-DEMO.md)** (đủ 19 tình huống, ~15 phút) · **[Sandbox chạy như Production](docs/KICH-BAN-SANDBOX.md)** (đổi dữ liệu thật trên Teams/Outlook/Azure Boards, Milo phản ứng).

**Mới dùng app?** Đọc **[Hướng dẫn sử dụng](docs/HUONG-DAN-SU-DUNG.md)**: Milo trên màn hình, bảng điều khiển từng trang, từng tính năng, cách dùng ở 3 môi trường, vì sao Milo không hiện. README này dành cho cài đặt, cấu hình và test.

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
| Tuỳ chọn · tính năng chăm sóc | `…Wellbeing__MicroBreakEveryMinutes=0` để tắt nhắc uống nước, `…Wellbeing__FocusPlan=false`… | Mặc định bật hết. Xem bảng đầy đủ ở [Tham chiếu biến `.env`](#tham-chiếu-biến-env) |

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

Làm xong mục **[Cài đặt từ đầu](#cài-đặt-từ-đầu)** ở trên: `dotnet test` ra `Passed! … 160`, và `.env` đã điền cho môi trường cần test.

Mở thẳng một môi trường: `--env Scenario` (= Demo), `--env Sandbox`, `--env Prod`. Nếu đã tick "Nhớ lựa chọn", **giữ Shift** khi mở app để hiện lại màn hình chọn.

Cả 3 môi trường đều là **cùng một app**:
- Milo sống ở **góc phải dưới màn hình thật**, ngay trên taskbar.
- Khay hệ thống có biểu tượng **chóp đuôi cáo**; chuột phải vào để mở menu.
- Có một **bảng điều khiển** (tông kem như thẻ của Milo): thanh bên trái chọn trang, dải trên cùng luôn ghi "Milo đang làm gì" bằng lời thường. Demo có các trang *Bắt đầu · Ngày mẫu · Thử tình huống · Milo của bạn · Bộ não Milo*; Sandbox/Prod có *Tổng quan · Kết nối · (Sandbox: Thử tình huống) · Milo của bạn · Bộ não Milo*.
- Hướng dẫn dùng app cho người mới, từng tính năng: **[docs/HUONG-DAN-SU-DUNG.md](docs/HUONG-DAN-SU-DUNG.md)**.

### 1. Demo (Scenario): không cần tài khoản, không cần mạng

Dùng để present và để kiểm tra đủ 16 case của prototype. Giờ, lịch, email, task và thao tác của người dùng đều theo **ngày mẫu Thứ Năm 24/9**.

**Cách nhanh nhất để xem đủ mọi case:** trang **Kịch bản trình diễn** → bật *Tự chạy qua các bước*. 24 bước (12 bước theo ngày mẫu, rồi 9 case còn lại gồm trò chuyện, trình chiếu, mood realtime, đồng phục Bosch), có gợi ý câu nói từng bước. Chi tiết: [docs/KICH-BAN-DEMO.md](docs/KICH-BAN-DEMO.md).

**Mood realtime:** trang *Bắt đầu* (và *Kịch bản trình diễn*) có thẻ **Mood realtime**:
- Kéo *Căng thẳng giả lập* 0–60, hoặc bấm *Nghỉ cùng Milo (+3)* / *Xong 1 task (+2)*: điểm tính lại ngay.
- Bật *Gọi Milo ra đứng ở góc*: Milo đứng ngoài, dáng và màu đổi theo điểm tức thì (kiệt sức thì có chữ z).

**1a. Để ngày mẫu tự chạy** (trang *Bắt đầu*: bật công tắc "Người dùng mẫu tự bấm nút", tốc độ *Vừa 120×*):

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

**1b. Tự bấm:** tắt công tắc "Người dùng mẫu tự bấm nút", rồi bấm các nút trên thẻ của Milo ngay trên desktop. Nên thử:
- *Để sau* 2 lần: lần thứ 3 thẻ không còn nút Để sau.
- *Không cần*: chờ lâu hơn, bấm lần 2 thì giãn ×3.
- Gõ chat "mệt quá": Milo vào vòng thở. Gõ "đang bận": tương đương Để sau.

**1c. Chạy từng case:** trang **Thử tình huống** → *Cho Milo làm ngay*, bấm 1 thẻ là Milo giao ngay (4 nhóm: Chào hỏi, Giúp việc, Chăm sóc, Mới thêm). Checklist:

| Nhóm | Case | Cần thấy |
| --- | --- | --- |
| Xã giao | Chào sáng · Chào hỏi · Tan tầm · Nhắc lại tan tầm | Hello! + bản tin · thẻ 1 nút "Cảm ơn Milo" · tổng kết 3 ô · 1 nút "Về thôi" |
| Hỗ trợ | Sắp họp · Lịch kín · Email chờ · Task kẹt · Task xong · Hết giờ tập trung | Thẻ Teams · lịch mini · 3 email · thanh sprint · bóng thoại 3s · bóng thoại 3s |
| Chăm sóc | Họp liên tục · Quá giờ · Chưa nghỉ trưa · Làm liền · Nghỉ quá ít · Phân mảnh | Nhãn màu riêng từng case, nút Đồng ý / Để sau (Np) / Không cần, ô chat |
| Người dùng | Dashboard | 4 quả quanh Milo: nho (mood) · cam (cuộc họp, múi đã ăn = đã họp) · anh đào (email chờ) · táo cắn dở (sprint); rê chuột lên từng quả xem chi tiết. Bấm **Chi tiết** trên thanh tiêu đề: bảng nhỏ cỡ 1 thẻ ngay trên đầu Milo (dòng thời gian, Office Vibe, cuộc họp sắp tới; trang Tuần có 7 quả nho, thống kê, bạn trả lời Milo thế nào) |
| Mở rộng | Giữ giờ tập trung · Báo cáo tuần · Nghỉ ngắn | Thẻ "khoảng trống dài nhất 16:15–17:45" → *Giữ 1h30* · thẻ "Tuần trước của bạn" + 1 mẹo → *Xem chùm nho* mở dashboard tuần · bóng thoại 5 giây, không nút |

**1d. Bẻ kịch bản** (trang **Thử tình huống** → *Giả vờ bạn đang…*, bật/tắt công tắc):
- *Đang gõ phím*: lời nhắc bị hoãn; gõ liên tục 5' thì chỉ hiện nhãn gọn "Milo có lời nhắn".
- *Toàn màn hình* hoặc *Không làm phiền*: Milo im lặng và hiện chấm chờ; **bấm chấm chờ** thì thẻ bung ra ngay kể cả đang họp.
- *Nhảy việc 12 lần/giờ*: case Phân mảnh.
- *Giả lập ngày căng*: Milo đổi dáng mệt, có chữ z bay.
- *Teams: đang trình chiếu*: Milo trốn hẳn, kể cả chóp đuôi và chấm chờ; bấm lại thì hiện lại.
- Trang **Milo của bạn** → *Tủ đồ*: bấm khăn quàng / kẹp hoa / mũ nồi / đồng phục Bosch để Milo mặc ngay (ngày mẫu có sẵn chuỗi 15 ngày về đúng giờ).
- Ở thẻ **Tan tầm** (18:00): bấm *Vui / Bình thường / Mệt* để thấy điểm đổi (+3 / 0 / −6), bấm *Giữ 10' nghỉ lúc 15:30* cho chuỗi họp ngày mai.

**1e. Tương tác chung** (cả 3 môi trường):
- Rê chuột lên chóp đuôi **0,6 giây** thì Milo ló đầu; bấm vào đuôi thì mở dashboard; Esc để đóng.
- **Kéo chóp đuôi** sang góc khác thì Milo neo góc đó; góc trái lật ngang, góc trên thò xuống.
- Menu khay có: Mở dashboard · Mở bảng điều khiển · Phát/tạm dừng · Làm lại ngày mẫu · Thoát.

### 2. Sandbox: dữ liệu thật của tenant `mindiful.onmicrosoft.com`

Điều kiện: đã setup theo [docs/KET-NOI-SANDBOX.md](docs/KET-NOI-SANDBOX.md) mục 1, và đã điền `MINDITFUL_SANDBOX_ADO_PAT` trong `.env`.

1. `dotnet run --project src/Minditful.App -- --env Sandbox`. **Lần đầu** trình duyệt mở màn hình đăng nhập: chọn **thulu@mindiful.onmicrosoft.com**, rồi đồng ý quyền nếu được hỏi.
2. Trang **Tổng quan** của bảng điều khiển phải có 2 chấm xanh:
   - *Microsoft 365*: "Đã đăng nhập thulu@… · quyền: …", không có chữ "THIẾU".
   - *Azure Boards*: "N work item đang làm · Sprint 1 x/y".
   - Trang **Thử tình huống** → *Chuẩn bị* ghi "Ngưỡng đang dùng: task kẹt ≥ 0 ngày · làm liền 20' …".
3. Chạy **checklist** ở mục 7 của KET-NOI-SANDBOX.md. Trang **Thử tình huống** có sẵn công cụ:

| Muốn test | Dùng |
| --- | --- |
| Chào sáng lại (bước 1) | **Reset ngày (chào sáng lại)** |
| Sắp họp, Lịch kín (bước 2, 4) mà không cần đồng nghiệp | **Tạo dữ liệu mẫu**: Teams meeting sau 7' + chuỗi 3 cuộc sau 52' + 6 work item |
| Tan tầm ngay (bước 12) | **Giờ về = bây giờ + 2'** |
| Xoá PAT (bước 14) | **Xoá PAT**: dashboard phải có dòng "Chưa kết nối Azure Boards" |
| Một case bất kỳ ngay lập tức | *Cho Milo làm ngay* → bấm thẻ case (bỏ qua điều kiện và ngân sách) |
| Giả lập gõ phím / rời máy / toàn màn hình / DND / trình chiếu | *Giả vờ bạn đang…* (công tắc đè lên tín hiệu thật, tắt để trả lại) |

4. Kiểm tra hành động thật:
   - *Giữ chỗ*: Outlook của thulu@ có sự kiện "Nghỉ cùng Milo" (tentative, category **Milo**).
   - *Khoá 90 phút*: có sự kiện "Tập trung: #id" (busy), và Teams chuyển **Do not disturb** nếu Teams đang mở.
   - Share màn hình trong Teams: người xem **không thấy** Milo.
5. Kiểm tra dữ liệu local: dashboard → *Tuần này →* → rê chuột lên **chùm nho** để xem **Thống kê tuần**. Trang **Milo của bạn** → *Riêng tư & dữ liệu* ghi chính sách xoá và có nút **Xoá toàn bộ dữ liệu thống kê ngay**.
6. (Có API key) điền `ANTHROPIC_API_KEY`, đặt `…Features__Mood=Hybrid` và `…Features__Meetings=Llm`, mở lại app:
   - Trang *Bộ não Milo* có "Nguồn: luật X + Claude ±Y" và câu nhận xét; trang *Kết nối* → Claude chấm xanh.
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
4. Khởi động cùng Windows: menu khay → **Khởi động cùng Windows** (hoặc bảng điều khiển → *Milo của bạn*). Tắt mặc định. Bật thì Milo ghi `"<đường dẫn Minditful.exe>" --env Prod` vào `HKCU\...\CurrentVersion\Run` (không cần admin). Đăng xuất rồi đăng nhập lại để thử. Sandbox cũng có công tắc này; mỗi lần chỉ 1 môi trường tự chạy.
5. Prod dùng **ngưỡng chuẩn** của tài liệu: làm liền 120', task kẹt ≥ 3 ngày, 2 lời nhắc cách nhau ≥ 15'. Vì vậy trong 1 buổi sẽ thấy ít lời nhắc hơn Sandbox. Đó là thiết kế, không phải lỗi.

### 4. Kịch bản present ~10 phút

| Phút | Làm gì | Nói gì |
| --- | --- | --- |
| 0–1 | Mở app → màn hình chọn 3 môi trường | "Một app, 3 môi trường: Demo chạy kịch bản, Sandbox là tenant thử, Prod là Bosch" |
| 1–3 | Chọn **Demo**, tốc độ *Nhanh 300×*; trang *Ngày mẫu* → bấm mốc **08:58** | Milo sống ở góc màn hình thật; chào sáng + bản tin |
| 3–4 | Mốc **09:25** → **09:30** | Nhắc họp đúng lúc; vào họp thì Milo **im lặng**, lời nhắc dồn thành chấm chờ |
| 4–5 | Mốc **10:37** | JumpIn sau họp, Task kẹt → khoá 90' tập trung (tạo lịch + Teams DND) |
| 5–6 | Mốc **16:12**, tắt "Người dùng mẫu tự bấm nút", tự bấm *Đồng ý* | Vòng thở 4-4-4 ngay trong thẻ |
| 6–7 | Rê chuột lên đuôi → bấm → rê lên từng quả → *Tuần này →* | Dashboard trái cây: nho mood, cam họp, anh đào email, táo sprint; tuần là chùm nho 7 ngày + thống kê tuần |
| 7–8 | Trang **Bộ não Milo** trên bảng điều khiển | Mọi quyết định có lý do: cổng im lặng, ngân sách 15', hàng đợi ưu tiên; mood tính minh bạch |
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

Danh sách đầy đủ từng biến, giá trị hợp lệ và khi nào nên đổi: xem mục [Tham chiếu biến `.env`](#tham-chiếu-biến-env) ngay bên dưới.

PAT và API key cũng có thể nhập trong Bảng điều khiển; khi đó chúng được lưu mã hoá DPAPI trên máy.

### Tham chiếu biến `.env`

**Cách viết:**
- Mỗi dòng `TÊN=giá trị`, không cần dấu nháy, không có khoảng trắng thừa. Dòng bắt đầu bằng `#` là chú thích.
- **Để trống sau dấu `=`** nghĩa là dùng mặc định trong `src/Minditful.App/appsettings.json` (cột *Mặc định* dưới đây).
- `true` / `false` viết thường. Giờ viết dạng `HH:mm` (vd. `08:30`). Số phút là số nguyên.
- Tên dài `MINDITFUL__Minditful__A__B` chính là khoá `Minditful:A:B` trong appsettings.json (mỗi `__` là một cấp).
- **Sửa xong phải thoát hẳn app** (chuột phải icon ở khay → *Thoát*) rồi mở lại. App chỉ đọc `.env` lúc khởi động.
- Biến môi trường đặt thật trong Windows luôn thắng giá trị trong `.env`.

Trong các bảng, `…` là viết tắt của `MINDITFUL__Minditful__`.

**Chung**

| Biến | Giá trị | Mặc định | Khi nào đổi |
| --- | --- | --- | --- |
| `MINDITFUL_ENV` | `Scenario` (= `Demo`) · `Sandbox` · `Prod` (= `Production`) | trống = hiện màn hình chọn | Muốn app mở thẳng 1 môi trường, không hỏi. `--env` trên dòng lệnh thắng biến này |

**Giờ làm (`WorkDay`)**

| Biến | Giá trị | Mặc định | Khi nào đổi |
| --- | --- | --- | --- |
| `…WorkDay__Mode` | `Flexible` · `Fixed` | `Flexible` | `Flexible` kiểu Bosch: giờ vào = lần mở máy đầu ngày. `Fixed` nếu bạn làm giờ cố định |
| `…WorkDay__FlexEarliestStart` | `HH:mm` | `08:00` | Mở máy sớm hơn giờ này vẫn tính là vào lúc này |
| `…WorkDay__FlexLatestStart` | `HH:mm` | `10:00` | Mở máy muộn hơn giờ này vẫn tính là vào lúc này |
| `…WorkDay__FlexHours` | số giờ | `9` | 8 tiếng làm + 1 tiếng nghỉ trưa. Giờ về = giờ vào + số này |
| `…WorkDay__Start` / `…End` | `HH:mm` | `09:00` / `18:00` | Chỉ dùng khi `Mode=Fixed` |
| `…WorkDay__AwayAfterMinutes` | phút | `5` | Rời máy (idle) bao lâu thì tính là 1 lần nghỉ |

**Dữ liệu cá nhân trên máy (`Storage`)**

| Biến | Giá trị | Mặc định | Khi nào đổi |
| --- | --- | --- | --- |
| `…Storage__RetentionPeriod` | `Week` · `Month` | `Week` | `Week`: sang thứ Hai tự xoá. `Month`: sang ngày 1 tự xoá |
| `…Storage__KeepPreviousPeriod` | `true` · `false` | `true` | `false` = chỉ giữ tuần/tháng hiện tại (chặt nhất). Khi đó chùm nho 7 ngày và "so với tuần trước" đầu tuần sẽ trống |
| `…Storage__MoodSampleMinutes` | phút | `15` | Nhịp lưu mẫu mood |

**Tính năng chăm sóc mở rộng (`Wellbeing`, chỉ Sandbox/Production)**

| Biến | Giá trị | Mặc định | Khi nào đổi |
| --- | --- | --- | --- |
| `…Wellbeing__FocusPlan` | `true` · `false` | `true` | Tắt nếu không muốn Milo đề nghị giữ giờ tập trung |
| `…Wellbeing__FocusPlanMinMinutes` | phút | `60` | Khoảng trống ngắn nhất để đề nghị. Hạ xuống `30` khi test cho nhanh |
| `…Wellbeing__WeekReport` | `true` · `false` | `true` | Báo cáo tuần sáng thứ Hai |
| `…Wellbeing__MicroBreakEveryMinutes` | phút · `0` = tắt | `50` | Nhắc nghỉ ngắn (uống nước, vươn vai). Để `0` nếu thấy phiền |
| `…Wellbeing__MicroBreakMaxPerDay` | số lần | `6` | Trần số lần nhắc uống nước mỗi ngày |
| `…Wellbeing__EveningCheck` | `true` · `false` | `true` | Thẻ tan tầm hỏi "Hôm nay thấy sao?" và gợi ý nghỉ giữa chuỗi họp ngày mai |
| `…Wellbeing__HideWhenPresenting` | `true` · `false` | `true` | Milo trốn hẳn khi Teams báo đang trình chiếu |
| `…Wellbeing__Wardrobe` | `true` · `false` | `true` | Tủ đồ (phụ kiện khi về đúng giờ nhiều ngày liền) |

**Claude (`Llm`, tuỳ chọn)**

| Biến | Giá trị | Mặc định | Khi nào đổi |
| --- | --- | --- | --- |
| `ANTHROPIC_API_KEY` | `sk-ant-…` | trống = không dùng Claude | Có key thì dán vào. Hoặc dán ở ô *Lưu key* trong Bảng điều khiển |
| `…Llm__Enabled` | `true` · `false` | `true` | Công tắc tổng. `false` = chỉ luật + câu mẫu dù có key |
| `…Llm__Model` | tên model | `claude-opus-5` | Thường không cần đổi |
| `…Llm__UseInDemo` | `true` · `false` | `false` | Bật để Demo cũng gọi Claude |
| `…Llm__Features__Lines` | `true` · `false` | `true` | Claude viết câu thoại trên thẻ |
| `…Llm__Features__Chat` | `true` · `false` | `true` | Claude trả lời chat tự do |
| `…Llm__Features__Mood` | `Rules` · `Hybrid` · `Llm` | `Rules` | `Hybrid` = luật + Claude chỉnh ±10 điểm (khuyên dùng khi có key). `Llm` = Claude chấm hẳn |
| `…Llm__Features__Meetings` | `Rules` · `Llm` | `Rules` | `Llm` = Claude đánh giá mức nặng từng cuộc họp |
| `…Llm__Features__MoodIntervalMinutes` | phút | `30` | Bao lâu hỏi Claude về mood 1 lần |
| `…Llm__Features__IncludeChatInMood` | `true` · `false` | `false` | Gửi kèm tối đa 5 câu bạn tự gõ cho Milo để Claude đọc cảm xúc |

Claude chỉ nhận tên case và số liệu, không bao giờ nhận tiêu đề/nội dung email, cuộc họp hay task.

**Sandbox và Production**

| Biến | Giá trị | Mặc định | Khi nào đổi |
| --- | --- | --- | --- |
| `MINDITFUL_SANDBOX_ADO_PAT` | PAT | trống | **Bắt buộc** để Sandbox đọc Azure Boards. Scope *Work Items (Read & write)* + *Project and Team (Read)* |
| `…Sandbox__TestMode` | `true` · `false` | `true` | Chế độ Sandbox lúc mở app lần đầu. `true` = **chế độ test**: có trang *Thử tình huống* để ép Milo làm như Demo, ngưỡng rút ngắn, bảng điều khiển tự mở. `false` = **chạy như Production**. Đổi được ngay trong bảng điều khiển / menu khay, lựa chọn đó được nhớ và thắng biến này |
| `…Sandbox__Graph__ClientId` / `…TenantId` | GUID | có sẵn trong appsettings.json | Chỉ khi tạo lại app registration |
| `…Sandbox__AzureDevOps__Organization` | tên org | `mindiful-sandbox` | Chỉ khi đổi org |
| `…Production__AzureDevOps__Organization` / `…Project` / `…Team` | tên | trống | **Bắt buộc** cho Production: org/project/team Azure DevOps của Bosch |
| `MINDITFUL_PROD_ADO_PAT` | PAT | trống | **Bắt buộc** cho Production. Scope *Work Items (Read)* + *Project and Team (Read)* |

**Công thức hay dùng** (chép vào `.env`, các dòng khác để nguyên):

```ini
# Chỉ chạy Sandbox, chế độ test (ép Milo làm như Demo; nhắc uống nước sau 5', giữ giờ tập trung từ 30' trống)
MINDITFUL_ENV=Sandbox
MINDITFUL_SANDBOX_ADO_PAT=<PAT>
MINDITFUL__Minditful__Sandbox__TestMode=true

# Xem Sandbox chạy y như Production (không công cụ test, ngưỡng chuẩn)
MINDITFUL__Minditful__Sandbox__TestMode=false

# Có API key Claude: bật đủ Lớp 2
ANTHROPIC_API_KEY=sk-ant-...
MINDITFUL__Minditful__Llm__Features__Mood=Hybrid
MINDITFUL__Minditful__Llm__Features__Meetings=Llm

# Làm giờ cố định 08:30–17:30 thay cho giờ linh hoạt
MINDITFUL__Minditful__WorkDay__Mode=Fixed
MINDITFUL__Minditful__WorkDay__Start=08:30
MINDITFUL__Minditful__WorkDay__End=17:30

# Thấy phiền: tắt nhắc uống nước và báo cáo tuần
MINDITFUL__Minditful__Wellbeing__MicroBreakEveryMinutes=0
MINDITFUL__Minditful__Wellbeing__WeekReport=false

# Riêng tư chặt nhất: chỉ giữ dữ liệu tuần hiện tại
MINDITFUL__Minditful__Storage__KeepPreviousPeriod=false
```

Kiểm tra app đã nhận cấu hình: mở **bảng điều khiển**. Trang *Milo của bạn* → *Milo chăm sóc bạn thế nào* ghi từng tính năng Bật/Tắt; trang *Kết nối* → *Claude* ghi "Đang dùng claude-opus-5" hay "Chưa có API key"; dải trên cùng và trang *Tổng quan* ghi giờ làm hôm nay.

`src/Minditful.App/appsettings.json` giữ các giá trị mặc định không bí mật. Giờ làm (`WorkDay`: mặc định **Flexible** kiểu Bosch, bắt đầu = lần mở máy đầu ngày trong 08:00–10:00, làm 9 tiếng → 8→17, 9→18, 10→19; đặt `Mode=Fixed` để dùng `Start/End` cố định), ngưỡng rời máy, ngưỡng phân mảnh và nhịp làm mới dữ liệu nằm trong mục `WorkDay`.

### Sandbox (tenant `mindiful.onmicrosoft.com`)

Hướng dẫn đầy đủ: **[docs/KET-NOI-SANDBOX.md](docs/KET-NOI-SANDBOX.md)**. Tài liệu gồm app registration, quyền Graph, Teams/Outlook/Azure DevOps, cách kiểm tra bằng tay, checklist 23 bước test và các lỗi thường gặp.

TenantId, ClientId, org `mindiful-sandbox`, project `Milo-Sandbox` và team `Milo-Sandbox Team` đã có sẵn trong appsettings.json. Việc còn lại:

1. Điền `MINDITFUL_SANDBOX_ADO_PAT` vào `.env`. PAT do **thulu@** tạo, hoặc dán vào ô *Lưu PAT* trong Bảng điều khiển.
2. Chạy `--env Sandbox`, bấm *Đăng nhập Microsoft* rồi chọn **thulu@mindiful.onmicrosoft.com**.
3. Làm theo checklist ở mục 7 của hướng dẫn. Trang **Thử tình huống** của bảng điều khiển có sẵn:
   - **Reset ngày (chào sáng lại)**.
   - **Giờ về = bây giờ + 2 phút**: test Tan tầm ngay.
   - **Tạo dữ liệu mẫu**: tự tạo Teams meeting và work item.
   - *Cho Milo làm ngay*: chạy thử 1 case.
   - *Giả vờ bạn đang…*: đè tín hiệu thật.
   Trang **Kết nối** có **Lưu PAT / Xoá PAT**.

**Sandbox có 2 chế độ**, là giao thoa giữa Demo và Production. Đổi ở bảng điều khiển → *Tổng quan* → *Chế độ Sandbox*, hoặc menu khay → *Chế độ test*. Đổi lúc đang chạy, không cần mở lại app; lựa chọn được nhớ trên máy.

| | Chế độ test (như Demo) | Chạy như Production |
| --- | --- | --- |
| Trang *Thử tình huống* (ép Milo làm từng tình huống, giả vờ gõ phím / rời máy / trình chiếu…) | Có | Ẩn |
| Ngưỡng (`BehaviorOverrides`) | **Rút gọn**: task Active 0 ngày đã tính là kẹt, email chờ 0 ngày, làm liền 20', 2 lời nhắc cách nhau 3', ghé ngang 3–5', chấm chờ 5', uống nước mỗi 5', giữ giờ tập trung từ 30' trống | Chuẩn như Production: làm liền 120', 15' giữa 2 lời nhắc, task kẹt ≥ 3 ngày… |
| Tín hiệu giả lập | Dùng được | Bị bỏ, dùng tín hiệu thật của Windows/Teams |
| Bảng điều khiển khi mở app | Tự mở | Không tự mở (mở từ khay) |

Dùng *Chạy như Production* để xem Milo trên Production trông và cư xử thế nào ngay trên tenant thử. Production thật không có chế độ test.

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
  Engine/                     Catalog (16 episode + 3 case mở rộng, clip, mức mood) · MiloEngine: tín hiệu, Rule Engine, Điều phối 8 bước,
                              episode Vào→Ở lại→Ra, phản hồi & chat, Mood Engine, ghé ngang · Wardrobe (tủ đồ)
  Scenario/DemoScenario.cs    Ngày mẫu 24/9: lịch, email, task, kịch bản người dùng, tự trả lời, 17 mốc
  Presentation/               Nội dung thẻ/dashboard/chú thích + keyframes hoạt ảnh (chép từ CSS prototype)
src/Minditful.Integrations    MSAL, Graph (calendarView, messages, presence, events), Azure Boards (WIQL, iteration),
                              LiveWorkDataProvider, LiveActionSink, SandboxSeeder, lịch sử quả nho local
src/Minditful.App             WPF: Launcher · CompanionWindow (overlay Milo trên desktop, chung cho cả 3 môi trường)
                              DemoSession (đồng hồ + dữ liệu kịch bản) / LiveSession (đồng hồ thật + Graph/Azure Boards)
                              DemoControlWindow (điều khiển kịch bản) · ControlCenterWindow (kết nối, công cụ Sandbox)
                              MiloLayer (Milo, chóp đuôi, thì thầm, thẻ, dashboard, chấm chờ, hiệu ứng) · BrainPanel
                              FruitDashboardView (4 quả) · DetailDashboardView (bảng chi tiết nhỏ) · MiloSkin (+ phụ kiện tủ đồ)
                              WindowsActivityMonitor (khoá máy, idle, gõ phím, toàn màn hình, chuyển app) · LiveSession
tests/Minditful.Core.Tests    Ngày mẫu khớp mục 13 (08:58 chào sáng … 18:31 về thôi, 54 điểm), im lặng suốt họp, render mọi khung;
                              AllCasesTests: 16 episode tự bật đúng luật + mọi nút của mọi thẻ + chat, chấm chờ, chen ngang, cổng ngắt
                              LimitationsTests: lời thoại/LLM (không lộ tiêu đề), khung clip, cá nhân hoá 7 ngày, log phản hồi
                              ExtendedFeaturesTests: 7 tính năng mở rộng + bảng chi tiết · DotEnvTests: .env, biến README/.env.sample khớp appsettings
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

## Chọn AI để test (miễn phí)

App gọi AI qua 2 đường:
- **Claude:** SDK Anthropic, cần `ANTHROPIC_API_KEY`.
- **Mọi dịch vụ tương thích OpenAI:** đặt `…Llm__Provider=OpenAI`, `…Llm__BaseUrl`, `…Llm__Model`, và key trong `LLM_API_KEY` (hoặc ô *Lưu key* ở trang Kết nối).

Code không cần sửa gì khi đổi dịch vụ.

| Dịch vụ | Chi phí / giới hạn | Điền vào `.env` | Hợp với |
| --- | --- | --- | --- |
| **Ollama** (chạy trên máy) — **khuyên dùng để test** | Miễn phí, **không giới hạn**, dữ liệu không rời máy. Cần máy khá (card NVIDIA càng tốt) và tải model vài GB | `Provider=OpenAI` · `BaseUrl=http://localhost:11434/v1` · `Model=qwen2.5:7b` · không cần key | Chạy bộ kiểm chứng nhiều lần, demo không lo hết lượt |
| **Groq** | Gói miễn phí: mỗi key 1.000 request/ngày và 8.000 token/phút (mỗi lần chấm mood tốn khoảng 1.650 token) | `Provider=OpenAI` · `BaseUrl=https://api.groq.com/openai/v1` · `Model=qwen/qwen3.8-27b` · `LLM_API_KEY=gsk_…` | Rất nhanh, đủ nhiều lượt kiểm chứng/ngày |
| **Google Gemini** | Gói miễn phí: model *Flash-Lite* thường nhiều lượt/ngày hơn *Flash*; Google đổi giới hạn thường xuyên, xem số thật trong AI Studio | `Provider=OpenAI` · `BaseUrl=https://generativelanguage.googleapis.com/v1beta/openai/` · `Model=gemini-2.5-flash-lite` · `LLM_API_KEY=…` | Khi đã có key Gemini |
| **OpenRouter** | Model `:free`: khoảng 50 request/ngày khi tài khoản chưa nạp tiền | `Provider=OpenAI` · `BaseUrl=https://openrouter.ai/api/v1` · `Model=<tên model>:free` | Thử nhiều model khác nhau |
| **Claude** | Trả phí theo lượng dùng | `ANTHROPIC_API_KEY=sk-ant-…` (Provider để trống) | Chất lượng tiếng Việt tốt nhất, dùng khi lên Production |

Giới hạn gói miễn phí thay đổi thường xuyên; con số trên chỉ để ước lượng, luôn xem trang quản lý của từng dịch vụ.

**Cài Ollama trên Windows (khoảng 10 phút):**
1. Tải bản cài ở ollama.com rồi cài.
2. Mở PowerShell: `ollama pull qwen2.5:7b`. Qwen đọc và viết tiếng Việt khá tốt.
3. Điền 3 dòng trên vào `.env`, mở lại Milo.
4. Kiểm tra: trang *Mood Engine* phải ghi "Sẵn sàng: Ollama · qwen2.5:7b".

**Dùng nhiều key Groq xoay vòng:** điền các key vào `LLM_API_KEY`, ngăn bằng dấu phẩy: `LLM_API_KEY=gsk_aaa,gsk_bbb,gsk_ccc`.
- Mỗi lần gọi dùng key kế tiếp, nên lượt được chia đều cho các key.
- Key nào báo hết lượt (429) thì nghỉ đúng khoảng thời gian dịch vụ yêu cầu; key bị từ chối (401/403) nghỉ 1 giờ. App tự chuyển ngay sang key khác trong cùng lần gọi.
- Hết cả mấy key thì Milo chấm bằng luật, không gửi request thừa.
- Trang *Mood Engine* hiện số key còn lượt, vd. "(2/3 key còn lượt)".
- **Chọn model trên Groq** (đo ngày 29/09/2026 với prompt hiện tại; Qwen: 10 ngày mẫu × 3 lần hỏi, gpt-oss: 4 ngày mẫu × 2–4 lần hỏi):

  | Model | Hỏi lại cùng ngày | Nhận xét |
  | --- | --- | --- |
  | `qwen/qwen3.8-27b` — **khuyên dùng** | Lệch 0 điểm, cả 10 ngày | Bộ 2 đạt 11/11; bộ 3 r = 0,95. Trò chuyện tiếng Việt tự nhiên nhất |
  | `openai/gpt-oss-120b` | Lệch tới 10–15 điểm | Dùng được nhưng kém ổn định |
  | `openai/gpt-oss-20b` | Lệch tới hơn 30 điểm | Có lần chấm ngày quá giờ 2 tiếng thành 0 · Kiệt sức. Không nên dùng để chấm |

- Model `openai/gpt-oss-…` có bước suy luận trước khi trả lời; app tự gửi `reasoning_effort: low` để model viết kịp JSON.
- Groq báo hết lượt trong phút (429) thì bộ kiểm chứng tự chờ 30 giây rồi hỏi lại, không tính là AI trả lời sai.
- Lưu ý: Groq tính giới hạn theo **tổ chức (organization)**, không theo key. Nhiều key trong cùng 1 tổ chức dùng chung một hạn mức, nên xoay vòng không tăng thêm lượt. Hãy đọc điều khoản của Groq trước khi dùng key từ nhiều tài khoản.

**Mỗi lần dùng tốn bao nhiêu request:**
- Bộ kiểm chứng AI: `10 ngày mẫu × số lần hỏi lại` → 20 hoặc 30 request/lượt.
- Demo tua nhanh nên chỉ chấm mood bằng AI tối đa 1 lần/phút thật (`Llm.DemoMoodMinSeconds`).
- Trang *Mood Engine* hiện số request đã dùng.

**Riêng tư:** AI chỉ nhận con số (không tiêu đề, không nội dung). Gói miễn phí của một số dịch vụ có thể dùng dữ liệu gửi lên để cải thiện sản phẩm. Với dữ liệu Bosch thật, chỉ dùng Ollama trên máy hoặc dịch vụ trả phí đã được duyệt.

### Trang Mood Engine (bảng điều khiển Demo, Sandbox, Production)

- **Luật hay AI:**
  - Chấm mood: *Luật* / *Luật + AI (±10 điểm)* / *AI chấm hẳn*.
  - Đánh giá cuộc họp: *Luật* / *AI*.
  - Đổi ngay lúc đang chạy. Nút *Hỏi AI chấm ngay*. Dòng trạng thái ghi điểm luật và điểm AI.
- **3 bộ kiểm chứng:**
  1. **Chứng minh luật hợp lý:** chạy ngay, không cần mạng.
  2. **AI hợp lý và ổn định:** trả lời đúng dạng, hỏi lại lệch ≤ 10 điểm, xếp đúng thứ tự ngày nặng/nhẹ.
  3. **So sánh luật với AI:** lệch trung bình, tương quan, cùng mức mood, cùng thứ tự.
- *Lưu báo cáo* ra file Markdown trên Desktop để đưa vào bài trình bày. Chi tiết cách chấm: [docs/CO-SO-KHOA-HOC.md](docs/CO-SO-KHOA-HOC.md).

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

7 tính năng thêm ngoài prototype. Sandbox/Production bật theo `Wellbeing` trong `appsettings.json` hoặc `.env`. Demo tắt sẵn để ngày mẫu giữ đúng các mốc của tài liệu; bật thử từng cái ở trang **Thử tình huống** → nhóm *Mới thêm* của bảng điều khiển Demo.

| Tính năng | Khi nào | Milo làm gì | Cấu hình |
| --- | --- | --- | --- |
| Giữ giờ tập trung | 1 lần/ngày, sau lần mở máy đầu 20 phút, trước 15:00, khi còn khoảng trống ≥ 60 phút | Đề nghị giữ khoảng trống dài nhất (tối đa 90 phút) trong lịch (busy). Tới giờ tự bật Không làm phiền, hết giờ bóng thoại "… phút sâu xong rồi!". Không có `Calendars.ReadWrite` thì chỉ nhắc | `FocusPlan`, `FocusPlanMinMinutes` |
| Báo cáo tuần | Sáng thứ Hai, ngay sau Chào sáng | Điểm TB, giờ họp, số lần nghỉ, ngày tốt/mệt nhất của tuần trước + 1 mẹo chọn theo điểm yếu nhất. *Xem chùm nho* mở dashboard tuần | `WeekReport` |
| Hôm nay thấy sao? | Thẻ Tan tầm (và Nhắc lại tan tầm nếu chưa trả lời) | 3 nút Vui / Bình thường / Mệt. Chỉ lưu trên máy; "Mệt" trừ 6 điểm, "Vui" cộng 3; gửi cho Claude khi bật Mood Hybrid/Llm; thống kê tuần có "Bạn tự thấy" | `EveningCheck` |
| Nghỉ giữa chuỗi họp ngày mai | Thẻ Tan tầm, khi mai có ≥ 3 cuộc họp liền | *Giữ 10' nghỉ lúc HH:mm* tạo sự kiện tentative trong lịch ngày mai | `EveningCheck` |
| Nghỉ ngắn (uống nước, vươn vai) | Mỗi 50 phút ngồi máy liên tục (không tính giờ họp), tối đa 6 lần/ngày | Ló lên 5 giây với 1 bóng thoại, không nút, không tính ngân sách lời nhắc. Rời máy ≥ 5 phút thì đếm lại | `MicroBreakEveryMinutes` (0 = tắt), `MicroBreakMaxPerDay` |
| Trốn khi trình chiếu | Teams presence = Presenting | Trốn hẳn, kể cả chóp đuôi và chấm chờ; thẻ đang mở thu lại | `HideWhenPresenting` |
| Tủ đồ của Milo | Về đúng giờ (quá giờ < 15 phút) 3 / 5 / 10 / 15 ngày liền | Mở khoá khăn quàng / kẹp hoa / mũ nồi / **đồng phục Bosch** (phần thưởng cao nhất), sáng hôm sau thẻ Chào sáng báo. Chọn món ở bảng điều khiển → *Milo của bạn* (mặc định: món mới nhất) | `Wardrobe` |

Test nhanh trên Sandbox: trang **Thử tình huống** → nhóm *Mới thêm* (Giữ giờ tập trung, Báo cáo tuần, Nghỉ ngắn); công tắc *Đang trình chiếu*; *Giờ về = bây giờ + 2 phút* để thấy thẻ Tan tầm có 3 nút cảm xúc. Báo cáo tuần cần dữ liệu tuần trước trên máy (chạy app ít nhất 1 ngày tuần trước).

## Logo và đồng phục Bosch

- **Logo** (`src/Minditful.App/Assets/Brand/`):
  - Hình: Milo đội mũ lưỡi trai đỏ Bosch trên nền kem, đáy là 3 dải màu đặc chia đều: đỏ `#E20015` · xanh dương `#007BC0` · xanh lá `#00884A`. Không dùng màu chuyển.
  - Dùng cho: file `Minditful.exe`, thanh tiêu đề, taskbar, biểu tượng ở khay, thanh bên bảng điều khiển, màn hình chọn môi trường.
  - `logo.svg` là bản gốc; `milo.ico` (16–256 px) và `logo.png` được xuất từ đó. Xem các cỡ: [docs/brand/logo-cac-co.png](docs/brand/logo-cac-co.png).
- **Dải 3 màu Bosch** (đỏ · xanh dương · xanh lá, màu đặc) chạy trên đầu bảng điều khiển và màn hình chọn môi trường.
- **Đồng phục Bosch** là món cao nhất trong tủ đồ, mở khoá khi về đúng giờ **15 ngày liền**:
  - Gồm mũ lưỡi trai đỏ có băng 3 màu và thẻ nhân viên: dây xanh vòng qua cổ, móc kẹp, thẻ trắng đầu đỏ có vạch 3 màu.
  - Đi theo mọi dáng của Milo: [docs/brand/milo-dong-phuc-bosch.png](docs/brand/milo-dong-phuc-bosch.png).
  - Ngày mẫu Demo có sẵn chuỗi 15 ngày, nên Milo mặc sẵn để present. Đổi món ở *Milo của bạn* → *Tủ đồ*.
- Logo **không dùng biểu tượng chính thức của Bosch** (vòng tròn "armature"), chỉ dùng 3 màu Bosch. Nếu ban tổ chức cho phép dùng logo chính thức, thay `logo.svg` rồi xuất lại `milo.ico` và `logo.png`.

## Góc neo (§9.1)

Kéo chóp đuôi rồi thả ở đâu thì Milo neo vào **góc gần nhất** của màn hình đó (hỗ trợ nhiều màn hình). Bấm mà không kéo thì vẫn mở dashboard.

- Góc trái: Milo được lật ngang.
- Góc trên: Milo thò xuống từ mép trên; thẻ và dashboard mọc xuống dưới.
- Góc neo lưu riêng từng môi trường. Bảng điều khiển → *Milo của bạn* có 4 nút chọn góc (cả 3 môi trường); cũng kéo chóp đuôi được.

## Dữ liệu cá nhân trên máy (SQLite, tự xoá)

Mỗi môi trường có 1 file `%LOCALAPPDATA%\Minditful\<môi trường>\minditful.db`, gồm các bảng:
- `day_record`: điểm, phút họp, nghỉ, tập trung, câu trả lời "Hôm nay thấy sao?"… mỗi ngày.
- `day_start`: giờ mở máy đầu ngày (giờ làm linh hoạt).
- `validation_week`: kiểm chứng điểm, mỗi tuần 2 con số (điểm Milo trung bình tuần, điểm WHO-5). Giữ 12 tuần (`Storage.ValidationWeeks`) vì cần vài tuần mới tính được tương quan.
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
