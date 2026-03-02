Build an Admin UI for an AI Competency Assessment System using Angular (latest stable).

This is an INTERNAL ADMIN application (not public-facing).
Target audience: system admins, educators, and assessment designers.

========================
TECH STACK & RULES
========================
- Angular (latest)
- Standalone components ONLY
- Angular Router
- Reactive Forms with strict typing
- Angular Signals for state (NgRx only if justified)
- Angular Bootstrap for UI
- TypeScript strict mode ON
- Clean architecture and folder structure
- Unit tests (Jasmine/Karma or Jest)

If backend APIs are not provided:
- Implement a mock REST API layer (in-memory)
- Support pagination, filtering, and sorting
- Structure code so real APIs can replace mocks later

========================
CORE FEATURES
========================

A. MANAGE TEST QUESTIONS

1. Question List
- Table view with:
  - filters: competency group, competency level, difficulty, question type, status, tags
  - sorting
  - pagination
- Bulk actions:
  - publish / unpublish
  - archive
  - soft delete
  - tagging

2. Create / Edit Question
Supported types:
- Multiple Choice
- Multi-select
- True / False
- Short Answer
- Scenario-based

Fields:
- title
- prompt
- options[] (if applicable)
- correct answer(s)
- explanation / rationale
- difficulty
- estimated time
- tags[]
- competencyGroupId
- competencyLevelId
- status

Competency Levels:
- Level 1: Foundation
- Level 2: Apply
- Level 3: Create

Forms MUST:
- use strong validation
- provide clear admin UX
- prevent invalid saves

3. Generate Test Questions (AI-assisted)
- Admin inputs:
  - competency group
  - competency level
  - number of questions
  - question types
  - difficulty distribution
  - language (VN / EN)
  - optional topic hints
- Generate draft questions
- Allow editing before saving
- Persist AI metadata:
  - generation params
  - model/provider
  - createdAt
  - createdBy

========================
B. MANAGE TESTS
========================

1. Generate Tests
- Test blueprint:
  - name
  - description
  - duration
  - passing criteria
- Question selection rules:
  - competency group & level
  - difficulty
  - total questions
  - randomization
  - per-competency quotas
- Preview test before saving

2. View Tests
- Test list
- Detail view
- Clone test
- Archive test
- Coverage summary by competency group & level

3. Manage Rubrics
- CRUD rubrics
- Rubric structure:
  - criteria mapped to competency group + level
  - scoring scale (e.g. 0–4)
  - descriptors and examples
- Attach rubrics to tests or exams

========================
C. MANAGE EXAMS
========================

1. View Exam Results
- List exam attempts:
  - candidate
  - test
  - date
  - score
  - pass/fail
  - status
- Detail view:
  - question responses
  - rubric evaluations
  - per-competency breakdown

2. Reports & Analytics
- Dashboard:
  - overall pass rate
  - average score
  - score distribution
  - competency heatmaps
- Item analysis:
  - question difficulty
  - discrimination (optional)
- CSV export support

========================
ROUTING
========================
/questions
/questions/new
/questions/:id/edit
/questions/generate
/tests
/tests/generate
/tests/:id
/rubrics
/rubrics/new
/rubrics/:id/edit
/exams/results
/exams/results/:id
/reports

========================
DATA MODELS (FRONTEND)
========================
Define strongly-typed interfaces for:
- CompetencyGroup
- CompetencyLevel
- Question
- Rubric
- Test
- ExamAttempt
- ReportViewModel

========================
QUALITY REQUIREMENTS
========================
- Clear folder structure:
  core/
  shared/
  features/
  api/
  models/
- Unit tests for:
  - one list view (filter + pagination)
  - one form (validation)
  - one API service
- README.md must explain:
  - project setup
  - architecture decisions
  - API/mocking strategy
  - AI generation flow

========================
EXECUTION ORDER
========================
1. App scaffold + layout + routing
2. Question CRUD
3. AI question generation
4. Test generation
5. Rubrics
6. Exam results & reports
7. Tests, polish, documentation

========================
OUTPUT EXPECTATION
========================
- Full runnable Angular project
- No placeholders for core logic
- Code-first, minimal narration
- Clear separation between UI, state, and API layers
