---
name: ai-cas-vn-question-generation
description: Sinh câu hỏi đánh giá năng lực sử dụng AI bằng tiếng Việt cho dự án AI-CAS-VN (luận văn thạc sĩ KHMT). Sử dụng skill này bất cứ khi nào người dùng yêu cầu sinh/tạo câu hỏi trắc nghiệm (MCQ) hoặc câu hỏi tình huống cho khung năng lực AI 7 tiêu chí × 3 mức (Nền tảng AI, Dữ liệu, Tư duy phản biện, Ứng dụng AI, Đạo đức AI, Công cụ AI, Tương lai công việc), khi nhắc đến "AI-CAS", "khung năng lực AI", "đánh giá năng lực AI", "câu hỏi cho luận văn AI", hoặc khi cần sinh đề thi đánh giá năng lực AI cho sinh viên/lập trình viên CNTT Việt Nam. Áp dụng cả khi người dùng chỉ nói chung chung như "sinh câu hỏi" trong bối cảnh đang làm dự án này.
---

# AI-CAS-VN: Sinh câu hỏi đánh giá năng lực AI

Skill này hỗ trợ sinh câu hỏi đánh giá năng lực sử dụng AI cho sinh viên/lập trình viên ngành CNTT Việt Nam, phục vụ luận văn thạc sĩ với đề tài "Nghiên cứu phương pháp học máy trong đánh giá năng lực sử dụng AI".

Khung gồm **7 tiêu chí × 3 mức** = 21 ô năng lực:

| # | Tiêu chí | Tên tiếng Anh |
|---|----------|---------------|
| 1 | Nền tảng AI | Fundamental |
| 2 | Dữ liệu | Data |
| 3 | Tư duy phản biện | Critical Thinking |
| 4 | Ứng dụng AI trong CNTT | AI Use Cases |
| 5 | Đạo đức AI | AI Ethics |
| 6 | Công cụ AI cho lập trình viên | AI Tools |
| 7 | Tương lai công việc | Future of Work |

Mức: **Cơ bản (Basic) → Trung cấp (Intermediate) → Nâng cao (Advanced)**.

## Quy trình tổng thể

Khi người dùng yêu cầu sinh câu hỏi, làm theo các bước:

1. **Xác định tham số**: tiêu chí (1-7), mức (Cơ bản/Trung cấp/Nâng cao), loại câu hỏi (MCQ / Tình huống), số lượng cần sinh.
   - Nếu người dùng chưa nói rõ, hỏi ngắn gọn để xác định trước khi sinh.
   - Mặc định nếu không nói: 10 câu MCQ cho mức yêu cầu.

2. **Đọc framework cho tiêu chí cần sinh**: mở `references/framework.md` và đọc CHỈ phần của tiêu chí đó (mục tương ứng). Lấy ra: định nghĩa, hành vi quan sát được của mức cần sinh.

3. **Đọc ví dụ mẫu**: mở `references/examples-mcq.md` (cho MCQ) hoặc `references/examples-situational.md` (cho tình huống). Tìm ví dụ cùng tiêu chí và mức, chọn 1-2 câu chất lượng tốt làm few-shot.

4. **Áp dụng template prompt**: đọc `assets/prompt-mcq.md` hoặc `assets/prompt-situational.md`, điền các giá trị `{...}` với nội dung từ bước 2 và 3.

5. **Sinh câu hỏi**: tạo câu hỏi theo đúng format JSON quy định trong template. Sinh số lượng người dùng yêu cầu.

6. **Tự kiểm tra trước khi giao**: chạy qua checklist trong `assets/quality-checklist.md`. Bỏ hoặc sửa các câu fail checklist.

7. **Giao output**: trả lại JSON câu hỏi đã qua tự kiểm. Đánh dấu các câu cần chuyên gia review thêm nếu có nghi ngờ.

## Khi nào dùng loại câu hỏi nào

**MCQ (trắc nghiệm 4 lựa chọn)** — dùng khi:
- Cần đo kiến thức/nhận diện/áp dụng cụ thể
- Cần auto-grade nhanh (dùng cho IRT calibration và CAT)
- Mức Cơ bản và Trung cấp chủ yếu dùng MCQ

**Câu tình huống (structured free text + rubric)** — dùng khi:
- Cần đo tư duy phản biện, ra quyết định, phân tích trade-off
- Mức Trung cấp và Nâng cao
- Đo soft skill (giao tiếp, ưu tiên, quản lý)

Cân bằng khuyến nghị cho cả bộ luận văn: ~80% MCQ + ~20% tình huống.

### Hình thức trả lời câu tình huống: Structured Free Text

Câu tình huống KHÔNG dùng free text thuần túy mở. Thay vào đó, người làm bài được yêu cầu trả lời theo **cấu trúc gợi ý** (instructed structure) gồm 2-4 phần. Ví dụ:
- Phần 1: Phân tích rủi ro / vấn đề chính (40-60 từ)
- Phần 2: Biện pháp đề xuất cụ thể (60-80 từ)
- Phần 3: Trade-off cần cân nhắc (30-50 từ)

Ưu điểm của cách tiếp cận này:
- Người làm bài tập trung vào đúng các khía cạnh cần đo
- Rubric khớp 1-1 với cấu trúc → chấm nhất quán hơn
- LLM-as-judge chấm chính xác và lặp lại được — vì có thể đối chiếu từng phần với rubric tương ứng
- Vẫn giữ tính mở (người làm tự viết nội dung) nhưng có khung

Độ dài trả lời tổng: **100-200 từ** (không phải 150-300 như free text thuần).

### Phương pháp chấm: LLM-as-judge

Câu tình huống được chấm bằng LLM-as-judge (Claude / GPT-4 / Gemini chấm theo rubric), không phải chuyên gia chấm thủ công. Lý do:
- Quy mô luận văn: ~50 thí sinh × 3-5 câu tình huống = 150-250 lượt chấm, khả thi với LLM
- Lặp lại được, có audit trail
- Có thể validate độ tin cậy bằng cách so sánh với chấm chuyên gia trên một subset (mục tiêu Cohen's Kappa ≥ 0.7)

Xem `references/scoring-guide.md` để biết chi tiết về prompt chấm điểm và quy trình validation.

### Kiến trúc bài kiểm tra 2 giai đoạn

Trong hệ thống AI-CAS-VN, đề xuất tách rõ 2 giai đoạn cho thí sinh:

**Giai đoạn 1 — CAT MCQ** (~20-30 câu, ~25 phút):
- Computerized Adaptive Testing dựa trên IRT 2PL/3PL
- Đo độ năng lực ước tính (θ) cho từng tiêu chí
- Chọn câu tiếp theo dựa trên trả lời câu trước
- Auto-grade ngay (đúng/sai)

**Giai đoạn 2 — Fixed-form tình huống** (3-5 câu, ~30 phút):
- Chọn 1 câu tình huống cho mỗi tiêu chí đang yếu (dựa trên θ từ Giai đoạn 1)
- Mức độ câu tình huống phù hợp với θ ước tính
- Chấm bằng LLM-as-judge sau khi nộp bài (không real-time)
- Mục đích: xác thực và đào sâu năng lực

Lý do tách 2 giai đoạn: MCQ và tình huống có UX, scoring logic, và adaptivity hoàn toàn khác nhau. Tách rõ giúp đơn giản hóa kiến trúc, tránh phức tạp hóa CAT engine.

## Quy tắc chất lượng cốt lõi

Mỗi câu sinh ra PHẢI:

1. **Đo đúng MỘT hành vi** trong khung — không cao/thấp hơn mức yêu cầu. Nếu khó định mức, mặc định mức thấp hơn.
2. **Có ngữ cảnh thực tế Việt Nam** — công ty/sản phẩm/quy định Việt Nam khi phù hợp; tránh ngữ cảnh chỉ Mỹ/EU.
3. **Cập nhật 2024-2026** — dùng tool/model hiện hành (Claude, GPT-4o, Gemini, Cursor, Copilot, MCP, RAG); tránh tool đã chết (Bard cũ, Codex cũ).
4. **Distractor hợp lý** — đáp án sai phải phản ánh hiểu lầm phổ biến, không sai vô lý.
5. **Một đáp án đúng tuyệt đối** — không "tất cả đều đúng" hay "tùy quan điểm" cho MCQ.
6. **Tiếng Việt chuẩn** — thuật ngữ chuyên ngành tiếng Anh giữ nguyên hoặc kèm tiếng Việt; không dịch ép.

## Cấu trúc tham chiếu các file

```
ai-cas-vn-question-generation/
├── SKILL.md (file này — workflow chính)
├── references/
│   ├── framework.md         (Khung 7 tiêu chí × 3 mức — định nghĩa + hành vi)
│   ├── examples-mcq.md      (21 câu MCQ mẫu chất lượng đã chuẩn)
│   ├── examples-situational.md (7 câu tình huống mẫu kèm rubric)
│   └── scoring-guide.md     (Hướng dẫn chấm tình huống bằng LLM-as-judge)
└── assets/
    ├── prompt-mcq.md           (Template prompt cho MCQ)
    ├── prompt-situational.md   (Template prompt cho tình huống)
    └── quality-checklist.md    (Checklist 10 mục QC)
```

**Cách đọc references**: KHÔNG đọc toàn bộ framework.md cùng lúc. Chỉ đọc phần tiêu chí đang cần. Mỗi tiêu chí trong framework.md được đánh dấu rõ bằng heading `## Tiêu chí N`.

## Format output

Mặc định trả về JSON array (cho dễ import vào hệ thống/spreadsheet). Nếu người dùng yêu cầu cụ thể (Markdown, plain text, file), điều chỉnh.

Schema MCQ:
```json
{
  "id": "C{tiêu chí}-L{mức}-{số thứ tự}",
  "criterion": "Tên tiêu chí",
  "criterion_id": 1,
  "level": "Cơ bản | Trung cấp | Nâng cao",
  "type": "mcq",
  "question": "Nội dung câu hỏi",
  "options": {"A": "...", "B": "...", "C": "...", "D": "..."},
  "correct_answer": "B",
  "explanation": "Vì sao đáp án này đúng, các đáp án khác sai vì sao",
  "behavior_addressed": "Hành vi quan sát được mà câu này đo",
  "difficulty_estimate": "easy | medium | hard"
}
```

Schema tình huống:
```json
{
  "id": "C{tiêu chí}-L{mức}-SIT-{số}",
  "criterion": "Tên tiêu chí",
  "criterion_id": 1,
  "level": "Trung cấp | Nâng cao",
  "type": "situational",
  "title": "Tiêu đề ngắn (8-12 từ)",
  "context": "Bối cảnh 3-6 câu",
  "question": "Câu hỏi mở",
  "response_structure": [
    "Phần 1: [Tên phần] ([khoảng từ])",
    "Phần 2: [Tên phần] ([khoảng từ])",
    "Phần 3: [Tên phần] ([khoảng từ])"
  ],
  "expected_response_length": "100-200 từ tổng",
  "scoring_method": "llm_as_judge",
  "rubric": [
    "(1đ) [Phần X] Tiêu chí cụ thể — kiểm tra được trong câu trả lời (ví dụ: 'có nhận diện được Y' chứ không phải 'hay/tốt')",
    "(1đ) [Phần X] ...",
    "(1đ) [Phần X] ...",
    "(1đ) [Phần X] ...",
    "(1đ) [Phần X] ..."
  ],
  "judge_prompt_hints": "Gợi ý cho LLM-as-judge: khi chấm, kiểm tra cụ thể... (ví dụ: thuật ngữ cần dùng, framework cần đề cập, từ khóa nhận diện)",
  "behaviors_addressed": ["Hành vi 1", "Hành vi 2"]
}
```

Quan trọng: mỗi tiêu chí chấm trong `rubric` phải có **prefix `[Phần X]`** chỉ rõ phần nào của response structure được chấm. Điều này giúp LLM-as-judge xác định đúng vùng văn bản để đối chiếu, tránh fail vì thí sinh viết đúng nhưng ở phần khác.

## Sau khi sinh: bước tiếp theo cho người dùng

Khi giao câu hỏi cho người dùng, nhắc họ về các bước downstream:

1. **Tự duyệt** — đọc lại từng câu, sửa các lỗi nhỏ về ngôn ngữ, chỉnh tone.
2. **Kiểm định nội dung (Content Validity)** — gửi cho 3-5 chuyên gia chấm Likert-4 trên 5 mục; tính I-CVI ≥ 0.78 mới giữ.
3. **Pilot test IRT** — sau khi qua CVI, cho ~100-150 SV làm để hiệu chuẩn độ khó (b) và độ phân biệt (a); loại câu có a < 0.5.
4. **Cuối cùng** — bộ câu hỏi đã qua 3 vòng lọc sẵn sàng cho thực nghiệm.

Không tự cho rằng câu sinh ra đã đủ chất lượng vào thẳng pilot — luôn cần CVI.

## Một số mẹo thực tế khi sinh

- **Đa dạng hóa dạng câu**: trong cùng một batch 10 câu, mix các dạng (định nghĩa có ngữ cảnh, so sánh, nhận diện tình huống, dự đoán hệ quả, lựa chọn cách làm tối ưu). Tránh 10 câu cùng dạng định nghĩa.
- **Tránh trùng ý**: kiểm tra không có 2 câu hỏi cùng một concept với wording khác. Khi nghi ngờ, viết tóm tắt 1 dòng cho từng câu và so sánh.
- **Difficulty estimate**: dùng intuition của mình; mức Cơ bản thường easy, Trung cấp medium, Nâng cao hard. Sẽ được hiệu chỉnh chính xác sau pilot IRT.
- **Few-shot trong prompt**: chọn 1-2 ví dụ mẫu có CHẤT LƯỢNG TỐT NHẤT cùng tiêu chí — không chọn 2 câu cùng dạng (sẽ làm LLM bám sát một dạng).
- **Khi cần sinh số lượng lớn**: chia nhỏ thành các batch 5-10 câu, sau mỗi batch tự duyệt trước khi sinh batch tiếp theo — nếu để sinh 50 câu một lúc thì chất lượng giảm dần.
