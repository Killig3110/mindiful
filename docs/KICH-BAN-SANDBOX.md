# Kịch bản present · Sandbox chạy như Production

Dành cho người present phần "Milo chạy thật". Sandbox dùng **tài khoản và dữ liệu thật** của tenant thử `mindiful.onmicrosoft.com`: Teams, Outlook, Azure Boards `mindiful-sandbox`. Ở chế độ **Chạy như Production**, Milo cư xử y như bản sẽ lên tenant Bosch: ngưỡng chuẩn, không có công cụ ép, không giả lập tín hiệu.

Thông điệp: **"Bạn đổi gì trên Teams / Outlook / Azure Boards, Milo thấy và phản ứng. Có mail mới, lời mời họp, task mới là Milo báo trong khoảng 20 giây."**

- Thời lượng: khoảng **9–11 phút** (phần C thêm 3 phút nếu muốn ép tình huống).
- Nên present sau phần Demo ([KICH-BAN-DEMO.md](KICH-BAN-DEMO.md)): Demo cho thấy Milo làm được gì, Sandbox chứng minh nó chạy với dữ liệu thật.
- Setup tenant: [KET-NOI-SANDBOX.md](KET-NOI-SANDBOX.md).

---

## 1. Milo đọc dữ liệu thật nhanh cỡ nào

| Nguồn | Tự đọc lại mỗi | Milo phản ứng với |
| --- | --- | --- |
| Trạng thái Teams | 30 giây | Vào cuộc gọi, trình chiếu, Do not disturb |
| Lịch Outlook / Teams | 30 giây ở Sandbox (Production 2 phút) | Lời mời họp mới, họp sắp bắt đầu, chuỗi họp liền |
| Email Outlook | 20 giây ở Sandbox (Production 5 phút) | **Email mới** gửi thẳng cho bạn, email hỏi thẳng bạn chưa trả lời, đã trả lời |
| Azure Boards | 30 giây ở Sandbox (Production 3 phút) | Task mới được giao, task sang Done, tiến độ sprint |

Không muốn chờ: bảng điều khiển → *Tổng quan* → **Làm mới ngay**. Mở khoá máy cũng làm Milo đọc lại tất cả.

Ở chế độ như Production, Milo **nhắc** theo ngưỡng chuẩn, nên có những lời nhắc không kịp xuất hiện trong 10 phút:
- Email phải chờ ≥ 1 ngày làm việc.
- Task kẹt ≥ 3 ngày.
- Làm liền 2 tiếng.

Kịch bản dưới đây chỉ dùng những phản ứng **không phụ thuộc ngưỡng**.

**Present qua Teams (chia sẻ màn hình cho ban giám khảo):** mặc định Milo bị ẩn khỏi màn hình chia sẻ và trốn khi trình chiếu, nên người xem sẽ không thấy gì. Trước khi chia sẻ, bật **Hiện Milo khi chia sẻ màn hình**: bảng điều khiển → *Tổng quan* → thẻ *Chế độ Sandbox*, hoặc menu khay. Tới bước 7 của phần B (chứng minh Milo ẩn khỏi màn hình chia sẻ) thì tắt công tắc này đi.

## 2. Chuẩn bị

### Hôm trước

| Việc | Cách làm |
| --- | --- |
| Đăng nhập | Chạy `Minditful.exe` → **Sandbox** → trình duyệt mở → đăng nhập **thulu@mindiful.onmicrosoft.com** → đồng ý quyền. Lần sau không hỏi lại |
| PAT | Đã có trong `.env`. Kiểm tra: *Tổng quan* có **2 chấm xanh** (Microsoft 365, Azure Boards) |
| Teams | Cài và đăng nhập Teams desktop bằng thulu@ trên máy present (Milo đọc trạng thái Teams từ đây) |
| Task có sẵn | Azure Boards `mindiful-sandbox` / `Milo-Sandbox` có vài task ở **Active** giao cho thulu@ (nút *Tạo dữ liệu mẫu* ở chế độ test tạo 6 task) |
| Người phụ | Nhờ 1 đồng nghiệp gửi mail cho thulu@ hôm trước, có dấu "?", ví dụ "Chốt scope sprint 43?", để hôm present email đã chờ ≥ 1 ngày |
| Tài khoản thứ 2 (bắt buộc cho phần realtime) | 1 tài khoản khác trong tenant (hoặc điện thoại đăng nhập tài khoản đó) để **gửi mail, gửi lời mời họp Teams, giao task Azure Boards** cho thulu@ ngay lúc present. Tự gửi cho chính mình thì Milo không báo (trừ khi đặt `Sandbox.IncludeSelfSentMail = true`) |
| Soạn sẵn | Soạn nháp sẵn trên tài khoản thứ 2: 1 email "Nhờ bạn review PR #512 trước 15:00 nhé", 1 lời mời họp "Sync nhanh về bản build mới" chiều nay, 1 task mới trong `Milo-Sandbox` chưa giao cho ai. Lúc present chỉ cần bấm Gửi / đổi người được giao |
| Bản app | Bản mới nhất có thẻ *Có mới*, công tắc *Hiện Milo khi chia sẻ màn hình*, gợi ý theo loại cuộc họp |

### Trước giờ present 15 phút

1. Mở app → **Sandbox** → bảng điều khiển → *Tổng quan* → thẻ **Chế độ Sandbox** → chọn **Chạy như Production**.
   - Nhãn thanh bên phải đổi thành **SANDBOX · NHƯ PRODUCTION**.
   - Trang *Thử tình huống* biến mất.
2. Mở sẵn 3 tab trình duyệt: **Outlook Calendar**, **Outlook Mail**, **Azure Boards** (board của Milo-Sandbox), đều bằng thulu@.
3. Tạo sẵn 1 cuộc họp Teams **bắt đầu sau 12 phút tính từ lúc bắt đầu phần Sandbox** (bước 5 cần nó). Tick *Teams meeting*. Đặt tên như *"Demo Milo cho ban giám khảo"*: thulu@ là người tổ chức nên thẻ Sắp họp hiện gợi ý cho người trình bày.
4. Tắt thông báo Windows khác. Để Teams mở nhưng thu nhỏ.
5. Chiếu cho người xem qua **Teams / Zoom**: bật **Hiện Milo khi chia sẻ màn hình** (*Tổng quan* → thẻ *Chế độ Sandbox*, hoặc menu khay). Chiếu bằng HDMI thì không cần.
6. Vừa present Demo: menu khay → **Chuyển môi trường → Sandbox** (hoặc bảng điều khiển → *⇄ Chuyển môi trường* dưới nhãn môi trường). Milo Demo đóng lại, Milo Sandbox mở ra, không cần tắt app bằng tay.

## 3. Kịch bản · khoảng 10 phút

### Phần A · Milo đang thấy gì (2 phút)

| # | Bạn làm | Người xem thấy | Bạn nói |
| --- | --- | --- | --- |
| 1 | Mở bảng điều khiển → **Tổng quan** | Thẻ *Chế độ Sandbox*, 3 thẻ kết nối: Microsoft 365 và Azure Boards chấm xanh, AI (Groq · Qwen) tuỳ chọn. Phần *Milo đang thấy*: số cuộc họp hôm nay, email chờ, task đang làm, sprint, điểm mood, giờ làm hôm nay | "Đây là tenant thật, không phải dữ liệu mẫu. Milo đọc thẳng từ Microsoft Graph và Azure DevOps, không có server trung gian." |
| 2 | Bấm chóp đuôi Milo ở góc màn hình | Dashboard 4 quả với số thật: cam = số cuộc họp hôm nay, táo = sprint thật | "Cùng dashboard như Demo, giờ là số liệu thật của tài khoản này." |

### Phần B · Đổi dữ liệu thật, Milo phản ứng (6 phút)

Sandbox đọc email mỗi ~20 giây, lịch và Azure Boards mỗi ~30 giây (Production 5 / 2 / 3 phút). Không muốn chờ: *Tổng quan* → **Làm mới ngay**.

Thao tác bấm từng bước trên Outlook / Teams / Azure Boards, Milo hiện gì và lỗi hay gặp của từng case: **mục 4**.

| # | Bạn làm (trên dịch vụ thật) | Chờ | Milo phản ứng | Bạn nói |
| --- | --- | --- | --- | --- |
| 1 | Người phụ (tài khoản thứ 2) **gửi 1 email** cho thulu@ | ≤ 20 giây | Milo ló lên thẻ **Có mới**: "… vừa gửi mail cho bạn" + tiêu đề, nút *Mở email* / *Đã xem* | "Có mail mới gửi thẳng cho bạn là Milo báo ngay, không cần mở Outlook." |
| 2 | Người phụ **gửi lời mời họp Teams**, rồi trên Azure Boards **giao 1 task mới** cho thulu@ | ≤ 30 giây | Thẻ gộp "2–3 thứ mới vừa tới": email, lời mời họp (kèm giờ), task #id | "Nhiều thứ tới liền thì Milo gộp 1 thẻ cho gọn. Đang họp thì Milo giữ lại, hết họp mới báo." |
| 3 | Azure Boards: kéo 1 task **Active → Closed** | ≤ 30 giây | Milo ló lên nhảy tưng "Xong #id rồi!" (tính cách Pha trộn: đôi khi là động tác *slay*), điểm mood cộng thêm | "Không cần báo cho Milo, xong việc trên Boards là Milo biết." |
| 4 | Bấm chóp đuôi → dashboard | — | Cam có thêm múi cho cuộc họp mới, anh đào là email chờ, táo là sprint thật | "Dashboard là số thật của bạn hôm nay." |
| 5 | (Cuộc họp tạo sẵn còn khoảng 5 phút) | Tới lúc còn 5 phút | Thẻ **Sắp họp** với tên cuộc họp, người tham gia, nút Tham gia, và gợi ý *"Bạn trình bày: uống ngụm nước, mở sẵn slide…"* | "Milo đoán loại cuộc họp từ tiêu đề và agenda ngay trên máy để gợi ý. AI chỉ nhận nhãn, không nhận chữ." |
| 6 | Bấm **Tham gia** trên thẻ | ≤ 30 giây | Teams mở cuộc họp. Milo và chóp đuôi **ẩn hẳn** | "Đang trong cuộc gọi thì Milo im lặng tuyệt đối. Nó đọc trạng thái Teams, không đoán." |
| 7 | Trong cuộc họp bấm **Share / Present** màn hình. Nếu đang bật *Hiện Milo khi chia sẻ màn hình* thì tắt ở bước này | Ngay (tắt công tắc) · ≤ 30 giây (trình chiếu) | Người xem màn hình chia sẻ **không thấy Milo** | "Milo không bao giờ lộ lên màn hình đang chia sẻ. Công tắc hiện Milo chỉ có ở Sandbox để demo; Production luôn ẩn." |
| 8 | Rời cuộc họp | ≤ 30 giây, sau đó chờ 2 phút | Chóp đuôi hiện lại. Lời nhắc dồn trong lúc họp (nếu có) được giao sau 2 phút ổn định | "Hết họp Milo không nhảy ra ngay mà để bạn thở 2 phút." |
| 9 | Teams: đặt trạng thái **Do not disturb** | ≤ 30 giây | Dải trên cùng bảng điều khiển: "Milo đang im lặng · vì không làm phiền" | "Tôn trọng trạng thái bạn tự đặt." Rồi trả Teams về *Available* |
| 10 | Nếu còn giờ: Outlook Mail → **trả lời** email chờ từ hôm qua | ≤ 20 giây | Quả anh đào biến mất | "Milo biết email nào đã trả lời. Nó chỉ đọc có trả lời hay chưa, không lưu nội dung." |

### Phần C (tuỳ chọn, 3 phút) · Ép Milo làm như Demo trên dữ liệu thật

| # | Bạn làm | Người xem thấy |
| --- | --- | --- |
| 1 | *Tổng quan* → *Chế độ Sandbox* → **Chế độ test (như Demo)** | Nhãn đổi thành SANDBOX · CHẾ ĐỘ TEST, trang *Thử tình huống* hiện lại |
| 2 | *Thử tình huống* → *Cho Milo làm ngay* → **Lịch kín** (cần ≥ 2 cuộc họp liền trong ngày) → Giữ chỗ | Sự kiện "Nghỉ cùng Milo" xuất hiện trong Outlook thật |
| 3 | **Task kẹt** → Khoá 90 phút | Outlook có "Tập trung: #id", Teams chuyển Do not disturb thật, và **Milo ngủ trên chóp đuôi** (chữ z bay, rê chuột thấy "tập trung tới HH:MM") |
| 4 | Công tắc **Ngày căng thẳng** | Điểm tụt 30, Milo nhạt màu, dáng mệt |
| 5 | Chuyển lại **Chạy như Production** | Công cụ ẩn đi, về ngưỡng chuẩn, bỏ giả lập |

Nói: *"Sandbox là giao thoa: ép Milo làm để kiểm thử như Demo, rồi bật lại như Production để xem bản thật."*

### Kết (30 giây)

> "Cùng một bộ não với Demo. Lên tenant Bosch chỉ cần IT cấp quyền cho app registration, không đổi code."

## 4. Chi tiết từng case realtime

Mỗi case ghi: **điều kiện**, **thao tác từng bước** trên dịch vụ thật, **thời gian chờ**, **Milo hiện gì**, **nói gì**, **lỗi hay gặp**.

- "Tài khoản thứ 2" = 1 tài khoản khác trong tenant `mindiful.onmicrosoft.com` (hoặc điện thoại đăng nhập tài khoản đó). thulu@ là tài khoản đang chạy Milo.
- Milo phải **đang mở trước** lúc bạn gửi. Thứ đã có sẵn lúc mở app chỉ được ghi nhận, không báo lại.
- Thời gian chờ ở Sandbox: email ≤ 20 giây, lịch và Azure Boards ≤ 30 giây, trạng thái Teams ≤ 30 giây. Không muốn chờ: bảng điều khiển → *Tổng quan* → **Làm mới ngay**.
- Production chậm hơn: email 5 phút, lịch 2 phút, Azure Boards 3 phút.
- Đang họp, trình chiếu, tập trung, Không làm phiền hay toàn màn hình thì thẻ không bật lên mà thành **chấm chờ** ở góc. Bấm chấm để mở ngay, hoặc chờ hết lý do im lặng + 2 phút.

### Tổng quan

| # | Case | Bạn làm | Chờ | Milo |
| --- | --- | --- | --- | --- |
| A | Email mới | Tài khoản thứ 2 gửi mail cho thulu@ | ≤ 20" | Thẻ **Có mới** · email |
| B | Lời mời họp mới | Tài khoản thứ 2 mời thulu@ vào cuộc họp Teams | ≤ 30" | Thẻ **Có mới** · lời mời họp |
| C | Task mới được giao | Azure Boards: giao task Active cho thulu@ | ≤ 30" | Thẻ **Có mới** · task |
| D | Nhiều thứ cùng lúc | Làm A + B + C liền nhau | ≤ 30" | 1 thẻ gộp "3 thứ mới vừa tới" |
| E | Task xong | Kéo task của thulu@ sang Closed | ≤ 30" | Nhảy tưng "Xong #id rồi!" |
| F | Sắp họp | Cuộc họp Teams online còn 5 phút | Đúng giờ | Thẻ **Sắp họp** + gợi ý theo loại cuộc họp |
| G | Đang họp | Tham gia cuộc họp | ≤ 30" | Ẩn hẳn, cả chóp đuôi |
| H | Chia sẻ màn hình | Share / Present trong Teams | Ngay / ≤ 30" | Người xem không thấy Milo |
| I | Không làm phiền | Teams đặt Do not disturb | ≤ 30" | Im lặng |
| J | Trả lời email chờ | Trả lời email có dấu "?" từ hôm qua | ≤ 20" | Quả anh đào biến mất |
| K | Khoá / mở khoá máy | Windows + L, rồi đăng nhập lại | Ngay | Nghỉ; mở khoá thì đọc lại dữ liệu |
| L | Milo làm giúp | Bấm *Giữ chỗ* / *Khoá 90 phút* trên thẻ | ≤ 5" | Outlook có sự kiện, Teams chuyển DND, Milo ngủ trên chóp đuôi |

### A. Email mới → thẻ "Có mới"

- **Điều kiện:** gửi **thẳng** cho thulu@ (ô *To*, không phải CC/BCC), từ tài khoản khác, trong 24 giờ qua.
- **Thao tác** (tài khoản thứ 2, Outlook web `outlook.office.com`):
  1. **New mail**.
  2. *To*: `thulu@mindiful.onmicrosoft.com`.
  3. *Subject*: `Nhờ bạn review PR #512 trước 15:00 nhé` (nội dung tuỳ ý).
  4. **Send**.
- **Chờ:** ≤ 20 giây.
- **Milo hiện:** ló lên chỉ tay, thẻ có nhãn *Outlook · email mới* · *vừa tới*, tiêu đề **"<Tên người gửi> vừa gửi mail cho bạn"**, 1 dòng tiêu đề email kèm chữ viết tắt tên. Nút **Mở email** (mở đúng email trong Outlook) · **Đã xem**. 20 giây không bấm thì thẻ thu lại, không nhắc lại.
- **Nói:** *"Có mail mới gửi thẳng cho bạn là Milo báo ngay, không cần mở Outlook. Mail CC hay mail nhóm thì Milo không làm phiền."*
- **Lỗi hay gặp:**
  - Gửi từ chính thulu@ → không báo. Chỉ để thử 1 tài khoản thì đặt `MINDITFUL__Minditful__Sandbox__IncludeSelfSentMail=true` trong `.env`, rồi mở lại app.
  - Để thulu@ ở CC → không báo.
  - Email tới **trước** khi mở app → không báo (coi là có sẵn).

### B. Lời mời họp mới → thẻ "Có mới"

- **Điều kiện:** cuộc họp **người khác** tạo, có mời thulu@, diễn ra **hôm nay hoặc ngày mai**, không phải cả ngày, không đặt *Show as: Free*.
- **Thao tác** (tài khoản thứ 2, Teams → **Calendar** → **New meeting**, hoặc Outlook web → Calendar → New event → bật *Teams meeting*):
  1. *Title*: `Sync nhanh về bản build mới`.
  2. *Required attendees*: `thulu@mindiful.onmicrosoft.com`.
  3. Giờ: chiều nay (hoặc ngày mai).
  4. **Send**.
- **Chờ:** ≤ 30 giây.
- **Milo hiện:** thẻ *Teams · lời mời họp mới*, tiêu đề **"Có lời mời họp mới lúc HH:MM"** (ngày mai thì "mai HH:MM"), dòng tên cuộc họp + người tổ chức. Nút **Xem cuộc họp** · **Đã xem**.
- **Mẹo:** tên cuộc họp có từ khoá như *planning*, *daily*, *demo*, *workshop*, *1:1* thì Milo đoán loại cuộc họp. Tới lúc còn 5 phút, thẻ Sắp họp hiện gợi ý tương ứng (xem F).
- **Lỗi hay gặp:** thulu@ tự tạo cuộc họp → không báo, vì không phải "lời mời". Cuộc họp tuần sau → không báo.

### C. Task mới được giao → thẻ "Có mới"

- **Điều kiện:** work item trong project `Milo-Sandbox`, loại *Task / Bug / User Story / Product Backlog Item / Issue*, **Assigned To = thulu@**, **State = Active**.
- **Thao tác** (Azure DevOps `dev.azure.com/mindiful-sandbox` → `Milo-Sandbox` → **Boards → Work items**):
  1. **New Work Item → Task**.
  2. *Title*: `Sửa lỗi đăng nhập trên màn hình Settings`.
  3. *Assigned To*: thulu@.
  4. *State*: **Active**.
  5. **Save**.

  Hoặc mở 1 task Active có sẵn của người khác, đổi *Assigned To* sang thulu@ rồi **Save**.
- **Chờ:** ≤ 30 giây.
- **Milo hiện:** thẻ *Azure Boards · task mới giao cho bạn*, tiêu đề **"Bạn vừa được giao 1 task mới"**, dòng `#id` + tên task. Nút **Mở task** (mở work item trên trình duyệt) · **Đã xem**.
- **Lỗi hay gặp:**
  - State để **New** → không tính. Chỉ trạng thái trong `ActiveStates` (mặc định *Active*) mới được đọc.
  - Tạo ở project khác → không thấy.
  - PAT hết hạn → trang *Tổng quan* báo Azure Boards vàng/đỏ.

### D. Nhiều thứ cùng lúc → 1 thẻ gộp

- **Thao tác:** làm A, B, C liền nhau trong khoảng 20 giây.
- **Milo hiện:** 1 thẻ nhãn *Outlook · Teams · Azure Boards*, tiêu đề **"3 thứ mới vừa tới"**, 3 dòng (email, cuộc họp, task), mới nhất ở trên. Thứ tới thêm lúc thẻ đang mở thì gộp vào luôn, không bật thẻ thứ 2.
- **Nói:** *"Nhiều thứ tới liền thì Milo gộp 1 lần cho gọn, không nhảy ra 3 lần."*

### E. Task xong → Milo ăn mừng

- **Điều kiện:** task giao cho thulu@, chuyển sang **Closed** hoặc **Resolved** hôm nay.
- **Thao tác:** Azure Boards → **Boards** → kéo thẻ task từ cột *Active* sang *Closed* (hoặc mở task → *State: Closed* → Save).
- **Chờ:** ≤ 30 giây.
- **Milo hiện:** ló lên nhảy tưng, bóng thoại **"Xong #id rồi!"**, điểm mood +2. Tính cách *Pha trộn* thì khoảng 1/3 số lần là động tác **slay** lấp lánh.
- **Nói:** *"Không cần báo cho Milo, xong việc trên Boards là Milo biết."*

### F. Sắp họp → thẻ "Sắp họp" có gợi ý

- **Điều kiện:** cuộc họp **Teams meeting** (online) trong lịch thulu@, còn **≤ 5 phút**, thulu@ không đang trong cuộc gọi khác.
- **Chuẩn bị:** tạo sẵn cuộc họp (thulu@ tổ chức hoặc được mời) bắt đầu đúng lúc cần demo. Đặt tên *"Demo Milo cho ban giám khảo"* thì Milo đoán là *Trình bày*.
- **Milo hiện:** thẻ *Teams · còn 5 phút*, tên cuộc họp, người tham gia, vai trò, câu gợi ý theo loại. Ví dụ bạn trình bày: *"uống ngụm nước, mở sẵn slide, hít sâu 3 nhịp rồi vào"*; tên có *planning*: *"ghi sẵn 1–2 ý chính"*. Nút **Tham gia** (mở Teams) · **Mở slide** (nếu bạn trình bày và có file đính kèm).
- **Nói:** *"Milo đoán loại cuộc họp từ tiêu đề và agenda ngay trên máy để gợi ý. AI chỉ nhận nhãn, không nhận chữ."*
- **Lỗi hay gặp:** cuộc hẹn thường (không bật Teams meeting) → không có thẻ Sắp họp, chỉ hiện trong dashboard.

### G. Đang họp → Milo im lặng

- **Thao tác:** bấm **Tham gia** trên thẻ F (hoặc vào cuộc họp trong Teams).
- **Chờ:** ≤ 30 giây, Teams presence chuyển *In a call*.
- **Milo hiện:** ẩn hẳn, cả chóp đuôi. Có lời nhắc hoặc email mới trong lúc họp thì góc màn hình có chấm **"n lời nhắc đang chờ"**.
- **Thử thêm:** trong lúc họp, tài khoản thứ 2 gửi 1 email → Milo **không** bật lên. Rời họp, 2 phút sau thẻ *Có mới* mới hiện.
- **Nói:** *"Đang trong cuộc gọi thì Milo im lặng tuyệt đối, và hết họp để bạn thở 2 phút rồi mới nói."*
- **Lỗi hay gặp:** Teams desktop không đăng nhập thulu@ → Milo đoán họp theo lịch (vẫn ẩn đúng giờ trong lịch).

### H. Chia sẻ màn hình / trình chiếu

- **Thao tác:** trong cuộc họp bấm **Share** → chọn màn hình. Nếu đang bật *Hiện Milo khi chia sẻ màn hình* thì tắt công tắc này lúc đó (*Tổng quan* hoặc menu khay).
- **Milo hiện:** người xem màn hình chia sẻ **không thấy Milo**. Teams báo *Presenting* thì Milo trốn hẳn cả trên màn hình của bạn.
- **Nói:** *"Milo không bao giờ lộ lên màn hình đang chia sẻ. Công tắc hiện Milo chỉ có ở Sandbox để demo."*

### I. Teams "Do not disturb" → Milo im lặng

- **Thao tác:** Teams → ảnh đại diện → trạng thái **Do not disturb**.
- **Chờ:** ≤ 30 giây.
- **Milo hiện:** dải trên cùng bảng điều khiển ghi *"Milo đang im lặng · vì không làm phiền"*; chóp đuôi mờ; lời nhắc thành chấm chờ.
- Xong trả về **Available**.

### J. Trả lời email chờ → quả anh đào biến mất

- **Điều kiện của "email chờ":** gửi thẳng cho thulu@, có dấu **"?"** trong tiêu đề/nội dung hoặc được cắm cờ, chưa trả lời, đã chờ **≥ 1 ngày làm việc** (chế độ test: 0 ngày). Thẻ *Email chờ* chỉ hiện sau 10:00 (chế độ test: bất kỳ lúc nào).
- **Chuẩn bị:** hôm trước nhờ người gửi thulu@ email "Chốt scope sprint 43?".
- **Thao tác:** bấm chóp đuôi → dashboard có quả anh đào → Outlook web → mở email đó → **Reply** → Send.
- **Chờ:** ≤ 20 giây.
- **Milo hiện:** quả anh đào biến mất khỏi dashboard.
- **Nói:** *"Milo chỉ biết email đã được trả lời hay chưa, không lưu nội dung."*

### K. Khoá / mở khoá máy (Windows)

- **Thao tác:** **Windows + L**, chờ vài giây, đăng nhập lại.
- **Milo hiện:** khoá máy thì Milo nghỉ hẳn (thời gian khoá tính là nghỉ). Mở khoá thì app đọc lại mọi nguồn ngay. Lần mở khoá đầu tiên trong ngày thì Milo **Chào sáng**.
- Rời máy (không chạm chuột/phím) ≥ 5 phút cũng tính là 1 lần nghỉ. Vắng ≥ 30 phút rồi quay lại, tính cách *Pha trộn* đôi khi có động tác **mạng nhện**.

### L. Milo làm giúp (ghi vào Outlook / Teams thật)

- **Điều kiện:** tenant đã cấp `Calendars.ReadWrite` và `Presence.ReadWrite` (trang *Tổng quan* không có chữ "THIẾU").
- **Thao tác và kết quả** (khi thẻ tương ứng hiện, hoặc chế độ test → *Thử tình huống*):

  | Bấm | Outlook / Teams thật |
  | --- | --- |
  | *Giữ chỗ* (Lịch kín) | Outlook Calendar có "Nghỉ cùng Milo", category **Milo**, tentative |
  | *Khoá 30' trong lịch* (Chưa nghỉ trưa) | "Nghỉ trưa · Milo giữ chỗ" |
  | *Giữ 10' nghỉ* (thẻ Tan tầm) | "Nghỉ cùng Milo" ngày mai |
  | *Khoá 90 phút* (Task kẹt) / *Tập trung 30 phút* | Sự kiện "Tập trung: #id" (busy) + Teams chuyển **Do not disturb**; Milo **ngủ trên chóp đuôi**; hết giờ Teams tự về trạng thái tự động |
  | *Giữ* (Giữ giờ tập trung) | "Tập trung · Milo giữ chỗ"; tới giờ tự bật Do not disturb |
  | *Tham gia* / *Mở email* / *Mở task* | Mở Teams / Outlook / Azure Boards |

- **Nói:** *"Milo không tự làm gì khi bạn chưa bấm. Chỉ 2 việc tự động nằm trong cái bạn đã đồng ý: tới giờ tập trung đã giữ thì bật Không làm phiền, hết giờ thì trả lại."*
- **Dọn sau khi demo:** xoá sự kiện category **Milo** trong Outlook, trả Teams về *Available*.

## 5. Dọn dẹp sau khi present

- **Outlook Calendar:** xoá các sự kiện có category **Milo** ("Nghỉ cùng Milo", "Tập trung…") và cuộc họp tạo cho buổi present.
- **Azure Boards:** kéo task vừa Closed về lại Active nếu muốn dùng lại lần sau.
- **Teams:** trạng thái về *Available*.
- Muốn lần sau mở lại vẫn chạy như Production: không cần làm gì, app nhớ chế độ đã chọn.

## 6. Sự cố khi đang present

| Sự cố | Xử lý nhanh |
| --- | --- |
| Azure Boards chấm vàng/đỏ | *Kết nối* → dán lại PAT → *Lưu PAT* → *Làm mới ngay*. PAT hết hạn thì tạo PAT mới (xem KET-NOI-SANDBOX.md mục 6.3) |
| Microsoft 365 báo cần đăng nhập lại | *Kết nối* → **Đăng nhập Microsoft** |
| Vào họp mà Milo không ẩn | Teams desktop phải đăng nhập bằng thulu@. Nếu Teams chưa mở, Milo đoán họp theo lịch (ẩn đúng giờ họp trong lịch) |
| Không thấy thẻ Sắp họp | Cuộc họp phải là **Teams meeting** (online). Cuộc hẹn thường chỉ hiện trong dashboard |
| Task Done mà Milo chưa ăn mừng | Bấm *Làm mới ngay*. Task phải giao cho thulu@ |
| Gửi mail mà không có thẻ *Có mới* | Mail phải gửi **thẳng** cho thulu@ (ô To, không phải CC) từ **tài khoản khác**. Thứ đã có trước khi mở app không được báo. Chờ ~20 giây hoặc *Làm mới ngay*. Đang họp / DND / tập trung thì thẻ nằm ở chấm chờ góc màn hình, bấm chấm để mở |
| Lời mời họp không được báo | Cuộc họp do chính thulu@ tạo thì không báo (không phải "lời mời"). Phải là cuộc hôm nay hoặc ngày mai |
| Task mới không được báo | Task phải ở trạng thái Active/New và **giao cho thulu@** |
| Người xem Teams không thấy Milo | Bật **Hiện Milo khi chia sẻ màn hình** (*Tổng quan* hoặc menu khay). Đang trình chiếu mà công tắc tắt thì Milo trốn hẳn: đó là đúng thiết kế |
| Cần chắc chắn có phản ứng | Chuyển **Chế độ test** và dùng *Cho Milo làm ngay* (phần C) |
