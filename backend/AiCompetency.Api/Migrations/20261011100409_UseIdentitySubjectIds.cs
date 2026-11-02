using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCompetency.Api.Migrations;

public partial class UseIdentitySubjectIds : Migration
{
    private static readonly string[] Tables =
        ["user_skill_mastery", "test_sessions", "competency_profiles", "adaptive_test_sessions"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The old fallback mixed candidates under integer IDs. Preserve those rows
        // in a legacy namespace; never invent an Identity owner for them.
        foreach (var table in Tables)
            migrationBuilder.Sql($"ALTER TABLE {table} ALTER COLUMN user_id TYPE text USING ('legacy:' || user_id::text);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Refuse to discard or convert real Identity IDs on rollback.
        foreach (var table in Tables)
            migrationBuilder.Sql($"""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM {table} WHERE user_id !~ '^legacy:[0-9]+$') THEN
                        RAISE EXCEPTION 'Cannot roll back identity migration: {table} contains Identity-owned rows.';
                    END IF;
                END $$;
                ALTER TABLE {table} ALTER COLUMN user_id TYPE integer USING substring(user_id FROM 8)::integer;
                """);
    }
}
