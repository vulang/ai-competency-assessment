# Template Prompt — Sinh câu hỏi tình huống (Structured Free Text)

> **Cách dùng**: Copy phần "PROMPT" dưới đây, điền các giá trị `{...}` với nội dung từ `references/framework.md` và `references/examples-situational.md`.

> **Khuyến nghị**: Mỗi lần sinh 2-3 câu tình huống.

> **Hình thức trả lời**: Câu tình huống dùng **structured free text** (thí sinh trả lời theo cấu trúc gợi ý 2-4 phần), KHÔNG dùng free text mở thuần túy. Chấm bằng **LLM-as-judge** theo rubric.

> **Lưu ý mức**: Câu tình huống thường ở mức Trung cấp hoặc Nâng cao. Mức Cơ bản không cần tình huống.

---

## PROMPT

```
[VAI TRÒ]
Bạn là chuyên gia thiết kế case study đánh giá năng lực sử dụng AI cho sinh viên/lập trình viên CNTT Việt Nam. Bạn am hiểu cả lý thuyết AI/ML và thực tiễn nghề lập trình hiện đại (2024-2026).

[NHIỆM VỤ]
Tạo {N=3} câu hỏi TÌNH HUỐNG dạng STRUCTURED FREE TEXT cho tiêu chí "{TÊN TIÊU CHÍ}" ở mức "{MỨC}". Câu trả lời sẽ được chấm bằng LLM-as-judge theo rubric.

[KHUNG NĂNG LỰC THAM CHIẾU]
Tiêu chí: {Tên tiêu chí}
Định nghĩa: {Định nghĩa lấy từ framework.md}
Mức {Mức}:
Hành vi quan sát được:
- {Behavior 1}
- {Behavior 2}
- {Behavior 3}
- {Behavior 4}

[YÊU CẦU CHẤT LƯỢNG TÌNH HUỐNG]
Mỗi câu phải:
1. Có bối cảnh cụ thể, sinh động (3-6 câu) — đặt người làm vào vai sinh viên / nhân viên / lead trong tình huống thực tế
2. Câu hỏi mở — yêu cầu phân tích/đề xuất/đánh giá; không có một đáp án đúng duy nhất
3. Bối cảnh gắn với ngữ cảnh Việt Nam khi có thể (công ty Việt, khách hàng Việt, Luật Bảo vệ dữ liệu cá nhân 2023, v.v.)
4. Bối cảnh có ít nhất 1 trade-off rõ ràng buộc người làm phải ưu tiên — tránh tình huống có "win-win" hiển nhiên
5. Bối cảnh đủ thông tin để trả lời nhưng KHÔNG "lộ" đáp án

[YÊU CẦU VỀ RESPONSE STRUCTURE]
Mỗi câu PHẢI có "response_structure" — 2-4 phần thí sinh phải trả lời, mỗi phần có:
- Tên phần ngắn gọn (ví dụ: "Phân tích rủi ro", "Biện pháp đề xuất", "Trade-off cần cân nhắc")
- Khoảng từ kỳ vọng (ví dụ: "40-60 từ")

Cấu trúc phổ biến (chọn phù hợp với câu hỏi):
- 3 phần cho phần lớn câu: (Phân tích / Đề xuất / Trade-off) HOẶC (Nhận diện vấn đề / Quy trình / Rủi ro tiếp theo) HOẶC (Kế hoạch giai đoạn 1 / giai đoạn 2 / Chỉ số đo lường)
- 2 phần cho câu đơn giản hơn: (Phân tích + Đề xuất)
- 4 phần cho câu phức tạp (mức Nâng cao): (Phân tích / Đề xuất / Trade-off / Rủi ro implementation)

Tổng độ dài: 100-200 từ.

[YÊU CẦU VỀ RUBRIC — RẤT QUAN TRỌNG]
Rubric phải KHỚP với response structure và phải LLM-FRIENDLY:
1. Đúng 5 tiêu chí, mỗi tiêu chí 1 điểm (tổng 5 điểm)
2. Mỗi tiêu chí có prefix "[Phần X]" chỉ rõ phần nào của response được chấm — ví dụ: "[Phần 1]", "[Phần 2-3]"
3. Mỗi tiêu chí mô tả CỤ THỂ điều cần kiểm tra trong câu trả lời — đủ rõ để LLM hoặc người khác đối chiếu được. Tránh các từ mơ hồ:
   - SAI: "Câu trả lời sâu sắc và thuyết phục"
   - SAI: "Tone giao tiếp phù hợp"
   - ĐÚNG: "[Phần 3] Có đề cập việc trao đổi với đồng đội/sếp trước khi quyết định; có ít nhất 1 câu thể hiện tinh thần hợp tác"
   - ĐÚNG: "[Phần 2] Đề xuất ít nhất 2 biện pháp kỹ thuật cụ thể (ví dụ: rate limit, PII redaction, audit log, RBAC, v.v.) — không chỉ nói chung 'cần bảo mật'"
4. Phân bổ tiêu chí cân bằng các loại kỹ năng:
   - 1-2 tiêu chí về kiến thức kỹ thuật chính xác (đúng/sai khái niệm)
   - 1-2 tiêu chí về tư duy phân tích / phản biện / trade-off
   - 1-2 tiêu chí về soft skill / approach (giao tiếp, ưu tiên, escalation)

[YÊU CẦU VỀ judge_prompt_hints]
Đây là gợi ý cho LLM-as-judge khi chấm câu này. Cần bao gồm:
- Thuật ngữ chuyên ngành kỳ vọng thí sinh dùng (nếu có)
- Framework / quy định kỳ vọng đề cập (nếu có)
- Cảnh báo các hiểu nhầm phổ biến (để LLM không chấm sai)
- 1-2 câu

[ĐA DẠNG HÓA TÌNH HUỐNG]
Nếu sinh nhiều câu cho cùng tiêu chí, mix các loại bối cảnh:
- Tình huống cá nhân (sinh viên / lập trình viên cá nhân)
- Tình huống team (trao đổi với đồng nghiệp / sếp)
- Tình huống tổ chức (quyết định cấp công ty / chiến lược)
- Tình huống có deadline / áp lực
- Tình huống có conflict giữa stakeholders

[FORMAT OUTPUT]
Trả lời chính xác theo JSON sau (không có text khác trước/sau JSON):
[
  {
    "id": "C{Mã tiêu chí 1-7}-L{Int|Adv}-SIT-{Số}",
    "criterion": "{Tên tiêu chí}",
    "criterion_id": {1-7},
    "level": "{Trung cấp | Nâng cao}",
    "type": "situational",
    "title": "Tiêu đề ngắn (8-12 từ)",
    "context": "Bối cảnh 3-6 câu...",
    "question": "Câu hỏi mở rõ ràng...",
    "response_structure": [
      "Phần 1: [Tên phần] (X-Y từ)",
      "Phần 2: [Tên phần] (X-Y từ)",
      "Phần 3: [Tên phần] (X-Y từ)"
    ],
    "expected_response_length": "100-200 từ tổng",
    "scoring_method": "llm_as_judge",
    "rubric": [
      "(1đ) [Phần X] Tiêu chí cụ thể, kiểm tra được",
      "(1đ) [Phần X] Tiêu chí cụ thể, kiểm tra được",
      "(1đ) [Phần X] Tiêu chí cụ thể, kiểm tra được",
      "(1đ) [Phần X] Tiêu chí cụ thể, kiểm tra được",
      "(1đ) [Phần X] Tiêu chí cụ thể, kiểm tra được"
    ],
    "judge_prompt_hints": "Gợi ý cho LLM-as-judge: kiểm tra X, Y, Z; chú ý hiểu nhầm A",
    "behaviors_addressed": ["Hành vi 1...", "Hành vi 2..."]
  }
]

[VÍ DỤ MẪU CHẤT LƯỢNG TỐT]
Câu mẫu cùng tiêu chí, đã có response_structure + rubric LLM-friendly. Học theo cấu trúc và chất lượng, KHÔNG trùng nội dung:

{Dán 1 câu mẫu từ examples-situational.md cùng tiêu chí}

Hãy sinh {N} câu mới ngay bây giờ.
```

---

## Mẹo sử dụng

**Khi rubric LLM sinh ra quá mơ hồ** (dùng từ "tốt", "hay", "phù hợp" mà không định lượng):
- Thêm vào prompt: "Mỗi tiêu chí rubric phải có thể kiểm tra bằng cách đếm hoặc nhận diện cụ thể trong câu trả lời. Sử dụng các động từ kiểm chứng được như 'có đề cập', 'liệt kê được', 'tham chiếu', 'phân biệt'. Tránh tuyệt đối các tính từ chủ quan như 'tốt', 'hay', 'sâu sắc', 'thuyết phục'."

**Khi response_structure quá chung chung**:
- Yêu cầu prompt: "Tên mỗi phần phải cụ thể với bối cảnh câu hỏi — không dùng tên chung chung như 'Phân tích' mà nên 'Phân tích rủi ro pháp lý cho dynamic pricing'."

**Khi rubric không khớp với response_structure**:
- Đây là lỗi phổ biến. Kiểm tra mỗi tiêu chí rubric có prefix `[Phần X]` không, và phần đó có tồn tại trong response_structure không. Nếu không khớp → bỏ hoặc sửa.

**Để câu chấm bằng LLM-as-judge nhất quán hơn**:
- Khi sinh xong, thử dùng prompt trong `references/scoring-guide.md` cho LLM chấm 2-3 đáp án thử (đáp án xuất sắc / trung bình / yếu) — xem điểm có hợp lý không. Nếu LLM chấm thiếu/thừa điểm, sửa rubric thêm cụ thể.

**Khi LLM sinh câu tình huống "đáp án quá hiển nhiên"**:
- Thêm trade-off rõ vào bối cảnh (deadline gấp, budget hạn chế, conflict giữa stakeholders, quy định pháp lý phức tạp)
- Yêu cầu: "Bối cảnh phải có ít nhất 1 trade-off rõ ràng buộc người làm phải ưu tiên"

**Mức Cơ bản**:
Nếu cần đo mức Cơ bản cho tiêu chí nào đó bằng tình huống (hiếm khi cần), giảm độ phức tạp:
- Bối cảnh ngắn hơn (2-3 câu)
- Chỉ 2 phần trong response_structure
- Độ dài 60-120 từ
