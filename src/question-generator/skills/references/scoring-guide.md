# Hướng dẫn chấm câu tình huống bằng LLM-as-judge

> File này hướng dẫn cách dùng LLM (Claude/GPT-4/Gemini) để chấm câu tình huống theo rubric. Bao gồm: prompt template cho việc chấm, quy trình validation độ tin cậy với chuyên gia, và mẹo thực tế.

## Tại sao dùng LLM-as-judge

Câu tình huống dùng **structured free text** (thí sinh trả lời theo cấu trúc gợi ý). Có 3 lựa chọn để chấm:

| Phương pháp | Ưu | Nhược | Phù hợp |
|-------------|-----|------|---------|
| Chuyên gia chấm thủ công | Chuẩn vàng, có nuance | Không scale (1500 lượt chấm cho 50 thí sinh × 30 câu) | Validation subset |
| LLM-as-judge | Scale tốt, lặp lại được, có audit trail | Cần validation, có thể miss nuance | Chấm chính cho luận văn |
| Hybrid | Cân bằng | Phức tạp hơn | Khi cần độ tin cậy cao |

Khuyến nghị cho luận văn AI-CAS-VN: **LLM-as-judge làm chính, chuyên gia validate ngẫu nhiên ~20% câu trả lời** để báo cáo Cohen's Kappa trong chương kết quả.

## Prompt template cho LLM-as-judge

Sao chép và điền các giá trị `{...}` với thông tin của câu hỏi cụ thể:

```
[VAI TRÒ]
Bạn là chuyên gia chấm điểm bài thi đánh giá năng lực sử dụng AI. Bạn chấm rất nghiêm túc, công bằng, và nhất quán — không thiên vị, không bù điểm vì câu trả lời "hay" mà không khớp rubric.

[NHIỆM VỤ]
Chấm câu trả lời của thí sinh cho câu hỏi tình huống dưới đây theo rubric đã cho. Mỗi tiêu chí rubric đáng 1 điểm — chỉ tính ĐỦ điểm hoặc 0 điểm cho mỗi tiêu chí (KHÔNG có 0.5 điểm). Tổng tối đa 5 điểm.

[CÂU HỎI]
Tiêu đề: {title}

Bối cảnh: {context}

Câu hỏi: {question}

Cấu trúc trả lời yêu cầu:
{response_structure}

[RUBRIC CHẤM ĐIỂM]
{rubric — mỗi item một dòng}

[GỢI Ý CHO NGƯỜI CHẤM]
{judge_prompt_hints}

[CÂU TRẢ LỜI CỦA THÍ SINH]
{student_response}

[YÊU CẦU OUTPUT]
Chấm theo từng tiêu chí một. Trả lời chính xác theo JSON sau:

{
  "scores": [
    {
      "criterion_num": 1,
      "criterion_text": "Chép lại tiêu chí rubric 1",
      "passed": true | false,
      "evidence": "Trích dẫn đoạn cụ thể trong câu trả lời thí sinh chứng minh đạt/không đạt (1-2 câu). Nếu không đạt, giải thích thiếu gì.",
      "score": 0 | 1
    },
    ... // 5 tiêu chí
  ],
  "total_score": <0-5>,
  "summary_feedback": "Nhận xét ngắn 1-3 câu về câu trả lời tổng thể (điểm mạnh chính, điểm cần cải thiện chính)"
}

[NGUYÊN TẮC CHẤM]
1. Chấm theo rubric, KHÔNG theo trực giác. Nếu câu trả lời hay nhưng không khớp rubric → không tính điểm
2. Nếu thí sinh đáp ứng tiêu chí ở phần SAI của response (ví dụ trả lời nội dung Phần 2 trong Phần 3) — VẪN tính điểm nếu nội dung đúng, nhưng note trong evidence
3. Khi rubric yêu cầu "ít nhất N yếu tố" — đếm cụ thể N yếu tố trong câu trả lời, không đoán
4. Khi rubric yêu cầu cụm/từ khóa cụ thể (ví dụ "tên framework"), kiểm tra exact match hoặc paraphrase chấp nhận được
5. Đối với tiêu chí về tone/approach — chấm theo dấu hiệu cụ thể trong judge_prompt_hints, không đoán cảm xúc
```

## Quy trình validation độ tin cậy

Trước khi dùng LLM-as-judge cho toàn bộ luận văn, cần validate. Quy trình:

### Bước 1: Tạo bộ validation
- Chọn 20-30 câu trả lời thật từ pilot test (mix mức độ: 5 xuất sắc, 10 trung bình, 5 yếu, 5 ngoài rìa)
- Cover ít nhất 3-4 tiêu chí khác nhau

### Bước 2: Chuyên gia chấm độc lập
- 2 chuyên gia chấm độc lập, không trao đổi
- Thảo luận khi bất đồng > 1 điểm
- Đây là "ground truth"

### Bước 3: LLM chấm cùng bộ
- Dùng cùng prompt template ở trên
- Chạy 3 lần với mỗi câu (đo độ ổn định nội tại của LLM) — lấy trung bình hoặc majority
- Có thể test 2-3 LLM khác nhau (Claude / GPT-4 / Gemini) chọn cái phù hợp nhất

### Bước 4: Tính độ tin cậy
- **Cohen's Kappa** giữa LLM và chuyên gia (ngưỡng chấp nhận: ≥ 0.7 = good, ≥ 0.8 = excellent)
- **Mean Absolute Error** trên thang điểm 0-5 (ngưỡng: ≤ 0.5)
- **Pearson/Spearman correlation** (ngưỡng: ≥ 0.8)

### Bước 5: Quyết định
- Nếu Kappa ≥ 0.7: dùng LLM-as-judge làm chính, validate ngẫu nhiên 10-20%
- Nếu Kappa 0.5-0.7: cải thiện prompt (thêm few-shot, làm rõ rubric), test lại
- Nếu Kappa < 0.5: rubric có vấn đề — sửa câu hỏi để rubric đo được tốt hơn

### Bước 6: Báo cáo trong luận văn
- Chương Phương pháp: mô tả LLM-as-judge + validation
- Chương Kết quả: báo cáo Kappa, MAE, correlation
- Phụ lục: ví dụ câu trả lời và đánh giá từ LLM + chuyên gia

## Few-shot trong prompt chấm (nâng cao)

Khi rubric phức tạp hoặc kết quả không nhất quán, thêm 2-3 ví dụ vào prompt:

```
[VÍ DỤ CHẤM MẪU]

Ví dụ 1 — Câu trả lời ĐẠT TỐT (5/5):
{Câu trả lời mẫu xuất sắc}
Chấm:
- Tiêu chí 1: ĐẠT, vì "{evidence}"
- ...
- Tổng: 5/5

Ví dụ 2 — Câu trả lời TRUNG BÌNH (3/5):
{Câu trả lời mẫu trung bình}
Chấm:
- Tiêu chí 1: ĐẠT, vì "{evidence}"
- Tiêu chí 2: KHÔNG ĐẠT, vì thiếu "{yếu tố cụ thể}"
- ...
- Tổng: 3/5

Ví dụ 3 — Câu trả lời YẾU (1/5):
{Câu trả lời mẫu yếu}
...
```

Nên tạo bộ ví dụ này cho mỗi tiêu chí năng lực, lưu trong `assets/judge-examples/` (chưa có sẵn — bạn cần xây sau khi có pilot data).

## Mẹo thực tế khi dùng LLM-as-judge

**Chọn LLM nào**:
- Claude (Anthropic) thường nghiêm và nhất quán hơn cho tasks chấm điểm có rubric
- GPT-4o nhanh và rẻ hơn nhưng có khuynh hướng "rộng tay" với điểm
- Gemini biến động hơn, không khuyến nghị làm chính
- Chạy thử cùng 1 câu với 2-3 LLM trong validation phase, chọn LLM nhất quán nhất

**Khi LLM "rộng tay" (cho điểm cao bất thường)**:
- Thêm vào prompt: "Bạn có khuynh hướng cho điểm rộng. Hãy chấm như giảng viên khó tính — chỉ cho điểm khi câu trả lời thỏa MÃ rubric, không phải khi nó 'hợp lý'."
- Yêu cầu trả về evidence CỤ THỂ — không cho điểm nếu không trích dẫn được

**Khi LLM "chặt tay" (cho điểm thấp bất thường)**:
- Kiểm tra rubric — có đòi hỏi quá cao không?
- Thêm vào prompt: "Tiêu chí 'có đề cập' không yêu cầu đầy đủ và chi tiết, chỉ cần xuất hiện rõ trong câu trả lời."

**Khi chấm câu trả lời tiếng Việt có thuật ngữ Anh**:
- LLM thường hiểu được cả tiếng Việt và Anh, nhưng kiểm tra Vietnamese-specific terms (ví dụ: "Luật Bảo vệ dữ liệu cá nhân", "phân biệt giá")
- Có thể thêm glossary nhỏ trong prompt nếu cần

**Khi câu trả lời thí sinh quá ngắn hoặc bỏ phần**:
- Nếu một phần response_structure bỏ trống → tất cả tiêu chí thuộc phần đó tự động 0 điểm
- LLM cần được nhắc rõ điều này trong prompt

## Format dữ liệu chấm

Sau khi chấm xong tất cả thí sinh, lưu theo format:

```json
{
  "scoring_session_id": "...",
  "scored_at": "2026-01-15T10:30:00Z",
  "judge_model": "claude-opus-4",
  "judge_prompt_version": "v1.2",
  "scores": [
    {
      "student_id": "S001",
      "question_id": "C5-Int-SIT-01",
      "student_response": "...",
      "scores": [
        {"criterion_num": 1, "passed": true, "evidence": "..."},
        ...
      ],
      "total_score": 4,
      "summary_feedback": "...",
      "validation": {
        "validated_by_expert": false,
        "expert_score": null,
        "agreement": null
      }
    }
  ]
}
```

Trường `validation` để sau bạn append kết quả chấm chuyên gia cho subset ngẫu nhiên. Giữ format này từ đầu giúp khi viết chương Kết quả của luận văn dễ tính các chỉ số agreement.

## Ngưỡng partial credit (cân nhắc)

Hiện tại quy định "1 điểm hoặc 0 điểm" cho mỗi tiêu chí — đơn giản hóa cho LLM-as-judge và phù hợp với IRT Partial Credit Model (PCM).

Nếu sau này muốn áp dụng partial credit (0.5 điểm cho đạt một phần):
- Rubric cần viết lại với 2-3 mức rõ ràng cho mỗi tiêu chí
- Prompt LLM cần thêm hướng dẫn cụ thể
- IRT cần dùng Generalized Partial Credit Model (GPCM)
- Khuyến nghị: KHÔNG dùng partial credit cho luận văn này — tăng phức tạp mà không tăng nhiều giá trị
