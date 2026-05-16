# Khung năng lực sử dụng AI — Chi tiết 7 tiêu chí × 3 mức

> **Cách đọc**: File này dùng để tham chiếu khi sinh câu hỏi. KHÔNG cần đọc toàn bộ — chỉ đọc phần tiêu chí đang cần sinh câu hỏi. Mỗi tiêu chí có heading `## Tiêu chí N: Tên`.

## Tổng quan

| # | Tiêu chí | Định nghĩa ngắn |
|---|----------|-----------------|
| 1 | Nền tảng AI (Fundamental) | Hiểu khái niệm, phân loại, nguyên lý cơ bản của AI |
| 2 | Dữ liệu (Data) | Hiểu, đánh giá, xử lý dữ liệu cho vòng đời AI |
| 3 | Tư duy phản biện (Critical Thinking) | Đánh giá phản biện output AI, kiểm chứng |
| 4 | Ứng dụng AI trong CNTT (AI Use Cases) | Áp dụng AI vào sản phẩm phần mềm |
| 5 | Đạo đức AI (AI Ethics) | Nhận diện, xử lý vấn đề đạo đức/pháp lý của AI |
| 6 | Công cụ AI (AI Tools) | Sử dụng AI tools (Copilot, Cursor, ChatGPT...) thành thạo |
| 7 | Tương lai công việc (Future of Work) | Hiểu tác động AI lên nghề và thích ứng |

---

## Tiêu chí 1: Nền tảng AI (Fundamental)

**Định nghĩa**: Hiểu các khái niệm, lịch sử, phân loại, và nguyên lý hoạt động cơ bản của trí tuệ nhân tạo, đủ để giao tiếp về AI và đánh giá ở mức tổng quan các giải pháp AI.

### Mức Cơ bản (Basic)

**Hành vi quan sát được**:
- Phân biệt được AI, ML, DL và Generative AI; giải thích đúng quan hệ tập con
- Liệt kê được các loại AI (Narrow / General / Super) và cho ví dụ thực tế cho từng loại
- Định nghĩa được các thuật ngữ cốt lõi: training data, model, prompt, inference, parameters
- Phân biệt được chương trình truyền thống và hệ thống dựa trên AI

**Bằng chứng từ CV**:
- Học môn "Nhập môn AI" / "AI/ML cơ bản" hoặc tương đương; điểm Pass trở lên
- Có chứng chỉ nhập môn (Coursera "AI for Everyone", Google AI Essentials, Elements of AI)
- Skill tag liên quan AI/ML/Python xuất hiện nhưng KHÔNG có dự án thực tế đi kèm

**Bằng chứng từ câu trả lời**:
- Sử dụng đúng thuật ngữ trong câu hỏi mở
- Trả lời đúng ≥70% câu trắc nghiệm mức Cơ bản tiêu chí này
- Mô tả được ví dụ ứng dụng AI quen thuộc (chatbot, gợi ý phim, dịch máy)

### Mức Trung cấp (Intermediate)

**Hành vi quan sát được**:
- Mô tả được vòng đời phát triển AI: thu thập → tiền xử lý → huấn luyện → đánh giá → triển khai → giám sát
- Hiểu khái niệm transformer, attention, fine-tuning, RAG ở mức tổng quan
- Đọc và tóm tắt được tài liệu kỹ thuật/khoa học về AI (paper introduction, blog post, technical doc)
- So sánh được training và inference về tài nguyên, mục đích, đặc điểm

**Bằng chứng từ CV**:
- Hoàn thành ≥1 đồ án/dự án có ứng dụng ML (đề án môn học, hackathon, internship)
- Khóa học chuyên sâu: Coursera "Machine Learning" (Andrew Ng), "Deep Learning Specialization", fast.ai
- Sử dụng được scikit-learn, PyTorch hoặc TensorFlow ở mức tutorial/notebook

**Bằng chứng từ câu trả lời**:
- Trả lời đúng ≥60% câu trắc nghiệm mức Trung cấp
- Trong câu tình huống, nêu được luồng giai đoạn của hệ thống AI
- Phân biệt được các loại bài toán ML: classification, regression, clustering, ranking

### Mức Nâng cao (Advanced)

**Hành vi quan sát được**:
- Đánh giá được điểm mạnh/yếu của các thuật toán ML khác nhau cho bài toán cụ thể
- So sánh được các kiến trúc model (CNN, RNN, Transformer, GNN) cho từng loại dữ liệu
- Giải thích được trade-off giữa độ chính xác, chi phí tính toán, dữ liệu cần thiết
- Đọc và phản biện được paper nghiên cứu (chỉ ra giả định, hạn chế, baseline phù hợp)

**Bằng chứng từ CV**:
- Có dự án ML thực tế triển khai / đóng góp open-source / publication / pre-print
- Vai trò "AI Engineer", "ML Engineer", "Data Scientist", "Researcher" trong kinh nghiệm làm việc
- Đóng góp bài blog kỹ thuật / talk / paper về AI

**Bằng chứng từ câu trả lời**:
- Trả lời đúng ≥50% câu trắc nghiệm mức Nâng cao
- Trong câu tình huống, đưa ra phân tích trade-off có cơ sở định lượng/định tính
- Đề xuất giải pháp khả thi với rationale rõ ràng và đề cập tới rủi ro

---

## Tiêu chí 2: Dữ liệu (Data)

**Định nghĩa**: Kỹ năng hiểu, đánh giá, xử lý và khai thác dữ liệu phục vụ vòng đời AI — từ thu thập đến giám sát trong production.

### Mức Cơ bản (Basic)

**Hành vi quan sát được**:
- Hiểu vai trò của dữ liệu trong AI ("garbage in, garbage out")
- Phân biệt được dữ liệu structured (bảng) và unstructured (text, ảnh, audio)
- Liệt kê được các nguồn dữ liệu phổ biến: database, API, web scraping, sensor, log
- Hiểu khái niệm features và labels trong supervised learning

**Bằng chứng từ CV**:
- Có học môn "Cơ sở dữ liệu" và đạt yêu cầu
- Sử dụng được SQL cơ bản (SELECT, JOIN, GROUP BY)
- Có làm bài tập đọc/ghi file CSV, JSON

**Bằng chứng từ câu trả lời**:
- Trả lời đúng ≥70% câu trắc nghiệm Cơ bản về dữ liệu
- Phân biệt đúng các loại dữ liệu trong câu hỏi
- Liệt kê được nguồn dữ liệu phù hợp cho bài toán mẫu

### Mức Trung cấp (Intermediate)

**Hành vi quan sát được**:
- Đánh giá được chất lượng dữ liệu theo 5 khía cạnh: completeness, consistency, accuracy, timeliness, relevance
- Áp dụng được pandas/Excel để khám phá và làm sạch dữ liệu cơ bản
- Nhận diện được vấn đề: missing data, duplicates, outliers, inconsistent formats
- Hiểu khái niệm bias trong dữ liệu (sampling bias, label bias, historical bias)

**Bằng chứng từ CV**:
- Có dự án/đồ án sử dụng pandas/numpy để xử lý dataset thực
- Khóa học "Data Analysis with Python", "Pandas", hoặc tương đương
- Vai trò "Data Analyst" / "Data Engineer" (junior/intern)

**Bằng chứng từ câu trả lời**:
- Trong câu tình huống, mô tả được quy trình EDA (Exploratory Data Analysis) cơ bản
- Nhận diện được issue dữ liệu trong các tình huống thực tế
- Đề xuất biện pháp xử lý phù hợp

### Mức Nâng cao (Advanced)

**Hành vi quan sát được**:
- Sử dụng Tableau/Power BI/matplotlib/seaborn để visualize và phân tích dữ liệu phức tạp
- Quyết định chiến lược thu thập, gán nhãn, augmentation cho dataset mới
- Áp dụng kỹ thuật xử lý imbalanced data (SMOTE, class weighting), missing data nâng cao
- Thiết kế data pipeline có khả năng mở rộng (scalable, monitored, reproducible)

**Bằng chứng từ CV**:
- Dự án xây dựng/duy trì data pipeline production
- Sử dụng Airflow / dbt / Spark / data warehouse
- Vai trò "Data Engineer", "ML Engineer", "Senior Data Analyst"

**Bằng chứng từ câu trả lời**:
- Đề xuất kiến trúc data pipeline có rationale
- Đánh giá được chiến lược augmentation phù hợp với domain
- Hiểu và giải quyết được data drift, concept drift

---

## Tiêu chí 3: Tư duy phản biện (Critical Thinking)

**Định nghĩa**: Năng lực đánh giá phản biện kết quả từ AI và các nguồn thông tin liên quan, nhận diện rủi ro về độ tin cậy và xây dựng quy trình kiểm chứng.

### Mức Cơ bản (Basic)

**Hành vi quan sát được**:
- Nhận biết AI có thể sai (hallucination), không tin tuyệt đối khi AI đưa ra số liệu/dữ kiện
- So sánh thông tin từ nhiều nguồn khi AI trả lời
- Đặt câu hỏi về độ tin cậy của output trước khi sử dụng
- Phân biệt được "AI nói nghe có lý" và "AI nói đúng"

**Bằng chứng từ CV**:
- Trong portfolio/blog có thảo luận hạn chế của công cụ AI đã dùng
- Tham gia cộng đồng AI (forum, discord) với thái độ học hỏi

**Bằng chứng từ câu trả lời**:
- Trong câu hỏi mở, đặt được câu hỏi kiểm chứng
- Nhận diện được tình huống AI có thể sai
- Trả lời đúng ≥70% câu trắc nghiệm Cơ bản

### Mức Trung cấp (Intermediate)

**Hành vi quan sát được**:
- So sánh được kết quả từ nhiều mô hình AI (ChatGPT vs Claude vs Gemini) cho cùng câu hỏi
- Áp dụng các phương pháp fact-check (đối chiếu với nguồn gốc, citation, knowledge base chính thức)
- Phân biệt khi nào nên dùng AI / khi nào không (tasks có sự thật cố định, tasks cần trách nhiệm)
- Nhận diện "confidence" (cường độ ngôn ngữ tự tin) khác "accuracy" của LLM

**Bằng chứng từ CV**:
- Đã viết blog/post về so sánh các AI tools
- Có kinh nghiệm thực tế với ≥3 LLM khác nhau
- Tham gia code review, peer review học thuật

**Bằng chứng từ câu trả lời**:
- Trong câu tình huống, đề xuất quy trình kiểm chứng nhiều bước
- Phân biệt được các loại lỗi của AI (hallucination, outdated, biased)
- Đưa ra ví dụ cụ thể về fact-check

### Mức Nâng cao (Advanced)

**Hành vi quan sát được**:
- Sử dụng công cụ chuyên dụng (Perplexity với citations, knowledge graphs, RAG verification)
- Đóng góp ý kiến chuyên môn để cải thiện hệ thống AI nội bộ (prompting guidelines, output policies)
- Hướng dẫn người khác về phương pháp kiểm chứng AI
- Thiết kế quy trình verification cho team / sản phẩm

**Bằng chứng từ CV**:
- Vai trò QA/Lead trong dự án AI có quality framework
- Đóng góp tài liệu hướng dẫn AI usage cho team/cộng đồng
- Bài talk/workshop về responsible AI usage

**Bằng chứng từ câu trả lời**:
- Thiết kế được verification framework cho use case cụ thể
- Đề cập đến observability, telemetry cho AI system
- Nêu được trade-off giữa speed và verification

---

## Tiêu chí 4: Ứng dụng AI trong CNTT (AI Use Cases)

**Định nghĩa**: Hiểu, đánh giá và áp dụng AI trong các tình huống thực tế của ngành Công nghệ Thông tin — từ trợ giúp lập trình đến tích hợp AI vào sản phẩm phần mềm.

### Mức Cơ bản (Basic)

**Hành vi quan sát được**:
- Liệt kê được các ứng dụng AI cho lập trình viên: code completion, code review, documentation, debug, test generation
- Hiểu các tool phổ biến: GitHub Copilot, Cursor, Claude Code, Tabnine, Codeium
- Nhận biết AI trong các software thường dùng (IDE features, smart linter, search)
- Phân biệt được AI feature tích hợp sẵn và AI tool độc lập

**Bằng chứng từ CV**:
- Sử dụng GitHub Copilot/Cursor trong dự án cá nhân (commit history, README đề cập)
- Có khóa học hoặc đọc tài liệu về AI cho dev
- Tham dự workshop về AI coding tools

**Bằng chứng từ câu trả lời**:
- Liệt kê đúng các use case phổ biến
- Phân biệt được AI feature trong các tool
- Trả lời đúng ≥70% câu trắc nghiệm Cơ bản

### Mức Trung cấp (Intermediate)

**Hành vi quan sát được**:
- Mô tả được nguyên lý: LLM, embeddings, RAG, fine-tuning, function calling
- Tích hợp được API AI (OpenAI, Anthropic, Gemini, Hugging Face) vào ứng dụng cá nhân
- Nhận diện rủi ro khi đưa AI vào sản phẩm: latency, cost, hallucination, dependency
- Áp dụng được technique cơ bản: prompt template, structured output (JSON schema), system prompt

**Bằng chứng từ CV**:
- Có dự án tích hợp LLM API (chatbot, summarizer, etc.) trên GitHub
- Sử dụng vector database (Pinecone, Chroma, pgvector) trong dự án
- Vai trò "AI Application Developer", "Full-stack with AI"

**Bằng chứng từ câu trả lời**:
- Mô tả đúng kiến trúc RAG và khi nào dùng
- Tính được chi phí cơ bản của LLM API call
- Đề xuất được technique phù hợp cho use case

### Mức Nâng cao (Advanced)

**Hành vi quan sát được**:
- Đánh giá tính phù hợp của AI cho bài toán (build vs buy, ML vs rule-based, model nào)
- Đánh giá khả năng bảo trì, mở rộng, observability của giải pháp AI
- Thiết kế kiến trúc cho ứng dụng AI phức tạp (multi-agent, RAG có vector DB, function calling, MCP)
- Hiểu MLOps / LLMOps và lifecycle quản lý mô hình production

**Bằng chứng từ CV**:
- Đã thiết kế và triển khai hệ thống AI production có người dùng thực
- Vai trò "AI Engineer", "ML Platform Engineer", "Tech Lead AI"
- Đóng góp architecture decision record (ADR), tech blog về AI system design

**Bằng chứng từ câu trả lời**:
- Trong câu tình huống, đề xuất kiến trúc có nhiều thành phần với rationale
- Đề cập tới evaluation, monitoring, fallback
- Cân nhắc được build vs buy với cơ sở định lượng

---

## Tiêu chí 5: Đạo đức AI (AI Ethics)

**Định nghĩa**: Nhận diện, đánh giá và xử lý các vấn đề đạo đức liên quan đến phát triển và sử dụng AI — từ rủi ro cá nhân đến tác động xã hội, bao gồm khía cạnh pháp lý.

### Mức Cơ bản (Basic)

**Hành vi quan sát được**:
- Liệt kê được các rủi ro chính: bias, privacy, misinformation, job displacement, environmental impact
- Hiểu các nguyên tắc cốt lõi: fairness, transparency, accountability, privacy
- Nhận biết khi nào sử dụng AI có thể vi phạm đạo đức (deepfake, surveillance, plagiarism học thuật)
- Hiểu rằng kết quả AI có thể ảnh hưởng tới con người thật

**Bằng chứng từ CV**:
- Có học môn liên quan đến đạo đức nghề nghiệp / Computing Ethics
- Tham dự workshop / talk về AI Ethics
- Đọc tài liệu nền (UNESCO, OECD AI Principles)

**Bằng chứng từ câu trả lời**:
- Nhận diện được rủi ro đạo đức trong tình huống quen thuộc
- Liệt kê đúng nguyên tắc đạo đức AI
- Trả lời đúng ≥70% câu trắc nghiệm Cơ bản

### Mức Trung cấp (Intermediate)

**Hành vi quan sát được**:
- Đánh giá rủi ro của ứng dụng AI cụ thể trong tổ chức (risk matrix, impact assessment)
- Đưa ra cảnh báo / đặt vấn đề khi có rủi ro đạo đức
- Hiểu các quy định pháp lý cơ bản: EU AI Act (categorize rủi ro), GDPR liên quan AI, Luật Bảo vệ dữ liệu cá nhân Việt Nam (2023)
- Áp dụng kiểm tra bias với tools đơn giản (fairness metrics, dataset audit)

**Bằng chứng từ CV**:
- Đã làm việc trong dự án có yêu cầu compliance
- Đóng góp pull request liên quan đến security/privacy/fairness
- Có chứng chỉ liên quan: GDPR, security best practices

**Bằng chứng từ câu trả lời**:
- Trong câu tình huống, đề xuất biện pháp giảm thiểu rủi ro
- Liên hệ được với quy định pháp lý
- Phân biệt được các loại bias và cách đo

### Mức Nâng cao (Advanced)

**Hành vi quan sát được**:
- Đóng góp xây dựng chính sách AI cho tổ chức (AI policy, usage guidelines, ethics review)
- Hướng dẫn đồng nghiệp về best practices đạo đức AI
- Thiết kế hệ thống có cơ chế bias-mitigation, audit log, explainability
- Tư vấn quyết định ethical / pháp lý cho leadership

**Bằng chứng từ CV**:
- Vai trò liên quan AI governance, responsible AI lead
- Đóng góp policy document, ethics review process cho tổ chức
- Bài talk/paper/blog về responsible AI

**Bằng chứng từ câu trả lời**:
- Trong câu tình huống cấp cao, đề xuất khung policy có cấu trúc
- Cân bằng được nhiều stakeholder (kỹ thuật, pháp lý, kinh doanh, người dùng)
- Đề cập tới audit, third-party review, monitoring lâu dài

---

## Tiêu chí 6: Công cụ AI cho lập trình viên (AI Tools)

**Định nghĩa**: Sử dụng thành thạo các công cụ AI trong công việc lập trình hằng ngày — từ chat-based assistant đến agentic workflow, kết hợp prompt engineering hiệu quả.

### Mức Cơ bản (Basic)

**Hành vi quan sát được**:
- Sử dụng được ChatGPT / Claude / Gemini cho hỏi đáp cơ bản về code (giải thích, sinh snippet, debug đơn giản)
- Sử dụng GitHub Copilot autocomplete, Cursor tab completion ở mức tiếp nhận gợi ý
- Hiểu prompting cơ bản: cụ thể, có ngữ cảnh, chỉ rõ output format mong muốn
- Nhận biết khi nào KHÔNG nên dùng AI (data nhạy cảm, code IP, password, secret)

**Bằng chứng từ CV**:
- Account ChatGPT/Claude/GitHub Copilot active (có nhắc đến trong CV)
- Repo cá nhân có dấu vết dùng AI tool (trong README, commit message)
- Học khóa cơ bản "Prompt Engineering for Developers"

**Bằng chứng từ câu trả lời**:
- Phân biệt được prompt tốt và prompt kém
- Nhận diện tình huống không nên dùng AI
- Trả lời đúng ≥70% câu trắc nghiệm Cơ bản

### Mức Trung cấp (Intermediate)

**Hành vi quan sát được**:
- Áp dụng prompt engineering nâng cao: few-shot, chain-of-thought, role assignment, structured output, prompt chaining
- Sử dụng các công cụ chuyên biệt: Cursor Agent mode, Claude Code, v0, Bolt
- Biết cách iterate với AI: refine prompt, provide feedback, restart context khi cần
- So sánh và chọn công cụ phù hợp cho từng task

**Bằng chứng từ CV**:
- Dự án sử dụng Cursor / Claude Code làm primary IDE
- Có mô tả trong portfolio về workflow AI-assisted
- Đóng góp prompt template / snippet công khai

**Bằng chứng từ câu trả lời**:
- Áp dụng đúng technique trong câu hỏi cụ thể
- So sánh được các tool một cách định lượng/định tính
- Mô tả được workflow iteration với AI

### Mức Nâng cao (Advanced)

**Hành vi quan sát được**:
- Đề xuất các use case mới và workflow mới cho team — vượt qua giai đoạn "AI = autocomplete"
- Đánh giá output AI một cách hệ thống để khuyến nghị adoption (eval criteria, A/B testing)
- Xây dựng workflow tự động hóa với AI (agents, MCP, function calling, custom tools)
- Cấu hình team setup: custom instructions, repository-level context, shared prompt library

**Bằng chứng từ CV**:
- Vai trò "Tech Lead", "Developer Advocate", "AI Champion" trong team
- Đóng góp template/setup AI tools cho organization
- Talk/blog về advanced AI workflow

**Bằng chứng từ câu trả lời**:
- Trong câu tình huống, thiết kế multi-step workflow với AI
- Đề cập tới evaluation, monitoring của workflow
- Cân nhắc được security, compliance trong workflow

---

## Tiêu chí 7: Tương lai công việc (Future of Work)

**Định nghĩa**: Hiểu tác động của AI lên ngành CNTT, lên bản thân và tổ chức; có chiến lược thích ứng và dẫn dắt thay đổi.

### Mức Cơ bản (Basic)

**Hành vi quan sát được**:
- Nhận diện được các task có thể bị AI thay thế/hỗ trợ trong ngành dev (boilerplate, doc, simple debug, test cases)
- Hiểu lợi ích và thách thức của AI cho nghề lập trình
- Có thái độ học hỏi liên tục, không phòng thủ trước thay đổi
- Phân biệt được task "tự động hóa" và task "augment" (tăng cường)

**Bằng chứng từ CV**:
- Continuous learning được thể hiện qua các khóa học gần đây trong CV
- Theo dõi thông tin ngành (subscribe newsletter, tham dự conference)
- Tự học AI tools mới (gần đây trong portfolio)

**Bằng chứng từ câu trả lời**:
- Liệt kê đúng task automatable / augmentable
- Trả lời với thái độ growth mindset
- Nhận diện được rủi ro cá nhân

### Mức Trung cấp (Intermediate)

**Hành vi quan sát được**:
- So sánh được tác động của AI với các thay đổi công nghệ trước (cloud, mobile, no-code)
- Đề xuất kế hoạch upskilling cho bản thân và team có cấu trúc
- Tham gia cộng đồng học hỏi AI (forum, conference, meetup, study group)
- Hiểu tác động đến vị trí entry-level và cách thích ứng

**Bằng chứng từ CV**:
- Đề cập kế hoạch học tập cá nhân trong CV (next 6-12 months)
- Hoạt động cộng đồng (mentoring, contributing to open-source)
- Đóng góp blog/talk về learning journey

**Bằng chứng từ câu trả lời**:
- Đưa ra kế hoạch upskilling cụ thể, có timeline
- Phân tích được tác động khác nhau theo seniority
- Tham chiếu tới case study lịch sử

### Mức Nâng cao (Advanced)

**Hành vi quan sát được**:
- Đánh giá tác động dài hạn của AI lên team / tổ chức / ngành
- Xây dựng chiến lược thích ứng cho team (training plan, role redefinition, hiring strategy)
- Dẫn dắt thay đổi văn hóa làm việc với AI (psychological safety, growth orientation)
- Tư vấn senior leadership về workforce transformation

**Bằng chứng từ CV**:
- Vai trò Engineering Manager / Director / VP Engineering
- Đóng góp transformation roadmap cho organization
- Đại diện công ty tại sự kiện / panel về AI in workplace

**Bằng chứng từ câu trả lời**:
- Thiết kế được chiến lược nhiều giai đoạn
- Cân bằng nhiều mục tiêu (productivity, người, văn hóa)
- Đề cập tới change management methodology
