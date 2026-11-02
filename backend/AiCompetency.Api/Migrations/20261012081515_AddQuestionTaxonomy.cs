using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCompetency.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "group_code",
                table: "questions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "target_level",
                table: "questions",
                type: "integer",
                nullable: true);

            // Preserve unmatched draft rows for review; published rows must be reconciled
            // before this migration can commit. Original legacy labels stay in metadata.
            migrationBuilder.Sql("""
                WITH mapped AS (
                    SELECT q.question_id,
                        COALESCE(d.code,
                            CASE regexp_replace(upper(coalesce(q.metadata->>'group', '')), '[^A-Z0-9]', '', 'g')
                                WHEN 'FOUNDATION' THEN 'FOUNDATION'
                                WHEN 'FUNDAMENTAL' THEN 'FOUNDATION'
                                WHEN 'AIFUNDAMENTALS' THEN 'FOUNDATION'
                                WHEN 'CG1' THEN 'FOUNDATION'
                                WHEN 'DATA' THEN 'DATA'
                                WHEN 'CRITICALTHINKING' THEN 'CRITICAL_THINKING'
                                WHEN 'AIUSECASES' THEN 'APPLICATION'
                                WHEN 'APPLICATION' THEN 'APPLICATION'
                                WHEN 'AIETHICS' THEN 'ETHICS'
                                WHEN 'ETHICS' THEN 'ETHICS'
                                WHEN 'AITOOLS' THEN 'TOOLS'
                                WHEN 'TOOLS' THEN 'TOOLS'
                                WHEN 'FUTUREOFWORK' THEN 'FUTURE_OF_WORK'
                            END) AS mapped_group,
                        CASE regexp_replace(upper(coalesce(q.metadata->>'level', '')), '[^A-Z0-9]', '', 'g')
                            WHEN 'BASIC' THEN 1 WHEN 'FOUNDATION' THEN 1
                            WHEN 'NENTANG' THEN 1 WHEN 'CL1' THEN 1
                            WHEN 'INTERMEDIATE' THEN 2 WHEN 'APPLY' THEN 2
                            WHEN 'APDUNG' THEN 2 WHEN 'CL2' THEN 2
                            WHEN 'ADVANCED' THEN 3 WHEN 'CREATE' THEN 3
                            WHEN 'KIENTAO' THEN 3 WHEN 'CL3' THEN 3
                        END AS mapped_level
                    FROM questions q
                    LEFT JOIN skills s ON s.skill_id = q.skill_id
                    LEFT JOIN domains d ON d.domain_id = s.domain_id
                )
                UPDATE questions q SET group_code = mapped.mapped_group, target_level = mapped.mapped_level,
                    metadata = CASE WHEN mapped.mapped_group IS NULL AND mapped.mapped_level IS NULL THEN q.metadata
                        ELSE (CASE WHEN mapped.mapped_group IS NOT NULL AND q.metadata ? 'group'
                                     AND q.metadata->>'group' <> mapped.mapped_group
                              THEN jsonb_set(q.metadata, '{legacyGroup}', q.metadata->'group')
                              ELSE q.metadata END)
                        || (CASE WHEN mapped.mapped_group IS NOT NULL
                            THEN jsonb_build_object('group', mapped.mapped_group) ELSE '{}'::jsonb END)
                        || (CASE WHEN mapped.mapped_level IS NOT NULL AND q.metadata ? 'level'
                                     AND q.metadata->>'level' <> (ARRAY['Basic','Intermediate','Advanced'])[mapped.mapped_level]
                            THEN jsonb_build_object('legacyLevel', q.metadata->>'level') ELSE '{}'::jsonb END)
                        || (CASE WHEN mapped.mapped_level IS NOT NULL
                            THEN jsonb_build_object('level', (ARRAY['Basic','Intermediate','Advanced'])[mapped.mapped_level])
                            ELSE '{}'::jsonb END)
                    END
                FROM mapped WHERE q.question_id = mapped.question_id;

                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM questions WHERE status = 'Published'
                        AND (group_code IS NULL OR target_level IS NULL)) THEN
                        RAISE EXCEPTION 'Published questions need group/level reconciliation before AddQuestionTaxonomy';
                    END IF;
                END $$;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_questions_group_code",
                table: "questions",
                sql: "group_code IS NULL OR group_code IN ('FOUNDATION','DATA','CRITICAL_THINKING','APPLICATION','ETHICS','TOOLS','FUTURE_OF_WORK')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_questions_published_classification",
                table: "questions",
                sql: "status <> 'Published' OR (group_code IS NOT NULL AND target_level IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_questions_target_level",
                table: "questions",
                sql: "target_level IS NULL OR target_level BETWEEN 1 AND 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_questions_group_code",
                table: "questions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_questions_published_classification",
                table: "questions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_questions_target_level",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "group_code",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "target_level",
                table: "questions");
        }
    }
}
