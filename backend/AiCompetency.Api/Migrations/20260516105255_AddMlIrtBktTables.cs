using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCompetency.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMlIrtBktTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "competency_profiles",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scores = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    theta = table.Column<decimal>(type: "numeric", nullable: true),
                    overall_level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    overall_score = table.Column<decimal>(type: "numeric", nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_profiles", x => x.profile_id);
                });

            migrationBuilder.CreateTable(
                name: "question_irt_params",
                columns: table => new
                {
                    question_id = table.Column<int>(type: "integer", nullable: false),
                    a_param = table.Column<decimal>(type: "numeric", nullable: false),
                    b_param = table.Column<decimal>(type: "numeric", nullable: false),
                    c_param = table.Column<decimal>(type: "numeric", nullable: false),
                    se_b = table.Column<decimal>(type: "numeric", nullable: true),
                    calibrated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    response_count = table.Column<int>(type: "integer", nullable: false),
                    is_calibrated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_irt_params", x => x.question_id);
                    table.ForeignKey(
                        name: "FK_question_irt_params_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "question_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "session_irt_estimates",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    theta = table.Column<decimal>(type: "numeric", nullable: false),
                    se_theta = table.Column<decimal>(type: "numeric", nullable: false),
                    method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    computed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_irt_estimates", x => x.session_id);
                    table.ForeignKey(
                        name: "FK_session_irt_estimates_test_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "test_sessions",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_skill_mastery",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    skill_id = table.Column<int>(type: "integer", nullable: false),
                    mastery_prob = table.Column<decimal>(type: "numeric", nullable: false),
                    response_count = table.Column<int>(type: "integer", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_skill_mastery", x => new { x.user_id, x.skill_id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "competency_profiles");

            migrationBuilder.DropTable(
                name: "question_irt_params");

            migrationBuilder.DropTable(
                name: "session_irt_estimates");

            migrationBuilder.DropTable(
                name: "user_skill_mastery");
        }
    }
}
