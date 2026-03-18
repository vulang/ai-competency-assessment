using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCompetency.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAdaptiveTestingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adaptive_test_sessions",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    current_ability_estimate = table.Column<decimal>(type: "numeric", nullable: false),
                    questions_asked_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adaptive_test_sessions", x => x.session_id);
                });

            migrationBuilder.CreateTable(
                name: "adaptive_responses",
                columns: table => new
                {
                    response_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<int>(type: "integer", nullable: false),
                    question_difficulty_level = table.Column<decimal>(type: "numeric", nullable: false),
                    user_answer = table.Column<string>(type: "text", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    ability_after_response = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adaptive_responses", x => x.response_id);
                    table.ForeignKey(
                        name: "FK_adaptive_responses_adaptive_test_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "adaptive_test_sessions",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_adaptive_responses_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "question_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adaptive_responses_question_id",
                table: "adaptive_responses",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_adaptive_responses_session_id",
                table: "adaptive_responses",
                column: "session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adaptive_responses");

            migrationBuilder.DropTable(
                name: "adaptive_test_sessions");
        }
    }
}
