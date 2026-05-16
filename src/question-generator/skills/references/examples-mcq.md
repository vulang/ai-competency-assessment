# Ví dụ câu hỏi trắc nghiệm (MCQ) — 21 câu mẫu

> **Cách dùng**: Khi sinh MCQ cho tiêu chí X mức Y, đọc 1-2 ví dụ cùng tiêu chí+mức từ file này để làm few-shot trong prompt. Ưu tiên chọn ví dụ ở mức TƯƠNG ĐƯƠNG với mức cần sinh.

> **Lưu ý**: Tất cả 21 câu mẫu dưới đây đều đã qua review nội bộ, có chất lượng tốt. Dùng làm chuẩn để đánh giá câu LLM sinh ra.

---

## Tiêu chí 1: Nền tảng AI

### [C1-Basic-01] Mức Cơ bản

**Hỏi**: GitHub Copilot là một công cụ thuộc loại nào?

- A. Chương trình truyền thống dựa trên rule-based
- B. Hệ thống AI dựa trên Large Language Model (LLM)
- C. Công cụ phân tích tĩnh code (static analyzer)
- D. Trình biên dịch (compiler)

**Đáp án**: B

**Giải thích**: Copilot dùng LLM được huấn luyện trên code công khai để gợi ý code, đây là hệ thống AI; không phải rule-based, không phải compiler hay static analyzer.

**Hành vi đo**: Phân biệt được chương trình truyền thống và hệ thống dựa trên AI.

---

### [C1-Intermediate-01] Mức Trung cấp

**Hỏi**: Trong vòng đời phát triển mô hình ML, kỹ thuật "fine-tuning" thuộc giai đoạn nào?

- A. Thu thập dữ liệu
- B. Tiền xử lý dữ liệu
- C. Huấn luyện mô hình
- D. Triển khai mô hình

**Đáp án**: C

**Giải thích**: Fine-tuning là quá trình huấn luyện tiếp một pre-trained model trên dữ liệu chuyên biệt — thuộc giai đoạn huấn luyện. Nó khác với inference (giai đoạn triển khai) hay data preprocessing.

**Hành vi đo**: Mô tả được vòng đời phát triển AI.

---

### [C1-Advanced-01] Mức Nâng cao

**Hỏi**: Cho bài toán phân loại 10,000 ảnh sản phẩm thành 50 danh mục (200 mẫu/danh mục), bạn muốn tối ưu cả độ chính xác và thời gian huấn luyện. Lựa chọn nào thường phù hợp nhất?

- A. Train từ đầu một CNN ResNet-50
- B. Fine-tune một pre-trained model như ResNet-50 hoặc EfficientNet (đã train ImageNet)
- C. Dùng SVM với HOG features thủ công
- D. Train Vision Transformer (ViT) từ đầu

**Đáp án**: B

**Giải thích**: Với dataset trung bình (~10k ảnh), fine-tune pre-trained model là cách hiệu quả nhất — tận dụng feature representations đã học, tiết kiệm thời gian, accuracy thường cao. Train từ đầu CNN/ViT cần dataset lớn hơn nhiều; SVM+HOG đã lỗi thời cho image classification.

**Hành vi đo**: Đánh giá trade-off giữa độ chính xác, chi phí tính toán, dữ liệu cần thiết.

---

## Tiêu chí 2: Dữ liệu

### [C2-Basic-01] Mức Cơ bản

**Hỏi**: Dữ liệu nào sau đây là dữ liệu phi cấu trúc (unstructured)?

- A. Bảng khách hàng trong CSDL MySQL
- B. File CSV chứa thông tin sản phẩm
- C. Email khách hàng gửi đến hỗ trợ
- D. Spreadsheet doanh thu hàng tháng

**Đáp án**: C

**Giải thích**: Email là văn bản tự do, không có schema cố định — đây là dữ liệu phi cấu trúc. A, B, D đều có cấu trúc cột-hàng rõ ràng (structured).

**Hành vi đo**: Phân biệt được dữ liệu structured và unstructured.

---

### [C2-Intermediate-01] Mức Trung cấp

**Hỏi**: Bạn có dataset 10,000 đơn hàng, cột "phương thức thanh toán" có: 9,800 "Tiền mặt", 100 "Chuyển khoản", 100 "Thẻ". Vấn đề chính khi dùng cột này huấn luyện mô hình phân loại là gì?

- A. Số lượng dữ liệu chưa đủ
- B. Mất cân bằng lớp (class imbalance) nghiêm trọng
- C. Dữ liệu là chữ, không thể dùng được
- D. Cần chuẩn hóa min-max

**Đáp án**: B

**Giải thích**: Tỷ lệ 98% - 1% - 1% là class imbalance nghiêm trọng. Mô hình sẽ thiên về dự đoán "Tiền mặt". Cần kỹ thuật xử lý: oversampling (SMOTE), class weighting, hoặc cost-sensitive learning. Dữ liệu chữ có thể encode (one-hot, label encoding).

**Hành vi đo**: Nhận diện được vấn đề trong dữ liệu.

---

### [C2-Advanced-01] Mức Nâng cao

**Hỏi**: Khi triển khai mô hình ML production cho dự đoán churn khách hàng, bạn nhận thấy phân phối input data thay đổi theo thời gian khiến accuracy giảm dần. Hiện tượng này gọi là gì và biện pháp phù hợp nhất?

- A. Overfitting — cần thêm regularization
- B. Data drift — cần monitoring và retrain định kỳ
- C. Vanishing gradient — cần đổi activation function
- D. Sampling bias — cần thu thập lại dataset gốc

**Đáp án**: B

**Giải thích**: Sự thay đổi phân phối input theo thời gian là "data drift" — vấn đề phổ biến trong production. Biện pháp: monitor feature distributions (PSI, KS test), thiết lập alert, schedule retrain hoặc adaptive learning. Overfitting và vanishing gradient là vấn đề training, không phải production drift.

**Hành vi đo**: Hiểu và giải quyết được data drift, concept drift.

---

## Tiêu chí 3: Tư duy phản biện

### [C3-Basic-01] Mức Cơ bản

**Hỏi**: Bạn hỏi ChatGPT "cho tôi 5 paper kinh điển về Reinforcement Learning" và nó trả lời danh sách. Hành động phù hợp tiếp theo là?

- A. Trích dẫn ngay vào báo cáo của bạn
- B. Tìm kiếm trên Google Scholar / arxiv để xác minh từng paper có thật và đúng tác giả
- C. Tin tưởng vì ChatGPT là LLM mạnh
- D. Chỉ đếm số lượng để xem đủ chưa

**Đáp án**: B

**Giải thích**: LLM thường "hallucinate" citations — tạo ra paper không có thật hoặc gán sai tác giả/năm. Luôn xác minh nguồn gốc trước khi sử dụng. Đây là một trong các lỗi phổ biến nhất của LLM trong context học thuật.

**Hành vi đo**: Đặt câu hỏi về độ tin cậy của output trước khi sử dụng.

---

### [C3-Intermediate-01] Mức Trung cấp

**Hỏi**: Khi ChatGPT trả lời với cường độ tự tin cao ("Đây CHẮC CHẮN là cách đúng"), điều đó có nghĩa gì?

- A. Mô hình đã xác minh thông tin và chắc chắn đúng
- B. Ngôn từ tự tin của LLM không phản ánh độ chính xác thực sự — vẫn cần kiểm chứng
- C. Câu trả lời được hậu kiểm bởi người dùng khác trong training data
- D. Mô hình đã tham chiếu nguồn chính thức và confirm

**Đáp án**: B

**Giải thích**: LLM được huấn luyện để trả lời tự nhiên, ngôn ngữ tự tin là phong cách viết chứ không phản ánh confidence calibration thực sự. Đây là điểm yếu nổi tiếng — gọi là "overconfidence" hoặc "miscalibration". Một số LLM mới có cải thiện nhưng vẫn cần kiểm chứng.

**Hành vi đo**: Nhận diện "confidence" khác "accuracy" của LLM.

---

### [C3-Advanced-01] Mức Nâng cao

**Hỏi**: Để xây dựng quy trình kiểm chứng output của LLM trong sản phẩm RAG nội bộ của công ty, biện pháp nào hiệu quả nhất?

- A. Yêu cầu LLM tự đánh giá độ tin cậy của câu trả lời
- B. Hiển thị citation của tài liệu nguồn được retrieve, có cơ chế trace ngược về document gốc
- C. Tăng nhiệt độ (temperature) để câu trả lời đa dạng hơn
- D. Dùng nhiều LLM cùng lúc và lấy majority vote

**Đáp án**: B

**Giải thích**: Citation traceability là cách verifiable nhất — người dùng có thể tự kiểm tra. LLM tự đánh giá thường không đáng tin (A). Tăng temperature giảm chất lượng (C). Majority vote tốn kém và không giải quyết hallucination khi tất cả LLM cùng sai (D). RAG với citation là pattern chuẩn cho enterprise AI.

**Hành vi đo**: Thiết kế quy trình verification cho team / sản phẩm.

---

## Tiêu chí 4: Ứng dụng AI trong CNTT

### [C4-Basic-01] Mức Cơ bản

**Hỏi**: Đâu KHÔNG phải là use case phổ biến của AI cho lập trình viên?

- A. Tự động sinh unit test từ code
- B. Giải thích đoạn code phức tạp bằng ngôn ngữ tự nhiên
- C. Thay thế hoàn toàn quá trình code review giữa các lập trình viên
- D. Sinh boilerplate code (CRUD, API endpoint)

**Đáp án**: C

**Giải thích**: AI hỗ trợ tốt sinh code, giải thích, sinh test, nhưng không thể thay thế hoàn toàn code review của con người — vốn cần hiểu business context, design philosophy, team conventions, và judgment về trade-off. Code review là quá trình giao tiếp, không chỉ là phát hiện lỗi.

**Hành vi đo**: Liệt kê được các ứng dụng AI cho lập trình viên.

---

### [C4-Intermediate-01] Mức Trung cấp

**Hỏi**: Bạn cần xây tính năng "tìm kiếm thông minh" trong tài liệu nội bộ công ty (200+ trang), có thể trả lời câu hỏi tự nhiên. Cách tiếp cận phù hợp nhất là?

- A. Fine-tune một LLM lớn trên toàn bộ tài liệu công ty
- B. Sử dụng RAG — embedding tài liệu vào vector DB rồi retrieve trước khi đưa cho LLM
- C. Gọi API GPT-4 mỗi câu hỏi với context là toàn bộ tài liệu
- D. Train một mô hình BERT từ đầu trên dữ liệu công ty

**Đáp án**: B

**Giải thích**: RAG là pattern phù hợp nhất: dữ liệu nội bộ thay đổi liên tục (RAG dễ update), không cần training (rẻ, nhanh), context window LLM giới hạn nên không thể đưa hết tài liệu (C không khả thi). Fine-tune đắt và lỗi thời nhanh (A); train từ đầu vô lý cho task này (D).

**Hành vi đo**: Mô tả đúng kiến trúc RAG và khi nào dùng.

---

### [C4-Advanced-01] Mức Nâng cao

**Hỏi**: Bạn thiết kế hệ thống AI agent tự động xử lý ticket support khách hàng. Yêu cầu nào quan trọng nhất cho production-readiness?

- A. Latency < 100ms cho mọi response
- B. Có guardrails (escalation rules, output validation), observability, và human-in-the-loop cho high-risk cases
- C. Chọn model có nhiều parameters nhất
- D. Hỗ trợ tất cả ngôn ngữ trên thế giới

**Đáp án**: B

**Giải thích**: Production-readiness của AI agent đặc biệt cần: guardrails (giới hạn agent action), observability (theo dõi failure modes, distribution của intent), HITL (escalate khi confidence thấp hoặc rủi ro cao). Latency và model size là yếu tố thứ cấp; ngôn ngữ phụ thuộc khách hàng thực tế.

**Hành vi đo**: Thiết kế kiến trúc cho ứng dụng AI phức tạp.

---

## Tiêu chí 5: Đạo đức AI

### [C5-Basic-01] Mức Cơ bản

**Hỏi**: Một công ty dùng AI để sàng lọc CV ứng viên dựa trên dữ liệu tuyển dụng 10 năm qua. Rủi ro đạo đức lớn nhất là gì?

- A. Tốc độ xử lý chậm hơn HR thủ công
- B. Tiêu tốn điện năng tính toán
- C. Mô hình có thể học và khuếch đại bias trong dữ liệu lịch sử (giới tính, độ tuổi, trường học...)
- D. Ứng viên không thích trải nghiệm với máy

**Đáp án**: C

**Giải thích**: AI sàng lọc CV được biết đến với rủi ro tái tạo bias từ dữ liệu lịch sử — Amazon từng phải dừng hệ thống AI HR vì ưu tiên CV nam giới (do dataset lịch sử bias). Đây là rủi ro đạo đức và pháp lý hàng đầu, có thể vi phạm luật chống phân biệt đối xử.

**Hành vi đo**: Nhận biết khi nào sử dụng AI có thể vi phạm đạo đức.

---

### [C5-Intermediate-01] Mức Trung cấp

**Hỏi**: Đội của bạn muốn dùng GPT-4 API để xử lý đơn khiếu nại khách hàng. Khiếu nại chứa thông tin cá nhân (tên, CMND, lịch sử giao dịch). Cách tiếp cận đúng đắn nhất?

- A. Gửi nguyên text khiếu nại lên API để có ngữ cảnh đầy đủ
- B. Mã hóa toàn bộ rồi gửi (dù LLM không thể hiểu)
- C. Thực hiện PII redaction (ẩn tên, CMND, ...) trước khi gửi; kiểm tra ToS provider; ký data processing agreement nếu cần
- D. Tự viết một mô hình thay thế cho mọi tác vụ (không khả thi)

**Đáp án**: C

**Giải thích**: GDPR và Luật Bảo vệ dữ liệu cá nhân Việt Nam (2023) yêu cầu xử lý PII có biện pháp bảo vệ. Best practice: redact PII trước khi gửi LLM bên thứ ba, đảm bảo có DPA với provider, hiểu chính sách data retention. Mã hóa làm LLM mất khả năng hiểu (B sai).

**Hành vi đo**: Hiểu các quy định pháp lý cơ bản (GDPR, Luật BVDLCN VN).

---

### [C5-Advanced-01] Mức Nâng cao

**Hỏi**: Bạn là tech lead trong công ty fintech, được giao xây dựng AI scoring tín dụng. Rủi ro đạo đức nào phải có biện pháp đối phó ở mức policy?

- A. Chỉ cần đảm bảo accuracy cao là đủ
- B. Cần audit bias định kỳ trên các nhóm bảo vệ (giới, tuổi, vùng miền), explainability cho từ chối, quy trình appeal cho khách hàng
- C. Chỉ cần tuân thủ luật bảo mật mạng
- D. Để team data tự quyết định toàn bộ

**Đáp án**: B

**Giải thích**: AI tín dụng ảnh hưởng tài chính cá nhân khách hàng — yêu cầu khắt khe về fairness audit (theo nhóm protected), explainability (per EU AI Act phân loại high-risk; Việt Nam đang xây dựng quy định), và quyền appeal. Đây là chuẩn của hệ thống AI rủi ro cao theo nhiều khung quy định quốc tế.

**Hành vi đo**: Đóng góp xây dựng chính sách AI cho tổ chức.

---

## Tiêu chí 6: Công cụ AI cho lập trình viên

### [C6-Basic-01] Mức Cơ bản

**Hỏi**: Khi viết prompt cho ChatGPT để tạo code, prompt nào sau đây có chất lượng tốt nhất?

- A. "Viết code"
- B. "Viết Python"
- C. "Viết hàm Python"
- D. "Viết hàm Python nhận vào danh sách số nguyên, trả về số trung vị, có docstring và xử lý trường hợp danh sách rỗng (raise ValueError)"

**Đáp án**: D

**Giải thích**: Prompt tốt cần cụ thể: (1) ngôn ngữ, (2) input/output rõ, (3) edge case, (4) format mong muốn (docstring). D có đầy đủ. A, B, C quá chung chung khiến output không kiểm soát được, dễ phải iterate nhiều lần.

**Hành vi đo**: Hiểu prompting cơ bản: cụ thể, có ngữ cảnh.

---

### [C6-Intermediate-01] Mức Trung cấp

**Hỏi**: Kỹ thuật "few-shot prompting" là gì?

- A. Chỉ dùng prompt rất ngắn (< 10 từ)
- B. Đưa vào prompt một vài ví dụ input-output mẫu để mô hình hiểu format và pattern mong muốn
- C. Gọi API nhiều lần và lấy trung bình kết quả
- D. Huấn luyện lại mô hình với ít dữ liệu (dưới 100 mẫu)

**Đáp án**: B

**Giải thích**: Few-shot prompting là cung cấp 2-5 ví dụ trong prompt (in-context learning), giúp mô hình hiểu nhiệm vụ mà không cần fine-tuning. Khác với zero-shot (không ví dụ) và fine-tuning (cập nhật weights). Đây là technique cơ bản trong prompt engineering.

**Hành vi đo**: Áp dụng prompt engineering nâng cao.

---

### [C6-Advanced-01] Mức Nâng cao

**Hỏi**: MCP (Model Context Protocol) trong agentic workflow có vai trò gì?

- A. Một thuật toán optimization mới cho LLM
- B. Giao thức chuẩn hóa cách AI agents kết nối với các nguồn dữ liệu/công cụ ngoài (database, API, file system, custom tools)
- C. Một loại model architecture thay thế Transformer
- D. Một thư viện monitoring cho LLM

**Đáp án**: B

**Giải thích**: MCP do Anthropic phát triển và open-source là giao thức mở để chuẩn hóa cách AI agents kết nối với external context (databases, APIs, tools). Tương tự như USB-C cho AI — cho phép các tool và LLM tương tác mà không cần custom integration cho mỗi cặp.

**Hành vi đo**: Xây dựng workflow tự động hóa với AI (agents, MCP).

---

## Tiêu chí 7: Tương lai công việc

### [C7-Basic-01] Mức Cơ bản

**Hỏi**: Theo các nghiên cứu hiện tại, AI có khả năng cao nhất sẽ thay thế/tự động hóa nhóm công việc nào trong ngành phần mềm?

- A. Đảm nhận hoàn toàn vai trò Software Architect
- B. Tự động hóa các task lặp lại như sinh boilerplate, viết test cơ bản, chuyển đổi code style, generate documentation
- C. Thay thế Project Manager
- D. Thay thế CTO

**Đáp án**: B

**Giải thích**: AI hiện tại tự động hóa tốt task có pattern rõ ràng, ít phụ thuộc context tổ chức. Các vai trò liên quan đến judgment, communication, leadership, design quyết định kiến trúc dài hạn vẫn cần con người.

**Hành vi đo**: Nhận diện được các task có thể bị AI thay thế/hỗ trợ.

---

### [C7-Intermediate-01] Mức Trung cấp

**Hỏi**: Lập trình viên junior cần điều chỉnh gì trong định hướng học tập để thích ứng với thời đại AI tools?

- A. Bỏ học cú pháp ngôn ngữ, chỉ tập trung học cách dùng AI
- B. Tập trung mạnh vào memorize syntax và API
- C. Vẫn học vững nền tảng (data structures, system design, debugging) NHƯNG tích lũy thêm kỹ năng dùng AI hiệu quả và kỹ năng review/verify output AI
- D. Học chuyển sang ngành khác để tránh bị thay thế

**Đáp án**: C

**Giải thích**: AI khuếch đại lập trình viên có nền tảng tốt. Quan trọng nhất vẫn là tư duy thuật toán, system design, debugging, hiểu code — cộng với năng lực dùng AI hiệu quả như công cụ.

**Hành vi đo**: Hiểu tác động đến vị trí entry-level và cách thích ứng.

---

### [C7-Advanced-01] Mức Nâng cao

**Hỏi**: Bạn là Engineering Manager. Khi đưa AI coding tools (Copilot, Cursor) vào team, chiến lược triển khai phù hợp nhất?

- A. Bắt buộc 100% team dùng ngay lập tức để tăng productivity
- B. Pilot với một nhóm nhỏ, đo lường impact (productivity, code quality, security), xây dựng guideline về use cases phù hợp/nhạy cảm, tổ chức training, sau đó rollout dần
- C. Cấm hoàn toàn để tránh rủi ro security
- D. Để mỗi người tự quyết định không cần phối hợp

**Đáp án**: B

**Giải thích**: Adoption AI tools cần lộ trình: pilot → đo lường (định lượng) → guideline → training → rollout. Bắt buộc đột ngột (A) gây resistance và lỗi nghiệp vụ. Cấm hoàn toàn (C) khiến team lạc hậu. Tự phát (D) tạo rủi ro security và inconsistency.

**Hành vi đo**: Xây dựng chiến lược thích ứng cho team.
