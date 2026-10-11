# Phase 0 — Foundation stabilization

Started 2026-10-11 from the thesis roadmap. Phase 0 implementation is **complete and locally verified**. The first hosted GitHub Actions run awaits pushing the changes. Existing database reconciliation remains an operator deployment step.

This first milestone fixes assessment identity, scoring, and request lifetime defects:

- Assessment sessions, mastery states, and competency profiles use the Identity subject string throughout .NET, Python, and Angular. There is no fallback to candidate `1`.
- Standard and adaptive tests share objective answer scoring. Duplicate answers are rejected; CAT loads previous responses before estimating theta and excludes manually graded question types. Its experimental pass threshold is theta >= 0 rather than the unreachable 6; this is not a validated competency cutoff.
- Standard-test profile processing uses a channel and a hosted worker with a separate DI scope. Only a persisted session ID enters the queue. Errors are logged. This queue is best effort and loses pending jobs on process restart; durable inference jobs remain a later milestone.
- Session IRT estimates are restricted to their owner. Admin question, test, generation, and competency APIs require the Admin role. Admin/Annotator/Candidate roles are seeded and the existing development admin receives Admin.
- Fake in-memory business APIs and the random difficulty/publishing endpoint are removed.
- Startup uses EF migrations. A design-time context factory lets tooling inspect the model without database seeding. Competency and test services use the configured API origin and `/api` prefix.

The second milestone adds candidate onboarding and frontend access control:

- Anonymous `POST /api/account/register` accepts email/password and assigns Candidate only. Passwords require 8–128 characters with uppercase, lowercase, number, and symbol. Duplicate email checks are case insensitive. User creation and role assignment share a transaction, so a failed assignment leaves no partial account.
- `/candidate/register` provides a typed reactive form with matching-password validation, server errors, and a link to sign in. Both login pages link to candidate signup.
- Admin routes require Admin. Candidate routes allow Candidate and Admin (the development admin can still test assessments). Guards run for both parent and child navigation; unauthorized accounts receive an access-denied page.
- SSO authorization-code exchange preserves the granted scopes; role claims reach both access and identity tokens. Both login methods choose the correct home page. The frontend retains all role claims and rejects expired sessions.
- The auth interceptor attaches tokens only to protected API requests on the configured origin. Registration, token exchange, and external URLs receive no bearer token. Protected API 401 responses clear local/OIDC state; login errors and 403 responses do not log the user out.

The third milestone connects questions and the experimental ML profile to a managed catalogue:

- EF now owns `frameworks`, `domains`, `skills`, and `criteria`, with foreign keys, unique catalogue entries, one active framework, positive weights, and difficulty/level constraints. The `p0-v1` compatibility seed contains seven domains, one general skill per domain, and 21 level descriptions copied from `src/question-generator/data/framework_mapping.yaml` into an embedded JSON resource. This is a coarse group-level skill mapping; reviewed behavioral indicators and the four-level CV rubric remain Phase 1 work.
- Authenticated `GET /api/framework/active` serves the catalogue. The question editor loads its group/skill options from this API, retains tags and difficulty on edit, and displays save failures.
- Manual and batch question saves resolve a valid active-framework skill. Explicit skill IDs must match the requested group. Invalid batches save no questions. New metadata uses the existing seven display names and Basic/Intermediate/Advanced; recognized legacy aliases are accepted. Ambiguous old group names are rejected for new saves.
- Item difficulty is independent of competency level. Generated values are preserved, absent values stay null, and out-of-range input returns 400. Manually edited answer arrays are stored as arrays so objective scoring can read them.
- Profile processing uses EF skill/domain queries; it no longer queries an external schema or fabricates an Unknown domain when metadata is unavailable. Tests exercise question creation, mastery updates, and persisted domain scores using a controlled ML HTTP stub.
- `db/schema.sql` is now generated from the complete EF migration chain rather than maintained separately.

The final milestone finishes the development baseline:

- All frontend assessment requests use the shared interceptor; `/api` uses the Angular development proxy or nginx compose proxy. The unused `ng2-charts` wrapper is removed and a clean `npm ci` succeeds. Production builds no longer fetch Google Fonts during compilation.
- API, frontend, and ML Dockerfiles, `infra/.env.example`, and `scripts/dev-up.sh` provide one-command development startup. The compose project has a separate `ai-cas-dev` identity and persistent volumes; validation uses isolated project names and random localhost ports.
- GitHub Actions checks .NET/Testcontainers, Angular ESLint/type checks/tests/build, Python Ruff/pytest for ML and generator helpers, schema verification, and full-stack acceptance. The EF tool is pinned in the repository manifest.
- Unused admin/candidate screens and the obsolete FLAN CLI/package are removed. The ML methods document explicitly identifies IRT/CAT/BKT as experimental extensions; the admin IRT analysis screen has a route with that label.
- Objective scoring resolves generator letter keys and displayed option text against the declared options. Candidate question delivery strips answer keys and hidden explanations. Results require completion. Standard-test passing thresholds round upward, including a one-question test. EF status concurrency checks allow only one competing submission to save responses and enqueue ML work.
- OIDC logout is configured, and the frontend forwards the stored ID token hint for password-flow signout. Login return URLs use local redirects.

## Database transition

The `UseIdentitySubjectIds` migration converts the four assessment `user_id` columns to text. Existing integer IDs become `legacy:<id>`. Previous fallback IDs contain mixed candidate data, so they cannot safely be assigned to any Identity account. Rows are preserved, but new candidate history will not include them. No existing database was modified as part of this milestone.

Databases created by `EnsureCreated` may lack `__EFMigrationsHistory`. Before starting the updated API against one, back it up and reconcile its schema with the existing migration chain. For disposable development data, use a fresh database name. Do not simply run the initial migrations over existing tables or mark migrations applied without checking their schema. Startup automatically migrates databases with valid migration history.

Rollback is guarded: it restores integer IDs only when all assessment rows still belong to the legacy namespace, and fails if any Identity-owned row exists. API and ML service must be deployed together because the profile contract now requires a string subject. Sign in again to receive the new role claims.

The `AddCompetencyCatalog` migration backfills null question skill IDs only for recognized group names. Ambiguous groups remain null and need admin review. Existing non-null skill mappings or mastery rows stop this migration for reconciliation, rather than being assigned to the new seed IDs. If the old hand-written SQL already created the four catalogue tables, reconcile that database with the migration before running startup; it does not automatically merge the schemas.

Legacy difficulty values outside 1–5 are retained in `metadata.legacyDifficultyLevel` and the scoring column becomes null. The catalogue migration refuses rollback while questions or mastery data reference it. Your existing application database was not modified; migration verification used a disposable PostgreSQL container.

## Validation

- All 42 backend xUnit tests pass. Regression tests cover independent candidate history, cross-candidate session and IRT access, admin HTTP authorization (401/403/200), actual answer scoring, duplicate submissions, full CAT response history, and worker scope lifetime. Assessment controller tests use EF InMemory with JSON converters. Registration HTTP tests use SQLite transactions and verify validation, duplicate emails, Candidate-only signup, role-assignment rollback, and both token grant paths. A PostgreSQL Testcontainers integration test now exercises real token issuance, candidate 403 responses, isolated sessions/history, answer-key stripping, rounded scoring thresholds, and competing submissions. The full browser SSO redirect flow remains outside automated acceptance. Catalogue tests also verify batch atomicity, independent difficulty, real domain metadata, and candidate mastery isolation.
- PostgreSQL 16 schema verification passed: fresh install, repeated idempotent application, foreign-key/difficulty constraints, recognized and ambiguous legacy groups, original difficulty preservation, and custom-mapping reconciliation guard. Run `./scripts/verify-schema.sh` with Docker and EF tooling available.
- All 24 Python tests pass (21 ML tests and 3 generator helper tests). They verify the existing IRT/BKT methods and string subject preservation in generated profiles.
- All 9 frontend unit tests pass with `npm test`, covering the registration form/service, role guards, expiry, auth interceptor, and catalogue-driven question editor. Tests run against Angular forms and HTTP testing APIs through Node; template compilation is checked by the build.
- Frontend ESLint/type checks, a clean `npm ci`, and the default production `npm run build` pass. Font inlining is disabled to keep build-time networking unnecessary.
- All nine compose services started successfully in isolated validation. The smoke test verified service health, frontend SPA routing and API proxy, two real candidate token/account flows, distinct histories/results, and seven-domain profiles from the running ML service. No external LLM generation request was made.
- Docker’s virtual disk reached capacity during image builds. Only task-owned cache records were removed. Final local acceptance used the built SDK-10-compatible/.NET-8-runtime images and temporary memory mounts for disposable data. The development compose file retains persistent volumes and needs sufficient Docker disk space. Existing images/volumes belonging to other projects were preserved.

Run:

```bash
dotnet test ai-competency-assessment.sln
.venv/bin/pytest -q
cd frontend/ai-competency-ui
npm run lint
npm test
npm run build
```

## Deployment follow-up and Phase 1

There is no remaining Phase 0 implementation item. Before using the persistent development stack on this machine, provide additional free Docker disk space. The hosted CI workflow has not run yet; all listed local equivalents passed. Do not reuse an older application database without the reconciliation described above.

Next is Phase 1: reviewed, versioned competency groups/behavior indicators, the four-level CV label scheme, and the annotation rubric. The `p0-v1` general-skill catalogue is an operational compatibility baseline, not a reviewed CV taxonomy.
