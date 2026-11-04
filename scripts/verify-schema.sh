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
dotnet ef migrations script RecordExpertReviewOfCvV1 AddParallelTestBranch --project backend/AiCompetency.Api --configuration Release --no-build --output "$validation_dir/testbranch.sql" > "$validation_dir/ef.log" 2>&1 || { cat "$validation_dir/ef.log"; exit 1; }
dotnet ef migrations script AddParallelTestBranch PublishTestLevelMappingV1 --project backend/AiCompetency.Api --configuration Release --no-build --output "$validation_dir/levels.sql" > "$validation_dir/ef.log" 2>&1 || { cat "$validation_dir/ef.log"; exit 1; }

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
  IF NOT EXISTS (SELECT 1 FROM test_level_mappings WHERE version = 'tl-v1-draft' AND status = 'Draft' AND NOT is_active
      AND intermediate_min_percent = 50 AND advanced_min_percent = 80 AND min_items_per_group = 5)
      OR NOT EXISTS (SELECT 1 FROM test_level_mappings WHERE version = 'tl-v1' AND status = 'Published' AND is_active
      AND intermediate_min_percent = 50 AND advanced_min_percent = 80 AND min_items_per_group = 5
      AND review_notes LIKE '%confirmed expert approval%')
      OR (SELECT count(*) FROM test_level_mappings WHERE is_active) <> 1
      OR (SELECT nextval(pg_get_serial_sequence('test_level_mappings', 'id'))) <= 2 THEN
    RAISE EXCEPTION 'Level mapping seeds, the active rule or the sequence are incorrect';
  END IF;
  BEGIN
    UPDATE test_level_mappings SET is_active = true WHERE version = 'tl-v1-draft';
    RAISE EXCEPTION 'Draft level mapping was allowed to become active';
  EXCEPTION WHEN check_violation THEN NULL; END;
  BEGIN
    UPDATE test_level_mappings SET advanced_min_percent = 40 WHERE version = 'tl-v1-draft';
    RAISE EXCEPTION 'Inverted level thresholds were accepted';
  EXCEPTION WHEN check_violation THEN NULL; END;
  BEGIN
    INSERT INTO questions (type, content, metadata, status, group_code, target_level)
    VALUES ('mcq_single', 'unreviewed', '{}', 'Reviewed', 'DATA', 1);
    RAISE EXCEPTION 'Reviewed status without a review record was accepted';
  EXCEPTION WHEN check_violation THEN NULL; END;
  BEGIN
    INSERT INTO questions (type, content, metadata, status, reviewed_by) VALUES ('mcq_single', 'half review', '{}', 'Draft', 'x');
    RAISE EXCEPTION 'Reviewer without review time was accepted';
  EXCEPTION WHEN check_violation THEN NULL; END;
  BEGIN
    INSERT INTO exams (title, description, duration_minutes, pass_score, question_count, status, created_at, updated_at, is_standard)
    VALUES ('No blueprint', '', 60, 1, 0, 'draft', now(), now(), true);
    RAISE EXCEPTION 'Standard exam without a blueprint was accepted';
  EXCEPTION WHEN check_violation THEN NULL; END;
  BEGIN
    INSERT INTO exams (title, description, duration_minutes, pass_score, question_count, status, created_at, updated_at, is_standard, blueprint)
    VALUES ('Standard A', '', 60, 1, 0, 'active', now(), now(), true, '{}'), ('Standard B', '', 60, 1, 0, 'active', now(), now(), true, '{}');
    RAISE EXCEPTION 'Two active standard exams were accepted';
  EXCEPTION WHEN unique_violation THEN NULL; END;
  BEGIN
    WITH exam AS (INSERT INTO exams (title, description, duration_minutes, pass_score, question_count, status, created_at, updated_at)
                  VALUES ('Scores', '', 60, 1, 0, 'active', now(), now()) RETURNING exam_id),
         session AS (INSERT INTO test_sessions (session_id, user_id, exam_id, start_time, total_score, status)
                     SELECT gen_random_uuid(), 'u', exam_id, now(), 0, 'Completed' FROM exam RETURNING session_id)
    INSERT INTO test_group_scores (session_id, group_code, raw_score, max_score, item_count, percent, level, computed_at)
    SELECT session_id, 'DATA', 4, 5, 5, 80, 3, now() FROM session;
    RAISE EXCEPTION 'A group level without its mapping version was accepted';
  EXCEPTION WHEN check_violation THEN NULL; END;
END $$;
CREATE DATABASE legacy;
CREATE DATABASE duplicate_exam_questions;
CREATE DATABASE active_level_mapping;
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
INSERT INTO exams (title, description, duration_minutes, pass_score, question_count, status, created_at, updated_at)
VALUES ('Standard Test', 'Dynamically generated standard assessment.', 60, 1, 1, 'active', now(), now()),
       ('Hand-built exam', 'Kept active', 30, 1, 1, 'active', now(), now());
INSERT INTO test_sessions (session_id, user_id, exam_id, start_time, total_score, status)
SELECT gen_random_uuid(), 'legacy-user', exam_id, now(), 1, 'Completed' FROM exams WHERE title = 'Standard Test';
SQL
run_sql legacy < "$validation_dir/testbranch.sql"
run_sql legacy <<'SQL'
DO $$ BEGIN
  IF (SELECT status FROM exams WHERE title = 'Standard Test') <> 'archived'
      OR (SELECT status FROM exams WHERE title = 'Hand-built exam') <> 'active'
      OR NOT EXISTS (SELECT 1 FROM test_sessions WHERE user_id = 'legacy-user')
      OR NOT EXISTS (SELECT 1 FROM questions WHERE content = 'recognized group' AND status = 'Published' AND reviewed_at IS NULL) THEN
    RAISE EXCEPTION 'Phase 2 migration changed legacy exams, sessions or questions incorrectly';
  END IF;
END $$;
INSERT INTO test_group_scores (session_id, group_code, raw_score, max_score, item_count, percent, computed_at)
SELECT session_id, g.code, g.raw, 5, g.items, g.pct, now()
FROM test_sessions, (VALUES ('DATA', 4, 5, 80), ('ETHICS', 2, 5, 40), ('TOOLS', 3, 3, 100)) AS g(code, raw, items, pct)
WHERE user_id = 'legacy-user';
SQL
run_sql legacy < "$validation_dir/levels.sql"
run_sql legacy <<'SQL'
DO $$ BEGIN
  IF (SELECT level FROM test_group_scores WHERE group_code = 'DATA') <> 3
      OR (SELECT level FROM test_group_scores WHERE group_code = 'ETHICS') <> 1
      OR (SELECT level FROM test_group_scores WHERE group_code = 'TOOLS') IS NOT NULL
      OR EXISTS (SELECT 1 FROM test_group_scores WHERE level_mapping_version IS DISTINCT FROM 'tl-v1') THEN
    RAISE EXCEPTION 'tl-v1 release did not backfill unmapped group scores correctly';
  END IF;
END $$;
SQL
for script in baseline catalog taxonomy rubric release review; do
  run_sql duplicate_exam_questions < "$validation_dir/$script.sql"
done
run_sql duplicate_exam_questions <<'SQL'
INSERT INTO questions (type, content, metadata, status) VALUES ('mcq_single', 'repeated', '{}', 'Draft');
INSERT INTO exams (title, description, duration_minutes, pass_score, question_count, status, created_at, updated_at)
VALUES ('Repeated', '', 60, 1, 2, 'active', now(), now());
INSERT INTO exam_questions (exam_id, question_id, order_index, point_value)
SELECT e.exam_id, q.question_id, n, 1 FROM exams e, questions q, generate_series(1, 2) n;
SQL
if docker exec -i "$container_name" psql -v ON_ERROR_STOP=1 -U postgres -d duplicate_exam_questions < "$validation_dir/testbranch.sql" > "$validation_dir/duplicate-guard.log" 2>&1; then
  echo 'Duplicate exam questions were incorrectly accepted.' >&2
  exit 1
fi
grep -q 'An exam lists the same question more than once' "$validation_dir/duplicate-guard.log"
run_sql duplicate_exam_questions <<'SQL'
DO $$ BEGIN
  IF to_regclass('test_group_scores') IS NOT NULL OR (SELECT count(*) FROM exam_questions) <> 2 THEN
    RAISE EXCEPTION 'Duplicate guard failed to preserve the pre-migration database';
  END IF;
END $$;
SQL
for script in baseline catalog taxonomy rubric release review testbranch; do
  run_sql active_level_mapping < "$validation_dir/$script.sql"
done
run_sql active_level_mapping <<'SQL'
UPDATE test_level_mappings SET status = 'Published', published_at = now(), reviewed_by = 'Earlier panel', is_active = true
WHERE version = 'tl-v1-draft';
SQL
if docker exec -i "$container_name" psql -v ON_ERROR_STOP=1 -U postgres -d active_level_mapping < "$validation_dir/levels.sql" > "$validation_dir/levels-guard.log" 2>&1; then
  echo 'An existing active level mapping was incorrectly replaced.' >&2
  exit 1
fi
grep -q 'tl-v1 release conflicts with existing level mappings' "$validation_dir/levels-guard.log"
run_sql active_level_mapping <<'SQL'
DO $$ BEGIN
  IF EXISTS (SELECT 1 FROM test_level_mappings WHERE version = 'tl-v1')
      OR NOT EXISTS (SELECT 1 FROM test_level_mappings WHERE version = 'tl-v1-draft' AND is_active) THEN
    RAISE EXCEPTION 'Level mapping guard modified the existing active rule';
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
echo 'PostgreSQL schema checks passed: fresh install, repeat apply, constraints, legacy backfill, test-branch migration, level rule release and backfill, existing release and reconciliation guards.'
