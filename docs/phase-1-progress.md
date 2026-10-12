# Phase 1 — Competency taxonomy and CV labels

Implemented 2026-10-12. The project owner approved the proposed indicators and CV rubric in conversation on 2026-10-12. This records project approval; no separate expert reviewer identity or annotation pilot result was supplied. The active CV framework is `cv-v1`. The earlier `cv-v1-draft` remains inactive for traceability.

- Add seven bilingual groups, four CV levels (0–3), 63 bilingual behavior indicators, and 28 group-level rubric rules. Level 0 means insufficient CV evidence, not low ability.
- Add Admin APIs and an editor at `/admin/framework` to clone, edit and publish later versions. Candidate reads expose published CV versions only. The Phase 0 `p0-v1` assessment skill catalogue remains active for existing question mappings.
- Add structured `questions.group_code` and `questions.target_level`. Legacy metadata with recognized labels is migrated; ambiguous drafts remain for review. Published questions with unresolved labels stop migration for reconciliation.
- Use canonical group and level codes in the question editor and generator, including the [published framework seed](../data/frameworks/cv-v1.yaml).
- Add the [annotation guide](annotation-guide.md), [approved Appendix A/B packet](appendix-a-b-v1.md), and versioned [AI tool dictionary](../data/dictionary/ai_tools_v1.yaml). The seed and packet are proposals aligned to the supplied thesis v8.1 §1.2 and §2.3; the manuscript lists Appendices A/B as planned but does not provide their full text.

The published version is a separate immutable copy of the seed content. A deployment with an existing `cv-v1`, conflicting seed IDs, or an already active CV version stops at the release migration for reconciliation; it does not replace that deployment's content. The owner approval is recorded in the seed and generated appendix packet. Independent expert credentials were not verified, so any research protocol requiring an external expert sign-off must obtain that separately. Double annotation of 20 deidentified CVs and agreement measurement remain in Phase 4.

## Verification

- All 47 backend tests, nine frontend tests, 24 Python tests, frontend lint/build, Ruff and the appendix freshness check pass.
- The disposable PostgreSQL schema check passes fresh and repeated application, an edited legacy draft, sequence values, constraints, legacy question backfill, and reconciliation guards for an existing active CV release.
