# Checklist Kiểm tra Chất lượng Câu hỏi (10 mục)

> **Cách dùng**: Sau khi LLM sinh câu hỏi, duyệt từng câu theo checklist này. Câu nào fail ≥2 mục thì **loại** hoặc **sửa**. Câu nào fail 1 mục → cân nhắc sửa, nếu fail nhẹ thì giữ và đánh dấu chờ chuyên gia review.

## Cho câu trắc nghiệm (MCQ)

### Mục 1: Đúng tiêu chí và mức
- [ ] Câu đo HÀNH VI nằm trong khung của tiêu chí ghi trên?
- [ ] Mức độ phù hợp (Basic/Int/Adv)? Không quá cao hoặc quá thấp?

### Mục 2: Một đáp án đúng tuyệt đối
- [ ] Chỉ có 1 đáp án đúng — đáp án đúng đúng tuyệt đối, không có debate?
- [ ] Test bằng cách hỏi đồng nghiệp / dùng LLM khác xem có đồng thuận không?

### Mục 3: Distractor hợp lý
- [ ] Các đáp án sai có "vẻ" hợp lý — phản ánh hiểu lầm phổ biến?
- [ ] Không có đáp án "rõ ràng vô lý" (ví dụ: "AI là người ngoài hành tinh")?
- [ ] Độ dài các option tương đương nhau (đáp án đúng không dài hơn hẳn các đáp án khác)?

### Mục 4: Sự kiện đúng
- [ ] Tên tool, model, paper, paper author, năm — đúng và còn cập nhật?
- [ ] Không tham chiếu công cụ đã chết (Bard cũ, Codex cũ, ...)?

### Mục 5: Tiếng Việt và thuật ngữ
- [ ] Tiếng Việt rõ ràng, không có lỗi ngữ pháp?
- [ ] Thuật ngữ chuyên ngành dùng đúng — tiếng Anh giữ nguyên hoặc kèm tiếng Việt, không dịch ép?
- [ ] Câu không có từ thừa, không tối nghĩa?

### Mục 6: Bối cảnh Việt Nam
- [ ] Khi câu hỏi có bối cảnh, có gần với SV/LTV Việt Nam không?
- [ ] Không quá lệch sang context Mỹ/EU (đôi khi cần thiết về regulation, nhưng nên có bản địa hóa khi có thể)?

### Mục 7: Cấu trúc câu hỏi
- [ ] Câu hỏi rõ ràng, không hiểu hai nghĩa?
- [ ] 4 đáp án mutually exclusive (không có overlap rõ)?
- [ ] Không có "Tất cả đáp án trên đều đúng" hoặc "Không đáp án nào đúng" (trừ khi có lý do mạnh)?

### Mục 8: Không trùng lặp
- [ ] Câu này không trùng ý với câu khác trong bộ?
- [ ] Nếu khó nhớ, viết tóm tắt 1 dòng (concept đo) và so sánh với các câu khác?

### Mục 9: Giải thích đầy đủ
- [ ] Phần explanation giải thích VÌ SAO đáp án đúng và VÌ SAO các đáp án khác sai?
- [ ] Đủ để người làm hiểu, không cần tra thêm?

### Mục 10: Đánh giá độ khó định tính
- [ ] Độ khó (difficulty_estimate) hợp lý với mức ghi?
- [ ] Câu Basic: trả lời được nếu đã học khái niệm cơ bản 1-2 buổi
- [ ] Câu Intermediate: cần đã thực hành 1-2 dự án
- [ ] Câu Advanced: cần kinh nghiệm thực tế hoặc đào sâu chuyên môn

---

## Cho câu tình huống (Structured Free Text)

> Câu tình huống dùng structured free text + LLM-as-judge. Checklist này kiểm tra cả phần câu hỏi VÀ phần response_structure / rubric.

### Mục 1: Bối cảnh đầy đủ và sinh động
- [ ] Có vai trò, ngữ cảnh tổ chức, ràng buộc rõ?
- [ ] Đủ thông tin để trả lời nhưng không "lộ" đáp án?
- [ ] 3-6 câu, không dài lê thê, không quá ngắn?
- [ ] Có ít nhất 1 trade-off / conflict / ràng buộc rõ ràng?

### Mục 2: Câu hỏi mở thực sự
- [ ] Có nhiều cách trả lời tốt khác nhau (không chỉ 1 "đáp án")?
- [ ] Yêu cầu phân tích/đề xuất/đánh giá, không chỉ recall?

### Mục 3: Response structure hợp lý
- [ ] Có 2-4 phần được chỉ định rõ?
- [ ] Mỗi phần có tên CỤ THỂ với bối cảnh (không chung chung như "Phân tích" mà "Phân tích rủi ro pháp lý")?
- [ ] Mỗi phần có khoảng từ kỳ vọng rõ ràng?
- [ ] Tổng độ dài 100-200 từ?

### Mục 4: Rubric LLM-friendly
- [ ] Đúng 5 tiêu chí, tổng 5 điểm?
- [ ] MỖI tiêu chí có prefix `[Phần X]` chỉ rõ phần nào của response được chấm?
- [ ] Mỗi tiêu chí KHÔNG dùng từ mơ hồ: "tốt", "hay", "sâu sắc", "phù hợp", "thuyết phục"?
- [ ] Mỗi tiêu chí mô tả CỤ THỂ điều cần kiểm tra (có động từ kiểm chứng: "có đề cập", "liệt kê được", "nhận diện", "phân biệt")?
- [ ] Khi yêu cầu số lượng ("ít nhất N yếu tố"), có ghi rõ N và liệt kê các yếu tố hợp lệ?

### Mục 5: Rubric khớp với response structure
- [ ] Mỗi tiêu chí rubric có thuộc về một Phần có thật trong response_structure?
- [ ] Không có Phần nào trong response_structure mà rubric không chấm?
- [ ] Phân bổ tiêu chí cân bằng giữa các phần (không tập trung hết vào 1 phần)?

### Mục 6: Rubric mix các loại kỹ năng
- [ ] Có 1-2 tiêu chí về kiến thức kỹ thuật chính xác (đúng/sai khái niệm)?
- [ ] Có 1-2 tiêu chí về tư duy phân tích / phản biện / trade-off?
- [ ] Có 1-2 tiêu chí về soft skill / approach (giao tiếp, ưu tiên, escalation)?

### Mục 7: Có judge_prompt_hints chất lượng
- [ ] Có hint cho LLM-as-judge gì cần kiểm tra cụ thể?
- [ ] Có cảnh báo các hiểu nhầm/sai lầm phổ biến để LLM không chấm sai?
- [ ] Có mô tả khi nào tính/không tính điểm cho tiêu chí dễ gây tranh cãi?

### Mục 8: Phù hợp mức năng lực
- [ ] Mức Trung cấp/Nâng cao có độ phức tạp tương ứng?
- [ ] Không quá khó (đòi hỏi senior khi mục tiêu là junior)?
- [ ] Không quá dễ (mức Nâng cao mà trả lời được không cần kinh nghiệm)?

### Mục 9: Sự kiện và bối cảnh đúng
- [ ] Nếu nhắc luật/quy định (GDPR, EU AI Act, Luật BVDLCN VN 2023), đúng và còn hiệu lực?
- [ ] Tool/công nghệ trong bối cảnh cập nhật?
- [ ] Bối cảnh Việt Nam khi phù hợp (không nhồi nhét nhưng không bỏ qua)?

### Mục 10: Không trùng và có thể chấm test
- [ ] Tình huống không lặp lại ý của câu khác trong bộ?
- [ ] Khi cho LLM chấm thử 2-3 đáp án (xuất sắc/trung bình/yếu), điểm có hợp lý không?
- [ ] Nếu LLM chấm không nhất quán (chạy 3 lần có 3 kết quả khác nhau), cần sửa rubric thêm cụ thể

---

## Tỷ lệ chấp nhận thực tế

Theo kinh nghiệm với LLM hiện tại (2024-2026):
- ~70% câu sinh ra sẽ pass checklist ngay
- ~20% cần chỉnh nhỏ (sửa từ ngữ, điều chỉnh distractor, sửa nhỏ trong giải thích)
- ~10% phải bỏ (sai sự kiện, không đo đúng tiêu chí, đáp án không rõ)

Nếu tỷ lệ bỏ > 20%, cần cải thiện prompt:
- Thêm few-shot tốt hơn
- Cụ thể hóa yêu cầu trong prompt
- Chia nhỏ batch (5 câu thay vì 10)

## Định dạng đánh dấu sau review

Trong file dataset cuối, thêm trường:
```json
{
  "review_status": "pending | accepted | needs_edit | rejected",
  "review_notes": "Ghi chú từ người duyệt",
  "checklist_failures": ["mục 3", "mục 8"]  // nếu có
}
```

Khi đưa qua chuyên gia kiểm định Content Validity (CVI), chỉ gửi các câu `accepted` hoặc `needs_edit` (đã sửa).
