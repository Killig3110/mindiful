# Cơ sở khoa học của điểm mood (Chỉ số cân bằng)

Tài liệu trả lời câu hỏi: **"Điểm của Milo lấy từ đâu, và làm sao biết nó đúng?"**

Tóm tắt 4 ý:
1. **Khung lý thuyết:** điểm được xây theo mô hình **Job Demands–Resources (JD-R)**, mô hình được dùng nhiều nhất về kiệt sức trong công việc. **Áp lực** (họp, làm liền, quá giờ, nhảy việc…) trừ điểm; **nguồn hồi phục** (nghỉ, tập trung trọn vẹn, xong việc) cộng điểm.
2. **Căn cứ từng khoản:** mỗi khoản cộng/trừ dựa trên nghiên cứu **công bố từ 09/2021 tới nay** (5 năm gần đây). Nghiên cứu cho biết **chiều tác động** và **ngưỡng**, không cho con số tuyệt đối. Vì vậy chiều và ngưỡng lấy từ nghiên cứu, còn trọng số là lựa chọn thiết kế.
3. **Chứng minh công thức đúng với mọi dữ liệu:** bộ test tự sinh **20.000 bộ số liệu ngẫu nhiên** và hàng chục ngày làm việc ngẫu nhiên, kiểm tra công thức luôn giữ đúng các kết luận nghiên cứu. Bộ test đã tìm ra 1 giới hạn thật (mục 6).
4. **Kiểm chứng với người thật:** mỗi tuần người dùng tự trả lời 5 câu **WHO-5** (thang đo của Tổ chức Y tế Thế giới). App tính tương quan với điểm Milo để hiệu chỉnh trọng số. Việc này cần chạy thử với nhóm thật trong khoảng 4 tuần.

Không cần LLM hay API trả phí cho bất kỳ bước nào (mục 7).

---

## 1. Khung lý thuyết

| Khung | Nói gì | Milo dùng thế nào |
| --- | --- | --- |
| **Job Demands–Resources** (Bakker, Demerouti & Sanz-Vergel, 2023) | Kiệt sức sinh ra khi **áp lực công việc** vượt **nguồn lực**; nguồn lực làm giảm tác hại của áp lực | Điểm = điểm gốc − áp lực + nguồn hồi phục |
| **Hồi phục sau công việc** (Sonnentag, Cheng & Parker, 2022) | Hồi phục diễn ra trong giờ nghỉ, buổi tối, cuối tuần; thiếu hồi phục thì mệt tích luỹ | Trừ khi nghỉ quá ít so với giờ đã làm; trừ khi quá giờ (mất thời gian hồi phục buổi tối) |

## 2. Các nguồn (chỉ dùng công bố từ 09/2021)

| # | Nguồn | Loại, cỡ mẫu | Kết luận dùng cho Milo |
| --- | --- | --- | --- |
| 1 | Bakker, Demerouti & Sanz-Vergel (2023). *Job Demands–Resources Theory: Ten Years Later*. Annual Review of Organizational Psychology and Organizational Behavior, 10, 25–53 | Tổng quan lý thuyết | Khung áp lực – nguồn lực của cả công thức |
| 2 | Sonnentag, Cheng & Parker (2022). *Recovery from Work: Advancing the Field Toward the Future*. Annual Review of Organizational Psychology and Organizational Behavior, 9, 33–60 | Tổng quan | Hồi phục trong giờ nghỉ và buổi tối; quá giờ lấy mất hồi phục |
| 3 | Albulescu et al. (2022). *"Give me a break!" A systematic review and meta-analysis on the efficacy of micro-breaks*. PLOS ONE 17(8): e0272460 | Phân tích tổng hợp, 22 mẫu, N = 2.335 | Nghỉ ngắn tăng sức (d = 0,36) và giảm mệt (d = 0,35). Việc nặng cần nghỉ lâu hơn 10 phút |
| 4 | Albulescu et al. (2025). *Short Breaks During the Workday and Employee-Related Outcomes: A Diary Study*. Psychological Reports | Nhật ký hằng ngày, 1 tuần làm việc | Nghỉ ngắn giảm mệt, tăng sức; **tương tác với khối lượng việc**: nghỉ càng quan trọng khi việc nặng |
| 5 | Nurmi & Pakarinen (2023). *Virtual Meeting Fatigue*. Journal of Occupational Health Psychology, 28(6), 343–362 | 44 người làm tri thức, 382 cuộc họp thật, đo nhịp tim | Họp trực tuyến gây mệt mỏi thụ động, làm giảm linh hoạt nhận thức sau họp |
| 6 | Becker, Kaltenegger, Nowak, Weigl & Rohleder (2023). *Biological stress responses to multitasking and work interruptions: A randomized controlled trial*. Psychoneuroendocrinology, 156, 106358 | Thử nghiệm ngẫu nhiên có đối chứng, N = 192 | Đa nhiệm và bị ngắt quãng kích hoạt phản ứng stress sinh học |
| 7 | Kim, Kwon, Yun, Lim, Woo & Kim (2024). *The association between long working hours, shift work, and suicidal ideation*. Scandinavian Journal of Work, Environment & Health | Phân tích tổng hợp 28 nghiên cứu | ≥ 55 giờ/tuần: OR 1,65; > 48 giờ: OR 1,62; **41–54 giờ: không có rủi ro rõ rệt** |
| 8 | Laker, Pereira, Budhwar & Malik (2022). *The Surprising Impact of Meeting-Free Days*. MIT Sloan Management Review | Khảo sát 76 công ty (bài thực tiễn, **không qua bình duyệt**) | 1 ngày không họp mỗi tuần: stress giảm 26% |
| 9 | Talens-Estarelles et al. (2023). *The effects of breaks on digital eye strain, dry eye and binocular vision: Testing the 20-20-20 rule*. Contact Lens and Anterior Eye | Thử nghiệm, 30 người | Quy tắc 20-20-20 **không giảm rõ rệt** triệu chứng mỏi mắt |
| 10 | Kliem et al. (2025). *Psychometric evaluation and updated community norms of the WHO-5 well-being index*. Frontiers in Psychology, 16, 1592614 | N = 2.515, đại diện dân số Đức | WHO-5 tin cậy (α = 0,95); chuẩn dân số M = 67,6, SD = 23,0. Dùng để kiểm chứng điểm Milo |

**Không dùng làm căn cứ:** nghiên cứu sóng não của Microsoft Human Factors Lab về họp liền nhau (công bố 04/2021, trước mốc 5 năm, không qua bình duyệt). Chỉ nhắc để tham khảo.

## 3. Công thức và căn cứ từng khoản

Điểm = `92 − tổng khoản trừ + tổng khoản cộng`, kẹp trong 0–100. Mã nguồn: `src/Minditful.Core/Engine/MoodModel.cs`.

| Khoản | Cách tính | Căn cứ (chiều / ngưỡng) | Phần là lựa chọn thiết kế |
| --- | --- | --- | --- |
| Họp nhiều | trừ 0,1/phút sau 3 giờ họp, tối đa 20 | [5] họp làm mệt, [8] bớt họp giảm stress | Mốc 3 giờ, hệ số 0,1 |
| Họp liền nhau | trừ 4 cho mỗi cuộc từ cuộc thứ 3 trong chuỗi liền (cách < 5 phút), tối đa 12 | [3][4] không có nghỉ giữa các việc thì mệt tích luỹ | Hệ số 4 |
| Làm liền không nghỉ | trừ 0,2/phút sau 90 phút không có lần nghỉ ≥ 5 phút, tối đa 15 | [3][4] nghỉ ngắn giảm mệt | Mốc 90 phút |
| Nghỉ quá ít | khi đã làm ≥ 2 giờ: trừ 0,5/phút thiếu so với mức 45 phút nghỉ / 8 giờ, tối đa 15 | [2] hồi phục trong giờ làm; [4] nghỉ quan trọng hơn khi việc nặng | Mức 45 phút / 8 giờ |
| Quá giờ hôm nay | trừ 0,33/phút, tối đa 25 (+5 nếu bắt đầu sớm hơn 30 phút) | [2] làm ngoài giờ lấy mất hồi phục buổi tối | Hệ số 0,33 |
| **Quá giờ cả tuần** (mới) | trừ 0,05/phút khi quá giờ cả tuần > 8 giờ (tức > 48 giờ/tuần), tối đa 10 | [7] rủi ro rõ từ > 48 giờ/tuần, 41–54 giờ chưa rõ | Hệ số 0,05 |
| Nhảy việc | trừ 2 cho mỗi lần chuyển việc vượt ngưỡng trong 1 giờ, tối đa 12 | [6] đa nhiệm / bị ngắt quãng gây stress | Ngưỡng 8 (ngày mẫu) / 30 (máy thật đếm cả chuyển cửa sổ) |
| Khối lượng việc | trừ 6 hoặc 10 khi số task đang làm gấp 1,5 hoặc 2 lần mức thường | [1] khối lượng việc là áp lực chính | Mốc 1,5 / 2 |
| Task kẹt, email chờ | trừ 2/task kẹt (tối đa 6), 1/email từ email thứ 3 (tối đa 4) | [1] việc dở, việc chờ là áp lực | Hệ số |
| Tự thấy "Mệt" / "Vui" | −6 / +3 | Tự đánh giá; đối chiếu hằng tuần bằng WHO-5 [10] | Hệ số |
| Nguồn hồi phục | +3/lần nghỉ cùng Milo (tối đa 12), +2/task xong (tối đa 6), +4/khối tập trung trọn vẹn (tối đa 8) | [1] nguồn lực; [3] nghỉ ngắn tăng sức | Hệ số |

**Thay đổi sau khi đối chiếu nghiên cứu:**
- Thêm khoản **quá giờ cả tuần > 48 giờ** theo [7]. Trước đây chỉ có quá giờ theo ngày.
- Bỏ quy tắc **20-20-20** vì [9] không thấy tác dụng rõ. Lời nhắc đổi thành **nghỉ ngắn** (uống nước, đứng dậy vươn vai) theo [3][4].

## 4. Chứng minh công thức đúng với mọi dữ liệu

File `tests/Minditful.Core.Tests/MoodEvidenceTests.cs`. Chạy:

```bash
dotnet test tests/Minditful.Core.Tests --filter MoodEvidenceTests
```

Cách làm là **property-based testing**:
- Không kiểm vài ví dụ chọn sẵn, mà sinh **20.000 bộ số liệu ngẫu nhiên**, cố tình gồm cả giá trị cực đoan (họp 10 tiếng, quá giờ 5 tiếng, 40 lần chuyển việc/giờ…).
- Với mỗi bộ, kiểm tra công thức giữ đúng từng kết luận nghiên cứu.
- Phần chạy qua engine thật thì sinh ngẫu nhiên lịch họp và giờ nghỉ cho cả ngày.
- Hạt giống ngẫu nhiên cố định, nên ai chạy cũng ra đúng kết quả đó.

| Tính chất được chứng minh | Căn cứ | Test |
| --- | --- | --- |
| Điểm luôn trong 0–100 | — | `Score_always_stays_between_0_and_100` |
| Tăng **bất kỳ** áp lực nào (10 loại) thì điểm **không bao giờ tăng** | [1] JD-R | `More_demand_never_raises_the_score_JDR` |
| Tăng **bất kỳ** nguồn hồi phục nào (4 loại) thì điểm **không bao giờ giảm** | [1] JD-R | `More_recovery_never_lowers_the_score_JDR` |
| Một lần nghỉ giúp **ít nhất bằng** ở ngày nặng so với ngày nhẹ | [4] tương tác khối lượng việc × nghỉ ngắn | `A_break_helps_at_least_as_much_on_a_heavy_day_as_on_a_light_day` |
| Quá giờ cả tuần chỉ bị trừ khi > 48 giờ; 55 giờ/tuần luôn thấp điểm hơn 45 giờ | [7] | `Weekly_overtime_counts_only_above_48_hours` |
| Chuyển việc bình thường (dưới ngưỡng) không bị trừ; vượt ngưỡng thì bị trừ | [6] | `Normal_switching_is_free_and_fragmentation_costs` |
| Mọi khoản trừ đều có nguồn | — | `Every_penalty_has_a_source` |
| Cùng các cuộc họp, xếp cách nhau 10 phút **không bao giờ** thấp điểm hơn xếp liền nhau (25 lịch ngẫu nhiên, chạy cả ngày qua engine) | [3][4] | `Gaps_between_meetings_never_score_worse_than_back_to_back` |
| Thêm 1 lần nghỉ 10 phút **không bao giờ** làm điểm cuối ngày thấp hơn (25 ngày ngẫu nhiên) | [3][4] | `An_extra_short_break_never_lowers_the_end_of_day_score` |
| Engine luôn tính đúng công thức với số liệu nó đang thấy, dù lịch và giờ nghỉ ra sao | — | `Engine_score_always_equals_the_model_on_its_own_inputs` |

Nếu sau này ai đổi trọng số mà làm công thức đi ngược nghiên cứu, `dotnet test` báo đỏ ngay và in ra bộ số liệu gây lỗi.

**Test chứng minh chiều và ngưỡng, không chứng minh con số tuyệt đối.** Nghiên cứu không nói "họp liền 3 cuộc = trừ đúng 4 điểm". Con số đúng đến đâu phải đo với người thật (mục 5).

## 5. Kiểm chứng với người thật (WHO-5)

**WHO-5 Well-Being Index:**
- 5 câu, mỗi câu 0–5. Điểm = tổng × 4, ra thang 0–100.
- Miễn phí, được kiểm định rộng rãi. Chuẩn dân số Đức 2025: M = 67,6, SD = 23,0 [10].
- Trong app: **bảng điều khiển → Kiểm chứng điểm** (Sandbox và Production).

**Quy trình chạy thử (pilot) đề xuất:**

| Bước | Làm gì |
| --- | --- |
| 1 | Ít nhất **8 người**, chạy Milo **4 tuần** ở Production (hoặc Sandbox chế độ như Production) |
| 2 | Mỗi thứ Sáu (hoặc sáng thứ Hai cho tuần trước) trả lời 5 câu WHO-5 ở trang *Kiểm chứng điểm*. App lưu cặp số (điểm Milo trung bình tuần, WHO-5) |
| 3 | Trang *Kiểm chứng điểm* hiện tương quan Pearson r của từng người sau ≥ 3 tuần |
| 4 | Cuối đợt: mỗi người bấm **Xuất CSV ẩn danh** (mã ngẫu nhiên, không tên), gộp lại. Trong Excel: `=CORREL(diem_milo; who5)` |
| 5 | **Đạt** nếu r gộp ≥ 0,3 (tương quan vừa) và cùng chiều ở đa số người |
| 6 | Chưa đạt: hồi quy WHO-5 theo từng khoản trừ (Excel `LINEST`) để tìm khoản nào đang quá nặng hoặc quá nhẹ, chỉnh hệ số trong `MoodModel.cs`, chạy lại `MoodEvidenceTests` để chắc vẫn đúng chiều nghiên cứu |

**Riêng tư khi kiểm chứng:**
- Chỉ lưu 2 con số mỗi tuần: điểm Milo trung bình tuần và điểm WHO-5.
- Giữ tối đa 12 tuần (`Storage.ValidationWeeks`), vì cần vài tuần mới tính được tương quan. Dữ liệu chi tiết từng ngày vẫn tự xoá theo tuần.
- Nút *Xoá toàn bộ dữ liệu thống kê* xoá cả phần này.

## 6. Giới hạn (nói thẳng khi bị hỏi)

- **Không phải công cụ y tế.** Milo không chẩn đoán kiệt sức hay trầm cảm. WHO-5 chỉ dùng để kiểm chứng, không dùng để kết luận về người dùng.
- **Hiệu ứng sàn:** ngày nặng tới mức điểm chạm 0 thì chỉ số không phân biệt được nữa (thêm nghỉ cũng vẫn là 0). Bộ test đã tìm ra điều này. Trường hợp này chỉ xảy ra khi dồn nhiều áp lực cực đoan cùng lúc; một ngày nặng thường gặp không chạm sàn (test `Floor_effect_needs_extreme_days`).
- **Trọng số chưa được hiệu chỉnh với người thật.** Cần đợt pilot ở mục 5.
- **Bản dịch WHO-5** là bản dịch làm việc, chưa phải bản tiếng Việt đã chuẩn hoá. Hỏi theo tuần thay vì 2 tuần như bản gốc để khớp với báo cáo tuần của Milo.
- **Khác biệt cá nhân:** JD-R ghi nhận cùng áp lực nhưng mỗi người chịu khác nhau [1]. Milo có cá nhân hoá 7 ngày cho lời nhắc, còn trọng số điểm hiện vẫn dùng chung.
- **Nguồn [8]** là khảo sát thực tiễn, không qua bình duyệt, chỉ dùng làm bằng chứng phụ cho khoản "họp nhiều".

## 7. Vì sao không cần LLM

- **LLM không chứng minh được công thức đúng.** Nó chỉ đưa ra nhận xét theo cảm nhận, mỗi lần chạy có thể khác nhau. Không lặp lại được thì không phải bằng chứng.
- **Property-based test** (mục 4) kiểm hàng nghìn trường hợp, chạy lại ra y hệt, miễn phí, và chạy tự động mỗi lần build.
- **Bằng chứng thực tế** chỉ có thể đến từ **người thật** (mục 5), không từ mô hình AI.
- Claude (nếu có API key) vẫn là tuỳ chọn để viết lời thoại tự nhiên hơn và chỉnh điểm tối đa ±10. Không có key, mọi phần trong tài liệu này vẫn chạy đủ.

## 8. Liên kết nguồn

1. Bakker, Demerouti & Sanz-Vergel (2023) — https://doi.org/10.1146/annurev-orgpsych-120920-053933
2. Sonnentag, Cheng & Parker (2022) — https://doi.org/10.1146/annurev-orgpsych-012420-091355
3. Albulescu et al. (2022) — https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0272460
4. Albulescu et al. (2025) — https://journals.sagepub.com/doi/10.1177/00332941251317632
5. Nurmi & Pakarinen (2023) — https://doi.org/10.1037/ocp0000362
6. Becker et al. (2023) — https://pubmed.ncbi.nlm.nih.gov/37542740/
7. Kim et al. (2024) — https://pmc.ncbi.nlm.nih.gov/articles/PMC11472300/
8. Laker et al. (2022) — https://sloanreview.mit.edu/article/the-surprising-impact-of-meeting-free-days/
9. Talens-Estarelles et al. (2023) — https://www.sciencedirect.com/science/article/pii/S1367048422001990
10. Kliem et al. (2025) — https://pmc.ncbi.nlm.nih.gov/articles/PMC12341540/
