using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AiCompetency.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddParallelTestBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM exam_questions GROUP BY exam_id, question_id HAVING count(*) > 1) THEN
                        RAISE EXCEPTION 'An exam lists the same question more than once; remove duplicate exam_questions rows before applying this migration';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_exam_questions_exam_id",
                table: "exam_questions");

            migrationBuilder.AddColumn<string>(
                name: "review_note",
                table: "questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reviewed_at",
                table: "questions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reviewed_by",
                table: "questions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "blueprint",
                table: "exams",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_standard",
                table: "exams",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "published_at",
                table: "exams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "test_group_scores",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    raw_score = table.Column<decimal>(type: "numeric", nullable: false),
                    max_score = table.Column<decimal>(type: "numeric", nullable: false),
                    item_count = table.Column<int>(type: "integer", nullable: false),
                    percent = table.Column<decimal>(type: "numeric", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: true),
                    level_mapping_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    computed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_group_scores", x => new { x.session_id, x.group_code });
                    table.CheckConstraint("ck_test_group_scores_group", "group_code IN ('FOUNDATION','DATA','CRITICAL_THINKING','APPLICATION','ETHICS','TOOLS','FUTURE_OF_WORK')");
                    table.CheckConstraint("ck_test_group_scores_level", "level IS NULL OR (level BETWEEN 1 AND 3 AND level_mapping_version IS NOT NULL)");
                    table.CheckConstraint("ck_test_group_scores_values", "item_count > 0 AND max_score > 0 AND raw_score BETWEEN 0 AND max_score AND percent BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_test_group_scores_proficiency_levels_level",
                        column: x => x.level,
                        principalTable: "proficiency_levels",
                        principalColumn: "Ordinal",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_group_scores_test_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "test_sessions",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_level_mappings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    intermediate_min_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    advanced_min_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    min_items_per_group = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    reviewed_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    review_notes = table.Column<string>(type: "text", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_level_mappings", x => x.id);
                    table.CheckConstraint("ck_test_level_mappings_publication", "(status = 'Draft' AND published_at IS NULL AND NOT is_active) OR (status = 'Published' AND published_at IS NOT NULL AND reviewed_by IS NOT NULL)");
                    table.CheckConstraint("ck_test_level_mappings_thresholds", "intermediate_min_percent > 0 AND intermediate_min_percent < advanced_min_percent AND advanced_min_percent <= 100 AND min_items_per_group >= 1");
                });

            migrationBuilder.InsertData(
                table: "test_level_mappings",
                columns: new[] { "id", "advanced_min_percent", "created_at", "intermediate_min_percent", "is_active", "min_items_per_group", "published_at", "review_notes", "reviewed_by", "source", "status", "version" },
                values: new object[] { 1, 80m, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 50m, false, 5, null, null, null, "Proposed for thesis §2.12: below 50% Basic, 50–79.99% Intermediate, 80% or more Advanced; at least five objective items per group. Expert review pending.", "Draft", "tl-v1-draft" });

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    PERFORM setval(pg_get_serial_sequence('test_level_mappings', 'id'),
                        GREATEST((SELECT MAX(id) FROM test_level_mappings) + 1,
                                 nextval(pg_get_serial_sequence('test_level_mappings', 'id'))), false);
                END $$;
                """);

            // Phase 0 start-standard created one hidden exam per attempt. Their sessions are kept,
            // but they are no longer offered to candidates.
            migrationBuilder.Sql("""
                UPDATE exams SET status = 'archived', updated_at = now()
                WHERE status = 'active' AND blueprint IS NULL AND title = 'Standard Test'
                  AND description = 'Dynamically generated standard assessment.';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_questions_review_record",
                table: "questions",
                sql: "(reviewed_at IS NULL) = (reviewed_by IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_questions_reviewed_status",
                table: "questions",
                sql: "status <> 'Reviewed' OR (reviewed_at IS NOT NULL AND group_code IS NOT NULL AND target_level IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_exams_is_standard",
                table: "exams",
                column: "is_standard",
                unique: true,
                filter: "is_standard AND status = 'active'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_exams_standard_blueprint",
                table: "exams",
                sql: "NOT is_standard OR blueprint IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_exam_questions_exam_id_question_id",
                table: "exam_questions",
                columns: new[] { "exam_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_group_scores_level",
                table: "test_group_scores",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "IX_test_level_mappings_is_active",
                table: "test_level_mappings",
                column: "is_active",
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_test_level_mappings_version",
                table: "test_level_mappings",
                column: "version",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_group_scores");

            migrationBuilder.DropTable(
                name: "test_level_mappings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_questions_review_record",
                table: "questions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_questions_reviewed_status",
                table: "questions");

            migrationBuilder.DropIndex(
                name: "IX_exams_is_standard",
                table: "exams");

            migrationBuilder.DropCheckConstraint(
                name: "ck_exams_standard_blueprint",
                table: "exams");

            migrationBuilder.DropIndex(
                name: "IX_exam_questions_exam_id_question_id",
                table: "exam_questions");

            migrationBuilder.DropColumn(
                name: "review_note",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "reviewed_by",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "blueprint",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "is_standard",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "published_at",
                table: "exams");

            migrationBuilder.CreateIndex(
                name: "IX_exam_questions_exam_id",
                table: "exam_questions",
                column: "exam_id");
        }
    }
}
