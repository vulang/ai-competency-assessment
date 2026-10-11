using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AiCompetency.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetencyCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Custom pre-EF skill IDs have no verified relationship to this new catalogue.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM questions WHERE skill_id IS NOT NULL)
                       OR EXISTS (SELECT 1 FROM user_skill_mastery) THEN
                        RAISE EXCEPTION 'Existing skill mappings require reconciliation before AddCompetencyCatalog.';
                    END IF;
                END $$;
                """);
            migrationBuilder.CreateTable(
                name: "frameworks",
                columns: table => new
                {
                    framework_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_frameworks", x => x.framework_id);
                });

            migrationBuilder.CreateTable(
                name: "domains",
                columns: table => new
                {
                    domain_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    framework_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    weight = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_domains", x => x.domain_id);
                    table.CheckConstraint("ck_domains_weight", "weight > 0");
                    table.ForeignKey(
                        name: "FK_domains_frameworks_framework_id",
                        column: x => x.framework_id,
                        principalTable: "frameworks",
                        principalColumn: "framework_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    skill_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    domain_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    weight = table.Column<decimal>(type: "numeric", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.skill_id);
                    table.CheckConstraint("ck_skills_weight", "weight > 0");
                    table.ForeignKey(
                        name: "FK_skills_domains_domain_id",
                        column: x => x.domain_id,
                        principalTable: "domains",
                        principalColumn: "domain_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "criteria",
                columns: table => new
                {
                    criteria_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    skill_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_criteria", x => x.criteria_id);
                    table.CheckConstraint("ck_criteria_level", "level BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_criteria_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "skill_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "frameworks",
                columns: new[] { "framework_id", "is_active", "name", "version" },
                values: new object[] { 1, true, "AI competency compatibility framework", "p0-v1" });

            migrationBuilder.InsertData(
                table: "domains",
                columns: new[] { "domain_id", "code", "framework_id", "name", "weight" },
                values: new object[,]
                {
                    { 1, "FOUNDATION", 1, "Fundamental", 1m },
                    { 2, "DATA", 1, "Data", 1m },
                    { 3, "CRITICAL_THINKING", 1, "Critical Thinking", 1m },
                    { 4, "APPLICATION", 1, "AI Use Cases", 1m },
                    { 5, "ETHICS", 1, "AI Ethics", 1m },
                    { 6, "TOOLS", 1, "AI Tools", 1m },
                    { 7, "FUTURE_OF_WORK", 1, "Future of Work", 1m }
                });

            migrationBuilder.InsertData(
                table: "skills",
                columns: new[] { "skill_id", "description", "domain_id", "is_default", "name", "weight" },
                values: new object[,]
                {
                    { 1, "Hiểu các thuật ngữ AI thông dụng, sự khác biệt giữa AI và chương trình truyền thống. Giải thích vai trò của dữ liệu và thuật toán; Đánh giá ưu nhược điểm của các thuật toán AI.", 1, true, "Fundamental", 1m },
                    { 2, "Hiểu vai trò của dữ liệu, quy trình thu thập, làm sạch, chuẩn hóa, phân tích và trực quan hóa dữ liệu bằng các công cụ hiện đại.", 2, true, "Data", 1m },
                    { 3, "Phân tích nguồn thông tin, đánh giá độ tin cậy, so sánh kết quả từ các công cụ AI và thực hiện xác thực kết quả (fact-check).", 3, true, "Critical Thinking", 1m },
                    { 4, "Ứng dụng AI trong các lĩnh vực, hiểu công nghệ nền tảng, thách thức triển khai và đánh giá tính khả thi lâu dài.", 4, true, "AI Use Cases", 1m },
                    { 5, "Nhận diện rủi ro (bias, riêng tư), tuân thủ nguyên tắc đạo đức và xây dựng văn hóa sử dụng AI trách nhiệm.", 5, true, "AI Ethics", 1m },
                    { 6, "Sử dụng và đánh giá các công cụ/nền tảng AI, tích hợp vào quy trình làm việc và đề xuất giải pháp cải thiện hiệu quả.", 6, true, "AI Tools", 1m },
                    { 7, "Tác động của AI đến việc làm, kỹ năng mới (upskilling), chiến lược tổ chức và thích ứng với môi trường làm việc AI.", 7, true, "Future of Work", 1m }
                });

            migrationBuilder.InsertData(
                table: "criteria",
                columns: new[] { "criteria_id", "description", "level", "name", "skill_id" },
                values: new object[,]
                {
                    { 1, "Hiểu thuật ngữ AI thông dụng và phân biệt AI với chương trình truyền thống", 1, "Basic", 1 },
                    { 2, "Xác định quy trình phát triển AI và hiểu tài liệu khoa học/chính sách", 2, "Intermediate", 1 },
                    { 3, "Giải thích vai trò dữ liệu/thuật toán và đánh giá các thuật toán AI", 3, "Advanced", 1 },
                    { 4, "Hiểu vai trò dữ liệu và nguồn thu thập, làm sạch cơ bản", 1, "Basic", 2 },
                    { 5, "Đánh giá chất lượng dữ liệu và sử dụng công cụ phân tích cơ bản", 2, "Intermediate", 2 },
                    { 6, "Sử dụng công cụ chuyên sâu để thiết kế và trực quan hóa dữ liệu", 3, "Advanced", 2 },
                    { 7, "Xác định nguồn thông tin và đánh giá độ tin cậy cơ bản", 1, "Basic", 3 },
                    { 8, "So sánh kết quả từ nhiều công cụ AI và phân tích phương pháp xác thực", 2, "Intermediate", 3 },
                    { 9, "Sử dụng công cụ chuyên dụng để kiểm tra tính chính xác và hiệu lực của AI", 3, "Advanced", 3 },
                    { 10, "Liệt kê ví dụ ứng dụng AI và xác định lợi ích/thách thức theo ngành", 1, "Basic", 4 },
                    { 11, "Mô tả công nghệ nền tảng và thách thức tiềm ẩn khi triển khai", 2, "Intermediate", 4 },
                    { 12, "Đánh giá tính phù hợp và bền vững lâu dài của giải pháp AI", 3, "Advanced", 4 },
                    { 13, "Liệt kê rủi ro AI và hiểu các nguyên tắc đạo đức cơ bản", 1, "Basic", 5 },
                    { 14, "Đánh giá mức độ rủi ro và hạn chế của các ứng dụng AI cụ thể", 2, "Intermediate", 5 },
                    { 15, "Xây dựng chính sách tổ chức và tư vấn văn hóa sử dụng AI đạo đức", 3, "Advanced", 5 },
                    { 16, "Liệt kê các công cụ AI và xác định lợi ích/hạn chế cơ bản", 1, "Basic", 6 },
                    { 17, "Nguyên tắc tích hợp AI vào công việc và đánh giá lựa chọn công cụ", 2, "Intermediate", 6 },
                    { 18, "Đề xuất kịch bản sử dụng mới và khuyến nghị tính tiếp cận của AI", 3, "Advanced", 6 },
                    { 19, "Xác định vai trò dễ bị tác động và nhận biết lợi ích/thách thức chung", 1, "Basic", 7 },
                    { 20, "Liên hệ thay đổi công nghệ quá khứ và đề xuất can thiệp đào tạo", 2, "Intermediate", 7 },
                    { 21, "Đánh giá tác động rộng lớn và phát triển chiến lược thích ứng của tổ chức", 3, "Advanced", 7 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_questions_skill_id",
                table: "questions",
                column: "skill_id");

            // Preserve legacy values produced by the random 1..10 seed, without
            // substituting an invented valid difficulty.
            migrationBuilder.Sql("""
                UPDATE questions
                SET metadata = jsonb_set(
                    CASE WHEN jsonb_typeof(metadata) = 'object' THEN metadata
                         ELSE jsonb_build_object('legacyMetadata', metadata) END,
                    '{legacyDifficultyLevel}', to_jsonb(difficulty_level)),
                    difficulty_level = NULL
                WHERE difficulty_level NOT BETWEEN 1 AND 5;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_questions_difficulty",
                table: "questions",
                sql: "difficulty_level IS NULL OR difficulty_level BETWEEN 1 AND 5");

            migrationBuilder.CreateIndex(
                name: "IX_criteria_skill_id_name_level",
                table: "criteria",
                columns: new[] { "skill_id", "name", "level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_domains_framework_id_code",
                table: "domains",
                columns: new[] { "framework_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_domains_framework_id_name",
                table: "domains",
                columns: new[] { "framework_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_frameworks_is_active",
                table: "frameworks",
                column: "is_active",
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_frameworks_version",
                table: "frameworks",
                column: "version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skills_domain_id",
                table: "skills",
                column: "domain_id",
                unique: true,
                filter: "is_default = true");

            migrationBuilder.CreateIndex(
                name: "IX_skills_domain_id_name",
                table: "skills",
                columns: new[] { "domain_id", "name" },
                unique: true);

            migrationBuilder.Sql("""
                UPDATE questions SET skill_id = 1
                WHERE skill_id IS NULL AND upper(regexp_replace(coalesce(metadata->>'group', ''), '[^a-zA-Z0-9]', '', 'g'))
                    IN ('AIFUNDAMENTALS', 'CG1', 'FOUNDATION', 'FUNDAMENTAL');
                """);
            migrationBuilder.Sql("""
                UPDATE questions SET skill_id = 2
                WHERE skill_id IS NULL AND upper(regexp_replace(coalesce(metadata->>'group', ''), '[^a-zA-Z0-9]', '', 'g'))
                    IN ('DATA');
                """);
            migrationBuilder.Sql("""
                UPDATE questions SET skill_id = 3
                WHERE skill_id IS NULL AND upper(regexp_replace(coalesce(metadata->>'group', ''), '[^a-zA-Z0-9]', '', 'g'))
                    IN ('CRITICALTHINKING');
                """);
            migrationBuilder.Sql("""
                UPDATE questions SET skill_id = 4
                WHERE skill_id IS NULL AND upper(regexp_replace(coalesce(metadata->>'group', ''), '[^a-zA-Z0-9]', '', 'g'))
                    IN ('AIUSECASES', 'APPLICATION');
                """);
            migrationBuilder.Sql("""
                UPDATE questions SET skill_id = 5
                WHERE skill_id IS NULL AND upper(regexp_replace(coalesce(metadata->>'group', ''), '[^a-zA-Z0-9]', '', 'g'))
                    IN ('AIETHICS', 'ETHICS');
                """);
            migrationBuilder.Sql("""
                UPDATE questions SET skill_id = 6
                WHERE skill_id IS NULL AND upper(regexp_replace(coalesce(metadata->>'group', ''), '[^a-zA-Z0-9]', '', 'g'))
                    IN ('AITOOLS', 'TOOLS');
                """);
            migrationBuilder.Sql("""
                UPDATE questions SET skill_id = 7
                WHERE skill_id IS NULL AND upper(regexp_replace(coalesce(metadata->>'group', ''), '[^a-zA-Z0-9]', '', 'g'))
                    IN ('FUTUREOFWORK');
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_questions_skills_skill_id",
                table: "questions",
                column: "skill_id",
                principalTable: "skills",
                principalColumn: "skill_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM questions WHERE skill_id IS NOT NULL)
                       OR EXISTS (SELECT 1 FROM user_skill_mastery) THEN
                        RAISE EXCEPTION 'Cannot remove a competency catalogue referenced by assessment data.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_questions_skills_skill_id",
                table: "questions");

            migrationBuilder.DropTable(
                name: "criteria");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "domains");

            migrationBuilder.DropTable(
                name: "frameworks");

            migrationBuilder.DropIndex(
                name: "IX_questions_skill_id",
                table: "questions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_questions_difficulty",
                table: "questions");
        }
    }
}
