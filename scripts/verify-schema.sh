#!/usr/bin/env bash
set -euo pipefail
repo_root=$(cd "$(dirname "$0")/.." && pwd)
cd "$repo_root"
validation_dir=$(mktemp -d)
container_name="ai-cas-schema-check-$$"
cleanup() {
  docker rm -f "$container_name" >/dev/null 2>&1 || true
  rm -rf "$validation_dir"
}
trap cleanup EXIT

dotnet build backend/AiCompetency.Api --configuration Release --no-restore -p:UseSharedCompilation=false --verbosity quiet > "$validation_dir/build.log" 2>&1 || { cat "$validation_dir/build.log"; exit 1; }
dotnet ef migrations script --idempotent --project backend/AiCompetency.Api --configuration Release --no-build --output "$validation_dir/schema.sql" > "$validation_dir/ef.log" 2>&1 || { cat "$validation_dir/ef.log"; exit 1; }
dotnet ef migrations script 0 UseIdentitySubjectIds --project backend/AiCompetency.Api --configuration Release --no-build --output "$validation_dir/baseline.sql" > "$validation_dir/ef.log" 2>&1 || { cat "$validation_dir/ef.log"; exit 1; }
dotnet ef migrations script UseIdentitySubjectIds AddCompetencyCatalog --project backend/AiCompetency.Api --configuration Release --no-build --output "$validation_dir/catalog.sql" > "$validation_dir/ef.log" 2>&1 || { cat "$validation_dir/ef.log"; exit 1; }
dotnet ef migrations script AddCompetencyCatalog AddQuestionTaxonomy --project backend/AiCompetency.Api --configuration Release --no-build --output "$validation_dir/taxonomy.sql" > "$validation_dir/ef.log" 2>&1 || { cat "$validation_dir/ef.log"; exit 1; }
dotnet ef migrations script AddQuestionTaxonomy AlignCvDraftRubricWithThesis --project backend/AiCompetency.Api --configuration Release --no-build --output "$validation_dir/rubric.sql" > "$validation_dir/ef.log" 2>&1 || { cat "$validation_dir/ef.log"; exit 1; }
dotnet ef migrations script AlignCvDraftRubricWithThesis PublishCvFrameworkV1 --project backend/AiCompetency.Api --configuration Release --no-build --output "$validation_dir/release.sql" > "$validation_dir/ef.log" 2>&1 || { cat "$validation_dir/ef.log"; exit 1; }
dotnet ef migrations script PublishCvFrameworkV1 RecordExpertReviewOfCvV1 --project backend/AiCompetency.Api --configuration Release --no-build --output "$validation_dir/review.sql" > "$validation_dir/ef.log" 2>&1 || { cat "$validation_dir/ef.log"; exit 1; }

docker run --rm --detach --network none --tmpfs /var/lib/postgresql/data:rw,size=512m --name "$container_name" -e POSTGRES_PASSWORD=schema-validation postgres:16 > /dev/null
for attempt in {1..30}; do
  if docker exec "$container_name" pg_isready -U postgres >/dev/null 2>&1; then break; fi
  sleep 1
done
run_sql() {
  local database=$1
  if ! docker exec -i "$container_name" psql -v ON_ERROR_STOP=1 -U postgres -d "$database" > "$validation_dir/sql.log" 2>&1; then
    cat "$validation_dir/sql.log"
    return 1
  fi
}
run_sql postgres < "$validation_dir/schema.sql"
run_sql postgres < "$validation_dir/schema.sql"
run_sql postgres <<'SQL'
DO $$ BEGIN
  IF (SELECT count(*) FROM domains) <> 7 OR (SELECT count(*) FROM skills) <> 7 OR (SELECT count(*) FROM criteria) <> 21 THEN
    RAISE EXCEPTION 'Incorrect catalogue seed counts';
  END IF;
  IF (SELECT count(*) FROM competency_groups) <> 14
      OR (SELECT count(*) FROM proficiency_levels) <> 4
      OR (SELECT count(*) FROM behavior_indicators) <> 126
      OR (SELECT count(*) FROM cv_rubric_rules) <> 56 THEN
    RAISE EXCEPTION 'Incorrect CV framework seed counts';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM proficiency_levels WHERE "Ordinal" = 0 AND "Code" = 'NO_EVIDENCE')
      OR (SELECT count(*) FROM competency_framework_versions WHERE "IsActive") <> 1
      OR NOT EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Version" = 'cv-v1' AND "Status" = 'Published' AND "IsActive")
      OR NOT EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Version" = 'cv-v1' AND "Source" LIKE '%Expert review and approval were confirmed%')
      OR NOT EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Version" = 'cv-v1-draft' AND "Status" = 'Draft' AND NOT "IsActive") THEN
    RAISE EXCEPTION 'Level zero or CV publication state is incorrect';
  END IF;
  BEGIN
    UPDATE competency_framework_versions SET "IsActive" = true WHERE "Version" = 'cv-v1-draft';
    RAISE EXCEPTION 'Draft framework was allowed to become active';
  EXCEPTION WHEN check_violation THEN NULL; END;
  IF (SELECT nextval(pg_get_serial_sequence('competency_framework_versions', 'Id'))) <= 2
      OR (SELECT nextval(pg_get_serial_sequence('behavior_indicators', 'Id'))) <= 126
      OR (SELECT nextval(pg_get_serial_sequence('cv_rubric_rules', 'Id'))) <= 56 THEN
    RAISE EXCEPTION 'CV seed identity sequences were not advanced';
  END IF;
  BEGIN
    UPDATE proficiency_levels SET "Code" = 'BASIC' WHERE "Ordinal" = 0;
    RAISE EXCEPTION 'Incorrect level code was accepted';
  EXCEPTION WHEN check_violation OR unique_violation THEN NULL; END;
  BEGIN
    UPDATE behavior_indicators SET "Level" = 0 WHERE "Id" = 1;
    RAISE EXCEPTION 'A no-evidence behavior indicator was accepted';
  EXCEPTION WHEN check_violation THEN NULL; END;
  BEGIN
    UPDATE behavior_indicators SET "FrameworkVersionId" = 999 WHERE "Id" = 1;
    RAISE EXCEPTION 'Cross-version indicator reference was accepted';
  EXCEPTION WHEN foreign_key_violation THEN NULL; END;
  BEGIN
    INSERT INTO questions (type, content, metadata, status)
    VALUES ('mcq_single', 'missing taxonomy', '{}', 'Published');
    RAISE EXCEPTION 'Published question without classification was accepted';
  EXCEPTION WHEN check_violation THEN NULL; END;
  BEGIN
    INSERT INTO questions (type, content, metadata, status, group_code, target_level)
    VALUES ('mcq_single', 'invalid taxonomy', '{}', 'Published', 'TOOLS', 0);
    RAISE EXCEPTION 'Invalid target level was accepted';
  EXCEPTION WHEN check_violation THEN NULL; END;
  BEGIN
    INSERT INTO questions (type, content, metadata, status, skill_id) VALUES ('mcq_single', 'invalid', '{}', 'Draft', 999);
    RAISE EXCEPTION 'Question skill foreign key did not reject an invalid ID';
  EXCEPTION WHEN foreign_key_violation THEN NULL; END;
  BEGIN
    INSERT INTO questions (type, content, metadata, status, difficulty_level) VALUES ('mcq_single', 'invalid', '{}', 'Draft', 10);
    RAISE EXCEPTION 'Difficulty constraint did not reject 10';
  EXCEPTION WHEN check_violation THEN NULL; END;
END $$;
CREATE DATABASE legacy;
CREATE DATABASE existing_cv;
CREATE DATABASE custom_mappings;
CREATE DATABASE unresolved_published;
SQL
run_sql legacy < "$validation_dir/baseline.sql"
run_sql legacy <<'SQL'
INSERT INTO questions (type, content, metadata, status, difficulty_level)
VALUES ('mcq_single', 'recognized group', '{"group":"AITools","level":"Advanced"}', 'Published', 10),
       ('mcq_single', 'unknown group', '{"group":"ambiguous old group"}', 'Draft', 2);
SQL
run_sql legacy < "$validation_dir/catalog.sql"
run_sql legacy < "$validation_dir/taxonomy.sql"
run_sql legacy <<'SQL'
UPDATE cv_rubric_rules SET "Example" = 'Expert edit retained' WHERE "Id" = 2;
SQL
run_sql legacy < "$validation_dir/rubric.sql"
run_sql legacy < "$validation_dir/release.sql"
run_sql legacy < "$validation_dir/review.sql"
run_sql legacy <<'SQL'
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM cv_rubric_rules WHERE "Id" = 2 AND "Example" = 'Expert edit retained')
    OR NOT EXISTS (SELECT 1 FROM cv_rubric_rules WHERE "Id" = 3 AND "AcceptedEvidence" LIKE '%vận dụng độc lập%') THEN
    RAISE EXCEPTION 'Thesis rubric migration overwrote an edited draft or missed seed rows';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM cv_rubric_rules WHERE "FrameworkVersionId" = 2 AND "GroupCode" = 'FOUNDATION' AND "Level" = 1 AND "AcceptedEvidence" LIKE '%học phần/chứng chỉ có sản phẩm%')
    OR NOT EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Version" = 'cv-v1' AND "IsActive" AND "Source" LIKE '%Expert review and approval were confirmed%') THEN
    RAISE EXCEPTION 'Published CV framework was not seeded independently of the edited draft';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM questions WHERE content = 'recognized group' AND skill_id = 6 AND difficulty_level IS NULL
    AND metadata->>'legacyDifficultyLevel' = '10' AND metadata->>'level' = 'Advanced'
    AND group_code = 'TOOLS' AND target_level = 3 AND metadata->>'legacyGroup' = 'AITools') THEN
    RAISE EXCEPTION 'Legacy backfill or difficulty preservation failed';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM questions WHERE content = 'unknown group' AND skill_id IS NULL AND difficulty_level = 2 AND group_code IS NULL) THEN
    RAISE EXCEPTION 'Ambiguous group was modified';
  END IF;
END $$;
SQL
run_sql existing_cv < "$validation_dir/baseline.sql"
run_sql existing_cv < "$validation_dir/catalog.sql"
run_sql existing_cv < "$validation_dir/taxonomy.sql"
run_sql existing_cv < "$validation_dir/rubric.sql"
run_sql existing_cv <<'SQL'
INSERT INTO competency_framework_versions ("Id", "Version", "Status", "Source", "PublishedAt", "IsActive")
VALUES (2, 'cv-v2', 'Published', 'Existing reviewed release', '2026-10-11T00:00:00+00:00', true);
SQL
if docker exec -i "$container_name" psql -v ON_ERROR_STOP=1 -U postgres -d existing_cv < "$validation_dir/release.sql" > "$validation_dir/release-guard.log" 2>&1; then
  echo 'Existing active CV release was incorrectly replaced.' >&2
  exit 1
fi
grep -q 'CV v1 release conflicts with existing framework data' "$validation_dir/release-guard.log"
run_sql existing_cv <<'SQL'
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Version" = 'cv-v2' AND "IsActive")
      OR EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Version" = 'cv-v1') THEN
    RAISE EXCEPTION 'CV release guard modified an existing active version';
  END IF;
END $$;
SQL
run_sql unresolved_published < "$validation_dir/baseline.sql"
run_sql unresolved_published < "$validation_dir/catalog.sql"
run_sql unresolved_published <<'SQL'
INSERT INTO questions (type, content, metadata, status)
VALUES ('mcq_single', 'needs review', '{"group":"unmapped group","level":"Basic"}', 'Published');
SQL
if docker exec -i "$container_name" psql -v ON_ERROR_STOP=1 -U postgres -d unresolved_published < "$validation_dir/taxonomy.sql" > "$validation_dir/taxonomy-guard.log" 2>&1; then
  echo 'Unresolved published question was incorrectly accepted.' >&2
  exit 1
fi
grep -q 'Published questions need group/level reconciliation' "$validation_dir/taxonomy-guard.log"
run_sql unresolved_published <<'SQL'
DO $$ BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'questions' AND column_name = 'group_code')
    OR (SELECT status FROM questions WHERE content = 'needs review') <> 'Published' THEN
    RAISE EXCEPTION 'Taxonomy guard failed to preserve the existing question';
  END IF;
END $$;
SQL
run_sql custom_mappings < "$validation_dir/baseline.sql"
run_sql custom_mappings <<'SQL'
INSERT INTO questions (type, content, metadata, status, skill_id) VALUES ('mcq_single', 'custom mapping', '{}', 'Draft', 99);
SQL
if docker exec -i "$container_name" psql -v ON_ERROR_STOP=1 -U postgres -d custom_mappings < "$validation_dir/catalog.sql" > "$validation_dir/guard.log" 2>&1; then
  echo 'Custom mappings were incorrectly accepted.' >&2
  exit 1
fi
grep -q 'Existing skill mappings require reconciliation' "$validation_dir/guard.log"
run_sql custom_mappings <<'SQL'
DO $$ BEGIN
  IF (SELECT skill_id FROM questions WHERE content = 'custom mapping') <> 99 OR to_regclass('frameworks') IS NOT NULL THEN
    RAISE EXCEPTION 'Guard failed to preserve the pre-migration database';
  END IF;
END $$;
SQL
echo 'PostgreSQL schema checks passed: fresh install, repeat apply, constraints, legacy backfill, existing release and reconciliation guards.'
