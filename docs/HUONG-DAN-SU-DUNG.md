# Hướng dẫn sử dụng Milo

Tài liệu cho người **dùng** app: đồng nghiệp dùng thử, người đi present, người test. Cài đặt và cấu hình kỹ thuật nằm ở [README](../README.md); cách app được xây nằm ở [KIEN-TRUC.md](KIEN-TRUC.md).

Mục lục:
1. [Milo là ai](#1-milo-là-ai)
2. [Mở app và chọn môi trường](#2-mở-app-và-chọn-môi-trường)
3. [Milo trên màn hình của bạn](#3-milo-trên-màn-hình-của-bạn)
4. [Bảng điều khiển](#4-bảng-điều-khiển)
5. [Milo làm được gì: từng tính năng](#5-milo-làm-được-gì-từng-tính-năng)
6. [Dùng theo từng môi trường](#6-dùng-theo-từng-môi-trường)
7. [Vì sao Milo không hiện?](#7-vì-sao-milo-không-hiện)
8. [Riêng tư](#8-riêng-tư)
9. [Hỏi nhanh](#9-hỏi-nhanh)

---

## 1. Milo là ai

Milo là một chú cáo nhỏ sống ở góc màn hình. Milo nhìn lịch họp, email và task của bạn, rồi nhắc đúng lúc: sắp họp, họp liền quá lâu, ngồi máy quá lâu, quá giờ về. Milo **không nói khi bạn đang bận** (đang họp, đang trình chiếu, đang tập trung) và mỗi lần chỉ nhắc 1 chuyện.

Bình thường Milo ẩn, chỉ còn **chóp đuôi màu cam** ở góc phải dưới màn hình, ngay trên thanh taskbar.

## 2. Mở app và chọn môi trường

Mở `Minditful.exe` (hoặc chạy từ Visual Studio). Màn hình đầu tiên hỏi **"Hôm nay chạy Milo ở đâu?"**:

| Chọn | Dành cho | Cần gì |
| --- | --- | --- |
| **Demo · xem thử, present** | Lần đầu dùng, đi present, xem Milo làm đủ mọi tình huống | Không cần tài khoản, không cần mạng |
| **Sandbox · thử với dữ liệu thật** | Nhóm phát triển test với tenant thử `mindiful.onmicrosoft.com` | Tài khoản thulu@, PAT Azure DevOps |
| **Production · dùng hằng ngày** | Dùng thật với tài khoản Bosch | Tài khoản Bosch; IT đã duyệt quyền |

Tick **"Nhớ lựa chọn"** để lần sau mở thẳng. Muốn chọn lại: **giữ Shift** khi mở app.

**Chuyển môi trường khi đang chạy** (không cần tắt app bằng tay):
- Menu khay → **Chuyển môi trường** → *Demo* / *Sandbox* / *Production*.
- Hoặc bảng điều khiển → **⇄ Chuyển môi trường** (ngay dưới nhãn môi trường ở thanh bên).
- Milo ở môi trường cũ lưu dữ liệu hôm nay rồi đóng, Milo ở môi trường mới mở ra. Mỗi môi trường có dữ liệu, đăng nhập, tủ đồ, cài đặt riêng nên không trộn lẫn.
- Đã tick "Nhớ lựa chọn" thì lần mở sau vào thẳng môi trường vừa chuyển tới.

App chạy nền. Biểu tượng **chóp đuôi cáo** nằm ở khay hệ thống (góc phải taskbar, có thể nằm trong mũi tên ^). Chuột phải vào biểu tượng:

- **Mở dashboard của Milo**
- **Mở bảng điều khiển**
- Demo: **Phát / tạm dừng ngày mẫu**, **Làm lại ngày mẫu**
- **Trò chuyện với Milo**, **Thay đồ cho Milo**
- Sandbox: **Chế độ test**, **Hiện Milo khi chia sẻ màn hình**; Sandbox/Production: **Khởi động cùng Windows**, **Đăng nhập Microsoft**, **Làm mới dữ liệu**
- **Chuyển môi trường**: sang Demo / Sandbox / Production ngay
- **Thoát Milo**: tắt hẳn app

Nhấp đúp biểu tượng cũng mở bảng điều khiển.

## 3. Milo trên màn hình của bạn

### 3.1 Chóp đuôi, ló đầu, dashboard

| Bạn làm | Milo làm |
| --- | --- |
| Rê chuột lên chóp đuôi khoảng nửa giây | Milo **ló đầu** và thì thầm 1 dòng, vd. "Hôm nay mọng 80 · chạm để xem" |
| Bấm chóp đuôi | Mở **dashboard 4 quả** quanh Milo |
| Chóp đuôi có số "1" | Có 1 lời nhắc bạn chưa trả lời. Bấm đuôi để mở lại lời nhắc đó |
| Kéo chóp đuôi sang góc khác rồi thả | Milo chuyển sang góc đó (trên trái, trên phải, dưới trái, dưới phải) |
| Bấm Milo, bấm ×, hoặc nhấn Esc | Đóng dashboard |
| Bấm **Trò chuyện** trên dashboard, bấm Milo lúc Milo đang đứng ở góc, hoặc menu khay → *Trò chuyện với Milo* | Mở **khung trò chuyện** (mục 3.4) |
| Chuột phải Milo hoặc chóp đuôi | Menu nhanh: *Thay đồ cho Milo* · *Trò chuyện với Milo* · *Mở dashboard* · *Tính cách Milo* |
| Bấm Milo 5 lần liền | "Ơ kìa!" rồi giả vờ ngất (trừ tính cách Dễ thương) |

Màu và dáng Milo đổi theo **điểm mood** của ngày: mọng (≥ 80) thì tươi tắn; mệt dần (40–59) thì nhạt màu, dáng uể oải; kiệt sức (< 40) thì có chữ "z" bay.

### 3.2 Dashboard 4 quả

| Quả | Ý nghĩa | Rê chuột lên để xem |
| --- | --- | --- |
| **Nho** (tím) | Điểm mood hôm nay. Quả càng mọng càng ổn, héo vàng là mệt | Câu tóm tắt, so với hôm qua, Office Vibe (tập trung / năng lượng / căng thẳng) |
| **Cam** | Cuộc họp: mỗi múi là 1 cuộc, múi đã ăn = đã họp xong | Lịch họp hôm nay, mức nặng từng cuộc |
| **Anh đào** | Mỗi quả là 1 email hỏi thẳng bạn mà chưa trả lời | 3 email chờ lâu nhất |
| **Táo** | Sprint: càng gần xong táo càng bị cắn nhiều | Điểm sprint, số task đang làm / kẹt |

Trên thanh tiêu đề nhỏ:
- **Tủ đồ**: mở tủ đồ ngay trên đầu Milo (mục 3.5). **Trò chuyện**: mở khung chat (mục 3.4).
- **Tuần này →**: nho thành **chùm 7 quả**, mỗi quả là 1 ngày (quả viền cam là hôm nay). Cam có 5 múi = 5 ngày làm việc.
- **Chi tiết**: mở **bảng nhỏ** ngay trên đầu Milo, cỡ 1 thẻ.
  - Trang *Hôm nay*: dòng thời gian giờ làm (họp, tập trung, nghỉ, vạch cam là bây giờ), Office Vibe, 3 cuộc họp sắp tới.
  - Trang *Tuần*: 7 quả nho, tổng giờ họp / nghỉ / tập trung / task, bạn trả lời Milo thế nào, bạn tự thấy thế nào, 1 mẹo.
  - Bấm **‹ 4 quả** để quay lại.

### 3.3 Thẻ nhắc và cách trả lời

Khi có chuyện cần nói, Milo leo lên và hiện **thẻ** phía trên đầu.

- **Nút chính** (cam hoặc nâu đậm): làm luôn. Ví dụ *Tham gia*, *Giữ chỗ trong lịch*, *Khoá 90 phút*, *Đồng ý, nghỉ chút*.
- **Để sau (15p)**: Milo hỏi lại sau, tối đa 2 lần/ngày cho mỗi chuyện.
- **Không cần**: Milo thôi nhắc chuyện đó 1 lúc. Bấm "Không cần" lần 2 trong ngày thì Milo giãn ra lâu hơn nữa.
- **Không trả lời**: sau 45 giây thẻ tự thu lại, chóp đuôi hiện số "1" giữ trong 30 phút.
- **Ô chat**: gõ tự nhiên.

| Bạn gõ | Milo hiểu |
| --- | --- |
| "ok", "đồng ý", "làm luôn" | Bấm nút chính |
| "đang bận", "lát nữa", "đang họp" | Để sau |
| "không", "thôi", "ổn mà" | Không cần |
| "mệt quá", "căng thẳng" | Rủ bạn thở cùng vài nhịp |
| "về thôi", "tan làm" | Ở thẻ tan tầm: chạy ra xe về nhà |
| "cảm ơn" | Milo vui, tính như nửa lần đồng ý |

Có AI (Claude, Groq, Ollama…) thì câu khó hiểu cũng được trả lời tự nhiên, và AI biết đúng tên nút chính trên thẻ. Không có thì Milo nhắc bạn chọn nút.

**Chấm chờ:**
- Lúc Milo phải im lặng (đang họp, toàn màn hình, không làm phiền…), **chóp đuôi mờ đi** để bạn biết Milo vẫn chạy.
- Lúc bạn đang trong **giờ tập trung**, Milo **nằm ngủ trên chóp đuôi**: mắt nhắm, đầu gục, có chữ "z" bay lên. Rê chuột lên đuôi để xem tập trung tới mấy giờ. Hết giờ tập trung thì Milo tỉnh dậy báo "xong rồi".
- Lời nhắc dồn lại thành 1 viên nhỏ "2 lời nhắc đang chờ" ở góc.
- Bấm vào viên đó thì chỉ thẻ bung ra ở sát góc, Milo vẫn ẩn để không chen vào cuộc họp.

### 3.4 Trò chuyện với Milo

Không cần đợi Milo nhắc, bạn có thể tự mở trò chuyện (cách mở ở mục 3.1).

- Milo mở đầu bằng 1 câu theo điểm hôm nay, kèm 3 câu gợi ý bấm nhanh: *Hôm nay mình sao rồi?*, *Mình thấy mệt*, *Lịch họp còn gì?*.
- Gõ gì cũng được. Khác với ô chat trên thẻ nhắc, gõ "ok" hay "không" ở đây **không** đóng khung, Milo chỉ trả lời.
- Nút **Thở 1 phút**: thở 4-4-4 cùng Milo, tính là 1 lần nghỉ. Nút **Xong** (hoặc Esc) để Milo leo xuống. 2 phút không gõ gì thì Milo tự chào rồi đi.
- **Có AI:** Milo trả lời tự nhiên, nối mạch 6 lượt gần nhất. AI chỉ nhận con số trong ngày (điểm, giờ họp, làm liền, nghỉ, quá giờ, giờ cuộc họp tới), **không** nhận tiêu đề email, cuộc họp hay task.
- **Không có AI** (hoặc AI lỗi, hết lượt): Milo trả lời theo từ khoá: điểm hôm nay, lịch họp, mệt, nghỉ, giờ về, cảm ơn, chào.
- Milo không trả lời câu hỏi lập trình hay kiến thức chung, chỉ là bạn đồng hành sức khoẻ.
- **Milo đề nghị tính năng ngay trong chat** (cả ở khung Trò chuyện lẫn ô chat trên thẻ nhắc): dưới câu trả lời hiện tối đa 2 nút "Milo có thể giúp". Bấm mới chạy, Milo không tự làm.

  | Bạn nói kiểu | Nút có thể hiện |
  | --- | --- |
  | "mệt quá", "căng thẳng" | *Thở 1 phút*, *Nghỉ 15 phút* |
  | "nhiều task quá", "sợ trễ deadline" | *Tìm giờ tập trung* (khoảng trống dài nhất trong lịch), *Tập trung 30 phút*, *Xem task kẹt* |
  | "bị ping liên tục", "không tập trung được" | *Tập trung 30 phút* (khoá 30 phút + Không làm phiền) |
  | "hôm nay mình sao rồi?" | *Xem hôm nay* (dashboard) |
  | "cảm ơn", chuyện đời thường | Không có nút |

  - Có AI thì AI chọn nút, chỉ trong danh sách trên. Không có AI thì chọn theo từ khoá.
  - Chỉ hiện nút dùng được lúc đó: đang họp thì không có *Tập trung 30 phút*; lịch không còn khoảng trống ≥ 60 phút thì không có *Tìm giờ tập trung*; không có task kẹt thì không có *Xem task kẹt*.
- Nút gợi ý có chữ dài hơn nút: rê chuột lên để chữ chạy ngang đọc hết.
- **An toàn:** câu có dấu hiệu khủng hoảng (vd. "muốn chết", "không muốn sống") luôn nhận 1 câu cố định khuyên tìm người thân tin cậy, chuyên gia tâm lý, hoặc gọi 115 nếu đang nguy hiểm. Câu đó không được gửi cho AI.

### 3.5 Tủ đồ: phối đồ cho Milo

**Mở ngay trên desktop, không cần bảng điều khiển** (dùng được cả ở Production):
- Chuột phải vào Milo hoặc chóp đuôi → **Thay đồ cho Milo**.
- Trên dashboard 4 quả → link **Tủ đồ**.
- Menu khay → **Thay đồ cho Milo**.
- Trong chat gõ "đổi đồ", "Milo mặc gì" → bấm nút **Mở tủ đồ**.

Bảng tủ đồ hiện ngay trên đầu Milo:
- **5 ô:** Mũ · Kẹp tóc · Kính · Cổ · Tay cầm. Mỗi ô 1 món; bấm món là Milo mặc ngay, bấm lại để cởi.
- **Đồng phục Bosch** chiếm cả ô Mũ và ô Cổ; mặc mũ hay đồ cổ khác thì cả bộ đồng phục được cởi.
- Món mờ là chưa mở khoá: rê chuột để xem điều kiện.
- **Phối nhanh:** *Ngẫu nhiên* · *Tự chọn* (món khó mở nhất đang có) · *Bỏ hết* · **+ Lưu bộ này** (tối đa 4 bộ; bấm 1 bộ để mặc lại, chuột phải để xoá).
- Dòng cuối: chuỗi về đúng giờ, số lần nghỉ, giờ tập trung, số món đã có và món sắp mở.

| Ô | Món | Mở khoá khi |
| --- | --- | --- |
| Mũ | Mũ nồi · **Đồng phục Bosch** (mũ + thẻ đeo cổ) | Về đúng giờ 10 · 15 ngày liền |
| | Tai nghe | Tập trung sâu tổng 5 giờ |
| | Mũ phù thuỷ · Mũ Noel | Mùa Halloween (20–31/10) · Noel (10–26/12) |
| Kẹp tóc | Kẹp hoa | Về đúng giờ 5 ngày liền |
| Kính | Kính tròn · Kính râm | Có sẵn · Nghỉ cùng Milo 20 lần |
| Cổ | Khăn quàng · Nơ cổ | Về đúng giờ 3 ngày liền · Tập trung sâu tổng 2 giờ |
| Tay cầm | Ly cà phê · Ly trà sữa | Có sẵn · Nghỉ cùng Milo 10 lần |
| | Bao lì xì · Lồng đèn ông sao | Mùa Tết (2 tuần trước tới 10 ngày sau mùng 1) · Trung thu (2 tuần trước tới 3 ngày sau rằm tháng 8) |

- Chỉ thưởng thói quen tốt, không bao giờ thưởng cho làm thêm giờ.
- Đồ theo mùa: mở khi tới mùa và **giữ luôn** sau mùa.
- "Về đúng giờ" nghĩa là quá giờ dưới 15 phút.

### 3.6 Tính cách Milo

Milo thường dễ thương, nhưng lâu lâu sẽ hài một chút bằng những động tác lấy cảm hứng từ meme:
- Slay khi bạn xong task.
- Liếc xéo "hmm…" khi có email chờ lâu.
- Toán bay quanh đầu khi có task kẹt.
- Nhảy vibe "TGIF" chiều thứ Sáu.
- Thanh "đang tải tuần mới" sáng thứ Hai.
- Phủ mạng nhện khi bạn vắng hơn 30 phút rồi quay lại.
- Nhấp cà phê giữa khói "mọi thứ vẫn ổn…" khi bạn quá giờ.
- Đi lừ đừ "NPC mode" khi họp liền quá nhiều.
- Bấm Milo 5 lần liền: Milo "ơ kìa!" rồi giả vờ ngất.

Không cần chỉnh gì cả. Nếu muốn, chuột phải Milo → **Tính cách Milo** để chọn:
- **Pha trộn** (mặc định): phần lớn dễ thương, lâu lâu hài.
- **Dễ thương:** luôn nhẹ nhàng, không meme.
- **Hài hước:** gặp dịp là diễn hài.

Dù tính cách nào, Milo cũng không diễn khi bạn đang họp, trình chiếu hay xem toàn màn hình.

## 4. Bảng điều khiển

Cửa sổ riêng, tông kem như thẻ của Milo. **Đóng cửa sổ này thì Milo vẫn chạy.**

- **Dải trên cùng:** hình Milo, câu "Milo đang làm gì" (vd. *Milo đang im lặng · Vì đang họp. Có 2 lời nhắc đang chờ*), đồng hồ, điểm mood.
- **Thanh bên trái:** chọn trang.

### 4.1 Demo

| Trang | Để làm gì |
| --- | --- |
| **Kịch bản trình diễn** | 28 bước đi qua đủ 22 tình huống + dashboard chi tiết, Milo ngủ khi tập trung, trình chiếu, mood realtime, 9 động tác hài, tủ đồ. Mỗi bước ghi *người xem thấy gì* và *bạn nói gì*. Bấm từng bước hoặc bật *Tự chạy qua các bước*. Kịch bản lời nói: [KICH-BAN-DEMO.md](KICH-BAN-DEMO.md) |
| **Bắt đầu** | Thẻ **Mood realtime**: kéo mức căng thẳng, bấm nghỉ / xong task, gọi Milo đứng ở góc để thấy dáng và màu đổi ngay. Phát / tạm dừng ngày mẫu, *Làm lại từ 08:50*, tốc độ *Chậm 60× · Vừa 120× · Nhanh 300×*. Công tắc **Người dùng mẫu tự bấm nút**: bật thì kịch bản tự trả lời, tắt thì bạn tự bấm trên thẻ. Số liệu hôm nay và mẹo tương tác |
| **Ngày mẫu** | 17 mốc của ngày Thứ Năm 24/9. Bấm 1 mốc để tua tới ngay trước lúc đó. Mốc đang diễn ra tô cam |
| **Thử tình huống** | *Cho Milo làm ngay*: 22 thẻ tình huống chia 4 nhóm (Chào hỏi, Giúp việc, Chăm sóc, Mới thêm), bấm là Milo làm luôn. *Có mới* bấm lần lượt ra email → lời mời họp → task mới. *Giả vờ bạn đang…*: công tắc gõ phím, rời máy, khoá máy, toàn màn hình, Không làm phiền, trình chiếu, ngày căng thẳng; và nút 1 lần (rời cuộc họp, nhảy việc, xong 1 task, mở dashboard) |
| **Milo của bạn** | Tủ đồ phối theo ô (bấm món để Milo mặc ngay), tính cách Milo + 9 nút xem thử động tác hài, chọn góc màn hình |
| **Mood Engine** | Công tắc chấm mood *Luật* / *Luật + AI* / *AI chấm hẳn* và đánh giá cuộc họp *Luật* / *AI*. **3 bộ kiểm chứng**: luật hợp lý, AI hợp lý và ổn định, so sánh luật với AI. *Lưu báo cáo* ra Desktop |
| **Bộ não Milo** | Nâng cao: trạng thái, điều gì đang khiến Milo im lặng, lời nhắc đang chờ, điểm mood được tính thế nào, nhật ký từng quyết định |

Lúc Milo ẩn, đồng hồ kịch bản tua nhanh. Lúc Milo xuất hiện, thời gian chạy thật để bạn xem trọn hoạt ảnh.

### 4.2 Sandbox và Production

| Trang | Để làm gì |
| --- | --- |
| **Tổng quan** | Sandbox: thẻ **Chế độ Sandbox** (chế độ test / như Production, công tắc *Hiện Milo khi chia sẻ màn hình*). 3 thẻ kết nối: *Microsoft 365*, *Azure Boards*, *AI*. **Chấm xanh** là ổn, **vàng** là cần làm thêm 1 bước, **đỏ** là lỗi (dòng chữ cạnh chấm ghi lý do bằng tiếng Việt). Phần *Milo đang thấy*: số cuộc họp, email chờ, task, sprint, điểm mood, cuộc họp kế tiếp, giờ làm hôm nay |
| **Kết nối** | *Đăng nhập / Đăng xuất Microsoft*, *Làm mới dữ liệu*. Ô dán **PAT** Azure DevOps (*Lưu PAT / Xoá PAT*). Ô dán **API key AI** (*Lưu key*; hoặc đặt trong `.env`). Tất cả lưu mã hoá trên máy |
| **Thử tình huống** (chỉ Sandbox ở chế độ test) | *Chuẩn bị*: Reset ngày (chào sáng lại), đặt giờ về (*Giờ về = bây giờ + 2 phút*), *Tạo dữ liệu mẫu* trong tenant. *Cho Milo làm ngay* và *Giả vờ bạn đang…* như Demo |
| **Mood Engine** | Như Demo: công tắc luật ↔ AI và 3 bộ kiểm chứng |
| **Kiểm chứng điểm** | 5 câu WHO-5 mỗi tuần, bảng so sánh điểm Milo với WHO-5, hệ số tương quan *r*, nút *Xuất CSV ẩn danh* (mục 5.5) |
| **Milo của bạn** | Tủ đồ, tính cách Milo, góc màn hình, công tắc **Khởi động cùng Windows**, *Milo chăm sóc bạn thế nào* (tính năng nào đang Bật/Tắt), *Riêng tư & dữ liệu* (Milo tự điều chỉnh theo 7 ngày ra sao, dữ liệu giữ trên máy, nút **Xoá toàn bộ dữ liệu thống kê ngay**) |
| **Bộ não Milo** | Như Demo |

Production mặc định không tự mở bảng điều khiển (Milo chỉ ở góc màn hình). Mở bằng biểu tượng ở khay.

**Chế độ Sandbox** (thẻ đầu tiên của trang *Tổng quan*, hoặc menu khay → *Chế độ test*). Sandbox là giao thoa giữa Demo và Production:

- **Chế độ test (như Demo)**
  - Có trang *Thử tình huống* để ép Milo làm bất kỳ tình huống nào, trên tài khoản và dữ liệu thật.
  - Ngưỡng rút ngắn (ngồi liền 20 phút đã nhắc, 3 phút giữa 2 lời nhắc, nhắc nghỉ ngắn mỗi 5 phút).
  - Bảng điều khiển tự mở khi chạy app.
  - Nhãn thanh bên: *SANDBOX · CHẾ ĐỘ TEST*.
- **Chạy như Production**
  - Ẩn trang *Thử tình huống*, bỏ mọi tín hiệu giả lập.
  - Về ngưỡng chuẩn: ngồi liền 2 tiếng mới nhắc, 15 phút giữa 2 lời nhắc.
  - Bảng điều khiển không tự mở.
  - Milo cư xử y như bản Production, dùng để xem trước bản thật.
  - Nhãn thanh bên: *SANDBOX · NHƯ PRODUCTION*.
- Ở cả 2 chế độ, Sandbox đọc dữ liệu nhanh hơn Production (email ~20 giây, lịch và task ~30 giây) để demo thẻ *Có mới*.

Đổi chế độ lúc nào cũng được, không cần mở lại app. Lần sau mở app sẽ nhớ chế độ đã chọn.

**Hiện Milo khi chia sẻ màn hình** (công tắc cùng thẻ, hoặc menu khay; chỉ có ở Sandbox):
- Tắt (mặc định): như Production. Milo không lọt vào màn hình chia sẻ, và trốn hẳn khi Teams báo đang trình chiếu.
- Bật: người xem Teams thấy Milo, trình chiếu cũng không làm Milo trốn. Dùng khi demo Milo qua Teams.
- Đổi có hiệu lực ngay, không cần mở lại app. Lựa chọn được nhớ cho lần sau.

## 5. Milo làm được gì: từng tính năng

Mỗi tính năng dưới đây đều có thẻ cùng tên ở trang **Thử tình huống** của bảng điều khiển (Demo, và Sandbox ở chế độ test) để xem ngay.

### 5.1 Chào hỏi

| Tính năng | Khi nào | Milo làm | Bạn trả lời |
| --- | --- | --- | --- |
| **Chào sáng** | Mở máy lần đầu trong ngày | Bám mép leo lên, "Hello!", bản tin: mấy cuộc họp, email chưa đọc, task đang dở, giờ về hôm nay, món mới trong tủ đồ | *Đã rõ* · *Nhắc lúc 10h* |
| **Chào hỏi** | Thỉnh thoảng khi bạn đang làm tốt | 1 câu động viên | *Cảm ơn Milo* |
| **Tan tầm** | Tới giờ về | Tổng kết ngày (giờ họp, lần nghỉ, tập trung, điểm), hỏi **"Hôm nay thấy sao?"** (Vui / Bình thường / Mệt), gợi ý giữ nghỉ cho chuỗi họp ngày mai | *Về thôi* (Milo chạy ra xe) · *Thêm 30 phút* (1 lần) |
| **Nhắc lại tan tầm** | Hết 30 phút làm thêm | Nhắc về | *Về thôi* |

Giờ về theo **giờ linh hoạt kiểu Bosch**: bắt đầu tính từ lúc bạn mở máy đầu ngày (trong khoảng 8:00–10:00), làm 9 tiếng. Vào 8h về 17h, vào 9h về 18h, vào 10h về 19h.

### 5.2 Giúp việc

| Tính năng | Khi nào | Milo làm | Bạn trả lời |
| --- | --- | --- | --- |
| **Sắp họp** | 5 phút trước cuộc họp Teams | Thẻ tên cuộc họp, người tham gia, vai trò; nếu bạn trình bày thì mời mở slide. Thêm 1 câu gợi ý theo loại cuộc họp Milo đoán từ tiêu đề và agenda: trình bày, ra quyết định, ngồi nghe, làm việc nhóm, 1:1 (đọc ngay trên máy, chữ gốc không gửi cho AI) | *Tham gia* (mở Teams) · *Mở slide* |
| **Lịch kín** | Buổi sáng/chiều có ≥ 3 cuộc họp liền nhau | Lịch mini, đề nghị chèn 10 phút nghỉ | *Giữ chỗ trong lịch* (tạo "Nghỉ cùng Milo" trong Outlook) · *Thôi* |
| **Email chờ** | Sau 10:00, có email hỏi thẳng bạn mà chưa trả lời | Liệt kê 3 email chờ lâu nhất | *Nhắc tôi lúc 16:00* · *Mở Outlook* |
| **Task kẹt** | Task dở nhiều ngày và có khoảng trống ≥ 45 phút | Đề nghị khoá giờ tập trung | *Khoá 90 phút*: chặn lịch + Teams "Không làm phiền", Milo im lặng tới hết khối |
| **Task xong** | Task chuyển sang Done | Nhảy tưng ăn mừng 3 giây | — |
| **Hết giờ tập trung** | Hết khối tập trung | Chúc mừng 3 giây | — |

### 5.3 Chăm sóc

Mỗi lần Milo chỉ nhắc 1 chuyện chăm sóc. Những chuyện khác gộp vào tổng kết cuối ngày.

| Tính năng | Khi nào | Milo rủ |
| --- | --- | --- |
| **Họp liên tục** | Vừa xong chuỗi ≥ 3 cuộc họp liền | Thở 4-4-4 cùng Milo (3 nhịp) |
| **Làm liền** | Ngồi máy 2 tiếng không nghỉ ≥ 5 phút | Thở 1 phút, uống nước |
| **Nghỉ quá ít** | Cả ngày nghỉ quá ít so với giờ đã làm | Nghỉ hẳn 15 phút, Milo canh giờ |
| **Chưa nghỉ trưa** | 12:30–14:00 mà chưa rời máy | Đi ăn thôi · Khoá 30' nghỉ trưa trong lịch |
| **Phân mảnh** | Nhảy qua lại giữa các app quá nhiều trong 1 giờ | Gom việc, tắt thông báo 30 phút |
| **Quá giờ** | Quá giờ về 30 phút mà vẫn làm | Chốt việc rồi về |

Vòng thở: vòng tròn phồng 4 giây (hít vào), giữ 4 giây, xẹp 4 giây (thở ra). Bấm *Dừng* lúc nào cũng được, vẫn tính là đã nghỉ.

### 5.4 Tính năng mở rộng

| Tính năng | Khi nào | Milo làm | Bạn trả lời |
| --- | --- | --- | --- |
| **Giữ giờ tập trung** | 1 lần/ngày, buổi sáng, khi lịch còn khoảng trống ≥ 60 phút | Đề nghị giữ khoảng trống dài nhất để tập trung | *Giữ*: tạo "Tập trung · Milo giữ chỗ" trong lịch; tới giờ Milo tự bật Không làm phiền · *Thôi* |
| **Báo cáo tuần** | Sáng thứ Hai | Tóm tắt tuần trước + 1 mẹo cho tuần mới | *Đã rõ* · *Xem chùm nho* |
| **Nghỉ ngắn** | Mỗi 50 phút ngồi máy liên tục (tối đa 6 lần/ngày) | Ló lên 5 giây: "Uống ngụm nước nha!" hoặc "Đứng dậy vươn vai 1 phút rồi làm tiếp nhé." | Không cần trả lời |
| **Hôm nay thấy sao?** | Trên thẻ tan tầm | 3 nút Vui / Bình thường / Mệt | Câu trả lời chỉ lưu trên máy, tính vào điểm mood |
| **Nghỉ giữa chuỗi họp ngày mai** | Trên thẻ tan tầm, khi mai có ≥ 3 cuộc họp liền | "Mai 13:30–16:15 có 3 cuộc họp liền" | *Giữ 10' nghỉ lúc …* |
| **Trốn khi trình chiếu** | Teams báo bạn đang trình chiếu | Trốn hẳn, kể cả chóp đuôi | — |
| **Trò chuyện** | Bất cứ lúc nào bạn muốn | Khung chat tự do, trả lời bằng AI hoặc theo từ khoá (mục 3.4) | Gõ tự do · *Thở 1 phút* · *Xong* |
| **Có mới** | Có email mới gửi thẳng cho bạn, lời mời họp mới, task mới được giao | Ló lên với thẻ nhỏ: ai gửi, tiêu đề. Nhiều thứ tới liền nhau gộp 1 thẻ. Đang họp / tập trung thì chờ ở chấm chờ. Production đọc email 5 phút/lần, Sandbox khoảng 20 giây | *Mở email* / *Xem cuộc họp* / *Mở task* · *Đã xem* (20 giây không bấm thì thu lại) |
| **Tủ đồ · phối đồ** | Mở khoá bằng thói quen tốt và theo mùa (mục 3.5) | 14 món chia 5 ô, phối nhiều món cùng lúc; sáng hôm sau Milo khoe món mới | Chuột phải Milo → *Thay đồ*, hoặc *Tủ đồ* trên dashboard, menu khay, chat "đổi đồ" |

Muốn tắt tính năng nào (vd. thấy nhắc nghỉ ngắn phiền): xem README mục **Tham chiếu biến `.env`**, nhóm `Wellbeing`.

### 5.5 Điểm mood

Mỗi ngày bắt đầu từ 92 điểm:
- **Bị trừ khi:** họp quá nhiều, họp liền, ngồi liền, quá giờ, nghỉ ít, nhảy việc, nhiều task dở, task kẹt, email chờ, hoặc bạn tự nói "Mệt" (−6).
- **Được cộng khi:** nghỉ cùng Milo, xong task, xong khối tập trung, bạn nói "Vui" (+3).

Có AI và bật chế độ *Luật + AI* (trang Mood Engine) thì AI được chỉnh thêm tối đa ±10 điểm. Trang **Bộ não Milo** ghi rõ từng khoản; rê chuột lên từng khoản để xem nghiên cứu làm căn cứ.

**Điểm lấy từ đâu:**
- Công thức theo mô hình Job Demands–Resources.
- Mỗi khoản dựa trên nghiên cứu công bố từ 2021 tới nay.
- Có bộ test chứng minh công thức luôn đúng chiều với các nghiên cứu đó.
- Chi tiết: [CO-SO-KHOA-HOC.md](CO-SO-KHOA-HOC.md).

**Kiểm chứng với chính bạn** (Sandbox / Production): bảng điều khiển → **Kiểm chứng điểm**.
- Mỗi cuối tuần trả lời 5 câu **WHO-5** (thang đo sức khoẻ tinh thần của Tổ chức Y tế Thế giới). Thứ Hai trả lời thì tính cho tuần trước.
- App so với điểm Milo trung bình tuần và hiện hệ số tương quan *r* sau ≥ 3 tuần. *r* ≥ 0,3 nghĩa là điểm Milo phản ánh khá đúng cảm nhận của bạn.
- Nút *Xuất CSV ẩn danh* để gộp dữ liệu cả nhóm khi chạy thử.
- Đây không phải công cụ chẩn đoán.

## 6. Dùng theo từng môi trường

### 6.1 Demo: xem thử trong 5 phút

1. Chọn **Demo**. Bảng điều khiển mở ở bên trái, Milo ở góc phải dưới.
2. Trang **Bắt đầu**: để công tắc *Người dùng mẫu tự bấm nút* bật, tốc độ *Vừa 120×*, bấm **Phát tiếp** nếu đang dừng. Nhìn góc màn hình: 08:58 Milo chào sáng.
3. Trang **Ngày mẫu**: bấm từng mốc để nhảy tới cảnh hay.
   - 09:25 sắp họp
   - 10:37 task kẹt
   - 13:15 dashboard
   - 16:12 thở cùng Milo
   - 18:31 chạy ra xe
4. Muốn tự bấm: tắt công tắc tự bấm, rồi bấm nút trên thẻ của Milo hoặc gõ chat.
5. Trang **Thử tình huống**: bấm bất kỳ thẻ nào để xem Milo làm tình huống đó. Bật *Đang trình chiếu* để thấy Milo trốn.
6. Trang **Milo của bạn**: tủ đồ mở full 14 món, phối thử; chọn tính cách Milo; 9 nút xem thử động tác hài.
7. Trên desktop: **chuột phải Milo** → *Thay đồ cho Milo* / *Trò chuyện với Milo* / *Tính cách Milo*.
8. Trang **Thử tình huống** → *Có mới*: bấm 3 lần để thấy email mới → lời mời họp → task mới gộp 1 thẻ.

Present: dùng trang **Kịch bản trình diễn** và làm theo [KICH-BAN-DEMO.md](KICH-BAN-DEMO.md). Demo với dữ liệu thật: [KICH-BAN-SANDBOX.md](KICH-BAN-SANDBOX.md). Để bảng điều khiển ở màn hình thứ hai, màn hình chính chỉ có Milo. Chạy thử 1 lượt trước, rồi *Làm lại từ 08:50*.

### 6.2 Sandbox: thử với dữ liệu thật

1. Chọn **Sandbox**. Trình duyệt mở đăng nhập: chọn **thulu@mindiful.onmicrosoft.com**, đồng ý quyền.
2. Trang **Tổng quan**: đợi 2 chấm xanh (Microsoft 365, Azure Boards). Azure Boards vàng thì sang trang **Kết nối**, dán PAT, *Lưu PAT*.
3. Trang **Thử tình huống** → **Tạo dữ liệu mẫu**: tạo 1 cuộc họp Teams sau 7 phút, chuỗi 3 cuộc họp liền, 6 task. Khoảng 2 phút sau Milo nhắc sắp họp.
4. Bấm nút trên thẻ để thấy hành động thật:
   - *Giữ chỗ*: Outlook có "Nghỉ cùng Milo".
   - *Khoá 90 phút*: Teams chuyển Không làm phiền.
5. *Giờ về = bây giờ + 2 phút*: xem thẻ tan tầm, bấm *Mệt*, *Về thôi*.
6. **Realtime:** từ 1 tài khoản khác, gửi email / lời mời họp Teams cho tài khoản Sandbox, hoặc giao 1 task Azure Boards. Khoảng 20–30 giây sau Milo báo thẻ **Có mới** (Sandbox đọc email mỗi ~20 giây, lịch và task ~30 giây; Production 5 / 2 / 3 phút).
7. Demo cho người xem qua Teams: bật **Hiện Milo khi chia sẻ màn hình** (*Tổng quan* → thẻ *Chế độ Sandbox*, hoặc menu khay). Mặc định tắt, giống Production.
8. Checklist đầy đủ: [KET-NOI-SANDBOX.md](KET-NOI-SANDBOX.md) mục 7. Test xong xoá các sự kiện có category **Milo** trong Outlook.
9. Muốn xem bản Production trông thế nào: *Tổng quan* → *Chế độ Sandbox* → **Chạy như Production**. Trang *Thử tình huống* ẩn đi, Milo tự chạy với ngưỡng chuẩn. Muốn ép Milo làm lại thì chuyển về **Chế độ test**.

Ở chế độ test, Sandbox dùng ngưỡng rút ngắn (vd. ngồi liền 20 phút đã nhắc) để test trong 1 buổi.

### 6.3 Production: dùng hằng ngày

1. Chọn **Production**, đăng nhập bằng tài khoản Bosch. Tick *Nhớ lựa chọn*.
2. Không cần làm gì thêm: Milo tự chào sáng, nhắc họp, im lặng khi bạn họp hoặc trình chiếu, rủ nghỉ khi cần, nhắc về.
3. Nếu thẻ ghi "Nhắc tôi lúc đó" thay cho "Giữ chỗ trong lịch": tenant chưa cấp quyền ghi lịch. Milo vẫn nhắc đúng giờ nhưng không ghi vào Outlook. Đây là đúng thiết kế.
4. Muốn Milo nói tự nhiên hơn: cấu hình AI (API key ở trang **Kết nối** hoặc trong `.env`; không bắt buộc). Với dữ liệu Bosch thật chỉ dùng dịch vụ AI đã được duyệt.
5. Muốn Milo tự chạy mỗi lần mở máy: menu khay → tick **Khởi động cùng Windows**, hoặc bảng điều khiển → **Milo của bạn** → công tắc *Khởi động cùng Windows*.
   - Tắt mặc định. Bỏ tick là Milo thôi tự chạy.
   - Milo mở thẳng đúng môi trường đã bật, không hiện màn hình chọn.
   - Mỗi lần chỉ 1 môi trường được tự chạy (Sandbox hoặc Production). Demo không có công tắc này.
   - Không cần quyền admin: Milo ghi 1 mục `Minditful.Milo` trong `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` của riêng tài khoản bạn. Cũng thấy và tắt được ở Task Manager → Startup apps.
   - Giải nén bản mới sang thư mục khác thì mở Milo 1 lần, Milo tự cập nhật đường dẫn.

## 7. Vì sao Milo không hiện?

Nhìn dải trên cùng của bảng điều khiển, hoặc trang **Bộ não Milo** → *Điều đang khiến Milo im lặng*.

| Dải trên cùng ghi | Lý do | Milo sẽ… |
| --- | --- | --- |
| Milo đang nghỉ | Chưa tới giờ làm, máy khoá, hoặc đã tan tầm | Chào sáng lần mở máy đầu tiên của ngày mai |
| Milo đang im lặng · vì đang họp | Teams báo đang trong cuộc gọi, hoặc lịch đang có họp. Chóp đuôi mờ đi | Hết họp chờ 2 phút rồi mới nói |
| … vì toàn màn hình / không làm phiền | Bạn đang cần tập trung | Dồn lời nhắc thành chấm chờ |
| … vì giờ tập trung | Bạn đang trong khối tập trung (tự bấm *Tập trung 30 phút*, *Khoá 90 phút*, hoặc giờ đã giữ trong lịch). Milo ngủ trên chóp đuôi | Hết giờ thì tỉnh dậy, báo "… phút sâu xong rồi!" |
| Milo đang trốn | Bạn đang trình chiếu | Hiện lại khi thôi trình chiếu |
| Milo đang ẩn ở góc màn hình | Không có gì cần nói, hoặc đã nhắc đủ số lần (tối đa 3 lần/giờ, 10 lần/ngày, cách nhau ≥ 15 phút) | Chờ đúng lúc |

Không thấy cả chóp đuôi:
1. Kiểm tra Milo có đang ở góc khác không (trang *Milo của bạn* → góc màn hình).
2. Kiểm tra biểu tượng ở khay còn không. Nếu mất thì app đã tắt, mở lại.

## 8. Riêng tư

- Milo **chỉ lưu số liệu** trên máy bạn (điểm, số phút, số lần). Không lưu tiêu đề hay nội dung email, cuộc họp, task.
- Dữ liệu **tự xoá theo tuần**. Máy chỉ giữ tuần này và tuần trước để vẽ chùm nho và so sánh.
- Muốn xoá ngay: *Milo của bạn* → **Xoá toàn bộ dữ liệu thống kê ngay**.
- AI (nếu bật) chỉ nhận tên tình huống, con số và nhãn, vd. "3 cuộc họp liền, 2h40", "loại cuộc họp: Ra quyết định". Không bao giờ nhận tiêu đề, agenda hay nội dung. Tiêu đề và agenda cuộc họp chỉ được đọc ngay trên máy để đoán loại.
- Milo **không đọc phím bạn gõ**, chỉ biết lúc nào có thao tác.
- Ở Sandbox/Production, người xem màn hình chia sẻ **không thấy Milo** (Sandbox có công tắc để hiện khi demo).
- PAT, API key, đăng nhập được mã hoá theo tài khoản Windows của bạn.

## 9. Hỏi nhanh

**Milo nhắc nhiều quá?** Bấm *Không cần*. Milo tự thưa dần những chuyện bạn hay từ chối (xem *Milo của bạn* → *Milo tự điều chỉnh theo 7 ngày*). Tắt hẳn tính năng mở rộng trong `.env` nhóm `Wellbeing`.

**Muốn đổi giờ về hôm nay?** Giờ về tính tự động từ lúc mở máy. Muốn giờ cố định: README → Tham chiếu biến `.env` → `WorkDay__Mode=Fixed`.

**Milo che mất nút của app khác?** Kéo chóp đuôi sang góc khác. Chỗ trống quanh Milo cho chuột bấm xuyên qua.

**Sửa `.env` mà không thấy đổi?** Thoát hẳn Milo ở khay rồi mở lại.

**Đang present mà Milo nhảy ra?** Ở Production/Sandbox Milo tự trốn khi Teams báo đang trình chiếu. Ở Demo bật công tắc *Teams: đang trình chiếu* trên bảng điều khiển.

**Gửi mail mà Milo không báo?** Mail phải gửi thẳng cho bạn (ô To) từ tài khoản khác. Những thứ có sẵn lúc mở app không được báo lại. Đang họp hay tập trung thì thẻ chờ ở chấm chờ. Production đọc email 5 phút/lần, Sandbox ~20 giây; bấm *Làm mới ngay* nếu không muốn chờ.

**Milo đọc tiêu đề cuộc họp của tôi à?** Có, nhưng chỉ **ngay trên máy** để đoán loại cuộc họp (trình bày, ra quyết định, ngồi nghe, làm việc nhóm, 1:1) và đưa gợi ý trên thẻ Sắp họp. AI chỉ nhận **nhãn** đó, không nhận tiêu đề hay agenda.

**Milo có tự đặt lịch, đổi trạng thái Teams, mở Outlook mà không hỏi không?** Không. Mọi hành động chỉ chạy **sau khi bạn bấm** nút trên thẻ:
- *Giữ chỗ* / *Khoá 30' nghỉ trưa* / *Giữ 10' nghỉ ngày mai* → tạo sự kiện trong Outlook (cần quyền ghi lịch; thiếu quyền thì chỉ nhắc).
- *Khoá 90 phút* / *Tập trung 30 phút* → chặn lịch "Tập trung" + đặt Teams *Không làm phiền*.
- *Giữ* ở thẻ Giữ giờ tập trung → giữ chỗ trong lịch; **tới giờ đó** Milo tự bật Không làm phiền (bạn đã đồng ý từ trước).
- **Hết khối tập trung** Milo tự trả Teams về trạng thái tự động, chỉ khi chính Milo đã bật Không làm phiền.
- *Tham gia*, *Mở slide*, *Mở Outlook*, *Mở email*, *Mở task* → mở Teams / Outlook / Azure Boards.

Ở Demo, công tắc *Người dùng mẫu tự bấm nút* bấm hộ theo kịch bản, và mọi hành động chỉ ghi vào nhật ký, không đụng dịch vụ thật.

**Không thích Milo làm trò?** Chuột phải Milo → *Tính cách Milo* → **Dễ thương**.

**Muốn Milo luôn chạy khi mở máy?** Menu khay → *Khởi động cùng Windows* (Sandbox/Production).

**Có lỗi?** Dải trên cùng và trang *Tổng quan* ghi lý do bằng tiếng Việt. App gặp lỗi lạ thì xem `%LOCALAPPDATA%\Minditful\crash.log`.
