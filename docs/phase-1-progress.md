# Phase 1 — Competency taxonomy and CV labels

Status: complete on 2026-10-12. Local checks and all four [hosted GitHub Actions jobs](https://github.com/vulang/ai-competency-assessment/actions/runs/41474221369) pass on branch `phase-1-framework`. The project owner confirmed expert review and approval of the proposed indicators and CV rubric in conversation on 2026-10-12. The expert's name and review date were not supplied. The active CV framework is `cv-v1`; the earlier `cv-v1-draft` remains inactive for traceability.

- Add seven bilingual groups, four CV levels (0–3), 63 bilingual behavior indicators, and 28 group-level rubric rules. Level 0 means insufficient CV evidence, not low ability.
- Add Admin APIs and an editor at `/admin/framework` to clone, edit and publish later versions. Candidate reads expose published CV versions only. The Phase 0 `p0-v1` assessment skill catalogue remains active for existing question mappings.
- Add structured `questions.group_code` and `questions.target_level`. Legacy metadata with recognized labels is migrated; ambiguous drafts remain for review. Published questions with unresolved labels stop migration for reconciliation.
- Use canonical group and level codes in the question editor and generator, including the [published framework seed](../data/frameworks/cv-v1.yaml).
- Add the [annotation guide](annotation-guide.md), [approved Appendix A/B packet](appendix-a-b-v1.md), and versioned [AI tool dictionary](../data/dictionary/ai_tools_v1.yaml). The reviewed content aligns to the supplied thesis v8.1 §1.2 and §2.3; the manuscript lists Appendices A/B as planned but does not provide their full text.

The published version is a separate immutable copy of the seed content. A deployment with an existing `cv-v1`, conflicting seed IDs, or an already active CV version stops at the release migration for reconciliation; it does not replace that deployment's content. The owner's confirmation of expert review is recorded in the seed and generated appendix packet. A research protocol requiring a named expert or signed review record still needs that detail. Double annotation of 20 deidentified CVs and agreement measurement remain in Phase 4.

## Acceptance

- Published questions require a valid `group_code` and `target_level`; the migration stops for reconciliation when an older published question cannot be mapped.
- Publishing a revised CV framework creates a new version and preserves earlier indicators and rubric rules. The workflow tests exercise the version switch; the migration guard protects an already active release.
- The project owner confirmed expert approval of [Appendices A/B v1](appendix-a-b-v1.md). A named or signed expert record can be added when available, without changing the published content.

## Verification

- All 47 backend tests, nine frontend tests, 24 Python tests, frontend lint/build, Ruff and the appendix freshness check pass.
- The disposable PostgreSQL schema check passes fresh and repeated application, an edited legacy draft, sequence values, constraints, legacy question backfill, and reconciliation guards for an existing active CV release.
- Hosted CI passed all four jobs for commit `2b5f898`: backend tests and PostgreSQL schema checks, frontend lint/tests/build, Python lint/tests with the appendix freshness check, and Compose acceptance.
