# Phase 2 — Parallel test branch and CTT

Status: complete on 2026-11-04 on branch `phase-2-test-branch`.

- **Level rule:** the project owner confirmed expert approval of the proposed score-to-level rule, and it is active as `tl-v1`. The expert's name and review date were not supplied.
- **Objective-only forms:** the project owner confirmed that the supervisor agreed to use only objective items in experimental forms and CTT.
- **Verification:** local checks pass, including Compose acceptance. The implementation commit `411adf9` passed all four [hosted CI jobs](https://github.com/vulang/ai-competency-assessment/actions/runs/41755598023); the release of `tl-v1` is verified below.

## What changed

- **Question review.** The status flow is `Draft → Reviewed → Published → Archived`.
  - `POST /api/questions/{id}/review` records the reviewer, time and note. It only accepts classified questions. For objective items, the answer key must resolve to one of the options.
  - New questions cannot be created as published, and publishing requires a review.
  - A change to the prompt, options, key, context, type, group or level clears the review and returns the question to draft.
  - A question that has been delivered (has responses or is on a published form) cannot be edited or deleted; archive it instead.
- **Bank coverage.** `GET /api/questions/coverage` and `/admin/questions/coverage` count questions by group × level × type and status. They highlight groups with fewer usable items than a standard form needs, and link to the generator prefilled for the gap.
- **Blueprints and forms.** `exams.blueprint` stores per-group quotas, an optional level mix, a seed and the pass percentage.
  - Assembly is deterministic. Questions are ranked by SplitMix64(seed, question id), quotas are split across levels by largest remainder, and short levels are filled from the same group with a warning.
  - Only questions that are reviewed, published and objective (`mcq_single`, `mcq_multi`, `true_false`) are eligible.
  - Admins can preview, create, edit the blueprint, reassemble (same or new seed), set question order, publish, clone, archive, and delete unused drafts.
  - Publishing freezes the form and fixes the raw pass mark.
  - A form marked **standard** must have 5–6 items per group and 35–42 in total (thesis §2.12). At most one standard form can be active.
- **Candidate flow.** `start-standard` starts the active standard form; it no longer draws 50 random questions. Phase 0 hidden per-attempt exams are archived by the migration; their sessions are kept. Scoring is shared through `IScoringService` (standard and adaptive).
- **Group scores and levels.** At submission, `test_group_scores` stores raw score, maximum, item count and percent for each group. Only objective items count, and unanswered items score 0.
  - A level (1–3) comes from the active `test_level_mappings` version, and the row records which version was used.
  - Publishing a mapping requires a named approver. It backfills group scores recorded before any mapping existed and never rewrites scores mapped by an earlier version. Published mappings are immutable.
- **Profiles.** `GET /api/competency/test-profile` returns the seven groups from the candidate's latest completed test. The result page and `/candidate/competency-profile` show seven-group percentages and levels, with no overall score. The θ/BKT profile endpoints are retained behind the feature flag.
- **CTT.** `POST /ctt/analyze` in `ml_service` and `GET /api/tests/{id}/item-analysis` compute the report from stored responses each time (method below). `/admin/tests/{id}/analysis` shows reliability, a score histogram, flagged items and the distractor table. It exports the items table (Appendix H), reliability by group, and the pseudonymous 0/1 response matrix as CSV for independent re-analysis.
- **IRT/CAT/BKT.** These sit behind `Features:AdaptiveTesting`, which defaults to `false`:
  - The endpoints return 404.
  - The profile worker does not run.
  - The candidate adaptive button and admin IRT page are hidden.
  - The code and tests are kept for future work.
- **Dashboard and results.** `/api/reports/summary`, `/attempts` and `/attempts/{id}` replace the mock report service. They provide pass rate, average score, score distribution, a group heatmap with level counts, the attempt list, and attempt detail with group breakdown.

## CTT method

Inputs are dichotomous scores (respondents × objective items) from completed sessions of one frozen form, ordered by completion time. Open scenario items are excluded and counted separately.

| Statistic | Definition |
|---|---|
| Difficulty *p* | Proportion correct |
| Corrected item–total *r* | Pearson *r* of the item with the total minus the item; also computed within the item's group |
| Discrimination index *D* | *p* in the top 27% minus *p* in the bottom 27% by total score (stable order for ties) |
| Distractors | Selection rate overall and in the upper/lower 27%; option–rest *r*; omit rate |
| Cronbach's α | k/(k−1)·(1 − Σσ²ᵢ/σ²ₓ) with sample variances, over items with variance; also α if item deleted, and per group |
| McDonald's ω | (Σλ)²/((Σλ)² + Σ(1−λ²)) from a one-factor model fitted to inter-item Pearson (phi) correlations by iterated principal-axis factoring (minres solution); Heywood cases are clipped at 0.995 and reported |

Default flags: *p* < 0.2 (too hard), *p* > 0.9 (too easy), corrected *r* < 0.2 (low; negative if < 0), α rises if the item is deleted, a distractor chosen by < 5% of respondents, or a distractor that correlates positively with the rest score. With fewer than 30 respondents, the page labels results as exploratory.

Tests check:
- α against the covariance form and KR-20.
- ω against exact one-factor correlation matrices, including equality with standardized α for equal loadings.
- Each item statistic against direct numpy computation.

Omega uses Pearson (phi) correlations, not tetrachoric correlations. With dichotomous items, this tends to underestimate loadings. Report it as such, or re-estimate from the exported matrix if the thesis needs tetrachoric ω.

## Decisions taken

- **Open scenario questions are excluded from experimental forms and CTT.** Thesis §2.12 analyses distractors, which apply to objective items. The situational emphasis is met with scenario-based multiple-choice items. Rubric scoring of open answers was not built. The project owner confirmed the supervisor's agreement on 2026-11-04 (roadmap §8.2 item 4).
- **IRT/CAT/BKT stay behind a flag** rather than being removed (roadmap §8.2 item 3).
- **The test profile is the set of group scores of the latest completed session**, not a separate `test_profiles` table.
- **Legacy published questions without a review record stay published** but are not eligible for new forms until reviewed. No review was fabricated in the migration.

## Score-to-level rule

`tl-v1` is the active rule. It was published by migration `PublishTestLevelMappingV1` with the cut points of `tl-v1-draft`, which stays unchanged as the reviewed proposal.

| Level | Rule |
|---|---|
| Basic | below 50% |
| Intermediate | 50% to below 80% |
| Advanced | 80% or more |
| No level | fewer than five objective items in the group |

With five items, these cut points place 0–2 correct at Basic, 3 at Intermediate and 4–5 at Advanced. With six items, they place 0–2, 3–4 and 5–6.

- **Source of approval:** the record notes the project owner's confirmation of expert approval on 2026-11-04. A research protocol that needs a named expert or a signed review still needs those details; they can be added to the documentation without changing the rule.
- **Earlier scores:** the migration gives `tl-v1` levels to group scores recorded without a rule, exactly as publishing through the API does.
- **Existing rules:** if a deployment already has an active rule or a conflicting `tl-v1` row, the migration stops for reconciliation instead of replacing it.
- **Changing the rule:** later changes need a new version at `/admin/tests/level-mapping`. Each group score keeps the version that produced its level.

The Δ threshold for the "consistent" state in the CV × test integration layer remains open for Phase 9 (roadmap §8.2 item 5).

## Acceptance

- Build a standard form of 35–42 items with the required quotas: blueprint validation, publish checks, unit tests, the PostgreSQL integration test and Compose acceptance all cover it.
- Candidates receive a seven-group profile with levels from the approved `tl-v1` rule after submission.
- The CTT report is recomputed from stored responses on every request, and the response matrix can be exported.
- The Appendix H table can be filled from the items and reliability CSV exports.

## Verification

- Backend: 72 unit tests and the PostgreSQL integration test pass (Release configuration). The integration test covers review, standard form publication, concurrent submission, group scores, mapping backfill, reports, ML unavailability (503) and draft reordering under the new unique index.
- The schema check passes:
  - fresh and repeated application;
  - new constraints;
  - the draft and approved rule seeds, the single active rule, and the sequence;
  - the `tl-v1` backfill of unmapped group scores, and the guard that stops the release when another rule is already active;
  - archiving of legacy hidden exams without touching sessions or unreviewed published questions;
  - the guard against duplicate exam questions.
- Python: 32 tests (29 in `ml_service`), Ruff, and the appendix freshness check pass.
- Frontend: lint, 15 unit tests and the production build pass. A one-off strict-template build found no errors in the new templates.
- Compose acceptance (`VALIDATION_REUSE_IMAGES=1 VALIDATION_TMPFS=1 ./scripts/verify-stack.sh`, with freshly built API, ML and UI images) passes:
  - reviewed questions;
  - publication of a 35-item standard form;
  - two isolated candidates;
  - seven-group scores with `tl-v1` levels;
  - CTT computed by the real ML service;
  - reports.

  The validation override now clears host ports with `!reset []`. Compose releases before 2.24 ignore `!override`, so another local stack holding port 9001 had blocked MinIO.
