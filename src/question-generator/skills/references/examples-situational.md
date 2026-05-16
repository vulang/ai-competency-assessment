# Ví dụ câu hỏi tình huống (Structured Free Text) — 7 câu mẫu

> **Cách dùng**: Khi sinh câu tình huống cho tiêu chí X, đọc ví dụ cùng tiêu chí từ file này làm few-shot. Mỗi tiêu chí có 1 câu mẫu.

> **Format**: Mỗi câu mẫu có đầy đủ: `title` + `context` + `question` + `response_structure` (2-4 phần) + `rubric` LLM-friendly + `judge_prompt_hints` + `behaviors_addressed`. Đây là chuẩn để học theo khi sinh câu mới.

> **Lưu ý chấm**: Tất cả câu được chấm bằng **LLM-as-judge** theo rubric. Xem `scoring-guide.md` để biết cách chấm.

---

## [C1-Int-SIT-01] Tiêu chí 1: Nền tảng AI — Mức Trung cấp

**Tiêu đề**: Lựa chọn cách tiếp cận cho hệ thống chấm bài luận tự động

**Bối cảnh**: Bạn là sinh viên năm 4 ngành CNTT, được giao đề tài "Hệ thống chấm điểm bài luận tiếng Việt tự động". Đối tượng người dùng là giảng viên tiếng Việt phổ thông, kỳ vọng chấm được 200 bài/ngày. Trưởng nhóm hỏi bạn nên chọn cách tiếp cận nào trong 3 phương án: (1) Train một mô hình từ đầu, (2) Fine-tune PhoBERT, (3) Dùng API GPT-4 với prompt engineering. Deadline đồ án: 4 tháng. Budget compute hạn chế (chỉ có 1 GPU consumer tier).

**Câu hỏi**: Hãy mô tả ngắn gọn bản chất kỹ thuật của 3 phương án, đề xuất phương án nên chọn với lý do cụ thể, và liệt kê các câu hỏi quan trọng bạn cần làm rõ trước khi quyết định cuối.

**Response structure**:
- Phần 1: Mô tả 3 phương án về data needs / chi phí / tính kiểm soát (60-80 từ)
- Phần 2: Đề xuất phương án với rationale có yếu tố cụ thể (40-60 từ)
- Phần 3: 2-3 câu hỏi quan trọng cần làm rõ (20-40 từ)

**Expected response length**: 120-180 từ tổng

**Scoring method**: `llm_as_judge`

**Rubric (5 điểm)**:
- (1đ) [Phần 1] Mô tả "train từ đầu" có ít nhất 2/3 yếu tố: cần dataset lớn (10k+ mẫu), tốn compute, không khả thi với 1 GPU consumer
- (1đ) [Phần 1] Mô tả "fine-tune PhoBERT" có ít nhất 2/3 yếu tố: cần dataset gán nhãn vài nghìn mẫu, training trên 1 GPU vừa, output controllable
- (1đ) [Phần 1] Mô tả "API LLM" có ít nhất 2/3 yếu tố: không cần training, prototype nhanh, chi phí per call, output ít controllable / privacy issue
- (1đ) [Phần 2] Đề xuất MỘT phương án cụ thể (không "tùy") với rationale có ít nhất 1 yếu tố định lượng (dataset size / chi phí ước tính / thời gian deploy)
- (1đ) [Phần 3] Đặt được ít nhất 2 câu hỏi quan trọng: ví dụ dataset có sẵn không, tiêu chí chấm chi tiết ra sao, có privacy concern không, có ngân sách API không

**Judge prompt hints**: Kiểm tra thí sinh có hiểu trade-off "build vs buy" giữa fine-tune và API; có nhận ra train từ đầu KHÔNG khả thi với 1 GPU consumer cho NLP task; có nhắc tới PhoBERT là model NLP tiếng Việt phù hợp. Lưu ý: cả 3 phương án đều có thể "đúng" tùy ràng buộc — đừng trừ điểm vì chọn phương án khác mình kỳ vọng, miễn rationale có cơ sở.

**Behaviors addressed**: Mô tả vòng đời ML; so sánh trade-off giữa train từ đầu, fine-tune, API.

---

## [C2-Int-SIT-01] Tiêu chí 2: Dữ liệu — Mức Trung cấp

**Tiêu đề**: Đánh giá chất lượng dataset CV trước khi training

**Bối cảnh**: Bạn được giao xử lý dataset CV của ứng viên thực tập tại một công ty công nghệ ở TP.HCM. Dataset có 5,000 CV ở định dạng PDF (mix giữa text-based PDF và scan ảnh), đã có nhãn "Pass/Fail" từ HR trong 3 năm qua. Yêu cầu: huấn luyện mô hình sàng lọc CV tự động. Sếp muốn deploy trong 2 tháng.

**Câu hỏi**: Bạn sẽ làm những bước gì để đánh giá chất lượng dataset trước khi training? Liệt kê các vấn đề tiềm ẩn, đề xuất cách xử lý, và ưu tiên vấn đề nào cần giải quyết trước.

**Response structure**:
- Phần 1: Các vấn đề tiềm ẩn cần kiểm tra (60-80 từ)
- Phần 2: Cách kiểm tra và xử lý cho mỗi vấn đề (60-80 từ)
- Phần 3: Vấn đề ưu tiên cao nhất + lý do (20-40 từ)

**Expected response length**: 140-200 từ tổng

**Scoring method**: `llm_as_judge`

**Rubric (5 điểm)**:
- (1đ) [Phần 1] Đề cập vấn đề parsing PDF — có ít nhất 1 trong: scan ảnh cần OCR, encoding tiếng Việt, format không nhất quán
- (1đ) [Phần 1] Đề cập kiểm tra phân phối nhãn Pass/Fail — có thể có class imbalance
- (1đ) [Phần 1] Nhận diện rủi ro bias trong labeling HR — có ít nhất 1 nhóm cụ thể: giới tính / trường / tuổi / vùng miền, không chỉ nói "bias" chung
- (1đ) [Phần 2] Đề xuất biện pháp xử lý CỤ THỂ (không chỉ nói "kiểm tra"): ví dụ pdfplumber + Tesseract Việt, fairness audit với disparate impact ratio, sampling/SMOTE/class weight, manual relabel subset
- (1đ) [Phần 3] Có ưu tiên rõ ràng (chọn 1-2 vấn đề trước) với rationale (rủi ro pháp lý / dependency / cost) — KHÔNG liệt kê lại tất cả

**Judge prompt hints**: Đặc biệt chú ý phần "fairness audit": chấm 1 điểm nếu thí sinh nhận ra HR labeling có thể chứa bias lịch sử (Amazon case là ví dụ kinh điển). Nếu chỉ nói "kiểm tra bias" mà không đề cập nhóm cụ thể nào → không tính điểm tiêu chí này. Lưu ý PDF scan vs text-based là chi tiết quan trọng trong bối cảnh — cần được nhận diện.

**Behaviors addressed**: Đánh giá chất lượng dữ liệu theo nhiều khía cạnh; nhận diện bias lịch sử.

---

## [C3-Int-SIT-01] Tiêu chí 3: Tư duy phản biện — Mức Trung cấp

**Tiêu đề**: Xử lý báo cáo Literature Review do ChatGPT tạo

**Bối cảnh**: Bạn làm việc nhóm 4 người cho đồ án môn học. Một thành viên dùng ChatGPT để tổng hợp "tình hình nghiên cứu" cho phần Literature Review. Báo cáo có 15 paper được trích dẫn với tác giả, năm, journal đầy đủ. Anh ấy đề nghị nộp ngay cho giảng viên ngày mai. Bạn lo lắng nhưng không muốn làm tổn thương quan hệ trong nhóm.

**Câu hỏi**: Bạn sẽ phản hồi với đồng đội thế nào? Đề xuất quy trình kiểm chứng cụ thể và một quy trình team về dùng LLM cho academic writing trong tương lai.

**Response structure**:
- Phần 1: Phản hồi đồng đội + rủi ro cụ thể của báo cáo (50-70 từ)
- Phần 2: Quy trình kiểm chứng từng paper (40-60 từ)
- Phần 3: Đề xuất quy trình team dài hạn (30-50 từ)

**Expected response length**: 120-180 từ tổng

**Scoring method**: `llm_as_judge`

**Rubric (5 điểm)**:
- (1đ) [Phần 1] Nhận diện rủi ro hallucinate citation của LLM — paper có thể không tồn tại HOẶC tác giả/năm sai HOẶC journal sai
- (1đ) [Phần 1] Tone tôn trọng đồng đội — KHÔNG có câu mang nghĩa đổ lỗi cá nhân ("bạn sai rồi", "không nên làm vậy"); có ít nhất 1 cụm thể hiện hợp tác ("cùng kiểm tra", "đề xuất là", "có thể chúng ta")
- (1đ) [Phần 2] Đề xuất bước kiểm chứng cụ thể — phải có ít nhất 1 công cụ/nguồn cụ thể: Google Scholar, DOI lookup, Semantic Scholar, arxiv, hoặc database journal cụ thể. KHÔNG chấp nhận chỉ "tìm trên Google" hoặc "kiểm tra mạng"
- (1đ) [Phần 2] Phân biệt được "paper có thật" với "paper có nội dung đúng như mô tả" — LLM có thể paraphrase sai nội dung paper có thật
- (1đ) [Phần 3] Đề xuất quy trình team có ít nhất 2 yếu tố cụ thể: ví dụ (a) cho phép LLM hỗ trợ drafting nhưng citation phải verify thủ công, (b) cách trích dẫn LLM tool nếu dùng, (c) phân chia trách nhiệm fact-check

**Judge prompt hints**: Tone là một tiêu chí khó chấm. Quy tắc: nếu phần 1 mở đầu bằng câu thể hiện chia sẻ vấn đề ("Mình lo lắng vì...", "Có một vấn đề chúng ta cần cùng xử lý...") → tính điểm tone. Nếu mở đầu bằng câu chỉ trích cá nhân → không tính. Trung lập (báo cáo sự kiện không tone) thì cân nhắc theo phần sau.

**Behaviors addressed**: Áp dụng fact-check có phương pháp; giao tiếp về rủi ro AI trong team.

---

## [C4-Adv-SIT-01] Tiêu chí 4: Ứng dụng AI — Mức Nâng cao

**Tiêu đề**: Thiết kế kiến trúc chatbot Q&A nội bộ cho công ty

**Bối cảnh**: Công ty bạn (500 nhân viên, fintech) muốn xây ứng dụng nội bộ giúp nhân viên hỏi đáp các quy định, chính sách (HR handbook, policy compliance, 200+ trang). Lãnh đạo muốn "có AI chatbot", yêu cầu MVP trong 2 tháng. Dữ liệu chính sách cập nhật hàng tháng. Một số tài liệu có phân quyền theo cấp bậc.

**Câu hỏi**: Hãy đề xuất kiến trúc hệ thống, roadmap triển khai, và các rủi ro cần lưu ý với cơ chế giám sát.

**Response structure**:
- Phần 1: Kiến trúc hệ thống + các components chính (50-70 từ)
- Phần 2: Roadmap triển khai theo pha (50-70 từ)
- Phần 3: Rủi ro chính + cơ chế evaluation/monitoring (30-50 từ)

**Expected response length**: 130-190 từ tổng

**Scoring method**: `llm_as_judge`

**Rubric (5 điểm)**:
- (1đ) [Phần 1] Đề xuất kiến trúc RAG với ít nhất 3 components rõ ràng trong: vector database (Pinecone/Chroma/pgvector), embedding model, LLM, retriever; có rationale tại sao RAG (data thay đổi tháng, cần citation)
- (1đ) [Phần 1] Có cân nhắc rõ build vs buy / self-host vs API với ít nhất 1 lý do: cost / latency / data privacy / control. Trong bối cảnh fintech có nhắc tới data sensitivity là một plus
- (1đ) [Phần 1 hoặc 2] Có đề cập tới phân quyền (access control) cho tài liệu phân cấp — đây là chi tiết trong bối cảnh
- (1đ) [Phần 2] Roadmap có ít nhất 4 pha phân biệt rõ — ví dụ: data preparation → indexing → retrieval pipeline → generation + UI → evaluation → deployment
- (1đ) [Phần 3] Đề xuất cơ chế evaluation cụ thể — có ít nhất 1 trong: test set với câu hỏi mẫu, faithfulness metric (RAGAS), citation correctness, user feedback loop, hallucination monitoring

**Judge prompt hints**: Chú ý chi tiết "tài liệu phân quyền" trong bối cảnh — đây là test xem thí sinh đọc kỹ. Bối cảnh fintech ngụ ý cần data privacy cao. Nếu thí sinh đề xuất tự-host LLM (Llama/Mistral) → tính điểm tiêu chí 2 vì hợp với fintech context.

**Behaviors addressed**: Thiết kế kiến trúc AI phức tạp; cân nhắc build vs buy có cơ sở.

---

## [C5-Int-SIT-01] Tiêu chí 5: Đạo đức AI — Mức Trung cấp

**Tiêu đề**: Phản biện đề xuất Dynamic Pricing với AI

**Bối cảnh**: Bạn đang làm intern tại công ty thương mại điện tử Việt Nam. Sếp giao bạn xây tính năng "gợi ý giá bán động" (dynamic pricing) — AI đề xuất giá bán riêng cho từng khách hàng dựa trên lịch sử mua sắm, vị trí (tỉnh thành), thiết bị truy cập, thời gian xem sản phẩm. Sếp kỳ vọng tăng doanh thu 15%. Deadline 1 tháng. Bạn cảm thấy lo lắng nhưng là intern, không muốn làm sếp khó chịu.

**Câu hỏi**: Hệ thống này có rủi ro đạo đức/pháp lý gì? Bạn trao đổi với sếp như thế nào và đề xuất biện pháp gì để cân bằng giữa mục tiêu kinh doanh và rủi ro?

**Response structure**:
- Phần 1: Phân tích các rủi ro đạo đức/pháp lý cụ thể (50-70 từ)
- Phần 2: Cách trao đổi với sếp + biện pháp cụ thể đề xuất (40-60 từ)
- Phần 3: Trade-off thực tế cần cân nhắc (30-50 từ)

**Expected response length**: 120-180 từ tổng

**Scoring method**: `llm_as_judge`

**Rubric (5 điểm)**:
- (1đ) [Phần 1] Nhận diện rủi ro phân biệt giá (price discrimination) — có nhắc tới khả năng vi phạm Luật Bảo vệ người tiêu dùng VN HOẶC Luật chống phân biệt
- (1đ) [Phần 1] Đề cập rủi ro privacy/consent — thu thập và phân tích dữ liệu hành vi tinh vi cần consent rõ ràng theo Luật Bảo vệ dữ liệu cá nhân 2023
- (1đ) [Phần 1] Nhận diện bias với nhóm dễ tổn thương — có ít nhất 1 nhóm cụ thể: vùng nông thôn / người già / người khuyết tật / khách hàng ít kinh nghiệm online. KHÔNG chấp nhận chỉ "có bias" chung
- (1đ) [Phần 2] Tone phản biện chuyên nghiệp — KHÔNG từ chối thẳng ("không nên làm"), có đặt vấn đề có cơ sở; có biện pháp cụ thể: ví dụ minh bạch giá niêm yết, giới hạn biên độ giá theo nhóm, audit định kỳ, opt-out cho khách hàng
- (1đ) [Phần 3] Cân bằng được mục tiêu kinh doanh (tăng doanh thu) với đạo đức/pháp lý — KHÔNG phải "không làm tuyệt đối" cũng KHÔNG phải "làm theo yêu cầu"; có đề xuất phiên bản giảm rủi ro

**Judge prompt hints**: Đây là test cân bằng pragmatic ethics với business reality. Câu trả lời tốt sẽ KHÔNG bỏ tính năng hoàn toàn nhưng cũng KHÔNG làm theo yêu cầu nguyên gốc. Trade-off lý tưởng: phiên bản dynamic pricing có giới hạn biên độ + minh bạch + audit, vẫn có khả năng tăng doanh thu nhưng rủi ro thấp hơn. Lưu ý: Luật BVDLCN VN có hiệu lực 7/2023 — thí sinh có thể nhắc tới.

**Behaviors addressed**: Đánh giá rủi ro ethical có cơ sở pháp lý; đặt vấn đề chuyên nghiệp trong tổ chức.

---

## [C6-Int-SIT-01] Tiêu chí 6: Công cụ AI — Mức Trung cấp

**Tiêu đề**: Workflow refactor module Java legacy với AI tools

**Bối cảnh**: Bạn nhận được task: refactor một module Java legacy 3,000 dòng để chuyển sang microservices. Code cũ 8 năm, không có test, có một số business logic đặc thù không có document. Bạn có Cursor và GitHub Copilot trong tay. Deadline 2 tuần. Team có 1 senior dev có thể tham vấn.

**Câu hỏi**: Mô tả workflow của bạn — chia task ra sao, dùng AI ở giai đoạn nào, đâu là chỗ AI giúp ích nhiều nhất, đâu là chỗ KHÔNG nên dùng AI, và quy trình kiểm soát chất lượng.

**Response structure**:
- Phần 1: Phân chia task + giai đoạn dùng AI (50-70 từ)
- Phần 2: Chỗ AI giúp ích nhất + chỗ KHÔNG dùng AI (40-60 từ)
- Phần 3: Quy trình kiểm soát chất lượng (30-50 từ)

**Expected response length**: 120-180 từ tổng

**Scoring method**: `llm_as_judge`

**Rubric (5 điểm)**:
- (1đ) [Phần 1] Bắt đầu bằng phân tích/hiểu code — có đề cập dùng AI để giải thích/tóm tắt module trước khi sửa (vì code 8 năm không document)
- (1đ) [Phần 2] Phân biệt task AI tốt: có ít nhất 2 trong các loại: refactor mechanical (rename, extract method), sinh boilerplate microservice (Spring Boot scaffolding), sinh test ban đầu cho legacy code
- (1đ) [Phần 2] Phân biệt task KHÔNG nên dùng AI: có nhắc cụ thể tới một trong: service boundary decision, communication pattern (REST vs gRPC vs event), data ownership, business logic đặc thù cần tham vấn senior. KHÔNG chấp nhận chỉ nói "kiến trúc" chung chung
- (1đ) [Phần 3] Quy trình review cụ thể — có ít nhất 2 yếu tố: chạy test sau mỗi step, code review từng PR nhỏ, không commit blind output AI, tham vấn senior khi có business logic đặc thù
- (1đ) [Phần 1 hoặc 3] Có cân nhắc deadline 2 tuần — đề cập scope hợp lý hoặc ưu tiên phần nào trước (ví dụ: tách dần thay vì refactor toàn bộ một lượt)

**Judge prompt hints**: Bối cảnh "không có test" + "8 năm" + "business logic đặc thù" là 3 chi tiết quan trọng. Thí sinh giỏi sẽ nhận ra: cần sinh test trước (regression safety), cần hiểu code trước (đoạn nào quan trọng), cần tham vấn senior cho business logic. Nếu thí sinh đề xuất "để AI tự quyết kiến trúc microservice" → trừ điểm tiêu chí 3.

**Behaviors addressed**: Áp dụng AI tools có chiến lược; phân biệt task phù hợp/không phù hợp cho AI.

---

## [C7-Adv-SIT-01] Tiêu chí 7: Tương lai công việc — Mức Nâng cao

**Tiêu đề**: Quản lý team trước áp lực "30% productivity với AI"

**Bối cảnh**: Bạn là Engineering Manager team 12 người (4 Junior, 6 Mid-level, 2 Senior) tại một công ty Việt Nam quy mô vừa. Lãnh đạo công ty thông báo: "Trong 6 tháng tới, mỗi team phải tăng 30% productivity bằng AI tools, nếu không sẽ phải giảm headcount." Team bạn lo lắng. Bạn có ngân sách training nhỏ và không có chỉ thị cụ thể về cách đo "productivity".

**Câu hỏi**: Bạn xử lý tình huống này thế nào? Đề xuất kế hoạch giao tiếp với team, chiến lược theo từng cấp Junior/Mid/Senior, và cách đo lường thành công.

**Response structure**:
- Phần 1: Giao tiếp với team + định khung vấn đề (40-60 từ)
- Phần 2: Kế hoạch theo từng cấp Junior/Mid/Senior (60-80 từ)
- Phần 3: Pilot + chỉ số đo lường + checkpoint (40-60 từ)

**Expected response length**: 140-200 từ tổng

**Scoring method**: `llm_as_judge`

**Rubric (5 điểm)**:
- (1đ) [Phần 1] Giao tiếp minh bạch — có đề cập việc nói rõ áp lực với team (không giấu) NHƯNG có định khung tích cực (cơ hội học / team growth / job security); KHÔNG có dấu hiệu hoảng loạn hoặc đe dọa
- (1đ) [Phần 2] Phân biệt cụ thể nhu cầu của ít nhất 2/3 cấp: Junior cần học vững nền tảng + dùng AI có kiểm soát + mentoring; Mid là core implementer + chuẩn hóa workflow; Senior dẫn dắt + định hướng tooling + review junior
- (1đ) [Phần 2 hoặc 3] Có plan training/upskilling cụ thể — ít nhất 2 hoạt động: mentoring pair, study group, workshop, lunch-and-learn, shared prompt library, code review focused on AI usage
- (1đ) [Phần 3] Pilot có structure — đo lường impact định lượng trước rollout, KHÔNG bắt buộc 100% team dùng đột ngột
- (1đ) [Phần 3] Có chỉ số thành công đo lường được — ít nhất 2 metric cụ thể: cycle time, deploy frequency, defect rate, PR review time, story points/sprint, hoặc developer satisfaction score. KHÔNG chấp nhận chỉ "productivity tăng" hoặc "team hạnh phúc hơn"

**Judge prompt hints**: Đây là câu hỏi về management + change management. Câu trả lời tốt cân bằng được psychological safety (team lo lắng, cần reassure) với accountability (lãnh đạo có deadline thật). Cảnh báo: thí sinh hay rơi vào 1 trong 2 cực — quá cứng (chỉ đo productivity, ép team) hoặc quá mềm (chỉ trấn an, không có action). Câu trả lời tốt có cả hai.

**Behaviors addressed**: Xây dựng chiến lược thích ứng cho team; dẫn dắt thay đổi văn hóa làm việc với AI.
