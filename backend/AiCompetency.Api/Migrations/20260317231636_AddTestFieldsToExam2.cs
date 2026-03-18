using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCompetency.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTestFieldsToExam2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_published",
                table: "exams");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "exams",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "exams",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "question_count",
                table: "exams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "exams",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "exams",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "description",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "question_count",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "status",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "exams");

            migrationBuilder.AddColumn<bool>(
                name: "is_published",
                table: "exams",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
