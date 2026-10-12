using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCompetency.Api.Migrations
{
    /// <inheritdoc />
    public partial class RecordExpertReviewOfCvV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE competency_framework_versions
                SET "Source" = 'Proposed indicators and rubric aligned to thesis v8.1 sections 1.2 and 2.3. Expert review and approval were confirmed by the project owner on 2026-10-12; the reviewer name and review date were not supplied. The thesis lists Appendices A/B as planned without their full text.'
                WHERE "Id" = 2 AND "Version" = 'cv-v1' AND "Status" = 'Published'
                  AND "Source" = 'Project owner approval on 2026-10-12 of the proposed indicators and rubric aligned to thesis v8.1 sections 1.2 and 2.3. The supplied thesis lists Appendices A/B as planned without their full text.';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE competency_framework_versions
                SET "Source" = 'Project owner approval on 2026-10-12 of the proposed indicators and rubric aligned to thesis v8.1 sections 1.2 and 2.3. The supplied thesis lists Appendices A/B as planned without their full text.'
                WHERE "Id" = 2 AND "Version" = 'cv-v1' AND "Status" = 'Published'
                  AND "Source" = 'Proposed indicators and rubric aligned to thesis v8.1 sections 1.2 and 2.3. Expert review and approval were confirmed by the project owner on 2026-10-12; the reviewer name and review date were not supplied. The thesis lists Appendices A/B as planned without their full text.';
                """);
        }
    }
}
