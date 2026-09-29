# Kịch bản present · Sandbox chạy như Production

Dành cho người present phần "Milo chạy thật". Sandbox dùng **tài khoản và dữ liệu thật** của tenant thử `mindiful.onmicrosoft.com`: Teams, Outlook, Azure Boards `mindiful-sandbox`. Ở chế độ **Chạy như Production**, Milo cư xử y như bản sẽ lên tenant Bosch: ngưỡng chuẩn, không có công cụ ép, không giả lập tín hiệu.

Thông điệp: **"Bạn đổi gì trên Teams / Outlook / Azure Boards, Milo thấy và phản ứng."**

- Thời lượng: khoảng **11 phút** (phần C thêm 3 phút nếu muốn ép tình huống).
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

**Present qua Teams (chia sẻ màn hình cho ban giám khảo):** mặc định Milo bị ẩn khỏi màn hình chia sẻ và trốn khi trình chiếu, nên người xem sẽ không thấy gì. Trước khi chia sẻ, bật **Hiện Milo khi chia sẻ màn hình**: bảng điều khiển → *Tổng quan* → thẻ *Chế độ Sandbox*, hoặc menu khay. Tới bước 4 của phần B (chứng minh Milo ẩn khỏi màn hình chia sẻ) thì tắt công tắc này đi.

## 2. Chuẩn bị

### Hôm trước

| Việc | Cách làm |
| --- | --- |
| Đăng nhập | Chạy `Minditful.exe` → **Sandbox** → trình duyệt mở → đăng nhập **thulu@mindiful.onmicrosoft.com** → đồng ý quyền. Lần sau không hỏi lại |
| PAT | Đã có trong `.env`. Kiểm tra: *Tổng quan* có **2 chấm xanh** (Microsoft 365, Azure Boards) |
| Teams | Cài và đăng nhập Teams desktop bằng thulu@ trên máy present (Milo đọc trạng thái Teams từ đây) |
| Task có sẵn | Azure Boards `mindiful-sandbox` / `Milo-Sandbox` có vài task ở **Active** giao cho thulu@ (nút *Tạo dữ liệu mẫu* ở chế độ test tạo 6 task) |
| Người phụ | Nhờ 1 đồng nghiệp gửi mail cho thulu@ hôm trước, có dấu "?", ví dụ "Chốt scope sprint 43?", để hôm present email đã chờ ≥ 1 ngày |

### Trước giờ present 15 phút

1. Mở app → **Sandbox** → bảng điều khiển → *Tổng quan* → thẻ **Chế độ Sandbox** → chọn **Chạy như Production**.
   - Nhãn thanh bên phải đổi thành **SANDBOX · NHƯ PRODUCTION**.
   - Trang *Thử tình huống* biến mất.
2. Mở sẵn 3 tab trình duyệt: **Outlook Calendar**, **Outlook Mail**, **Azure Boards** (board của Milo-Sandbox), đều bằng thulu@.
3. Tạo sẵn 1 cuộc họp Teams **bắt đầu sau 12 phút** (mục 3, bước 2 cần nó). Tick *Teams meeting*.
4. Tắt thông báo Windows khác. Để Teams mở nhưng thu nhỏ.

## 3. Kịch bản · khoảng 10 phút

### Phần A · Milo đang thấy gì (2 phút)

| # | Bạn làm | Người xem thấy | Bạn nói |
| --- | --- | --- | --- |
| 1 | Mở bảng điều khiển → **Tổng quan** | 3 thẻ kết nối: Microsoft 365 và Azure Boards chấm xanh, Claude tuỳ chọn. Phần *Milo đang thấy*: số cuộc họp hôm nay, email chờ, task đang làm, sprint, điểm mood, giờ làm hôm nay | "Đây là tenant thật, không phải dữ liệu mẫu. Milo đọc thẳng từ Microsoft Graph và Azure DevOps, không có server trung gian." |
| 2 | Bấm chóp đuôi Milo ở góc màn hình | Dashboard 4 quả với số thật: cam = số cuộc họp hôm nay, táo = sprint thật | "Cùng dashboard như Demo, giờ là số liệu thật của tài khoản này." |

### Phần B · Đổi dữ liệu thật, Milo phản ứng (6 phút)

| # | Bạn làm (trên dịch vụ thật) | Chờ | Milo phản ứng | Bạn nói |
| --- | --- | --- | --- | --- |
| 0 | Từ **tài khoản test thứ 2** (hoặc điện thoại), gửi 1 email tới tài khoản đang chạy Milo. Tiếp theo: gửi 1 lời mời họp Teams, rồi trên Azure Boards giao 1 task mới cho tài khoản này | ≤ 20 giây (email), ≤ 30 giây (họp, task) | Milo ló lên thẻ **Có mới**: "… vừa gửi mail cho bạn" + tiêu đề, nút *Mở email*. Tới liền nhau thì gộp "3 thứ mới vừa tới" | "Có email mới, lời mời họp hay task mới là Milo báo ngay, không cần mở Outlook. Đang họp thì Milo giữ lại, hết họp mới báo." |
| 1 | Azure Boards: kéo 1 task **Active → Closed** | ≤ 3 phút (hoặc *Làm mới ngay*) | Milo ló lên nhảy tưng "Xong #id rồi!", điểm mood cộng thêm | "Không cần báo cho Milo, xong việc trên Boards là Milo biết." |
| 2 | (Cuộc họp tạo sẵn còn khoảng 5 phút) | Tới lúc còn 5 phút | Thẻ **Sắp họp** với tên cuộc họp, người tham gia, nút Tham gia | "Họp Teams sắp bắt đầu, Milo nhắc đúng 5 phút trước." |
| 3 | Bấm **Tham gia** trên thẻ | ≤ 30 giây | Teams mở cuộc họp. Milo và chóp đuôi **ẩn hẳn** | "Đang trong cuộc gọi thì Milo im lặng tuyệt đối. Nó đọc trạng thái Teams, không đoán." |
| 4 | Trong cuộc họp bấm **Share / Present** màn hình. Nếu đang bật *Hiện Milo khi chia sẻ màn hình* thì tắt ở bước này | Ngay (tắt công tắc) · ≤ 30 giây (trình chiếu) | Người xem màn hình chia sẻ **không thấy Milo** | "Milo không bao giờ lộ lên màn hình đang chia sẻ. Công tắc này chỉ có ở Sandbox để demo; Production luôn ẩn." |
| 5 | Rời cuộc họp | ≤ 30 giây, sau đó chờ 2 phút | Chóp đuôi hiện lại. Lời nhắc dồn trong lúc họp (nếu có) được giao sau 2 phút ổn định | "Hết họp Milo không nhảy ra ngay mà để bạn thở 2 phút." |
| 6 | Teams: đặt trạng thái **Do not disturb** | ≤ 30 giây | Dải trên cùng bảng điều khiển: "Milo đang im lặng · vì không làm phiền" | "Tôn trọng trạng thái bạn tự đặt." Rồi trả Teams về *Available* |
| 7 | Outlook Mail: mở email đồng nghiệp gửi hôm qua → dashboard có 1 quả anh đào. **Trả lời** email đó | ≤ 5 phút (hoặc *Làm mới ngay*) | Quả anh đào biến mất | "Milo biết email nào đã trả lời. Nó chỉ đọc có trả lời hay chưa, không lưu nội dung." |
| 8 | Nếu còn giờ: chờ thẻ **Email chờ** (sau 10:00) hoặc thẻ chăm sóc tới, bấm **Giữ chỗ** / **Khoá** trên thẻ | — | Outlook Calendar có sự kiện "Nghỉ cùng Milo" / "Tập trung", category **Milo**. Khoá tập trung thì Teams chuyển Do not disturb | "Chiều ngược lại: Milo ghi vào lịch và Teams của bạn khi bạn đồng ý." |

Thứ tự 2–5 phụ thuộc giờ họp đã tạo. Nếu họp bắt đầu sớm hơn dự kiến thì làm bước 2–5 trước bước 1.

### Phần C (tuỳ chọn, 3 phút) · Ép Milo làm như Demo trên dữ liệu thật

| # | Bạn làm | Người xem thấy |
| --- | --- | --- |
| 1 | *Tổng quan* → *Chế độ Sandbox* → **Chế độ test (như Demo)** | Nhãn đổi thành SANDBOX · CHẾ ĐỘ TEST, trang *Thử tình huống* hiện lại |
| 2 | *Thử tình huống* → *Cho Milo làm ngay* → **Lịch kín** (cần ≥ 2 cuộc họp liền trong ngày) → Giữ chỗ | Sự kiện "Nghỉ cùng Milo" xuất hiện trong Outlook thật |
| 3 | **Task kẹt** → Khoá 90 phút | Outlook có "Tập trung: #id", Teams chuyển Do not disturb thật |
| 4 | Công tắc **Ngày căng thẳng** | Điểm tụt 30, Milo nhạt màu, dáng mệt |
| 5 | Chuyển lại **Chạy như Production** | Công cụ ẩn đi, về ngưỡng chuẩn, bỏ giả lập |

Nói: *"Sandbox là giao thoa: ép Milo làm để kiểm thử như Demo, rồi bật lại như Production để xem bản thật."*

### Kết (30 giây)

> "Cùng một bộ não với Demo. Lên tenant Bosch chỉ cần IT cấp quyền cho app registration, không đổi code."

## 4. Dọn dẹp sau khi present

- **Outlook Calendar:** xoá các sự kiện có category **Milo** ("Nghỉ cùng Milo", "Tập trung…") và cuộc họp tạo cho buổi present.
- **Azure Boards:** kéo task vừa Closed về lại Active nếu muốn dùng lại lần sau.
- **Teams:** trạng thái về *Available*.
- Muốn lần sau mở lại vẫn chạy như Production: không cần làm gì, app nhớ chế độ đã chọn.

## 5. Sự cố khi đang present

| Sự cố | Xử lý nhanh |
| --- | --- |
| Azure Boards chấm vàng/đỏ | *Kết nối* → dán lại PAT → *Lưu PAT* → *Làm mới ngay*. PAT hết hạn thì tạo PAT mới (xem KET-NOI-SANDBOX.md mục 6.3) |
| Microsoft 365 báo cần đăng nhập lại | *Kết nối* → **Đăng nhập Microsoft** |
| Vào họp mà Milo không ẩn | Teams desktop phải đăng nhập bằng thulu@. Nếu Teams chưa mở, Milo đoán họp theo lịch (ẩn đúng giờ họp trong lịch) |
| Không thấy thẻ Sắp họp | Cuộc họp phải là **Teams meeting** (online). Cuộc hẹn thường chỉ hiện trong dashboard |
| Task Done mà Milo chưa ăn mừng | Bấm *Làm mới ngay*. Task phải giao cho thulu@ |
| Cần chắc chắn có phản ứng | Chuyển **Chế độ test** và dùng *Cho Milo làm ngay* (phần C) |
