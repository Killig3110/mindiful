# Kịch bản hành vi Milo

Sep 26, 2026 · @Le Minh Quan

Milo ẩn gần như cả ngày, chỉ chừa chóp đuôi ở góc màn hình. Mỗi lần xuất hiện là một episode Vào → Ở lại → Ra do bộ Điều phối chọn: không bao giờ chen vào cuộc họp, tối đa 1 lời nhắc chủ động mỗi 15 phút. Tài liệu này nối đặc tả Minditful với 19 board "Một ngày cùng Milo"; mỗi luật ghi rõ nguồn (§ = mục đặc tả, B = số board).

Xem toàn bộ kịch bản chạy thật trên một ngày mẫu tại [prototype Milo sống](https://claude.ai/artifact/UTn1g3vX9ieZi4A7XNwhii).

## 1. Quyết định đã chốt

Chỗ nào đặc tả và board nói khác nhau, bảng dưới ghi bên được chọn. Riêng mục 1 và 3 đã được nhóm xác nhận ngày 26/9.

| Chủ đề | Đặc tả nói | Board nói | Chốt |
| --- | --- | --- | --- |
| Milo hiện bao lâu | Ẩn phần lớn thời gian (§4.5) | Ngồi góc 90% thời gian (B03) | **Ẩn.** Ở góc chỉ còn chóp đuôi, để giữ được thao tác rê chuột/bấm của B03. Milo ghé ngang 30–60 phút một lần |
| Danh mục case | 6 case (§8b) | 3 case: họp liên tục, quá giờ, chưa nghỉ trưa (B13) | Gộp thành 6 case chăm sóc + 7 episode hỗ trợ/xã giao. Workload spike chỉ trừ điểm và vào bản tin sáng |
| Cách rời đi | Climb-out (§4.2) | Leo xuống 4 nhịp (B15) | Luôn leo xuống. Có 3 biến thể: bản ngắn, bản nhanh khi bị ngắt, chạy ra xe lúc tan tầm |
| Nhiều case cùng lúc | Chọn 1 case nặng nhất, phần còn lại vào tổng kết (§8b) | — | Giữ nguyên cho case chăm sóc. Episode hỗ trợ không gộp mà xếp hàng chờ |
| Thời điểm nhắc | Không chen giữa cuộc họp (§4.3) | Chỉ ở khoảng trống, đợi 2 phút sau họp (B11, B12) | Có cổng im lặng. Sau họp đợi 2 phút; nếu cuộc kế tiếp bắt đầu trong vòng 5 phút thì bỏ qua |
| Bấm "Không cần" | Ẩn case đó 1 tiếng (§4.3b) | Bị từ chối 2 lần/ngày thì giãn nhắc (B13) | Áp dụng cả hai |
| Office Vibe | Radar 3 trục (§4.4) | 3 hàng trái cây (B06) | Theo board, vẫn giữ 3 trục: Tập trung / Năng lượng / Căng thẳng |
| Multi-ring tuần | Các vòng đồng tâm (§4.4) | Chùm nho 7 ngày (B05) | Chùm nho, mỗi quả là 1 ngày. Dữ liệu lưu cuối ngày như §4.4 |
| Ngưỡng mood | Chưa chốt (§10) | 72 = cân bằng, 58 = mệt dần (B05, B10) | ≥80 mọng · 60–79 cân bằng · 40–59 mệt dần · dưới 40 kiệt sức |
| Âm thanh | Có nhạc thư giãn (§4.3) | Im lặng mặc định (B17) | Mặc định im lặng, người dùng bật trong cài đặt |

## 2. Kiến trúc hành vi

Mọi hành vi đi qua một đường duy nhất. Chỉ **Điều phối** được quyết định Milo có xuất hiện hay không; Rule Engine chỉ đề xuất. Đây là Lớp 1 của §8b, còn LLM (Lớp 2) chỉ nằm trong hộp Animation + lời thoại.

&#91;embedded content: kiến trúc hành vi · 6 tầng, 1 vòng phản hồi\]

Mood Engine còn điều khiển trực tiếp dáng đứng của Milo (mục 11) mà không cần qua Điều phối, vì đổi dáng không làm phiền ai.

**Tín hiệu tính mỗi 60 giây** (animation chạy riêng, không chờ nhịp này):

| Tín hiệu | Cách tính | Nguồn |
| --- | --- | --- |
| Đang họp | Presence activity là InACall, InAConferenceCall, Presenting hoặc InAMeeting | Graph `/me/presence` |
| Toàn màn hình | Cửa sổ foreground phủ kín màn hình (trình chiếu, xem video) | OS, active window |
| Đang gõ | Idle dưới 3 giây | `getSystemIdleTime()` |
| Một lần nghỉ | Idle ≥ 5 phút hoặc khoá máy ≥ 5 phút, **không** trong cuộc họp (ngồi im trong call không tính là nghỉ) | `powerMonitor` lock/unlock, suspend/resume |
| Chuỗi làm liền | Số phút từ lần nghỉ gần nhất, họp cũng tính là làm | Tổng hợp |
| Tổng nghỉ hôm nay | Cộng dồn các lần nghỉ trong khung giờ làm | Tổng hợp |
| Đã nghỉ trưa | Có lần nghỉ ≥ 20 phút trong khoảng 11:00–14:00 | Tổng hợp |
| Chuỗi họp | Các cuộc họp liền nhau, khoảng cách dưới 5 phút | Graph `/me/calendarView` |
| Họp kế tiếp | Số phút tới cuộc họp gần nhất | Graph `/me/calendarView` |
| Phút quá giờ | Số phút có thao tác sau giờ kết thúc khung đã đăng ký | OS + cài đặt onboarding |
| Chuyển việc/giờ | Số lần đổi giữa họp, IDE và việc khác trong 60 phút gần nhất | Active window + presence |
| Task kẹt | Work item của bạn ở Active/In Progress ≥ 3 ngày làm việc | Azure DevOps WIQL (PAT) |
| Email chờ | Bạn ở ô To, có dấu "?" hoặc cờ, chưa trả lời, chờ ≥ 1 ngày làm việc | Graph `/me/messages` |
| Workload | Số task In Progress so với trung bình sprint | Azure DevOps |

## 3. Trạng thái hiện diện

Phần lớn thời gian Milo ở trạng thái **Ẩn**: trên màn hình chỉ còn một chóp đuôi cáo khoảng 28px ở góc phải dưới, sát taskbar. Chóp đuôi này đảm nhận các việc mà B03 giao cho Milo khi ngồi góc: có quầng thở 5 giây/nhịp, màu theo mood, rê chuột vào được, bấm vào được, kéo sang góc khác được.

&#91;embedded content: trạng thái hiện diện · 6 trạng thái\]

Mọi nhánh đều kết thúc ở Ẩn. Chỉ Điều phối (mục 4) hoặc chính người dùng mới đưa Milo sang trạng thái Đang nói.

| Từ | Sự kiện | Sang | Clip |
| --- | --- | --- | --- |
| Nghỉ làm | Mở khoá/đăng nhập lần đầu trong ngày (sau 04:00) | Đang nói (Chào sáng) | Bám mép → leo lên → Hello |
| Ẩn | Rê chuột lên chóp đuôi 600ms | Ló đầu | Ngóc đầu 300ms + thì thầm |
| Ló đầu | Chuột rời đi | Ẩn | Tụt xuống 300ms |
| Ẩn / Ló đầu | Bấm | Đang nói (Dashboard) | Nảy lên 1 → 1.15 → 1 |
| Ẩn | Hẹn giờ ghé ngang 30–60 phút, hàng đợi rỗng, không gõ phím | Ghé ngang | Leo lên chậm, nhìn quanh, leo xuống bản ngắn |
| Ẩn | Điều phối chọn một episode | Đang nói | ClimbIn (11 khung, 8fps) |
| Bất kỳ | Cổng im lặng bật (họp, toàn màn hình, giờ tập trung, Không làm phiền) | Im lặng | Thụt xuống nhanh 0.7s nếu đang hiện; đuôi cũng ẩn |
| Im lặng | Cổng tắt, không có lời nhắc chờ | Ẩn | Chóp đuôi hiện lại (fade 300ms), Milo **không** nhảy ra (B12) |
| Im lặng | Cổng tắt, trống ≥ 2 phút, có lời nhắc, cuộc họp sau cách ≥ 5 phút | Đang nói | JumpIn: bật vòng cung từ mép phải (B12) |
| Đang nói | Xong / Để sau / đóng dashboard | Ẩn | Leo xuống đầy đủ (B15) |
| Đang nói | Không cần | Ẩn | Leo xuống bản ngắn, ngó lại 1 nhịp |
| Đang nói | Hết giờ chờ mà không trả lời | Ẩn + chấm số trên đuôi | Thẻ thu vào Milo, leo xuống bản ngắn |
| Đang nói (Tan tầm) | Về thôi | Nghỉ làm | Chạy ra xe |
| Nghỉ làm | Vẫn thao tác liên tục 15 phút sau "Về thôi" | Ẩn | Chóp đuôi hiện lại, case Quá giờ được phép chạy |
| Bất kỳ | Khoá máy / sleep | Nghỉ (ẩn cả đuôi) | — ; mở lại thì về trạng thái trước đó |

Cửa sổ overlay của Milo bật chế độ content protection (`setContentProtection(true)` trong Electron). Khi bạn share màn hình, người xem không thấy Milo hay chấm chờ.

## 4. Điều phối: khi nào Milo được nói

Mỗi 60 giây, Điều phối chạy 8 bước theo đúng thứ tự dưới đây và chọn **nhiều nhất 1** episode:

1. **Nhận ứng viên.** Rule Engine trả về các case đang đủ điều kiện, kèm số liệu và mức độ.
2. **Lọc theo trí nhớ.** Bỏ case đang bị Để sau hoặc Không cần, case đã hết lượt trong ngày/buổi, và case trùng lần vừa nhắc (cùng chuỗi họp, cùng task, cùng cuộc họp).
3. **Xếp hàng.** Mỗi case giữ 1 chỗ trong hàng đợi và luôn mang số liệu mới nhất. Case hết hạn thì bị xóa, ví dụ Sắp họp hết hạn khi cuộc họp bắt đầu.
4. **Cổng im lặng.** Nếu cổng đang đóng: không giao gì, góc màn hình chỉ hiện chấm "N lời nhắc đang chờ" (B11).
5. **Ổn định sau họp.** Vừa hết họp thì đợi 2 phút. Nếu cuộc kế tiếp bắt đầu trong vòng 5 phút thì giữ nguyên hàng đợi, chờ khoảng trống sau (B12).
6. **Ngân sách.** Hai lần nhắc chủ động cách nhau ≥ 15 phút; ≤ 3 lần/giờ; ≤ 10 lần/ngày.
7. **Cổng gõ phím.** Đang gõ thì chờ khoảng dừng ≥ 3 giây. Gõ liên tục quá 5 phút thì Milo chỉ ló đầu kèm nhãn gọn "Milo có lời nhắn", không bung thẻ. Bấm vào nhãn thì mới mở.
8. **Chọn 1.** Lấy case có ưu tiên cao nhất; ngang ưu tiên thì lấy mức độ nặng hơn, rồi tới case vào hàng trước. Nếu case được chọn là case chăm sóc, các case chăm sóc khác đang chờ sẽ được gộp vào tổng kết cuối ngày (§8b). Case hỗ trợ thì tiếp tục chờ.

Ba luật hoãn bổ sung (đã kiểm chứng trên prototype):

- **Rời máy:** người dùng idle và không trong cuộc họp thì Milo hoãn giao mọi thứ, trừ P0, vì Milo không nói với màn hình trống.
- **Đang rê chuột lên đuôi:** Milo không chen lời nhắc vào lúc người dùng đang tương tác.
- **Case hết đúng:** mỗi phút Milo kiểm tra lại điều kiện của từng case trong hàng đợi. Case nào không còn đúng thì bị xóa, ví dụ Nghỉ quá ít sẽ bị xóa sau khi bạn đã nghỉ trưa 40 phút.

### Cổng im lặng

| Cổng | Điều kiện | Milo | Lời nhắc mới |
| --- | --- | --- | --- |
| Đang họp | Presence InACall, InAConferenceCall, Presenting, InAMeeting | Ẩn cả đuôi | Vào hàng đợi, hiện chấm chờ |
| Toàn màn hình | App foreground phủ kín màn hình | Ẩn cả đuôi | Vào hàng đợi, hiện chấm chờ |
| Giờ tập trung | Khối tập trung do Milo khoá (B09) đang chạy | Ẩn cả đuôi | Vào hàng đợi, hiện chấm chờ |
| Không làm phiền | Người dùng tự đặt Teams sang Do not disturb | Ẩn cả đuôi | Vào hàng đợi, hiện chấm chờ |
| Khoá máy | Lock, sleep, suspend | Không hiện gì | Vào hàng đợi, không chấm |
| Nghỉ làm | Trước lần mở máy đầu ngày, hoặc sau "Về thôi" | Không hiện gì | Chỉ case Quá giờ được xét |

Cổng im lặng chỉ có một ngoại lệ: người dùng tự bấm vào chấm chờ. Khi đó lời nhắc mở ngay, kể cả đang trong cuộc họp.

### Mức ưu tiên

| Mức | Episode | Ngân sách | Cổng gõ phím |
| --- | --- | --- | --- |
| P0 | Người dùng tự mở: dashboard, bấm chấm chờ, chat | Miễn | Miễn |
| P1 | Chào sáng, Sắp họp | Miễn | Miễn |
| P2 | Tan tầm, Họp liên tục, Nghỉ quá ít, Quá giờ (mức cao) | Tính | Chờ |
| P3 | Làm liền không nghỉ, Chưa nghỉ trưa, Phân mảnh, Quá giờ (mức trung bình), Nhắc lại tan tầm | Tính | Chờ |
| P4 | Lịch kín, Task kẹt, Email chờ | Tính | Chờ |
| P5 | Task xong, Chào hỏi ngẫu nhiên | Task xong miễn, Chào hỏi tính | Chờ |

### Ngắt giữa chừng

- Milo đang nói mà cổng im lặng bật, ví dụ bạn bấm Join: thẻ thu thành chấm, Milo thụt xuống nhanh 0.7s, episode quay lại hàng đợi. Riêng Sắp họp thì bị xóa luôn vì đã hết hạn.
- Sắp họp (P1) được chen ngang một thẻ P3–P5 đang chờ phản hồi. Thẻ bị chen quay lại hàng đợi và **không** bị tính là bị bỏ qua.
- Ngoài hai trường hợp trên, episode không bao giờ bị cắt ngang: episode nào đã bắt đầu thì chạy tới hết.

## 5. Danh mục episode

Milo có 16 episode có lời. Ngoài ra có 3 hành vi không lời là Ló đầu, Ghé ngang và Mệt dần; ba hành vi này không bị tính vào ngân sách làm phiền.

| # | Episode | Nhóm | Nguồn | Kích hoạt | Ưu tiên | Giới hạn |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Chào sáng + bản tin | Xã giao | B01, B02, §4.5 | Mở khoá/đăng nhập lần đầu sau 04:00 | P1 | 1/ngày |
| 2 | Sắp họp | Hỗ trợ | B04 | 5 phút trước sự kiện có isOnlineMeeting | P1 | 1/cuộc họp |
| 3 | Lịch kín | Hỗ trợ | B07 | Trong 4 giờ tới có ≥ 3 cuộc họp liền nhau, cách nhau dưới 5 phút | P4 | 1/buổi |
| 4 | Email chờ | Hỗ trợ | B08 | Có email hỏi thẳng bạn đã chờ ≥ 1 ngày làm việc | P4 | 1/buổi |
| 5 | Task kẹt, đề xuất khoá giờ tập trung | Hỗ trợ | B09 | Work item Active ≥ 3 ngày làm việc | P4 | 1/ngày/task |
| 6 | Task xong | Hỗ trợ | B18 | Work item chuyển sang Done | P5 | Không giới hạn |
| 7 | Họp liên tục | Chăm sóc | B13, §8b | ≥ 3 cuộc họp liền nhau vừa kết thúc trong 60 phút qua | P2 | 3/ngày, 1/chuỗi họp |
| 8 | Quá giờ | Chăm sóc | B13, §8b | Còn thao tác hơn 30 phút sau giờ kết thúc | P2/P3 | 4/ngày |
| 9 | Chưa nghỉ trưa | Chăm sóc | B13, B18 | 12:30–14:00 mà chưa có lần nghỉ ≥ 20 phút kể từ 11:00 | P3 | 2/ngày |
| 10 | Làm liền không nghỉ | Chăm sóc | §8b | Không có lần idle ≥ 5 phút trong suốt ≥ 120 phút | P3 | 3/ngày, 1/chuỗi |
| 11 | Nghỉ quá ít | Chăm sóc | §8b | Đã làm ≥ 3 giờ và tổng nghỉ dưới 50% mức kỳ vọng | P2 | 2/ngày |
| 12 | Phân mảnh | Chăm sóc | §8b, B09 | Hơn 8 lần chuyển việc trong 1 giờ | P3 | 2/ngày |
| 13 | Chào hỏi ngẫu nhiên | Xã giao | §4.2 | Ngẫu nhiên trong 10:00–16:30, điểm ≥ 60, ≥ 2 giờ từ lần nói trước | P5 | 2/ngày |
| 14 | Tan tầm + tổng kết | Xã giao | B16, §4.5 | Đúng giờ kết thúc khung đã đăng ký | P2 | 1/ngày |
| 15 | Nhắc lại tan tầm | Xã giao | B16 | 30 phút sau khi bấm "Thêm 30 phút" | P3 | 1/ngày |
| 16 | Dashboard | User mở | B05, B06, §4.4 | Bấm chóp đuôi hoặc bấm Milo | P0 | — |

Workload spike (§8b) không có episode riêng. Nó trừ điểm mood và thêm một dòng vào bản tin sáng, ví dụ: "Hôm nay 6 task đang mở, nhiều hơn thường lệ. Chọn 1 việc quan trọng nhất nhé?"

## 6. Kịch bản mở đầu và kết thúc ngày

Mỗi episode trong các mục 6–9 viết theo cùng một khung: Điều kiện → Vào → Nói → Phản hồi → Ra. Câu thoại trong ngoặc kép là template mặc định; khi có Lớp 2 thì LLM viết lại (mục 14).

### 6.1 Chào sáng + bản tin (B01, B02)

| Nhịp | Kịch bản |
| --- | --- |
| Điều kiện | Lần mở khoá, đăng nhập hoặc resume đầu tiên sau 04:00. Nếu lúc đó đang họp thì đợi cổng im lặng mở. |
| Vào (6 giây) | 0–1.8s: hai bàn chân bám mép taskbar, chỉ ló tai và mắt, đung đưa ±3°. 1.8–3.4s: kéo người lên 2 nhịp, hơi chật vật cho đáng yêu. Sau đó nhảy tưng 2 lần, chữ "Hello!" bật ra kèm vài hạt lấp lánh. Bấm vào Milo lúc đang leo thì bỏ qua đoạn leo, nhảy thẳng tới Hello. |
| Nói | Thẻ mọc lên sau Hello (fade + scale 0.9 → 1, 240ms), Milo vẫy chậm. "Chào buổi sáng! Hôm nay có 3 cuộc họp, cuộc đầu lúc 9:30. Milo xem qua giúp bạn rồi nè:" + 2 dòng: **7** email chưa đọc (Outlook), **2** task đang dở (Azure Boards). Thêm tối đa 1 dòng nếu có: tổng kết hôm qua khi hôm qua tắt máy ngang, hoặc cảnh báo workload spike. |
| Phản hồi | **Đã rõ** → leo xuống. **Nhắc lúc 10h** → hẹn lại đúng 1 lần, không lặp, rồi leo xuống. Không chạm 20 giây → thẻ thu nhỏ vào Milo, Milo leo xuống bản ngắn, chóp đuôi mang chấm "1" để mở lại được tới 10:00. |
| Ra | Leo xuống đầy đủ (mục 8.4). Từ lần sau trong ngày Milo chỉ dùng ClimbIn thường, không Hello. |
| Dữ liệu | `/me/calendarView` hôm nay; `/me/messages` đếm isRead = false; WIQL đếm task của bạn ở In Progress; tổng kết hôm qua từ lịch sử local. |

Bản tin sáng chỉ hiện 1 lần/ngày và không nằm trong dashboard (B02).

### 6.2 Tan tầm + tổng kết (B16)

| Nhịp | Kịch bản |
| --- | --- |
| Điều kiện | Đúng giờ kết thúc khung đã đăng ký (17:00 / 18:00 / 19:00). Đang họp thì đợi họp xong (luật B11). |
| Vào | ClimbIn rồi vươn vai (clip mới, 6 khung). |
| Nói | Nhãn "18:00 · hết giờ làm". "Hôm nay vậy là đủ rồi, về thôi!" + 3 ô: **3h20** họp · **2 lần** nghỉ cùng Milo · **1h40** tập trung sâu. Dòng "Quả nho hôm nay: 72 điểm, mọng hơn hôm qua". Dòng hẹn: "Mai 9:00 có Daily — Milo nhắc lúc mở máy." Các case chăm sóc bị gộp trong ngày hiện thành 1 dòng nhỏ, ví dụ "Còn 1 lần làm liền 2h10 chưa nghỉ". |
| Phản hồi | **Về thôi** → chạy ra xe, sang Nghỉ làm. **Thêm 30 phút** (chỉ được 1 lần) → Milo leo xuống, chóp đuôi chuyển dáng mệt, 30 phút sau chạy episode Nhắc lại tan tầm. Không trả lời 60 giây → thu gọn thành chấm. |
| Ra | Chạy ra xe (9 khung, 10fps). Đây là chỗ dùng cố định của clip này, thay cho random 4% ở code hiện tại. |
| Dữ liệu | Tổng kết ngày được ghi vào lịch sử local (mục 11) để tạo quả nho của ngày. |

### 6.3 Nhắc lại tan tầm (B16)

30 phút sau "Thêm 30 phút", Milo leo lên ở dáng mệt: "Hết 30 phút rồi nè. Mình về nhé?" Thẻ chỉ có một nút **Về thôi**, không hỏi lại lần nữa. Nếu bạn vẫn tiếp tục làm, từ đây case Quá giờ (mục 8) tiếp quản.

### 6.4 Tắt máy ngang

Nếu máy shutdown hoặc sleep sau giờ kết thúc trừ 60 phút, Milo không cố hiện lên. Tổng kết được lưu im lặng và xuất hiện trong bản tin sáng hôm sau, ví dụ "Hôm qua: 72 điểm, họp 3h20".

## 7. Kịch bản hỗ trợ công việc

Nhóm episode này giúp bạn làm việc ít vấp hơn. Episode nào cần đọc hay ghi dữ liệu ngoài quyền MVP sẽ tự hạ cấp khi thiếu quyền (mục 14).

### 7.1 Sắp họp (B04)

| Nhịp | Kịch bản |
| --- | --- |
| Điều kiện | Còn 5 phút tới sự kiện có isOnlineMeeting = true, và bạn không ở trong một cuộc gọi khác. Episode hết hạn đúng lúc cuộc họp bắt đầu. |
| Vào | ClimbIn (11 khung, 8fps), sau đó chỉ tay (Reminder nghiêng sang, lặp 1.4s). Icon chuông trong nhãn rung 1s. |
| Nói | Nhãn "Teams · còn 5 phút" · 9:30–10:30 · **Sprint Planning** · avatar người tham gia · vai trò của bạn (Trình bày / Bắt buộc / Tuỳ chọn, lấy từ Meeting Classifier local). Nếu bạn trình bày và sự kiện có file đính kèm: "Milo mở sẵn slide 'Sprint 42' cho bạn nhé?" |
| Phản hồi | **Tham gia** → mở onlineMeeting.joinUrl, Milo thụt xuống nhanh vì cổng họp sắp đóng. **Mở slide** → mở file đính kèm, thẻ vẫn giữ nút Tham gia. Không bấm gì 60 giây → tự thu vào, không nhắc lại cuộc họp này. Không có nút Để sau. |
| Ra | Leo xuống, hoặc thụt nhanh nếu đã bấm Tham gia. |

Sắp họp cùng Chào sáng là hai episode duy nhất không bị ngân sách và cổng gõ phím chặn.

### 7.2 Lịch kín, giữ chỗ nghỉ (B07)

| Nhịp | Kịch bản |
| --- | --- |
| Điều kiện | Trong 4 giờ tới có ≥ 3 sự kiện liên tiếp, cách nhau dưới 5 phút, chưa cuộc nào bắt đầu. Chuỗi họp phải nằm trong buổi hiện tại. Mỗi buổi (sáng/chiều) chỉ đề xuất 1 lần. |
| Vào | ClimbIn rồi Reminder lắc nhẹ (2.4s). |
| Nói | Nhãn "Lịch Outlook · chiều nay kín". "3 cuộc họp nối liền, không có phút nào nghỉ. Chèn 10 phút vào giữa nhé?" Kèm lịch mini 14:00–16:00. |
| Phản hồi | **Giữ chỗ trong lịch** → `POST /me/events` "Nghỉ cùng Milo" 10 phút, showAs = tentative, đặt ngay sau cuộc thứ 2. Khối Nghỉ trượt vào lịch mini (chiều cao 0 → 18px, 400ms), dấu "Đã giữ" bật 1.1× rồi về 1. **Thôi** → không hỏi lại trong hôm đó. |
| Ra | Leo xuống. |
| Về sau | Tới giờ khối Nghỉ, nếu bạn đã ra khỏi cuộc gọi, Milo nhảy ra với kịch bản Họp liên tục (có vòng thở), không cần đợi đủ 3 cuộc. Nếu bạn vẫn đang trong cuộc gọi thì khối Nghỉ chỉ là lời hẹn trên lịch, Milo giữ im lặng. |

### 7.3 Email chờ bạn (B08)

| Nhịp | Kịch bản |
| --- | --- |
| Điều kiện | Email chưa trả lời, bạn nằm trong ô To, có dấu "?" hoặc gắn cờ, đã chờ ≥ 1 ngày làm việc. Chỉ giao ở khoảng trống còn ≥ 10 phút tới cuộc họp sau. Tối đa 1 lần/buổi. Không chạy trước 10:00 vì bản tin sáng đã báo số email. |
| Vào | ClimbIn rồi chỉ tay. Các dòng email trượt vào lệch nhau 200ms; nhãn "2 ngày" đậm nhạt theo nhịp 1.6s. |
| Nói | "3 email hỏi thẳng bạn, chưa trả lời:", tối đa 3 email, sắp theo số ngày đã chờ. Mỗi dòng gồm người gửi, tiêu đề và số ngày chờ. |
| Phản hồi | **Nhắc tôi lúc 16:00** → hẹn đúng 1 lần; nếu đã qua 16:00 thì nút đổi thành giờ hiện tại + 1 giờ. **Mở Outlook** → mở thẳng email đầu tiên. Không trả lời 45 giây → thu thành chấm. |
| Ra | Leo xuống. |

Milo không đọc hộ cả hộp thư. Trong dashboard email chỉ còn 1 con số để Milo không thành hộp thư thứ hai. Việc lọc email chạy local, và tiêu đề email không bao giờ được gửi cho LLM.

### 7.4 Task kẹt, khoá giờ tập trung (B09)

| Nhịp | Kịch bản |
| --- | --- |
| Điều kiện | Work item của bạn ở Active/In Progress ≥ 3 ngày làm việc, và lịch còn khoảng trống ≥ 45 phút. Thời lượng đề xuất là 90 phút hoặc bằng khoảng trống nếu khoảng trống ngắn hơn. |
| Vào | ClimbIn rồi Reminder lắc nhẹ. Thanh sprint chạy 0 → 62% trong 1.4s. |
| Nói | Nhãn "Azure Boards · Sprint 42" · "Sprint còn 3 ngày · 13/21 điểm xong" · dòng #4821 Refactor login flow, nhãn "dở 3 ngày". "Chặn 90 phút tập trung cho #4821 và bật 'Không làm phiền' trên Teams?" |
| Phản hồi | **Khoá 90 phút** → tạo sự kiện "Tập trung: #4821" và đặt presence DoNotDisturb tới hết khối; chấm trạng thái đổi xanh → đỏ; dòng xác nhận "Teams: Không làm phiền đến 16:15 · lịch đã chặn". **Để sau** → hỏi lại sau 60 phút. |
| Ra | Thụt xuống nhanh, sang Im lặng với cổng Giờ tập trung. |
| Kết thúc khối | Milo luôn ló lên 3 giây (ưu tiên P1): "90 phút sâu xong rồi!" (+4 điểm). Nếu có lời nhắc đang chờ, Milo giao luôn ngay sau đó. |

Khi sprint còn ≤ 3 ngày, Milo chỉ hiện thanh tiến độ trong dashboard, không cảnh báo đỏ gây áp lực (B18).

### 7.5 Task xong (B18)

Work item chuyển sang Done thì Milo ló lên từ chóp đuôi, nhảy tưng kèm lấp lánh, bóng thoại hiện 3 giây "Xong #4821 rồi! Quả nho hôm nay mọng thêm chút." rồi tụt xuống. Không có thẻ, không có nút, +2 điểm. Nếu lúc đó đang im lặng, sự kiện được giữ tối đa 2 giờ rồi bỏ.

## 8. Kịch bản chăm sóc

Cả 6 case chăm sóc dùng chung một khung episode (B13). Giữa các case chỉ khác nhãn, màu, câu thoại và việc xảy ra khi bạn bấm Đồng ý.

### 8.1 Khung chung

1. **Vào.** ClimbIn, hoặc JumpIn nếu vừa hết họp. Milo chuyển sang Reminder lặp chậm 6fps; bóng thoại pop-in 220ms sau khi leo xong.
2. **Thẻ.** Bên trái là nhãn màu riêng của case kèm số liệu, ví dụ "Họp liên tục · 3h20"; bên phải là "trống tới 13:13". Tiếp theo là 1 câu nhắc dưới 25 từ, 1 nút chính **Đồng ý**, 2 nút phụ **Để sau (N phút)** / **Không cần**, và ô "Nói gì đó với Milo…".
3. **Đồng ý** → hoạt động riêng của case (bảng 8.2) → Milo vẫy cảm ơn 1 vòng → leo xuống.
4. **Để sau** → leo xuống ngay, sau N phút nhắc lại nếu điều kiện vẫn còn đúng. Mỗi case được Để sau tối đa 2 lần/ngày; từ lần thứ 3 thẻ chỉ còn Đồng ý và Không cần.
5. **Không cần** → Milo leo xuống bản ngắn, ngó lại 1 nhịp khi đang bám mép. Case đó bị ẩn 60 phút. Nếu bạn bấm Không cần lần thứ 2 trong ngày, mọi thời gian chờ của case đó nhân 3 cho tới hết ngày.
6. **Không trả lời 45 giây** → thẻ thu vào Milo, Milo leo xuống, chóp đuôi mang chấm "1". Chấm tồn tại 30 phút rồi tự bỏ.

### 8.2 Sáu case

| Case | Nhãn · màu | Điều kiện | Mức | Đồng ý dẫn tới | Để sau |
| --- | --- | --- | --- | --- | --- |
| Họp liên tục | "Họp liên tục · 3h20" · cam #E8A33D | ≥ 3 cuộc liền nhau (cách nhau dưới 5 phút) vừa kết thúc trong 60 phút qua; còn ≥ 5 phút tới cuộc sau | Cao | Thở 4-4-4 × 3 nhịp, rồi gợi ý đứng dậy 5–10 phút | 15 phút |
| Quá giờ | "Quá giờ làm · 35 phút" · đỏ #D1495B | Còn thao tác hơn 30 phút sau giờ kết thúc (tính từ lúc hết "Thêm 30 phút" nếu có). Cứ mỗi 30 phút thì tăng 1 mức | Trung bình → Cao | "Chốt việc, về thôi": chạy ra xe, sang Nghỉ làm | 10 phút |
| Chưa nghỉ trưa | "Chưa nghỉ trưa" · xanh lá #7FA65A | 12:30–14:00 mà chưa có lần nghỉ ≥ 20 phút kể từ 11:00 | Trung bình | "Đi ăn thôi": Milo vẫy rồi đi, **không** có vòng thở. Nút phụ "Khoá 30' trong lịch" nếu lịch còn trống (B18) | 15 phút |
| Làm liền không nghỉ | "Làm liền · 2h10" · xanh ngọc #3E8E9E *(đề xuất)* | Không có lần idle ≥ 5 phút trong suốt ≥ 120 phút; thời gian họp cũng tính là làm | Trung bình | Thở 1 phút (3 nhịp) + nhắc uống nước | 15 phút |
| Nghỉ quá ít | "Nghỉ quá ít · 12 phút" · tím #8C5BB5 *(đề xuất)* | Đã làm ≥ 3 giờ và tổng nghỉ < 50% × 45 phút × (giờ đã làm / 8) | Cao | Nghỉ 15 phút: thở 5 nhịp, "Milo giữ chỗ 15 phút", chóp đuôi đếm ngược | 20 phút |
| Phân mảnh | "Bị cắt vụn · 11 lần/giờ" · nâu cam #C07A2C *(đề xuất)* | Hơn 8 lần chuyển việc trong 60 phút, không nằm trong chuỗi họp | Trung bình | Khoá tập trung 30 phút + Không làm phiền, giống mục 7.4 | 30 phút |

Câu thoại mẫu cho mỗi case:

- Họp liên tục: "Bạn họp liền 3 tiếng rồi đó. Mình nghỉ 15 phút trước buổi Demo nhé?"
- Quá giờ: "Đã quá giờ về 35 phút rồi. Chốt việc đang dở rồi mình về nhé?"
- Chưa nghỉ trưa: "12:40 rồi mà bạn chưa rời máy. Đi ăn trưa thôi!"
- Làm liền: "Bạn làm liền 2 tiếng không rời mắt khỏi màn hình. Hít thở 1 phút rồi uống nước nhé?"
- Nghỉ quá ít: "Hôm nay bạn mới nghỉ tổng cộng 12 phút. Nghỉ hẳn 15 phút nhé, Milo canh giờ cho."
- Phân mảnh: "Giờ vừa rồi bạn chuyển việc 11 lần. Gom việc lại và tắt thông báo 30 phút nhé?"

### 8.3 Thở cùng Milo (B14)

Bấm "Đồng ý" không chỉ đóng bóng thoại: ngay trong thẻ, Milo dẫn 3 nhịp thở 4-4-4 (hít 4s · giữ 4s · thở ra 4s), mất khoảng 40 giây. Vòng tròn phồng và xẹp theo nhịp, Milo ở dáng Idle chậm lại còn 4fps cho khớp. Nhãn ghi "Thở 4-4-4 · nhịp 1/3", dòng nhỏ "Thở theo vòng tròn, không cần nhìn chữ". Nút **Dừng** bấm lúc nào cũng được và vẫn tính là đã nghỉ. Hết nhịp thì hiện "Cảm ơn đã nghỉ cùng Milo!" kèm lấp lánh, rồi Milo leo xuống. Case Nghỉ quá ít dùng 5 nhịp.

### 8.4 Leo xuống (B15)

Đây là cách rời đi mặc định sau mỗi lần Milo nói xong, dài khoảng 4–5 giây: vẫy tạm biệt "Mình đi nha, lát gặp!" (0–1.2s) → tụt xuống, nhún lên rồi rơi → bám mép đung đưa ±3° trong 1.2s → đuôi khuất sau cùng, chỉ còn lại chóp đuôi ở góc. Có 3 biến thể:

- **Bản ngắn** (khi bấm Không cần, khi hết giờ chờ, sau Ghé ngang): bỏ đoạn vẫy, chỉ ngó lại 1 nhịp khi bám mép.
- **Bản nhanh** (khi bị cổng im lặng ngắt): thụt xuống trong 0.7s.
- **Chạy ra xe**: chỉ dùng lúc tan tầm.

## 9. Hành vi không lời và khi người dùng chủ động

Các hành vi ở mục này cho bạn thấy mood mà không cần popup, và không trừ vào ngân sách làm phiền.

### 9.1 Chóp đuôi (thay cho "nghỉ ở góc" của B03)

- Chóp đuôi cáo khoảng 28px nằm sát mép taskbar ở góc đã neo. Quanh đuôi có quầng sáng thở rất chậm, 5 giây/nhịp.
- Màu và độ bão hoà đổi theo mức mood (mục 11). Khi mood xuống Mệt dần, đuôi nhạt dần trong 4 giây.
- Có lời nhắc bị thu gọn thì trên đuôi hiện chấm số màu cam. Trong lúc nghỉ 15 phút, chấm này đổi thành đồng hồ đếm ngược.
- Kéo đuôi sang góc khác để đổi góc neo (4 góc).

### 9.2 Ló đầu khi rê chuột (B03)

Rê chuột lên đuôi 600ms thì Milo ngóc đầu lên nửa người trong 300ms (dùng khung giữa của Greet), kèm một dòng thì thầm, không chạy climb-in:

- Mọng / Cân bằng: "Hôm nay mọng **72** · chạm để xem"
- Mệt dần: "Nho hôm nay đang héo dần · 58" (B10)
- Kiệt sức: "Nho hôm nay héo quá · 36 · nghỉ chút nha"

Chuột rời đi thì Milo tụt xuống. Bấm vào thì mở dashboard.

### 9.3 Ghé ngang

- **Khi nào:** hẹn giờ ngẫu nhiên 30–60 phút, chỉ chạy khi hàng đợi rỗng, không bị cổng im lặng và bạn không đang gõ phím.
- **Diễn ra:** Milo leo lên chậm, không có bóng thoại, đứng 8 giây nhìn quanh và chớp mắt, rồi leo xuống bản ngắn.
- **Dáng:** đúng với mood lúc đó. Khi mood ở Kiệt sức, 4% số lần Milo rời đi bằng clip "Đào bới kiệt sức" (easter egg).

Ghé ngang là cách Milo nhắc bạn rằng nó vẫn còn đó dù ẩn cả ngày, thay cho nhịp 50–90 giây trong code hiện tại.

### 9.4 Mệt dần (B10)

Khi điểm xuống dưới 60, Milo không bật popup và không phát âm thanh, chỉ đổi dáng. Chóp đuôi và mọi lần Milo ló lên chuyển sang IdleTired (7 khung, 5fps); saturate giảm 1 → 0.75 trong 4 giây; mắt nhắm lâu hơn (\~300ms). Dưới 40 thì saturate còn 0.55 và có 3 chữ z bay lên lệch nhau 0.8s. Khi điểm tăng lại, màu hồi dần về bình thường. Bạn tự để ý nếu muốn.

### 9.5 Chào hỏi ngẫu nhiên (§4.2)

Milo ClimbIn rồi vẫy, nói 1 câu tích cực, ví dụ "Bạn tập trung được 1h20 rồi đó. Uống ngụm nước cho tỉnh nè." Thẻ có 1 nút **Cảm ơn Milo** và tự đóng sau 15 giây. Chỉ chạy khi điểm ≥ 60, tức là không dùng chào hỏi để an ủi lúc bạn đang mệt.

### 9.6 Dashboard (B05, B06)

| Nhịp | Kịch bản |
| --- | --- |
| Mở | Bấm chóp đuôi hoặc bấm Milo → Milo leo lên và nảy (scale 1 → 1.15 → 1, 320ms) → panel rộng 360px mọc từ Milo lên (260ms) → các quả nho hiện lần lượt, mỗi quả trễ 40ms. |
| Hôm nay | Chùm nho 7 ngày là nhân vật chính; quả hôm nay nằm dưới cùng, có vòng cam nét đứt. Điểm **72** · "Mọng và đều — hôm nay đang cân bằng" · "Hôm qua 58 · quả vàng là thứ Ba mệt". 3 ô: Teams (3 cuộc, 2h15 họp), Outlook (4 email chờ bạn), Boards (2 task đang làm). 1 dòng "Sắp tới". Không có thanh cuộn, không có bảng. |
| Tương tác | Chạm một quả nho → hiện ngày và điểm của quả đó. "Xem cả tuần →" đổi nội dung ngay trong panel (trượt ngang 24px + fade 200ms), Milo thu từ 120 xuống 96px. |
| Cả tuần | 7 ngày dạng quả nho (to nhỏ và màu theo điểm); Office Vibe 3 hàng trái cây Tập trung / Năng lượng / Căng thẳng (0–5); tiến độ sprint; email đang chờ; lịch hôm nay, mỗi cuộc chỉ có giờ, tên và 1 nhãn (Trình bày / Bắt buộc / Tuỳ chọn / Nghỉ). "← Hôm nay" để quay lại. |
| Đóng | Bấm Milo, nút X hoặc Esc → panel thu ngược lại → Milo leo xuống. |

### 9.7 Bấm vào chấm chờ (B11)

Bấm vào chấm "N lời nhắc đang chờ" thì lời nhắc đứng đầu hàng mở ra ngay, kể cả khi đang họp. Milo không leo lên, chỉ thẻ bung ra từ chấm. Đây là hành động của người dùng (P0) nên không tính vào ngân sách.

## 10. Phản hồi của người dùng

Có 5 kiểu phản hồi với một lời nhắc, và kiểu nào cũng được ghi log (§4.3b). Bảng dưới áp dụng cho case chăm sóc; episode hỗ trợ có nút riêng đã ghi ở mục 7.

| Phản hồi | Milo làm | Điểm mood | Lần nhắc sau | Log |
| --- | --- | --- | --- | --- |
| Đồng ý | Hoạt động của case → vẫy cảm ơn + lấp lánh → leo xuống | +3 (tối đa +12/ngày) | Case nghỉ cho tới khi điều kiện xuất hiện lại từ đầu (chuỗi họp mới, chuỗi làm liền mới) | accepted |
| Để sau | Leo xuống ngay | 0 | Sau N phút của case; tối đa 2 lần/ngày | snoozed |
| Không cần | Leo xuống bản ngắn, ngó lại 1 nhịp | 0 | Ẩn 60 phút (Chưa nghỉ trưa, Nghỉ quá ít: 90 phút); lần thứ 2 trong ngày → giãn ×3 | dismissed |
| Im lặng hết giờ | Thẻ thu vào Milo, chấm số trên đuôi | 0 | Chấm giữ 30 phút rồi bỏ; sau đó xử lý như Để sau 30 phút | ignored |
| Gõ chat | Theo ý định nhận ra được (bảng dưới) | Theo ý định | Theo ý định | chat + ý định |

### Chat tự do

Trước hết, một bộ nhận diện từ khoá chạy local; không khớp thì mới gọi LLM. Nhờ vậy tính năng chat vẫn chạy khi chưa có Lớp 2 (§4.3b).

| Người dùng gõ | Ý định | Milo trả lời và làm |
| --- | --- | --- |
| "đang họp", "bận", "lát nữa" | Bận | "Hiểu rồi, bạn đang bận. 15 phút nữa Milo quay lại nhé." → xử lý như Để sau |
| "cảm ơn", "ok", "thanks" | Cảm ơn | "Hihi, Milo vui lắm!" → vẫy rồi đi, tính nửa Đồng ý (+1) |
| "mệt", "căng", "đuối" | Mệt | "Vậy thở cùng Milo vài nhịp nha." → vào vòng thở |
| "không", "thôi", "tôi ổn" | Từ chối | "Okie, Milo tôn trọng bạn." → xử lý như Không cần |
| Các câu khác | Không rõ | Có LLM: trả lời theo ngữ cảnh trong 2.5 giây, thẻ vẫn mở. Không có LLM hoặc quá thời gian: "Milo nghe rồi nè. Bạn chọn một nút bên trên giúp Milo nhé." |

Mỗi lần chat, thời gian chờ hết giờ của thẻ được tính lại từ đầu để Milo không thu thẻ giữa lúc bạn đang gõ.

## 11. Mood Engine

Điểm bắt đầu mỗi ngày ở 92, bị trừ theo 7 yếu tố của §8 và được cộng khi bạn nghỉ hoặc hoàn thành việc. Điểm được tính lại mỗi phút. Các hệ số dưới đây là điểm xuất phát để nhóm tinh chỉnh (§10).

```latex
\text{score} = \operatorname{clamp}\Big(92 - \sum \text{phạt} + \sum \text{thưởng},\ 0,\ 100\Big)
```

| Thành phần | Cách trừ | Trừ tối đa |
| --- | --- | --- |
| Thời lượng họp | 0.1 × số phút họp vượt 180 phút | 20 |
| Chuỗi họp | 4 × mỗi cuộc vượt 2 trong chuỗi dài nhất hôm nay | 12 |
| Làm liền | 0.2 × số phút vượt 90 của chuỗi hiện tại (phần này hồi lại khi bạn nghỉ) | 15 |
| Quá giờ | 0.33 × số phút có thao tác sau giờ kết thúc; trừ thêm 5 nếu bắt đầu sớm hơn khung 30 phút | 25 + 5 |
| Thiếu nghỉ | 0.5 × (45 × giờ đã làm / 8 − phút đã nghỉ), bắt đầu tính sau 2 giờ làm | 15 |
| Phân mảnh | 2 × số lần chuyển việc vượt 8 lần/giờ | 12 |
| Workload | Task In Progress / trung bình sprint: trên 1.5 → trừ 6, trên 2 → trừ 10; thêm 2 cho mỗi task kẹt | 10 + 6 |
| Email chờ | 1 × mỗi email chờ vượt 2 | 4 |

Điểm thưởng: +3 cho mỗi lần Đồng ý nghỉ (tối đa +12), +2 cho mỗi task Done (tối đa +6), +4 cho mỗi khối tập trung hoàn thành (tối đa +8).

### Bốn mức và cách Milo thể hiện

| Mức | Điểm | Dáng Milo | Quả nho | Thì thầm |
| --- | --- | --- | --- | --- |
| Mọng | ≥ 80 | Idle vui (7 khung, 6fps), quầng sáng ấm | Tím đậm, chín | "Hôm nay mọng 85" |
| Cân bằng | 60–79 | Idle | Tím vừa | "Mọng và đều — đang cân bằng" |
| Mệt dần | 40–59 | IdleTired (5fps), saturate 0.75, chớp mắt chậm | Tím nhạt | "Nho hôm nay đang héo dần" |
| Kiệt sức | dưới 40 | IdleTired, saturate 0.55, chữ z bay | Khô vàng | "Nho hôm nay héo quá" |

Để Milo không đổi dáng qua lại liên tục, điểm phải vượt ranh giới ít nhất 3 điểm thì mới đổi mức (ví dụ phải xuống 57 mới sang Mệt dần, lên 63 mới về Cân bằng). Mỗi lần đổi mức kéo dài 4 giây.

### Office Vibe (3 hàng, mỗi hàng 0–5 quả)

- **Tập trung** = 5 × (phiên tập trung dài nhất hôm nay / 90 phút), nhân với hệ số giảm dần khi số lần chuyển việc vượt 4 lần/giờ.
- **Năng lượng** = điểm / 20, làm tròn.
- **Căng thẳng** = (phạt họp + quá giờ + workload + email + thiếu nghỉ) / 40 × 5.

### Lưu cuối ngày

Lúc tan tầm hoặc khi tắt máy, Milo ghi 1 bản ghi vào SQLite hoặc lowdb local (§4.4, §5): ngày, điểm cuối ngày, phút họp, số lần nghỉ cùng Milo, phút tập trung sâu, số task Done, phút quá giờ, 3 chỉ số Office Vibe, và số lần hiện / đồng ý / để sau / không cần / bỏ qua của từng case. Bản ghi này dùng để vẽ chùm nho 7 ngày, viết bản tin sáng và cá nhân hoá (mục 14).

## 12. Bản đồ animation

Mọi episode đều chạy theo vòng của B17: Vào → Ở lại → Ra → Nghỉ. Để Milo chạy đủ kịch bản này, nhóm có sẵn 11 clip và cần vẽ thêm 11 clip.

| Nhịp | Clip | Thông số | Dùng trong | Tư thế SVG | Tình trạng |
| --- | --- | --- | --- | --- | --- |
| Vào | Bám mép → leo lên | hanging + ClimbIn, 3.4s | Chào sáng | idle | Có sẵn |
| Vào | Hello! | Greet + nhảy tưng, 2.3s | Chào sáng | greeting | Cần vẽ |
| Vào | ClimbIn | 11 khung · 8fps | Mặc định cho mọi episode khi Milo đang ẩn | idle | Có sẵn |
| Vào | JumpIn | 9 khung · 10fps + tiếp đất nén 1.14×0.86, bụi 2 bên chân | Sau họp có lời nhắc chờ | idle | Có sẵn |
| Vào | Leo chậm | ClimbIn chậm 50%, không bóng thoại | Ghé ngang, Task xong | idle | Cần vẽ |
| Vào | Nảy lên | scale 1 → 1.15 → 1, 320ms | Dashboard | idle | Cần vẽ (chỉ là transform) |
| Ở lại | Greet | 7 khung · 7fps | Chào sáng, cảm ơn, Chào hỏi, Tan tầm | wave | Có sẵn |
| Ở lại | Reminder | 9 khung · 6fps, lắc nhẹ 2.4s | 6 case chăm sóc, Lịch kín, Task kẹt | care | Có sẵn |
| Ở lại | Chỉ tay | Reminder nghiêng sang, lặp 1.4s | Sắp họp, Email chờ | point | Cần vẽ |
| Ở lại | Thở cùng Milo | Idle 4fps + vòng thở 4-4-4 | Sau Đồng ý ở case có vòng thở | breathe | Cần vẽ |
| Ở lại | Nhảy tưng mừng | Greet + lấp lánh, 1.8s | Task xong, Giữ chỗ lịch, hết khối tập trung | greeting | Cần vẽ |
| Ở lại | Vươn vai | 6 khung | Tan tầm | greeting | Cần vẽ |
| Ra | Leo xuống | vẫy → tụt → bám mép 1 nhịp → đuôi khuất, \~4.2s | Mặc định | wave | Có sẵn |
| Ra | Leo xuống bản ngắn | bỏ vẫy, ngó lại 1 nhịp, \~2.6s | Không cần, hết giờ chờ, Ghé ngang | idle | Có sẵn (cắt từ Leo xuống) |
| Ra | Thụt nhanh | 0.7s | Bị cổng im lặng ngắt, sau Tham gia họp | idle | Cần vẽ |
| Ra | Chạy ra xe | 9 khung · 10fps | Chỉ lúc tan tầm | run | Cần vẽ |
| Ra | Đào bới kiệt sức | 9 khung · 8fps | Easter egg 4% khi mood Kiệt sức | tired | Có sẵn |
| Nghỉ | Idle / IdleTired | 7 khung · 6 / 5fps, ping-pong | Mọi lúc Milo đứng, theo mood | idle / tired | Có sẵn |
| Nghỉ | Chớp mắt | có sẵn trong SVG cáo, 4.5s | Luôn chạy | — | Có sẵn |
| Nghỉ | Nhìn quanh | có sẵn trong SVG cáo, 7s | Ghé ngang | — | Có sẵn |
| Nghỉ | Ngóc đầu | khung giữa của Greet, 300ms | Ló đầu khi rê chuột | greeting | Cần vẽ |
| Nghỉ | Chóp đuôi | sprite \~28px + quầng thở 5s, màu theo mood | Trạng thái Ẩn | cắt từ tail | Cần vẽ (mới, do chọn ẩn) |

Ba luật chung cho mọi clip (B17):

- **Nối giữa 2 clip:** crossfade dip 40% trong 280ms. Code hiện đang để 15% / 200ms.
- **Giữ chân cố định:** mọi clip xuất cùng baseline chân để Milo không "nhảy" vị trí khi đổi clip.
- **Im lặng mặc định:** không clip nào kèm âm thanh; bật âm là tuỳ chọn trong cài đặt. `prefers-reduced-motion` thì các clip rút về 1 khung.

## 13. Một ngày mẫu

Ngày mẫu là Thứ Năm 24/9, khung giờ làm việc 9:00–18:00. Đây cũng chính là ngày được chạy trong prototype "Milo sống", nên mọi mốc dưới đây đều là kết quả mà bộ quy tắc thật sự tính ra.

- **Lịch họp:** 9:30–10:30 Sprint Planning (bạn trình bày). Buổi chiều có 3 cuộc liền nhau: 13:30 Design sync, 14:30 1:1 với lead, 15:30–16:10 Demo review.
- **Khối lượng việc:** 6 task đang mở (trung bình sprint là 2.8), trong đó 2 task kẹt; 5 email đang chờ bạn trả lời.
- **Kết quả:** Milo nói 13 lần, trong đó chỉ 5 lần là lời nhắc chủ động trong giờ làm. Suốt 3h40 họp, Milo hoàn toàn im lặng.

| Giờ | Chuyện xảy ra | Milo quyết định | Board |
| --- | --- | --- | --- |
| 08:58 | Mở khoá máy lần đầu trong ngày | Chào sáng: bám mép → leo lên → Hello → bản tin (4 họp, 7 email, 6 task kèm cảnh báo workload) → Đã rõ → leo xuống | B01, B02, B15 |
| 09:25 | Còn 5 phút tới Sprint Planning | Sắp họp (P1): chỉ tay, vai trò "Trình bày" → Mở slide → Tham gia → thụt xuống nhanh | B04 |
| 09:30–10:30 | Đang họp | Im lặng. Task kẹt và Email chờ vào hàng đợi; góc màn hình hiện "2 lời nhắc đang chờ" | B11 |
| 10:30 | Họp xong, bạn rời máy đi lấy cà phê | Đợi 2 phút ổn định, rồi hoãn tiếp vì không có ai trước màn hình | B12 |
| 10:37 | Quay lại máy (đã nghỉ 6 phút) | JumpIn → Task kẹt #4821 (thắng Email vì mức nặng hơn) → bấm Khoá 90 phút → bật Không làm phiền đến 12:07 → Im lặng | B09 |
| 12:07 | Hết khối tập trung | Milo ló lên "90 phút sâu xong rồi!" (+4 điểm). Ngay sau đó là Nghỉ quá ít (sau 3 giờ mới nghỉ 6 phút); bạn bấm Để sau vì sắp đi ăn | B09, B13 |
| 12:15–12:55 | Khoá máy đi ăn | Tính là nghỉ 40 phút và đã nghỉ trưa. Nghỉ quá ít không còn đúng nên bị xóa khỏi hàng đợi | — |
| 12:55 | Mở khoá | Lịch kín (chiều nay có 3 cuộc liền nhau) → Giữ chỗ → tạo "Nghỉ cùng Milo" 15:30–15:40, tentative | B07 |
| 13:10 | Đủ 15 phút kể từ lần nhắc trước | Email chờ: 5 email, hiện 3 email đầu → Mở Outlook | B08 |
| 13:14 | Rê chuột lên chóp đuôi | Ló đầu: "Hôm nay mọng 80 · chạm để xem" | B03 |
| 13:15 | Bấm vào đuôi | Dashboard → Xem cả tuần → đóng → leo xuống | B05, B06 |
| 13:25 | Còn 5 phút tới Design sync | Sắp họp → Tham gia. Lúc 14:25 và 15:25 Milo không nhắc vì bạn đang trong cuộc gọi | B04 |
| 13:30–16:10 | 3 cuộc họp liền nhau | Im lặng. 14:55 Làm liền 2 giờ vào hàng đợi. 15:30 tới khối Nghỉ nhưng bạn còn trong call. 16:06 điểm xuống 57 → Mệt dần (Milo chỉ đổi màu) | B10, B11 |
| 16:10 | Demo review kết thúc | Hàng đợi có Họp liên tục (P2) và Làm liền (P3). Đợi 2 phút | B12 |
| 16:12 | Đã trống 2 phút | JumpIn → "Họp liên tục · 2h40" → Đồng ý → thở 4-4-4 × 3 → cảm ơn → leo xuống. Làm liền được gộp vào tổng kết | B12–B15 |
| 17:01, 17:41 | Tới hẹn giờ ghé ngang | Ghé ngang 8 giây ở dáng mệt | B10 |
| 17:45 | #4821 chuyển Done | Task xong: nhảy tưng "Xong #4821 rồi!"; điểm lên 66, Milo về lại Cân bằng | B18 |
| 18:00 | Hết khung giờ | Tan tầm: tổng kết (họp 3h40, 1 lần nghỉ cùng Milo, tập trung 1h30, còn 1 lần làm liền chưa xử lý) → bấm Thêm 30 phút | B16 |
| 18:22 | Vẫn đang làm thêm | Điểm xuống 57 → Mệt dần | B10 |
| 18:31 | Hết 30 phút thêm | Nhắc lại tan tầm → Về thôi → chạy ra xe → Nghỉ làm, lưu quả nho của ngày: 54 điểm | B16 |

Trong prototype có thể bỏ chọn "Người dùng tự trả lời" để tự bấm. Bấm khác đi sẽ ra ngày khác đi, ví dụ: đồng ý Nghỉ quá ít thì được +3 điểm và Milo giữ chỗ 15 phút; không khoá máy lúc trưa thì 12:30 sẽ chạy case Chưa nghỉ trưa.

## 14. Dữ liệu, quyền, lời thoại và cá nhân hoá

Với 3 quyền đã đăng ký (User.Read, Presence.Read, Calendars.Read), Milo đã chạy được lõi MVP. Mỗi quyền còn thiếu chỉ làm mất đúng phần tính năng cần nó, phần còn lại vẫn chạy.

| Nguồn | Dùng cho | Quyền | Nếu thiếu |
| --- | --- | --- | --- |
| `powerMonitor` | Idle, khoá máy, sleep, lần mở máy đầu ngày | Local | — |
| Active window | Phân mảnh, cổng toàn màn hình, phiên tập trung | Local | Phân mảnh chỉ đếm chuyển họp ↔ không họp; tắt cổng toàn màn hình |
| `/me/presence` | Cổng đang họp | Presence.Read (đã có) | Cổng họp suy ra từ lịch |
| `/me/calendarView` | Sắp họp, chuỗi họp, khoảng trống, bản tin sáng | Calendars.Read (đã có) | — |
| `POST /me/events` | Giữ chỗ nghỉ, khoá tập trung, khoá trưa | Calendars.ReadWrite | Nút đổi thành "Nhắc tôi lúc đó", không ghi vào lịch |
| `/me/messages` | Email chờ, số email chưa đọc | Mail.Read | Tắt Email chờ; bản tin sáng bỏ dòng email |
| `setUserPreferredPresence` | Bật Không làm phiền khi khoá tập trung | Presence.ReadWrite | Vẫn tạo sự kiện, nhắc bạn tự bật DND |
| Azure DevOps WIQL | Task kẹt, Task xong, workload, thanh sprint | PAT lưu local | Tắt 4 tính năng này |
| Meeting Classifier (local, có sẵn) | Vai trò Trình bày / Bắt buộc / Tuỳ chọn | — | Ẩn nhãn vai trò |

**Mất mạng hoặc token hết hạn:** Rule Engine vẫn chạy trên dữ liệu local. Các case cần Graph tạm tắt. Milo không báo lỗi bằng popup, chỉ hiện một dòng nhỏ trong dashboard.

**Riêng tư:** mọi xử lý đều chạy local (B18). LLM chỉ nhận tên case và số liệu, ví dụ "3 cuộc họp liền, 2h40, chưa nghỉ". LLM không bao giờ nhận tiêu đề hay nội dung email, cuộc họp hay task.

### Lời thoại (Lớp 2, §8b)

- Khi một case vào hàng đợi, Milo gọi LLM **ngay lúc đó** để viết sẵn câu nhắc, thay vì đợi tới lúc hiện. Nhờ vậy khi giao lời nhắc không phải chờ.
- Timeout 2.5 giây. Hết giờ hoặc lỗi thì dùng template của case: mỗi case 3–5 biến thể, không lặp lại câu vừa dùng. Có thể thay template bằng pool câu do LLM sinh sẵn từ trước để demo an toàn.
- Ràng buộc: tiếng Việt, dưới 25 từ, giọng ấm, không dạy đời, có 1 hành động cụ thể, Milo xưng "Milo" hoặc "mình" và gọi người dùng là "bạn".
- Nhãn, số liệu và nút bấm luôn lấy từ template, LLM chỉ viết câu chính. Vì vậy LLM không thể làm sai số liệu hay đổi hành động của nút.

### Log và cá nhân hoá

Mỗi sự kiện được ghi 1 dòng gồm: thời điểm, episode/case, loại (shown, accepted, snoozed, dismissed, ignored, chat, gated, folded), thời gian từ lúc hiện tới lúc phản hồi, và điểm mood lúc đó. Từ log này có 3 luật điều chỉnh:

1. **Trong ngày:** 1 case bị Không cần 2 lần thì mọi thời gian chờ của case đó nhân 3 tới hết ngày (B13).
2. **Theo 7 ngày:** 1 case hiện ≥ 5 lần mà ≥ 60% là Không cần hoặc bị bỏ qua thì thời gian chờ nhân 2 và ngưỡng kích hoạt nâng thêm 15%. Ví dụ Làm liền tăng từ 120 lên 138 phút (§4.3b).
3. **Hay bấm Để sau:** 1 case bị Để sau ≥ 60% trong 7 ngày thì Milo thử giao case đó sớm hơn trong khoảng trống dài nhất kế tiếp thay vì khoảng trống đầu tiên.

Cá nhân hoá không bao giờ tắt hẳn một case chăm sóc có mức Cao, chỉ làm nó thưa đi. Người dùng muốn tắt hẳn thì tự tắt trong cài đặt.

## 15. Câu hỏi mở cho nhóm

- [ ] **Nhịp ghé ngang:** kịch bản đang để 30–60 phút vì Milo ẩn. Board 03 ghi 5–10 phút nhưng đó là khi Milo ngồi góc. Nhóm chốt con số nào?
- [ ] **Khối Nghỉ 10 phút (7.2):** khối này sẽ đè lên 10 phút đầu của cuộc họp thứ 3. Nhóm chấp nhận như một lời hẹn với chính mình, hay chỉ đặt khối khi có khoảng trống thật?
- [ ] **Màu cho 3 case mới** (Làm liền, Nghỉ quá ít, Phân mảnh): designer cần duyệt 3 màu đề xuất trong 8.2.
- [ ] **11 clip cần vẽ** (mục 12). Ưu tiên vẽ trước: Chóp đuôi, Chỉ tay, Thở cùng Milo, Chạy ra xe.
- [ ] **Quyền tenant Bosch:** Mail.Read, Calendars.ReadWrite và Presence.ReadWrite có cần admin consent không? Nếu cần thì bản demo chạy theo cột "Nếu thiếu" ở mục 14.
- [ ] **Baseline workload:** "trung bình sprint" lấy từ lịch sử Azure Boards hay từ log của Milo? Log của Milo cần vài tuần mới có đủ dữ liệu.
- [ ] **Hệ số mood:** cần chạy thử 2–3 ngày thật của nhóm để kiểm tra một ngày bình thường có rơi vào khoảng 65–75 không.

## 16. Tính năng mở rộng (thêm sau khi chốt kịch bản)

Các hành vi dưới đây không có trong bản chốt ban đầu, được thêm khi làm app. Chúng đi cùng khung điều phối ở mục 4 (cổng im lặng, ngân sách, hàng đợi) và bật/tắt từng cái bằng mục `Wellbeing` trong cấu hình.

| Hành vi | Loại / ưu tiên | Khi nào | Milo làm gì | Người dùng trả lời |
| --- | --- | --- | --- | --- |
| **Giữ giờ tập trung** | Hỗ trợ · P4 | 1 lần/ngày, ≥ 20 phút sau lần mở máy đầu, trước 15:00, còn khoảng trống ≥ 60 phút (bỏ giờ họp, 12:00–13:00) | Thẻ "Từ 16:15 tới 17:45 bạn trống 1h30. Milo giữ chỗ Tập trung nhé?" + task hợp để làm | *Giữ* → sự kiện busy trong lịch; tới giờ tự bật Không làm phiền như 7.4. *Thôi* → không hỏi lại hôm nay |
| **Báo cáo tuần** | Xã giao · P4 | Sáng thứ Hai, ngay sau Chào sáng, khi máy còn dữ liệu tuần trước | Điểm TB, giờ họp, số lần nghỉ, ngày tốt/mệt nhất + 1 mẹo theo điểm yếu nhất | *Đã rõ* · *Xem chùm nho* (mở dashboard tuần) |
| **Hôm nay thấy sao?** | Trong thẻ Tan tầm (6.2, 6.3) | Cùng lúc Tan tầm | 3 nút Vui / Bình thường / Mệt | Chỉ lưu trên máy. Mood Engine: Mệt −6, Vui +3. Gõ chat "mệt" ở thẻ tan tầm cũng tính là Mệt |
| **Nghỉ giữa chuỗi họp ngày mai** | Trong thẻ Tan tầm | Mai có ≥ 3 cuộc họp liền | "Mai 13:30–16:15 có 3 cuộc họp liền" | *Giữ 10' nghỉ lúc 15:30* → sự kiện tentative ngày mai (cách giữ chỗ như 7.2) |
| **Nghỉ ngắn** (uống nước, vươn vai) | Hỗ trợ · P5 · miễn ngân sách | Mỗi 50 phút ngồi máy liên tục (không tính giờ họp), tối đa 6 lần/ngày | Ló lên 5 giây, 1 bóng thoại, không thẻ, không nút (như 7.5) | Không cần trả lời. Rời máy ≥ 5 phút thì đếm lại |
| **Trốn khi trình chiếu** | Cổng im lặng mới | Teams presence = Presenting | Trốn hẳn, **kể cả chóp đuôi và chấm chờ**; thẻ đang mở thu lại | — |
| **Tủ đồ** | Không lời | Về đúng giờ (quá giờ < 15 phút) 3 / 5 / 10 / 15 ngày liền | Milo có khăn quàng / kẹp hoa / mũ nồi / đồng phục Bosch; sáng hôm sau bản tin Chào sáng báo món mới | Chọn món hoặc tắt ở Bảng điều khiển |

Dashboard (9.6) có thêm nút **Chi tiết**: một bảng nhỏ cỡ 1 thẻ ngay trên đầu Milo, gồm dòng thời gian giờ làm, Office Vibe, cuộc họp sắp tới (trang Hôm nay) hoặc 7 quả nho, thống kê tuần, cách bạn trả lời Milo (trang Tuần).

Câu hỏi mở thêm:

- [ ] **Nhịp nghỉ ngắn:** 50 phút có hợp với nhóm không? (Đã bỏ quy tắc 20-20-20 vì nghiên cứu 2023 không thấy tác dụng rõ — xem CO-SO-KHOA-HOC.md.)
- [ ] **Tủ đồ:** mốc 3/5/10/15 ngày và 4 món có đủ tạo động lực không? Designer có muốn vẽ thêm món theo mùa?

