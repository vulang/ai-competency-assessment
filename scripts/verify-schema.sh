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

docker run --rm --detach --network none --name "$container_name" -e POSTGRES_PASSWORD=schema-validation postgres:16 > /dev/null
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
CREATE DATABASE custom_mappings;
SQL
run_sql legacy < "$validation_dir/baseline.sql"
run_sql legacy <<'SQL'
INSERT INTO questions (type, content, metadata, status, difficulty_level)
VALUES ('mcq_single', 'recognized group', '{"group":"AITools","level":"Advanced"}', 'Published', 10),
       ('mcq_single', 'unknown group', '{"group":"ambiguous old group"}', 'Draft', 2);
SQL
run_sql legacy < "$validation_dir/catalog.sql"
run_sql legacy <<'SQL'
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM questions WHERE content = 'recognized group' AND skill_id = 6 AND difficulty_level IS NULL
    AND metadata->>'legacyDifficultyLevel' = '10' AND metadata->>'level' = 'Advanced') THEN
    RAISE EXCEPTION 'Legacy backfill or difficulty preservation failed';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM questions WHERE content = 'unknown group' AND skill_id IS NULL AND difficulty_level = 2) THEN
    RAISE EXCEPTION 'Ambiguous group was modified';
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
rg -q 'Existing skill mappings require reconciliation' "$validation_dir/guard.log"
run_sql custom_mappings <<'SQL'
DO $$ BEGIN
  IF (SELECT skill_id FROM questions WHERE content = 'custom mapping') <> 99 OR to_regclass('frameworks') IS NOT NULL THEN
    RAISE EXCEPTION 'Guard failed to preserve the pre-migration database';
  END IF;
END $$;
SQL
echo 'PostgreSQL schema checks passed: fresh install, repeat apply, constraints, legacy upgrade, custom mapping guard.'
