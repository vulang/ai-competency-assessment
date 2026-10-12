# CV annotation guide — v1

Status: project owner approved on 2026-10-12; annotation agreement has not been measured.
Framework: `cv-v1`, in [the published framework seed](../data/frameworks/cv-v1.yaml).
The seed uses JSON syntax, a subset of YAML 1.2, and is embedded directly in the
API assembly. It contains proposed indicators derived from the existing catalogue
and aligned to thesis v8.1 §1.2 and §2.3. The thesis lists Appendices A and B as
planned, without their full indicator set or annotation handbook. No separate expert
reviewer identity has been supplied.

## Labels and evidence

Annotate each of the seven groups independently. Use 0 (`NO_EVIDENCE`), 1 (`BASIC`),
2 (`INTERMEDIATE`), or 3 (`ADVANCED`). Level 0 means insufficient CV evidence;
it does not mean the candidate has no ability. Do not convert a test score into a CV label.

For every label 1–3, record at least one exact span from the preprocessed CV,
its section, start/end offsets, and the matching indicator code. Offsets use
half-open intervals in the stored normalized text; never calculate them on the raw
PDF or a differently normalized copy. Record framework and preprocessing versions.
A job title, tool list, self rating, or certificate name alone is insufficient.
Coursework or a certificate can support Basic when a concrete product and context are
described. A certificate title alone cannot. Intermediate requires independent
application with a technical choice or verification and a clear personal role.
Advanced requires evidence of evaluation, design, leadership, optimization, or
standardization at broader scope; tenure and job title alone do not qualify.

Choose the highest level supported by concrete actions, task context, and the
candidate's contribution. Do not require a metric for every Basic example. A metric
alone does not establish Advanced competence. Do not infer personal contributions
from a team's achievements, or use education prestige or demographic attributes.
Evidence may support multiple groups only when each mapping is justified separately.
Missing evidence and contradictory evidence should be recorded for adjudication.

## Boundary examples

These synthetic examples illustrate decisions; they are not training records.

- “Skills: ChatGPT, Python.” → TOOLS 0: names alone show no action.
- “Used ChatGPT to draft product descriptions and checked each output.” → TOOLS 1:
  concrete use and checking, without evidence of workflow integration.
- “Integrated an AI API into support triage and compared errors with the manual
  workflow.” → TOOLS 2 / APPLICATION 2 if the candidate's contribution is explicit.
- “Designed a monitored triage workflow; compared quality, cost and maintenance
  against manual triage before choosing the deployment.” → APPLICATION 3; verify
  the passage establishes design and evaluation rather than repeating a role title.
- “Developed an organizational AI policy and evaluated whether its controls reduced
  privacy incidents.” → ETHICS 3; use the full action/outcome passage as evidence.
- “Managed an AI team.” → no automatic Advanced label for any group.

The seed includes group-specific synthetic CV examples at levels 1–3. They illustrate
application of the thesis rules and are not real CV passages or pilot-validated examples.
Adjudication may refine accepted/rejected examples and determine whether particular
higher-level indicators require multiple spans.

## Pilot and review

Have two annotators label the same 20 deidentified CVs independently. Keep their
labels/spans separate. Discuss disagreements with an adjudicator, revise ambiguous
indicators and rubric examples, and record review decisions. Measure agreement in
Phase 4; do not claim validated agreement or coverage yet.

The project owner approved Appendices A/B and `cv-v1` is published as the active CV
version. Retain the draft and all indicator codes so existing annotations remain
interpretable. Use the admin editor to copy and review any later rubric revision.
