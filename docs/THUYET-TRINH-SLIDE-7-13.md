# Lời thuyết trình · Slide 7 → 13

Phần kỹ thuật của deck `Minditful_Milo_Pitch_v3.pptx`, từ **Kiến trúc** tới **Demo**. Tổng thời lượng khoảng **4 phút 20 giây** (tối thiểu 3 phút nếu bỏ phần *[có thể bỏ]*, tối đa 5 phút nếu nói chậm). Tốc độ tính theo khoảng 140 từ/phút.

| Slide | Nội dung | Thời lượng | Cộng dồn |
| --- | --- | --- | --- |
| 7 | Kiến trúc: ba project, một bộ não | 40" | 0:40 |
| 8 | Bên trong bộ não | 70" | 1:50 |
| 9 | Ba môi trường | 35" | 2:25 |
| 10 | Flow một ngày | 25" | 2:50 |
| 11 | Đội agents | 45" | 3:35 |
| 12 | Hướng phát triển | 35" | 4:10 |
| 13 | Mở màn demo | 10" | 4:20 |

Quy ước: chữ thường là **lời nói**, dòng bắt đầu bằng 👉 là **thao tác / chỉ vào đâu**, *[có thể bỏ]* là đoạn cắt được khi thiếu giờ.

---

## ⚠️ Sửa slide trước khi present

Deck được làm trước các bản cập nhật cuối, một số con số đã cũ. Lời nói bên dưới dùng số **hiện tại**. Nên sửa slide cho khớp, nếu không kịp thì cứ nói theo lời bên dưới.

| Slide | Đang ghi | Sửa thành |
| --- | --- | --- |
| 5, 7, 8, 11, 12 | "Claude", "Claude API", "ClaudeLineWriter", "Hybrid (Claude ±10)" | "AI (Claude hoặc Groq · Qwen)", "IMiloLlm", "Hybrid (AI ±10)" |
| 7, 8 | "Rule Engine (16 + 3 case)", "16 case (+3 mở rộng)" | "17 + 5 case" (22 tình huống) |
| 7, 11 | "160 unit test" | "265 unit test" |
| 9 | "Trình diễn đủ 19 tình huống trong khoảng 15 phút" | "Đủ 22 tình huống, 28 bước; bản present 13 phút" |
| 12 | "Bộ cài … tự chạy cùng Windows" | Giữ, nhưng *tự chạy cùng Windows* **đã có** (menu khay) |

---

## Slide 7 · Ba project, một bộ não (40")

👉 Chỉ lần lượt 3 cột Integrations → Core → App, rồi 4 dòng nguyên tắc ở dưới.

> Về kiến trúc: Milo có ba project nhưng chỉ **một bộ não**.
>
> Ở giữa là **Core**: toàn bộ luật, hàng đợi, điều phối, điểm mood và hoạt ảnh. Core không phụ thuộc Windows, không gọi mạng, nên chạy test được trên mọi máy. Hiện có **265 test tự động**.
>
> Bên cạnh là **Integrations**: nói chuyện với Microsoft Graph cho Teams, Outlook; Azure DevOps cho Boards; AI; và SQLite lưu trên máy.
>
> Còn **App** là WPF .NET 8: một cửa sổ trong suốt, chuột đi xuyên qua, Milo chỉ chiếm một góc nhỏ màn hình.
>
> Bốn nguyên tắc: một bộ não cho mọi nguồn dữ liệu. Engine chạy đồng bộ theo từng giây, còn gọi ra ngoài thì bất đồng bộ, nên mạng chậm Milo cũng không bị treo. Thiếu quyền thì chỉ tắt đúng tính năng đó. Và riêng tư mặc định: không có server riêng.

---

## Slide 8 · Từ tín hiệu đến một lời nhắc đúng lúc (70")

👉 Đi theo 5 ô số 1 → 5, sau đó chỉ ô Mood Engine, cuối cùng ô Lớp 2.

> Bên trong bộ não là một dây chuyền năm bước.
>
> **Một**: lịch, Teams presence, email, task và hoạt động máy (rảnh, toàn màn hình, khoá máy) được gom thành một ảnh chụp.
>
> **Hai**: mỗi phút, Rule Engine xét **22 tình huống**: 17 theo tài liệu kịch bản hành vi, cộng 5 mở rộng như trò chuyện hay báo email mới. Tình huống nào đủ điều kiện thì vào hàng đợi, ưu tiên từ P0 tới P5.
>
> **Ba**: mỗi giây, bộ điều phối kiểm tra **cổng im lặng** (đang họp, trình chiếu, tập trung, Không làm phiền) rồi tới **ngân sách**: hai lời nhắc cách nhau ít nhất 15 phút, tối đa 3 lời một giờ, 10 lời một ngày.
>
> **Bốn**: qua hết mới tới lượt Milo ló ra, nói **một câu**, bạn chọn bằng **một chạm**.
>
> **Năm**: chỉ khi bạn đồng ý, Milo mới giữ chỗ trên Outlook hay bật DND Teams. **Không có gì tự động sau lưng người dùng.**
>
> Song song là **Mood Engine**: 92 trừ điểm phạt, cộng điểm thưởng. Các khoản phạt theo mô hình JD-R trong nghiên cứu: họp quá 3 tiếng, họp liền, làm liền quá 90 phút, quá giờ, thiếu nghỉ, nhảy việc liên tục. Thưởng khi bạn nghỉ, xong task, có giờ tập trung.
>
> Lớp **AI là tuỳ chọn**: viết câu thoại, trò chuyện, và có thể chỉnh điểm tối đa ±10. AI chỉ nhận **con số và nhãn**, không bao giờ nhận tiêu đề hay nội dung email. Quá 2,5 giây hay hết lượt thì quay về luật: không có AI, app vẫn chạy đủ.

*[có thể bỏ]*
> Nhóm kiểm chứng AI bằng 3 bộ test. Model Qwen đang dùng xếp đúng 11 trên 11 cặp ngày nặng – nhẹ, và tương quan 0,95 với điểm luật.

---

## Slide 9 · Một bộ não, ba môi trường (35")

👉 Quét 3 cột Demo → Sandbox → Production, dừng ở câu cuối slide.

> Cùng bộ não đó chạy ba môi trường, chỉ khác nguồn thời gian và dữ liệu.
>
> **Demo** là một ngày mẫu Thứ Năm 24/9, không cần mạng, tua nhanh 300 lần, đủ 22 tình huống trong 28 bước.
>
> **Sandbox** là tenant thử của nhóm, API thật: gửi một email thật thì khoảng 20 giây sau Milo báo.
>
> **Production** là tenant Bosch: ngưỡng chuẩn, mặc định chỉ xin quyền đọc, và Milo luôn ẩn khi bạn chia sẻ màn hình.
>
> Đổi môi trường ngay từ menu khay, không cần cài lại.

---

## Slide 10 · Flow một ngày làm việc (25")

👉 Lướt trục thời gian từ 08:58 tới 18:00, dừng ở 4 ô nhịp dưới cùng.

> Một ngày cùng Milo: chào sáng với bản tin, nhắc trước cuộc họp 5 phút, trưa xem dashboard trái cây, nghỉ sau chuỗi họp 3 tiếng 20, thở 4-4-4, và 6 giờ chiều thì tổng kết ngày, "Về thôi".
>
> Nhịp lúc nào cũng vậy: **ló ra đúng lúc, nói một câu, bạn chọn một chạm, Milo tự rút lui.** Phần này mình cho xem trực tiếp ở demo.

---

## Slide 11 · Đội agents cùng xây sản phẩm (45")

👉 Đi theo 3 cột Phân tích → Code song song → Test, kết ở dòng "Vai trò của Thư".

> Về cách làm: sản phẩm được xây bởi một đội AI agents, **Thư điều phối**.
>
> **Bước một**: agent phân tích đọc đề bài, prototype HTML và tài liệu kịch bản hành vi, tách ra từng tình huống. Agent chốt nền tảng **WPF .NET 8** thay vì Electron: nhẹ hơn và gọi thẳng được Windows (biết khoá máy, toàn màn hình, ẩn khỏi màn hình chia sẻ). Rồi ra plan ba project.
>
> **Bước hai**: ba agent code song song, mỗi agent một project. Ranh giới project cũng là ranh giới việc, nên ít dẫm chân nhau.
>
> **Bước ba**: kiểm chứng. 265 test tự động; 20.000 bộ số ngẫu nhiên để chứng minh công thức mood luôn đi đúng chiều nghiên cứu; 3 bộ kiểm chứng AI; và test tay theo checklist trên Sandbox.
>
> Người giữ những quyết định quan trọng: **chia việc, duyệt từng kết quả, chốt ngưỡng và trải nghiệm.**

---

## Slide 12 · Chính xác hơn, và cài được trên máy Bosch (35")

👉 Chỉ thanh 4 bước ở trên, rồi 3 cột.

> Tiếp theo là ba bước.
>
> **Một, onboard máy Bosch**: IT đăng ký app trên Entra ID và duyệt quyền. Bản hiện tại chỉ xin quyền đọc; thiếu quyền nào thì tự tắt đúng tính năng đó. Khởi động cùng Windows đã có sẵn; còn lại là ký số bộ cài.
>
> **Hai, pilot**: ít nhất 8 người trong 4 tuần. Mỗi tuần người dùng làm bảng **WHO-5** ngay trong app để so với điểm của Milo, mục tiêu tương quan từ 0,3 trở lên. Nhóm nói thẳng: trọng số hiện tại lấy từ nghiên cứu, **chưa hiệu chuẩn trên người dùng thật**. Pilot là để làm việc đó.
>
> **Ba, mở rộng**: thêm nguồn như Jira chỉ cần viết một client mới, không đụng bộ não; bật chế độ Luật + AI sau khi qua kiểm chứng.

---

## Slide 13 · Demo (10")

👉 Nói xong chuyển sang màn hình Windows, mở bảng điều khiển Demo → *Kịch bản trình diễn*.

> Giờ mời mọi người xem Milo chạy thật trên Windows. Phần một là ngày mẫu ở tốc độ 300 lần; phần hai mình chuyển sang Sandbox, gửi email thật và xem Milo phản ứng.

Sau câu này chạy theo [KICH-BAN-DEMO.md](KICH-BAN-DEMO.md) mục **0.3** (Demo, 13 phút) rồi mục **0.4** (Sandbox, 9 phút). 6 mốc trên slide 13 ứng với các bước 1, 2, 3, 8, 9 và 11–12 của *Kịch bản trình diễn*.

---

## Câu hỏi dễ gặp ở phần này

| Câu hỏi | Trả lời ngắn |
| --- | --- |
| Milo có tự làm gì mà không hỏi không? | Không. Ghi lịch, bật DND, mở Outlook/Boards chỉ xảy ra sau khi bạn bấm. Production mặc định chỉ có quyền đọc |
| Dữ liệu đi đâu? | Chỉ số liệu lưu trên máy (SQLite), tự xoá theo tuần. Không có server riêng. AI chỉ nhận con số và nhãn |
| Điểm mood có chính xác không? | Công thức theo nghiên cứu JD-R, đã chứng minh đúng chiều bằng 20.000 bộ số; chưa hiệu chuẩn trên người thật, sẽ pilot với WHO-5 |
| Không có AI thì sao? | Chạy đủ bằng luật và câu mẫu; AI chỉ làm lời nói tự nhiên hơn và chỉnh điểm ±10 |
| Đánh giá cuộc họp thế nào mà không đọc nội dung? | Tiêu đề và agenda chỉ đọc **trên máy** để đoán loại (trình bày, ra quyết định, ngồi nghe…); ra ngoài chỉ có nhãn |
| Vì sao WPF mà không Electron? | Nhẹ hơn và gọi trực tiếp Windows: biết khoá máy, toàn màn hình, ẩn khỏi màn hình chia sẻ |
| Lên Bosch cần gì? | IT đăng ký app Entra ID + admin consent, cấp PAT hoặc Entra cho Azure Boards. Không đổi code |
