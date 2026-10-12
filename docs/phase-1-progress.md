# Phase 1 — Competency taxonomy and CV labels

Started 2026-10-12. The software and draft research materials are implemented.
**Phase 1 acceptance still requires expert review of Appendices A/B**; no expert
approval or CV annotation pilot was available in this checkout.

- Add separate, versioned CV framework tables with seven bilingual groups, levels
  0–3, 63 proposed bilingual indicators, and 28 rubric rules. The initial
  `cv-v1-draft` is inactive. Level 0 means insufficient CV evidence, not low ability.
- Add Admin APIs to copy versions, edit draft groups/indicators/rubric, and publish a
  reviewed copy with reviewer and decision recorded. Published content is read only
  through the API. Active versions switch in one transaction. Candidate reads expose
  published versions only.
- Add the admin framework editor at `/admin/framework` for viewing, editing,
  copying and publishing versions. The seeded draft name cannot be published.
- Add `questions.group_code` and `questions.target_level`; new manual and batch
  saves resolve them with the active skill. Migrate recognized legacy metadata to
  canonical codes while retaining original values in `legacyGroup`/`legacyLevel`.
  Ambiguous drafts remain for review. An unclassified published question stops the
  migration with a reconciliation error. Database constraints reject future
  published questions without a valid group and target level.
- Use the same codes in the Angular question editor and generator, generator plans,
  mapping, syllabus and prompt. `src/question-generator/src/taxonomy.py` reads the
  shared draft file for group/level validation; legacy display names remain accepted
  at generator ingress during transition. Item difficulty stays independent.
- Add [annotation guide](annotation-guide.md) and an initial versioned tool/action
  dictionary at `data/dictionary/ai_tools_v1.yaml`. Tool names alone count as weak
  signals. The 63 indicators and rubric examples are proposals, not verified
  excerpts from the unavailable thesis appendices.

The Phase 0 catalogue remains active for existing assessment skill mappings. Its
seven group codes now match the CV draft. Switching the old catalogue itself to a
reviewed version awaits expert approval; existing assessments retain their skill IDs.

## Verification

- All 47 backend tests pass, including PostgreSQL integration and version workflow
  tests. The full schema script passes fresh and repeated application, recognized
  legacy backfill, constraints and reconciliation guards in disposable PostgreSQL 16.
- Frontend lint, nine unit tests and production build pass. All 24 Python tests and
  Ruff checks pass. Compose configuration validates.
- Schema verification uses a temporary memory mount for its disposable database to
  avoid the host Docker disk limit. No existing application database was modified.

## Expert review handoff

Review [the proposed indicator set](../data/frameworks/cv-v1-draft.yaml) against
thesis Appendix A and [the annotation guide](annotation-guide.md) against Appendix B.
Confirm level boundaries, at least one source span for each positive CV label,
and accepted/rejected examples for each group. Record the reviewer and decision in
the admin editor, copy the draft to a release name such as `cv-v1`, revise the copy,
and publish it only after review. Pilot double annotation on 20 deidentified CVs
belongs to Phase 4; low agreement should trigger another rubric version.
