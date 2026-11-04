# AI Competency Assessment

.NET 8 API, Angular 17 UI, and Python services for assessing AI competency. Phase 0 establishes the working development baseline; Phase 1 adds the thesis-aligned competency taxonomy and CV rubric; Phase 2 adds the parallel test branch: reviewed questions, blueprint-built standard forms, seven-group scores and levels, and CTT item analysis. IRT/CAT/BKT are experimental extensions behind `Features:AdaptiveTesting` (off by default).

## Start the development stack

Install Docker with Docker Compose v2, then run from the repository root:

```bash
./scripts/dev-up.sh
```

The script copies `infra/.env.example` to `infra/.env` if needed and builds/starts PostgreSQL, the API, frontend, ML service, question generator, MongoDB, RabbitMQ, MinIO, and Qdrant. MinIO builds a pinned community release from source. The compose project is named `ai-cas-dev`; its volumes are separate from the old default `infra` project.

- UI: http://localhost:4200
- API / Swagger: http://localhost:5000/swagger
- ML service: http://localhost:8001/docs
- Question generator: http://localhost:8000/docs
- PostgreSQL: localhost:5434

Development admin: `admin@example.com` / `Password123!`. Candidates can register at `/candidate/register`. The seed admin credentials and development signing certificates are for this local stack.

Question generation requires a provider key in `infra/.env` and provider/model settings in `src/question-generator/configs/model.yaml`. Startup and automated acceptance checks make no paid LLM calls. CV analysis is a later roadmap phase.

## Test branch workflow

1. Create or generate questions (they start as drafts), review them from `/admin/questions`, then publish them. Only reviewed, published objective questions (`mcq_single`, `mcq_multi`, `true_false`) can be used in test forms; `/admin/questions/coverage` shows gaps per group and level.
2. Build a form at `/admin/tests/new`: set per-group quotas, an optional level mix and a seed, preview it, then save and publish. A **standard** form needs 5–6 items per group and 35–42 in total; candidates start it with “Start standard test”. Published forms are frozen.
3. Approve the score-to-level rule at `/admin/tests/level-mapping`. The seeded `tl-v1-draft` (below 50% Basic, 50–79.99% Intermediate, 80% or more Advanced, at least five items per group) is inactive until an expert approves it; until then candidates see group percentages without levels.
4. Read CTT results at `/admin/tests/{id}/analysis` and export the items, reliability and response-matrix CSV files.

Set `Features__AdaptiveTesting=true` on the API to re-enable the experimental adaptive test and IRT/BKT profile.

Stop the stack while preserving its data:

```bash
docker compose -f infra/docker-compose.yml down
```

Changing the public API/UI ports also requires matching the OIDC authority/redirect settings in `frontend/ai-competency-ui/src/environments/environment.ts` and backend client/CORS configuration.

## Existing databases

The API applies EF migrations at startup. An older database created with `EnsureCreated` or the former hand-written schema needs reconciliation before reuse. Do not apply the initial migration over existing tables. The new compose project provides an isolated fresh baseline; it does not migrate or delete the old project's data.

Read [Phase 0 database transition notes](docs/phase-0-progress.md) before pointing the API at an existing database. Historical fallback user IDs remain in a `legacy:` namespace; custom skill mappings stop the catalogue migration for review. No existing application database was modified during development verification.

## Run services from source

Use .NET 8 or a compatible newer SDK, Node.js 22, and Python 3.12. Start the supporting containers, then:

```bash
dotnet restore ai-competency-assessment.sln
dotnet tool restore
dotnet run --project backend/AiCompetency.Api --urls http://localhost:5000
```

In a second terminal:

```bash
cd frontend/ai-competency-ui
npm ci
npm start
```

`ng serve` proxies `/api` to port 5000. The auth interceptor adds tokens to protected API requests; components do not set their own bearer headers. OIDC authorization/token/logout requests go to the configured authority on port 5000.

To run ML locally:

```bash
python3 -m venv .venv
.venv/bin/pip install -r requirements-dev.txt
cd src/ml_service
../../.venv/bin/uvicorn main:app --host 127.0.0.1 --port 8001
```

If a compose service already publishes a port, stop that service before starting its source equivalent.

## Checks

```bash
dotnet test ai-competency-assessment.sln
cd frontend/ai-competency-ui
npm run lint
npm test
npm run build
cd ../..
.venv/bin/ruff check src/ml_service src/question-generator
.venv/bin/pytest -q
./scripts/verify-schema.sh
./scripts/verify-stack.sh
```

Backend integration tests use Testcontainers PostgreSQL and require Docker. To run only unit tests, add `--filter 'Category!=Integration'` to `dotnet test`. Schema checks and compose acceptance use disposable databases/projects and remove their own containers/volumes afterward. The compose validation script uses random localhost ports and the Compose `!override`/`!reset` features; Compose before 2.24 ignores `!override`, so stop other stacks that publish the default API/UI/ML/PostgreSQL ports when validating with an older release.

`verify-stack.sh` reviews and publishes 35 synthetic questions, publishes a standard form, registers two synthetic candidates with real access tokens, checks admin 403 responses, hidden adaptive endpoints and isolated histories, submits both tests, approves the draft level rule in its disposable database, and checks seven-group levels, CTT computed by the real ML service, and reports. It also verifies service health, frontend SPA routing, and the API proxy. External LLM generation and the full browser SSO redirect flow are outside this smoke test.

The Docker API build defaults to SDK 8 / runtime 8. For a machine with an already cached compatible build SDK, validation accepts an override:

```bash
VALIDATION_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0-noble ./scripts/verify-stack.sh
```

For disposable validation on a machine with limited Docker disk capacity, reuse the images built for this checkout and place validation data in temporary memory mounts:

```bash
VALIDATION_REUSE_IMAGES=1 VALIDATION_TMPFS=1 ./scripts/verify-stack.sh
```

This does not change the persistent volumes in the development compose configuration.

GitHub Actions runs backend/Testcontainers, frontend lint/tests/production build, Python lint/tests for both services, schema verification, and compose acceptance. All four jobs passed in the [Phase 0 acceptance run](https://github.com/vulang/ai-competency-assessment/actions/runs/38152153743), including a fresh SDK 8/runtime 8 build and persistent-volume Compose startup, and again in the [Phase 1 acceptance run](https://github.com/vulang/ai-competency-assessment/actions/runs/41474221369).

## Design and research documents

- [Architecture](docs/architecture.md)
- [Thesis roadmap](docs/roadmap.md)
- [Phase 0 implementation and verification](docs/phase-0-progress.md)
- [Phase 1 completion and verification](docs/phase-1-progress.md)
- [Phase 2 test branch, CTT method and verification](docs/phase-2-progress.md)
- [CV annotation guide](docs/annotation-guide.md)
- [Approved Appendices A/B](docs/appendix-a-b-v1.md)
- [Experimental IRT/CAT/BKT methods](docs/ml-methods.md)
- [Generated PostgreSQL migration script](db/schema.sql)

The `p0-v1` catalogue is a compatibility seed for existing assessment skill mappings. Phase 1 adds structured question group/level fields and active `cv-v1` with 63 bilingual behavior indicators and four-level CV rubric rules. The project owner confirmed expert review and approval on 2026-10-12; the earlier draft remains inactive. Admins can edit copied drafts at `/admin/framework` and publish later versions. Existing published questions with unmapped legacy labels stop migration for reconciliation. `db/schema.sql` is generated with `dotnet ef migrations script --idempotent`; EF migrations are the schema source of truth.
