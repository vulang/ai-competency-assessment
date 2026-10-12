using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AiCompetency.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCvFrameworkDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "competency_framework_versions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_framework_versions", x => x.Id);
                    table.CheckConstraint("ck_cv_framework_publication", "(\"Status\" = 'Draft' AND \"PublishedAt\" IS NULL AND NOT \"IsActive\") OR (\"Status\" = 'Published' AND \"PublishedAt\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "proficiency_levels",
                columns: table => new
                {
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NameVi = table.Column<string>(type: "text", nullable: false),
                    NameEn = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proficiency_levels", x => x.Ordinal);
                    table.CheckConstraint("ck_proficiency_level_code", "(\"Ordinal\" = 0 AND \"Code\" = 'NO_EVIDENCE') OR (\"Ordinal\" = 1 AND \"Code\" = 'BASIC') OR (\"Ordinal\" = 2 AND \"Code\" = 'INTERMEDIATE') OR (\"Ordinal\" = 3 AND \"Code\" = 'ADVANCED')");
                });

            migrationBuilder.CreateTable(
                name: "competency_groups",
                columns: table => new
                {
                    FrameworkVersionId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NameVi = table.Column<string>(type: "text", nullable: false),
                    NameEn = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_groups", x => new { x.FrameworkVersionId, x.Code });
                    table.CheckConstraint("ck_cv_group_code", "\"Code\" IN ('FOUNDATION','DATA','CRITICAL_THINKING','APPLICATION','ETHICS','TOOLS','FUTURE_OF_WORK')");
                    table.CheckConstraint("ck_cv_group_order", "\"Order\" BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_competency_groups_competency_framework_versions_FrameworkVe~",
                        column: x => x.FrameworkVersionId,
                        principalTable: "competency_framework_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "behavior_indicators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FrameworkVersionId = table.Column<int>(type: "integer", nullable: false),
                    GroupCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    TextVi = table.Column<string>(type: "text", nullable: false),
                    TextEn = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_behavior_indicators", x => x.Id);
                    table.CheckConstraint("ck_indicator_level", "\"Level\" BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_behavior_indicators_competency_groups_FrameworkVersionId_Gr~",
                        columns: x => new { x.FrameworkVersionId, x.GroupCode },
                        principalTable: "competency_groups",
                        principalColumns: new[] { "FrameworkVersionId", "Code" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_behavior_indicators_proficiency_levels_Level",
                        column: x => x.Level,
                        principalTable: "proficiency_levels",
                        principalColumn: "Ordinal",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cv_rubric_rules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FrameworkVersionId = table.Column<int>(type: "integer", nullable: false),
                    GroupCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    AcceptedEvidence = table.Column<string>(type: "text", nullable: false),
                    InsufficientEvidence = table.Column<string>(type: "text", nullable: false),
                    Example = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cv_rubric_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cv_rubric_rules_competency_groups_FrameworkVersionId_GroupC~",
                        columns: x => new { x.FrameworkVersionId, x.GroupCode },
                        principalTable: "competency_groups",
                        principalColumns: new[] { "FrameworkVersionId", "Code" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cv_rubric_rules_proficiency_levels_Level",
                        column: x => x.Level,
                        principalTable: "proficiency_levels",
                        principalColumn: "Ordinal",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "competency_framework_versions",
                columns: new[] { "Id", "IsActive", "PublishedAt", "Source", "Status", "Version" },
                values: new object[] { 1, false, null, "Proposed from repository Phase 0 catalogue and thesis roadmap; not a verbatim thesis appendix. Expert review pending.", "Draft", "cv-v1-draft" });

            migrationBuilder.InsertData(
                table: "proficiency_levels",
                columns: new[] { "Ordinal", "Code", "NameEn", "NameVi" },
                values: new object[,]
                {
                    { 0, "NO_EVIDENCE", "No evidence", "Không đủ bằng chứng" },
                    { 1, "BASIC", "Basic", "Cơ bản" },
                    { 2, "INTERMEDIATE", "Intermediate", "Trung cấp" },
                    { 3, "ADVANCED", "Advanced", "Nâng cao" }
                });

            migrationBuilder.InsertData(
                table: "competency_groups",
                columns: new[] { "Code", "FrameworkVersionId", "NameEn", "NameVi", "Order" },
                values: new object[,]
                {
                    { "APPLICATION", 1, "AI applications", "Ứng dụng AI", 4 },
                    { "CRITICAL_THINKING", 1, "Critical thinking", "Tư duy phản biện", 3 },
                    { "DATA", 1, "Data", "Dữ liệu", 2 },
                    { "ETHICS", 1, "AI ethics", "Đạo đức AI", 5 },
                    { "FOUNDATION", 1, "AI foundations", "Nền tảng AI", 1 },
                    { "FUTURE_OF_WORK", 1, "Future of work", "Tương lai việc làm", 7 },
                    { "TOOLS", 1, "AI tools", "Công cụ AI", 6 }
                });

            migrationBuilder.InsertData(
                table: "behavior_indicators",
                columns: new[] { "Id", "Code", "FrameworkVersionId", "GroupCode", "Level", "TextEn", "TextVi" },
                values: new object[,]
                {
                    { 1, "FOUNDATION.1.1", 1, "FOUNDATION", 1, "Explain AI in a specific project", "Giải thích AI trong một dự án cụ thể" },
                    { 2, "FOUNDATION.1.2", 1, "FOUNDATION", 1, "Distinguish AI prediction from programmed rules", "Phân biệt dự đoán AI với luật lập trình" },
                    { 3, "FOUNDATION.1.3", 1, "FOUNDATION", 1, "Describe the role of input data", "Mô tả vai trò dữ liệu đầu vào" },
                    { 4, "FOUNDATION.2.1", 1, "FOUNDATION", 2, "Describe completed model development steps", "Mô tả các bước phát triển mô hình đã thực hiện" },
                    { 5, "FOUNDATION.2.2", 1, "FOUNDATION", 2, "Explain a machine learning method selection", "Giải thích lựa chọn phương pháp học máy" },
                    { 6, "FOUNDATION.2.3", 1, "FOUNDATION", 2, "Interpret model evaluation results", "Diễn giải kết quả đánh giá mô hình" },
                    { 7, "FOUNDATION.3.1", 1, "FOUNDATION", 3, "Compare algorithms using measured criteria", "So sánh thuật toán bằng tiêu chí đo lường" },
                    { 8, "FOUNDATION.3.2", 1, "FOUNDATION", 3, "Analyze data and algorithm tradeoffs", "Phân tích đánh đổi dữ liệu và thuật toán" },
                    { 9, "FOUNDATION.3.3", 1, "FOUNDATION", 3, "Design an evaluation of model limitations", "Thiết kế phương án đánh giá giới hạn mô hình" },
                    { 10, "DATA.1.1", 1, "DATA", 1, "Collect data from identified sources", "Thu thập dữ liệu từ nguồn xác định" },
                    { 11, "DATA.1.2", 1, "DATA", 1, "Clean missing or duplicate values", "Làm sạch giá trị thiếu hoặc trùng lặp" },
                    { 12, "DATA.1.3", 1, "DATA", 1, "Describe data using tables or charts", "Mô tả dữ liệu bằng bảng hoặc biểu đồ" },
                    { 13, "DATA.2.1", 1, "DATA", 2, "Evaluate data quality against criteria", "Đánh giá chất lượng dữ liệu theo tiêu chí" },
                    { 14, "DATA.2.2", 1, "DATA", 2, "Build a data normalization workflow", "Xây dựng quy trình chuẩn hóa dữ liệu" },
                    { 15, "DATA.2.3", 1, "DATA", 2, "Analyze data to answer a project question", "Phân tích dữ liệu để trả lời câu hỏi dự án" },
                    { 16, "DATA.3.1", 1, "DATA", 3, "Design a data pipeline with quality checks", "Thiết kế pipeline dữ liệu có kiểm tra chất lượng" },
                    { 17, "DATA.3.2", 1, "DATA", 3, "Check data leakage across dataset splits", "Kiểm tra rò rỉ dữ liệu giữa các tập" },
                    { 18, "DATA.3.3", 1, "DATA", 3, "Design visualizations supporting decisions", "Thiết kế trực quan hóa hỗ trợ quyết định" },
                    { 19, "CRITICAL_THINKING.1.1", 1, "CRITICAL_THINKING", 1, "Identify sources of AI generated information", "Xác định nguồn của thông tin do AI tạo" },
                    { 20, "CRITICAL_THINKING.1.2", 1, "CRITICAL_THINKING", 1, "Check an AI result against an independent source", "Kiểm tra một kết quả AI bằng nguồn độc lập" },
                    { 21, "CRITICAL_THINKING.1.3", 1, "CRITICAL_THINKING", 1, "Identify limitations in an AI answer", "Nhận diện giới hạn trong câu trả lời AI" },
                    { 22, "CRITICAL_THINKING.2.1", 1, "CRITICAL_THINKING", 2, "Compare outputs from multiple AI tools", "So sánh đầu ra của nhiều công cụ AI" },
                    { 23, "CRITICAL_THINKING.2.2", 1, "CRITICAL_THINKING", 2, "Build criteria for verifying results", "Xây dựng tiêu chí kiểm chứng kết quả" },
                    { 24, "CRITICAL_THINKING.2.3", 1, "CRITICAL_THINKING", 2, "Analyze causes of incorrect AI results", "Phân tích nguyên nhân kết quả AI sai" },
                    { 25, "CRITICAL_THINKING.3.1", 1, "CRITICAL_THINKING", 3, "Design a reproducible AI verification workflow", "Thiết kế quy trình kiểm chứng AI có thể tái lập" },
                    { 26, "CRITICAL_THINKING.3.2", 1, "CRITICAL_THINKING", 3, "Evaluate validity of model conclusions", "Đánh giá tính hợp lệ của kết luận từ mô hình" },
                    { 27, "CRITICAL_THINKING.3.3", 1, "CRITICAL_THINKING", 3, "Test AI using counterexamples", "Kiểm thử AI với các trường hợp phản ví dụ" },
                    { 28, "APPLICATION.1.1", 1, "APPLICATION", 1, "Apply AI to a defined task", "Áp dụng AI cho một nhiệm vụ xác định" },
                    { 29, "APPLICATION.1.2", 1, "APPLICATION", 1, "Describe AI benefits in a work context", "Mô tả lợi ích AI trong bối cảnh công việc" },
                    { 30, "APPLICATION.1.3", 1, "APPLICATION", 1, "Identify obstacles to AI adoption", "Nhận diện trở ngại khi áp dụng AI" },
                    { 31, "APPLICATION.2.1", 1, "APPLICATION", 2, "Integrate AI into a real workflow", "Tích hợp AI vào quy trình thực tế" },
                    { 32, "APPLICATION.2.2", 1, "APPLICATION", 2, "Evaluate an AI solution against business criteria", "Đánh giá giải pháp AI bằng tiêu chí nghiệp vụ" },
                    { 33, "APPLICATION.2.3", 1, "APPLICATION", 2, "Handle errors in AI solution deployment", "Xử lý lỗi khi triển khai giải pháp AI" },
                    { 34, "APPLICATION.3.1", 1, "APPLICATION", 3, "Design an AI solution meeting practical constraints", "Thiết kế giải pháp AI đáp ứng ràng buộc thực tế" },
                    { 35, "APPLICATION.3.2", 1, "APPLICATION", 3, "Compare cost and effectiveness against a baseline", "So sánh chi phí và hiệu quả với phương án cơ sở" },
                    { 36, "APPLICATION.3.3", 1, "APPLICATION", 3, "Evaluate maintainability of an AI solution", "Đánh giá khả năng duy trì giải pháp AI" },
                    { 37, "ETHICS.1.1", 1, "ETHICS", 1, "Identify personal data in an AI task", "Nhận diện dữ liệu cá nhân trong tác vụ AI" },
                    { 38, "ETHICS.1.2", 1, "ETHICS", 1, "Describe bias risk in an application", "Mô tả rủi ro thiên lệch trong một ứng dụng" },
                    { 39, "ETHICS.1.3", 1, "ETHICS", 1, "Disclose AI use in a deliverable", "Ghi nhận việc sử dụng AI trong sản phẩm" },
                    { 40, "ETHICS.2.1", 1, "ETHICS", 2, "Apply data protection measures when using AI", "Áp dụng biện pháp bảo vệ dữ liệu khi dùng AI" },
                    { 41, "ETHICS.2.2", 1, "ETHICS", 2, "Evaluate AI risks by affected group", "Đánh giá rủi ro AI theo nhóm bị ảnh hưởng" },
                    { 42, "ETHICS.2.3", 1, "ETHICS", 2, "Perform bias checks against criteria", "Thực hiện kiểm tra thiên lệch có tiêu chí" },
                    { 43, "ETHICS.3.1", 1, "ETHICS", 3, "Develop responsible AI usage policies", "Xây dựng chính sách sử dụng AI có trách nhiệm" },
                    { 44, "ETHICS.3.2", 1, "ETHICS", 3, "Design an AI risk monitoring process", "Thiết kế quy trình giám sát rủi ro AI" },
                    { 45, "ETHICS.3.3", 1, "ETHICS", 3, "Evaluate risk mitigation effectiveness", "Đánh giá hiệu quả biện pháp giảm thiểu rủi ro" },
                    { 46, "TOOLS.1.1", 1, "TOOLS", 1, "Use an AI tool for a specific task", "Sử dụng công cụ AI cho tác vụ cụ thể" },
                    { 47, "TOOLS.1.2", 1, "TOOLS", 1, "Write a contextual instruction for an AI tool", "Viết yêu cầu có bối cảnh cho công cụ AI" },
                    { 48, "TOOLS.1.3", 1, "TOOLS", 1, "Check output before using it", "Kiểm tra đầu ra trước khi sử dụng" },
                    { 49, "TOOLS.2.1", 1, "TOOLS", 2, "Compare tools against work requirements", "So sánh công cụ theo nhu cầu công việc" },
                    { 50, "TOOLS.2.2", 1, "TOOLS", 2, "Integrate an AI tool using APIs or automation", "Tích hợp công cụ AI qua API hoặc tự động hóa" },
                    { 51, "TOOLS.2.3", 1, "TOOLS", 2, "Adjust a workflow based on output evaluation", "Điều chỉnh quy trình dựa trên đánh giá đầu ra" },
                    { 52, "TOOLS.3.1", 1, "TOOLS", 3, "Design coordination of multiple AI tools", "Thiết kế phối hợp nhiều công cụ AI" },
                    { 53, "TOOLS.3.2", 1, "TOOLS", 3, "Measure effectiveness of an AI tool workflow", "Đo hiệu quả quy trình dùng công cụ AI" },
                    { 54, "TOOLS.3.3", 1, "TOOLS", 3, "Propose improvements to AI tool accessibility", "Đề xuất cải tiến khả năng tiếp cận công cụ AI" },
                    { 55, "FUTURE_OF_WORK.1.1", 1, "FUTURE_OF_WORK", 1, "Identify tasks that AI may change", "Xác định nhiệm vụ có thể thay đổi do AI" },
                    { 56, "FUTURE_OF_WORK.1.2", 1, "FUTURE_OF_WORK", 1, "Describe skills needed to work with AI", "Mô tả kỹ năng cần học để làm việc với AI" },
                    { 57, "FUTURE_OF_WORK.1.3", 1, "FUTURE_OF_WORK", 1, "Participate in AI learning with specified content", "Tham gia hoạt động học kỹ năng AI có nội dung cụ thể" },
                    { 58, "FUTURE_OF_WORK.2.1", 1, "FUTURE_OF_WORK", 2, "Analyze role changes in a workflow", "Phân tích thay đổi vai trò trong một quy trình" },
                    { 59, "FUTURE_OF_WORK.2.2", 1, "FUTURE_OF_WORK", 2, "Propose training based on skill gaps", "Đề xuất kế hoạch đào tạo theo khoảng cách kỹ năng" },
                    { 60, "FUTURE_OF_WORK.2.3", 1, "FUTURE_OF_WORK", 2, "Evaluate a trial of AI assisted work", "Đánh giá kết quả thử nghiệm cách làm việc với AI" },
                    { 61, "FUTURE_OF_WORK.3.1", 1, "FUTURE_OF_WORK", 3, "Develop an organizational AI adaptation strategy", "Xây dựng chiến lược thích ứng AI cho tổ chức" },
                    { 62, "FUTURE_OF_WORK.3.2", 1, "FUTURE_OF_WORK", 3, "Evaluate AI impact on a workforce", "Đánh giá tác động AI đến lực lượng lao động" },
                    { 63, "FUTURE_OF_WORK.3.3", 1, "FUTURE_OF_WORK", 3, "Design criteria for tracking skill transformation", "Thiết kế tiêu chí theo dõi chuyển đổi kỹ năng" }
                });

            migrationBuilder.InsertData(
                table: "cv_rubric_rules",
                columns: new[] { "Id", "AcceptedEvidence", "Example", "FrameworkVersionId", "GroupCode", "InsufficientEvidence", "Level" },
                values: new object[,]
                {
                    { 1, "No valid span supports any indicator.", "Skills: ChatGPT, Python.", 1, "FOUNDATION", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 0 },
                    { 2, "A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Explain AI in a specific project' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "FOUNDATION", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 1 },
                    { 3, "A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Describe completed model development steps' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "FOUNDATION", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 2 },
                    { 4, "A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Compare algorithms using measured criteria' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "FOUNDATION", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 3 },
                    { 5, "No valid span supports any indicator.", "Skills: ChatGPT, Python.", 1, "DATA", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 0 },
                    { 6, "A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Collect data from identified sources' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "DATA", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 1 },
                    { 7, "A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Evaluate data quality against criteria' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "DATA", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 2 },
                    { 8, "A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Design a data pipeline with quality checks' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "DATA", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 3 },
                    { 9, "No valid span supports any indicator.", "Skills: ChatGPT, Python.", 1, "CRITICAL_THINKING", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 0 },
                    { 10, "A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Identify sources of AI generated information' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "CRITICAL_THINKING", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 1 },
                    { 11, "A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Compare outputs from multiple AI tools' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "CRITICAL_THINKING", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 2 },
                    { 12, "A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Design a reproducible AI verification workflow' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "CRITICAL_THINKING", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 3 },
                    { 13, "No valid span supports any indicator.", "Skills: ChatGPT, Python.", 1, "APPLICATION", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 0 },
                    { 14, "A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Apply AI to a defined task' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "APPLICATION", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 1 },
                    { 15, "A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Integrate AI into a real workflow' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "APPLICATION", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 2 },
                    { 16, "A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Design an AI solution meeting practical constraints' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "APPLICATION", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 3 },
                    { 17, "No valid span supports any indicator.", "Skills: ChatGPT, Python.", 1, "ETHICS", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 0 },
                    { 18, "A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Identify personal data in an AI task' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "ETHICS", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 1 },
                    { 19, "A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Apply data protection measures when using AI' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "ETHICS", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 2 },
                    { 20, "A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Develop responsible AI usage policies' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "ETHICS", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 3 },
                    { 21, "No valid span supports any indicator.", "Skills: ChatGPT, Python.", 1, "TOOLS", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 0 },
                    { 22, "A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Use an AI tool for a specific task' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "TOOLS", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 1 },
                    { 23, "A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Compare tools against work requirements' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "TOOLS", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 2 },
                    { 24, "A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Design coordination of multiple AI tools' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "TOOLS", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 3 },
                    { 25, "No valid span supports any indicator.", "Skills: ChatGPT, Python.", 1, "FUTURE_OF_WORK", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 0 },
                    { 26, "A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Identify tasks that AI may change' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "FUTURE_OF_WORK", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 1 },
                    { 27, "A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Analyze role changes in a workflow' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "FUTURE_OF_WORK", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 2 },
                    { 28, "A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.", "Synthetic annotation prompt: find a CV passage that demonstrates 'Develop an organizational AI adaptation strategy' and states the task context and candidate contribution. This prompt is not evidence itself.", 1, "FUTURE_OF_WORK", "Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_behavior_indicators_FrameworkVersionId_Code",
                table: "behavior_indicators",
                columns: new[] { "FrameworkVersionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_behavior_indicators_FrameworkVersionId_GroupCode",
                table: "behavior_indicators",
                columns: new[] { "FrameworkVersionId", "GroupCode" });

            migrationBuilder.CreateIndex(
                name: "IX_behavior_indicators_Level",
                table: "behavior_indicators",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_competency_framework_versions_IsActive",
                table: "competency_framework_versions",
                column: "IsActive",
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_competency_framework_versions_Version",
                table: "competency_framework_versions",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competency_groups_FrameworkVersionId_Order",
                table: "competency_groups",
                columns: new[] { "FrameworkVersionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cv_rubric_rules_FrameworkVersionId_GroupCode_Level",
                table: "cv_rubric_rules",
                columns: new[] { "FrameworkVersionId", "GroupCode", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cv_rubric_rules_Level",
                table: "cv_rubric_rules",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_proficiency_levels_Code",
                table: "proficiency_levels",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "behavior_indicators");

            migrationBuilder.DropTable(
                name: "cv_rubric_rules");

            migrationBuilder.DropTable(
                name: "competency_groups");

            migrationBuilder.DropTable(
                name: "proficiency_levels");

            migrationBuilder.DropTable(
                name: "competency_framework_versions");
        }
    }
}
