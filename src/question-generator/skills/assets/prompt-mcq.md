# Template Prompt — Sinh câu hỏi trắc nghiệm (MCQ)

> **Cách dùng**: Copy toàn bộ phần "PROMPT" dưới đây, điền các giá trị `{...}` với nội dung từ `references/framework.md` (cho tiêu chí + mức cần sinh) và từ `references/examples-mcq.md` (cho few-shot). Dán vào ChatGPT / Claude / Gemini và yêu cầu sinh câu hỏi.

> **Khuyến nghị**: Mỗi lần sinh 5-10 câu. Sinh nhiều hơn 10 trong một batch sẽ giảm chất lượng.

---

## PROMPT

```
[VAI TRÒ]
Bạn là chuyên gia thiết kế đề thi đánh giá năng lực sử dụng AI cho sinh viên/lập trình viên ngành Công nghệ Thông tin Việt Nam. Bạn am hiểu cả về lý thuyết AI/ML và thực tiễn ứng dụng AI trong nghề lập trình hiện đại (2024-2026).

[NHIỆM VỤ]
Tạo {N=10} câu hỏi TRẮC NGHIỆM 4 lựa chọn (A/B/C/D) cho tiêu chí năng lực "{TÊN TIÊU CHÍ}" ở mức "{MỨC}".

[KHUNG NĂNG LỰC THAM CHIẾU]
Tiêu chí: {Tên tiêu chí}
Định nghĩa: {Định nghĩa lấy từ framework.md}
Mức {Mức}:
Hành vi quan sát được:
- {Behavior 1}
- {Behavior 2}
- {Behavior 3}
- {Behavior 4}

[YÊU CẦU CHẤT LƯỢNG]
Mỗi câu hỏi PHẢI:
1. Đo đúng MỘT trong các hành vi quan sát được nêu trên (không cao/thấp hơn mức)
2. Có ngữ cảnh thực tế gắn với nghề lập trình / sinh viên CNTT Việt Nam
3. Tránh câu hỏi "trả lời được nếu nhớ định nghĩa từ điển" — ưu tiên câu hỏi yêu cầu áp dụng/phân tích/đánh giá
4. Phương án sai (distractor) phải hợp lý — phản ánh hiểu lầm phổ biến, không phải sai vô lý
5. CHỈ CÓ MỘT đáp án đúng tuyệt đối
6. Có giải thích đáp án rõ ràng (vì sao đúng, vì sao các đáp án khác sai)
7. Sử dụng tiếng Việt chuẩn; thuật ngữ chuyên ngành tiếng Anh giữ nguyên hoặc kèm tiếng Việt
8. Tránh tham chiếu chỉ đặc thù Mỹ — bối cảnh phải gần với SV/LTV Việt Nam
9. Không tham chiếu công cụ/model đã ngừng phát triển hoặc đổi tên (ví dụ: Bard cũ, Codex)
10. Không trùng lặp với ví dụ mẫu

[ĐA DẠNG HÓA]
Mix các dạng câu hỏi sau trong cùng một batch (mỗi dạng vài câu):
- Định nghĩa khái niệm có ngữ cảnh
- So sánh khái niệm/công cụ/cách tiếp cận
- Nhận diện trường hợp/tình huống
- Dự đoán kết quả/hệ quả
- Lựa chọn cách làm tối ưu

[FORMAT OUTPUT]
Trả lời chính xác theo JSON sau (không có text khác trước/sau JSON):
[
  {
    "id": "C{Mã tiêu chí 1-7}-L{Basic|Int|Adv}-{Số thứ tự 01-99}",
    "criterion": "{Tên tiêu chí}",
    "criterion_id": {1-7},
    "level": "{Cơ bản | Trung cấp | Nâng cao}",
    "type": "mcq",
    "question": "...",
    "options": {"A": "...", "B": "...", "C": "...", "D": "..."},
    "correct_answer": "A | B | C | D",
    "explanation": "Giải thích vì sao đáp án đúng, đáp án khác sai vì sao",
    "behavior_addressed": "Hành vi quan sát được mà câu này đo (chép lại từ khung)",
    "difficulty_estimate": "easy | medium | hard"
  }
]

[VÍ DỤ MẪU CHẤT LƯỢNG TỐT]
Dưới đây là 1-2 câu mẫu cho cùng tiêu chí, đã qua review. Hãy noi gương về chất lượng và cấu trúc, nhưng KHÔNG sinh câu trùng nội dung:

{Dán 1-2 câu mẫu từ examples-mcq.md cùng tiêu chí — có thể chọn câu cùng mức hoặc gần mức}

Hãy sinh {N} câu hỏi mới ngay bây giờ.
```

---

## Mẹo sử dụng prompt

**Chọn few-shot mẫu**:
- Lý tưởng: 1 câu cùng mức yêu cầu + 1 câu mức gần kề (để mô hình hiểu phạm vi)
- Tránh: chọn 2 câu cùng dạng (ví dụ cả 2 đều là "định nghĩa") — sẽ làm LLM bám theo một dạng

**Sau khi LLM trả lời**:
1. Parse JSON kiểm tra cấu trúc đầy đủ
2. Chạy qua `assets/quality-checklist.md` từng câu
3. Sửa nhỏ hoặc loại các câu fail
4. Lưu vào file CSV/JSON tập hợp cho luận văn

**Khi cần sinh thêm batch**:
- Đưa vào prompt batch sau danh sách các câu đã sinh ở batch trước (yêu cầu KHÔNG trùng)
- Hoặc đưa danh sách concept/topic đã cover để tránh lặp

**Khi câu sinh ra quá dễ / quá khó**:
- Quá dễ: thêm vào prompt "Yêu cầu câu hỏi cần áp dụng/phân tích, không chỉ nhớ định nghĩa"
- Quá khó: thêm "Đối tượng là sinh viên năm 3-4 ngành CNTT, không phải chuyên gia AI/ML"
- Quá dài/phức tạp: thêm "Câu hỏi và đáp án không quá 50 từ mỗi phần"
