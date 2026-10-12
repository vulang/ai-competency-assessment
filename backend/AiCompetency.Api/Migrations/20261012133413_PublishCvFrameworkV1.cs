using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AiCompetency.Api.Migrations
{
    /// <inheritdoc />
    public partial class PublishCvFrameworkV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 2 OR "Version" = 'cv-v1' OR "IsActive")
                        OR EXISTS (SELECT 1 FROM behavior_indicators WHERE "Id" BETWEEN 64 AND 126)
                        OR EXISTS (SELECT 1 FROM cv_rubric_rules WHERE "Id" BETWEEN 29 AND 56) THEN
                        RAISE EXCEPTION 'CV v1 release conflicts with existing framework data; reconcile version IDs and the active release before applying this migration';
                    END IF;
                END $$;
                """);

            migrationBuilder.InsertData(
                table: "competency_framework_versions",
                columns: new[] { "Id", "IsActive", "PublishedAt", "Source", "Status", "Version" },
                values: new object[] { 2, true, new DateTimeOffset(new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Project owner approval on 2026-10-12 of the proposed indicators and rubric aligned to thesis v8.1 sections 1.2 and 2.3. The supplied thesis lists Appendices A/B as planned without their full text.", "Published", "cv-v1" });

            migrationBuilder.InsertData(
                table: "competency_groups",
                columns: new[] { "Code", "FrameworkVersionId", "NameEn", "NameVi", "Order" },
                values: new object[,]
                {
                    { "APPLICATION", 2, "AI applications", "Ứng dụng AI", 4 },
                    { "CRITICAL_THINKING", 2, "Critical thinking", "Tư duy phản biện", 3 },
                    { "DATA", 2, "Data", "Dữ liệu", 2 },
                    { "ETHICS", 2, "AI ethics", "Đạo đức AI", 5 },
                    { "FOUNDATION", 2, "AI foundations", "Nền tảng AI", 1 },
                    { "FUTURE_OF_WORK", 2, "Future of work", "Tương lai việc làm", 7 },
                    { "TOOLS", 2, "AI tools", "Công cụ AI", 6 }
                });

            migrationBuilder.InsertData(
                table: "behavior_indicators",
                columns: new[] { "Id", "Code", "FrameworkVersionId", "GroupCode", "Level", "TextEn", "TextVi" },
                values: new object[,]
                {
                    { 64, "FOUNDATION.1.1", 2, "FOUNDATION", 1, "Explain AI in a specific project", "Giải thích AI trong một dự án cụ thể" },
                    { 65, "FOUNDATION.1.2", 2, "FOUNDATION", 1, "Distinguish AI prediction from programmed rules", "Phân biệt dự đoán AI với luật lập trình" },
                    { 66, "FOUNDATION.1.3", 2, "FOUNDATION", 1, "Describe the role of input data", "Mô tả vai trò dữ liệu đầu vào" },
                    { 67, "FOUNDATION.2.1", 2, "FOUNDATION", 2, "Describe completed model development steps", "Mô tả các bước phát triển mô hình đã thực hiện" },
                    { 68, "FOUNDATION.2.2", 2, "FOUNDATION", 2, "Explain a machine learning method selection", "Giải thích lựa chọn phương pháp học máy" },
                    { 69, "FOUNDATION.2.3", 2, "FOUNDATION", 2, "Interpret model evaluation results", "Diễn giải kết quả đánh giá mô hình" },
                    { 70, "FOUNDATION.3.1", 2, "FOUNDATION", 3, "Compare algorithms using measured criteria", "So sánh thuật toán bằng tiêu chí đo lường" },
                    { 71, "FOUNDATION.3.2", 2, "FOUNDATION", 3, "Analyze data and algorithm tradeoffs", "Phân tích đánh đổi dữ liệu và thuật toán" },
                    { 72, "FOUNDATION.3.3", 2, "FOUNDATION", 3, "Design an evaluation of model limitations", "Thiết kế phương án đánh giá giới hạn mô hình" },
                    { 73, "DATA.1.1", 2, "DATA", 1, "Collect data from identified sources", "Thu thập dữ liệu từ nguồn xác định" },
                    { 74, "DATA.1.2", 2, "DATA", 1, "Clean missing or duplicate values", "Làm sạch giá trị thiếu hoặc trùng lặp" },
                    { 75, "DATA.1.3", 2, "DATA", 1, "Describe data using tables or charts", "Mô tả dữ liệu bằng bảng hoặc biểu đồ" },
                    { 76, "DATA.2.1", 2, "DATA", 2, "Evaluate data quality against criteria", "Đánh giá chất lượng dữ liệu theo tiêu chí" },
                    { 77, "DATA.2.2", 2, "DATA", 2, "Build a data normalization workflow", "Xây dựng quy trình chuẩn hóa dữ liệu" },
                    { 78, "DATA.2.3", 2, "DATA", 2, "Analyze data to answer a project question", "Phân tích dữ liệu để trả lời câu hỏi dự án" },
                    { 79, "DATA.3.1", 2, "DATA", 3, "Design a data pipeline with quality checks", "Thiết kế pipeline dữ liệu có kiểm tra chất lượng" },
                    { 80, "DATA.3.2", 2, "DATA", 3, "Check data leakage across dataset splits", "Kiểm tra rò rỉ dữ liệu giữa các tập" },
                    { 81, "DATA.3.3", 2, "DATA", 3, "Design visualizations supporting decisions", "Thiết kế trực quan hóa hỗ trợ quyết định" },
                    { 82, "CRITICAL_THINKING.1.1", 2, "CRITICAL_THINKING", 1, "Identify sources of AI generated information", "Xác định nguồn của thông tin do AI tạo" },
                    { 83, "CRITICAL_THINKING.1.2", 2, "CRITICAL_THINKING", 1, "Check an AI result against an independent source", "Kiểm tra một kết quả AI bằng nguồn độc lập" },
                    { 84, "CRITICAL_THINKING.1.3", 2, "CRITICAL_THINKING", 1, "Identify limitations in an AI answer", "Nhận diện giới hạn trong câu trả lời AI" },
                    { 85, "CRITICAL_THINKING.2.1", 2, "CRITICAL_THINKING", 2, "Compare outputs from multiple AI tools", "So sánh đầu ra của nhiều công cụ AI" },
                    { 86, "CRITICAL_THINKING.2.2", 2, "CRITICAL_THINKING", 2, "Build criteria for verifying results", "Xây dựng tiêu chí kiểm chứng kết quả" },
                    { 87, "CRITICAL_THINKING.2.3", 2, "CRITICAL_THINKING", 2, "Analyze causes of incorrect AI results", "Phân tích nguyên nhân kết quả AI sai" },
                    { 88, "CRITICAL_THINKING.3.1", 2, "CRITICAL_THINKING", 3, "Design a reproducible AI verification workflow", "Thiết kế quy trình kiểm chứng AI có thể tái lập" },
                    { 89, "CRITICAL_THINKING.3.2", 2, "CRITICAL_THINKING", 3, "Evaluate validity of model conclusions", "Đánh giá tính hợp lệ của kết luận từ mô hình" },
                    { 90, "CRITICAL_THINKING.3.3", 2, "CRITICAL_THINKING", 3, "Test AI using counterexamples", "Kiểm thử AI với các trường hợp phản ví dụ" },
                    { 91, "APPLICATION.1.1", 2, "APPLICATION", 1, "Apply AI to a defined task", "Áp dụng AI cho một nhiệm vụ xác định" },
                    { 92, "APPLICATION.1.2", 2, "APPLICATION", 1, "Describe AI benefits in a work context", "Mô tả lợi ích AI trong bối cảnh công việc" },
                    { 93, "APPLICATION.1.3", 2, "APPLICATION", 1, "Identify obstacles to AI adoption", "Nhận diện trở ngại khi áp dụng AI" },
                    { 94, "APPLICATION.2.1", 2, "APPLICATION", 2, "Integrate AI into a real workflow", "Tích hợp AI vào quy trình thực tế" },
                    { 95, "APPLICATION.2.2", 2, "APPLICATION", 2, "Evaluate an AI solution against business criteria", "Đánh giá giải pháp AI bằng tiêu chí nghiệp vụ" },
                    { 96, "APPLICATION.2.3", 2, "APPLICATION", 2, "Handle errors in AI solution deployment", "Xử lý lỗi khi triển khai giải pháp AI" },
                    { 97, "APPLICATION.3.1", 2, "APPLICATION", 3, "Design an AI solution meeting practical constraints", "Thiết kế giải pháp AI đáp ứng ràng buộc thực tế" },
                    { 98, "APPLICATION.3.2", 2, "APPLICATION", 3, "Compare cost and effectiveness against a baseline", "So sánh chi phí và hiệu quả với phương án cơ sở" },
                    { 99, "APPLICATION.3.3", 2, "APPLICATION", 3, "Evaluate maintainability of an AI solution", "Đánh giá khả năng duy trì giải pháp AI" },
                    { 100, "ETHICS.1.1", 2, "ETHICS", 1, "Identify personal data in an AI task", "Nhận diện dữ liệu cá nhân trong tác vụ AI" },
                    { 101, "ETHICS.1.2", 2, "ETHICS", 1, "Describe bias risk in an application", "Mô tả rủi ro thiên lệch trong một ứng dụng" },
                    { 102, "ETHICS.1.3", 2, "ETHICS", 1, "Disclose AI use in a deliverable", "Ghi nhận việc sử dụng AI trong sản phẩm" },
                    { 103, "ETHICS.2.1", 2, "ETHICS", 2, "Apply data protection measures when using AI", "Áp dụng biện pháp bảo vệ dữ liệu khi dùng AI" },
                    { 104, "ETHICS.2.2", 2, "ETHICS", 2, "Evaluate AI risks by affected group", "Đánh giá rủi ro AI theo nhóm bị ảnh hưởng" },
                    { 105, "ETHICS.2.3", 2, "ETHICS", 2, "Perform bias checks against criteria", "Thực hiện kiểm tra thiên lệch có tiêu chí" },
                    { 106, "ETHICS.3.1", 2, "ETHICS", 3, "Develop responsible AI usage policies", "Xây dựng chính sách sử dụng AI có trách nhiệm" },
                    { 107, "ETHICS.3.2", 2, "ETHICS", 3, "Design an AI risk monitoring process", "Thiết kế quy trình giám sát rủi ro AI" },
                    { 108, "ETHICS.3.3", 2, "ETHICS", 3, "Evaluate risk mitigation effectiveness", "Đánh giá hiệu quả biện pháp giảm thiểu rủi ro" },
                    { 109, "TOOLS.1.1", 2, "TOOLS", 1, "Use an AI tool for a specific task", "Sử dụng công cụ AI cho tác vụ cụ thể" },
                    { 110, "TOOLS.1.2", 2, "TOOLS", 1, "Write a contextual instruction for an AI tool", "Viết yêu cầu có bối cảnh cho công cụ AI" },
                    { 111, "TOOLS.1.3", 2, "TOOLS", 1, "Check output before using it", "Kiểm tra đầu ra trước khi sử dụng" },
                    { 112, "TOOLS.2.1", 2, "TOOLS", 2, "Compare tools against work requirements", "So sánh công cụ theo nhu cầu công việc" },
                    { 113, "TOOLS.2.2", 2, "TOOLS", 2, "Integrate an AI tool using APIs or automation", "Tích hợp công cụ AI qua API hoặc tự động hóa" },
                    { 114, "TOOLS.2.3", 2, "TOOLS", 2, "Adjust a workflow based on output evaluation", "Điều chỉnh quy trình dựa trên đánh giá đầu ra" },
                    { 115, "TOOLS.3.1", 2, "TOOLS", 3, "Design coordination of multiple AI tools", "Thiết kế phối hợp nhiều công cụ AI" },
                    { 116, "TOOLS.3.2", 2, "TOOLS", 3, "Measure effectiveness of an AI tool workflow", "Đo hiệu quả quy trình dùng công cụ AI" },
                    { 117, "TOOLS.3.3", 2, "TOOLS", 3, "Propose improvements to AI tool accessibility", "Đề xuất cải tiến khả năng tiếp cận công cụ AI" },
                    { 118, "FUTURE_OF_WORK.1.1", 2, "FUTURE_OF_WORK", 1, "Identify tasks that AI may change", "Xác định nhiệm vụ có thể thay đổi do AI" },
                    { 119, "FUTURE_OF_WORK.1.2", 2, "FUTURE_OF_WORK", 1, "Describe skills needed to work with AI", "Mô tả kỹ năng cần học để làm việc với AI" },
                    { 120, "FUTURE_OF_WORK.1.3", 2, "FUTURE_OF_WORK", 1, "Participate in AI learning with specified content", "Tham gia hoạt động học kỹ năng AI có nội dung cụ thể" },
                    { 121, "FUTURE_OF_WORK.2.1", 2, "FUTURE_OF_WORK", 2, "Analyze role changes in a workflow", "Phân tích thay đổi vai trò trong một quy trình" },
                    { 122, "FUTURE_OF_WORK.2.2", 2, "FUTURE_OF_WORK", 2, "Propose training based on skill gaps", "Đề xuất kế hoạch đào tạo theo khoảng cách kỹ năng" },
                    { 123, "FUTURE_OF_WORK.2.3", 2, "FUTURE_OF_WORK", 2, "Evaluate a trial of AI assisted work", "Đánh giá kết quả thử nghiệm cách làm việc với AI" },
                    { 124, "FUTURE_OF_WORK.3.1", 2, "FUTURE_OF_WORK", 3, "Develop an organizational AI adaptation strategy", "Xây dựng chiến lược thích ứng AI cho tổ chức" },
                    { 125, "FUTURE_OF_WORK.3.2", 2, "FUTURE_OF_WORK", 3, "Evaluate AI impact on a workforce", "Đánh giá tác động AI đến lực lượng lao động" },
                    { 126, "FUTURE_OF_WORK.3.3", 2, "FUTURE_OF_WORK", 3, "Design criteria for tracking skill transformation", "Thiết kế tiêu chí theo dõi chuyển đổi kỹ năng" }
                });

            migrationBuilder.InsertData(
                table: "cv_rubric_rules",
                columns: new[] { "Id", "AcceptedEvidence", "Example", "FrameworkVersionId", "GroupCode", "InsufficientEvidence", "Level" },
                values: new object[,]
                {
                    { 29, "Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.", "Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.", 2, "FOUNDATION", "Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.", 0 },
                    { 30, "Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.", "Hoàn thành bài tập giải thích khác biệt giữa luật lập trình và dự đoán AI, kèm sản phẩm nộp.", 2, "FOUNDATION", "Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.", 1 },
                    { 31, "Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.", "Trong dự án phân loại văn bản, tự chọn phương pháp học máy và giải thích kết quả đánh giá.", 2, "FOUNDATION", "Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.", 2 },
                    { 32, "Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.", "Thiết kế quy trình so sánh hai mô hình, phân tích đánh đổi dữ liệu và phương pháp cho nhóm triển khai.", 2, "FOUNDATION", "Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.", 3 },
                    { 33, "Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.", "Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.", 2, "DATA", "Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.", 0 },
                    { 34, "Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.", "Trong học phần dữ liệu, làm sạch dữ liệu trùng và nộp bảng kết quả trước/sau.", 2, "DATA", "Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.", 1 },
                    { 35, "Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.", "Tự thiết kế bước kiểm tra dữ liệu thiếu cho dự án dự báo và đối chiếu chất lượng đầu ra.", 2, "DATA", "Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.", 2 },
                    { 36, "Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.", "Thiết kế pipeline dữ liệu có kiểm soát chất lượng và kiểm tra rò rỉ giữa các tập huấn luyện/kiểm định.", 2, "DATA", "Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.", 3 },
                    { 37, "Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.", "Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.", 2, "CRITICAL_THINKING", "Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.", 0 },
                    { 38, "Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.", "Đối chiếu câu trả lời của trợ lý AI với một nguồn độc lập trong bài tập và ghi lại sai khác.", 2, "CRITICAL_THINKING", "Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.", 1 },
                    { 39, "Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.", "Tự lập tiêu chí kiểm chứng, so sánh đầu ra hai công cụ AI và phân tích nguyên nhân lỗi.", 2, "CRITICAL_THINKING", "Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.", 2 },
                    { 40, "Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.", "Thiết kế quy trình kiểm chứng đầu ra AI cho nhóm, dùng phản ví dụ để đánh giá độ tin cậy.", 2, "CRITICAL_THINKING", "Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.", 3 },
                    { 41, "Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.", "Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.", 2, "APPLICATION", "Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.", 0 },
                    { 42, "Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.", "Dùng trợ lý AI để soạn bản nháp cho một tác vụ cụ thể và kiểm tra sản phẩm trước khi nộp.", 2, "APPLICATION", "Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.", 1 },
                    { 43, "Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.", "Tự tích hợp API AI vào quy trình phân loại yêu cầu và so sánh kết quả với cách làm thủ công.", 2, "APPLICATION", "Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.", 2 },
                    { 44, "Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.", "Thiết kế giải pháp AI cho quy trình hỗ trợ, đánh giá chất lượng, chi phí và khả năng vận hành.", 2, "APPLICATION", "Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.", 3 },
                    { 45, "Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.", "Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.", 2, "ETHICS", "Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.", 0 },
                    { 46, "Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.", "Trong bài tập ứng dụng AI, nhận diện dữ liệu cá nhân và mô tả biện pháp che thông tin.", 2, "ETHICS", "Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.", 1 },
                    { 47, "Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.", "Tự đánh giá rủi ro thiên lệch của hệ thống theo nhóm bị ảnh hưởng và kiểm tra biện pháp giảm thiểu.", 2, "ETHICS", "Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.", 2 },
                    { 48, "Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.", "Xây chính sách sử dụng AI cho đơn vị, thiết kế giám sát rủi ro và đánh giá hiệu quả biện pháp bảo vệ.", 2, "ETHICS", "Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.", 3 },
                    { 49, "Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.", "Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.", 2, "TOOLS", "Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.", 0 },
                    { 50, "Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.", "Dùng công cụ AI cho một tác vụ cụ thể, kiểm tra đầu ra và nộp sản phẩm đã sửa.", 2, "TOOLS", "Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.", 1 },
                    { 51, "Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.", "Tự chọn và tích hợp công cụ AI vào luồng hỗ trợ, đo lỗi trước và sau khi áp dụng.", 2, "TOOLS", "Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.", 2 },
                    { 52, "Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.", "Thiết kế luồng phối hợp nhiều công cụ AI cho nhóm và đánh giá hiệu quả, chi phí, khả năng tiếp cận.", 2, "TOOLS", "Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.", 3 },
                    { 53, "Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.", "Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.", 2, "FUTURE_OF_WORK", "Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.", 0 },
                    { 54, "Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.", "Hoàn thành học phần về AI với sản phẩm mô tả nhiệm vụ công việc có thể thay đổi.", 2, "FUTURE_OF_WORK", "Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.", 1 },
                    { 55, "Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.", "Tự phân tích khoảng cách kỹ năng trong nhóm và thử nghiệm kế hoạch đào tạo dùng AI.", 2, "FUTURE_OF_WORK", "Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.", 2 },
                    { 56, "Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.", "Xây chiến lược thích ứng AI cho tổ chức, theo dõi tác động tới vai trò và kết quả đào tạo.", 2, "FUTURE_OF_WORK", "Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.", 3 }
                });

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    PERFORM setval(pg_get_serial_sequence('competency_framework_versions', 'Id'),
                        GREATEST((SELECT MAX("Id") FROM competency_framework_versions) + 1,
                                 nextval(pg_get_serial_sequence('competency_framework_versions', 'Id'))), false);
                    PERFORM setval(pg_get_serial_sequence('behavior_indicators', 'Id'),
                        GREATEST((SELECT MAX("Id") FROM behavior_indicators) + 1,
                                 nextval(pg_get_serial_sequence('behavior_indicators', 'Id'))), false);
                    PERFORM setval(pg_get_serial_sequence('cv_rubric_rules', 'Id'),
                        GREATEST((SELECT MAX("Id") FROM cv_rubric_rules) + 1,
                                 nextval(pg_get_serial_sequence('cv_rubric_rules', 'Id'))), false);
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 92);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 93);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 94);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 95);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 96);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 97);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 98);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 99);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 118);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 119);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 120);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 121);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 122);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 123);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 124);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 125);

            migrationBuilder.DeleteData(
                table: "behavior_indicators",
                keyColumn: "Id",
                keyValue: 126);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "cv_rubric_rules",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "competency_groups",
                keyColumns: new[] { "Code", "FrameworkVersionId" },
                keyValues: new object[] { "APPLICATION", 2 });

            migrationBuilder.DeleteData(
                table: "competency_groups",
                keyColumns: new[] { "Code", "FrameworkVersionId" },
                keyValues: new object[] { "CRITICAL_THINKING", 2 });

            migrationBuilder.DeleteData(
                table: "competency_groups",
                keyColumns: new[] { "Code", "FrameworkVersionId" },
                keyValues: new object[] { "DATA", 2 });

            migrationBuilder.DeleteData(
                table: "competency_groups",
                keyColumns: new[] { "Code", "FrameworkVersionId" },
                keyValues: new object[] { "ETHICS", 2 });

            migrationBuilder.DeleteData(
                table: "competency_groups",
                keyColumns: new[] { "Code", "FrameworkVersionId" },
                keyValues: new object[] { "FOUNDATION", 2 });

            migrationBuilder.DeleteData(
                table: "competency_groups",
                keyColumns: new[] { "Code", "FrameworkVersionId" },
                keyValues: new object[] { "FUTURE_OF_WORK", 2 });

            migrationBuilder.DeleteData(
                table: "competency_groups",
                keyColumns: new[] { "Code", "FrameworkVersionId" },
                keyValues: new object[] { "TOOLS", 2 });

            migrationBuilder.DeleteData(
                table: "competency_framework_versions",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
