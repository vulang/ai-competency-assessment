using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCompetency.Api.Migrations
{
    /// <inheritdoc />
    public partial class PublishTestLevelMappingV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM test_level_mappings WHERE id = 2 OR version = 'tl-v1' OR is_active) THEN
                        RAISE EXCEPTION 'tl-v1 release conflicts with existing level mappings; reconcile mapping IDs and the active rule before applying this migration';
                    END IF;
                END $$;
                """);

            migrationBuilder.InsertData(
                table: "test_level_mappings",
                columns: new[] { "id", "advanced_min_percent", "created_at", "intermediate_min_percent", "is_active", "min_items_per_group", "published_at", "review_notes", "reviewed_by", "source", "status", "version" },
                values: new object[] { 2, 80m, new DateTimeOffset(new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 50m, true, 5, new DateTimeOffset(new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "The project owner confirmed expert approval of tl-v1-draft as proposed on 2026-11-04. The expert's name and review date were not supplied.", "Project owner confirmation of expert approval", "Thesis §2.12 rule from tl-v1-draft: below 50% Basic, 50–79.99% Intermediate, 80% or more Advanced; at least five objective items per group.", "Published", "tl-v1" });

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    PERFORM setval(pg_get_serial_sequence('test_level_mappings', 'id'),
                        GREATEST((SELECT MAX(id) FROM test_level_mappings) + 1,
                                 nextval(pg_get_serial_sequence('test_level_mappings', 'id'))), false);
                END $$;
                """);

            // Same rule as publishing through the API: scores recorded without a rule receive tl-v1 levels.
            migrationBuilder.Sql("""
                UPDATE test_group_scores
                SET level = CASE WHEN item_count < 5 THEN NULL WHEN percent >= 80 THEN 3 WHEN percent >= 50 THEN 2 ELSE 1 END,
                    level_mapping_version = 'tl-v1'
                WHERE level_mapping_version IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE test_group_scores SET level = NULL, level_mapping_version = NULL WHERE level_mapping_version = 'tl-v1';");

            migrationBuilder.DeleteData(
                table: "test_level_mappings",
                keyColumn: "id",
                keyValue: 2);
        }
    }
}
