#!/usr/bin/env python3
"""Render Appendix A/B packets from versioned framework seeds."""
import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LEVELS = {1: 'Basic', 2: 'Intermediate', 3: 'Advanced'}


def cell(value: str) -> str:
    return value.replace('|', '\\|').replace('\n', ' ')


def render(version: str) -> str:
    source = ROOT / f'data/frameworks/{version}.yaml'
    data = json.loads(source.read_text(encoding='utf-8'))
    draft = version.endswith('-draft')
    lines = [
        '# Proposed Appendices A and B — expert review copy' if draft else '# Appendices A and B — CV framework v1', '',
        f'Framework version: `{data["version"]}` · Status: {data["status"]}', '',
        'The supplied thesis v8.1 names Appendices A/B under “PHỤ LỤC DỰ KIẾN” but does not contain their full text.',
        ('These indicators are proposals aligned to §1.2; rubric boundaries follow §2.3. They require expert review.'
         if draft else 'These indicators align to §1.2 and the rubric boundaries follow §2.3. Expert review and approval were confirmed by the project owner on 2026-10-12.'), '',
        'For every positive CV label, the annotator must cite at least one exact span. Level 0 means insufficient CV evidence.', '',
        '## Appendix A — behavior indicators', ''
    ]
    for group in sorted(data['groups'], key=lambda g: g['order']):
        lines += [f'### {group["order"]}. {group["nameVi"]} / {group["nameEn"]} (`{group["code"]}`)', '',
                  '| Level | Indicator code | Vietnamese | English |', '| --- | --- | --- | --- |']
        for indicator in sorted(group['indicators'], key=lambda i: (i['level'], i['code'])):
            lines.append(f'| {LEVELS[indicator["level"]]} | `{indicator["code"]}` | {cell(indicator["textVi"])} | {cell(indicator["textEn"])} |')
        lines.append('')
    lines += ['## Appendix B — CV annotation rubric', '',
              'The examples below are synthetic. A title, tenure, self rating, or tool list alone is not enough to assign a positive level.', '']
    for group in sorted(data['groups'], key=lambda g: g['order']):
        lines += [f'### {group["nameVi"]} / {group["nameEn"]}', '',
                  '| Level | Accepted evidence | Insufficient evidence | Synthetic example |', '| --- | --- | --- | --- |']
        for rule in sorted(group['rubricRules'], key=lambda r: r['level']):
            level = '0 — Không đủ bằng chứng' if rule['level'] == 0 else LEVELS[rule['level']]
            lines.append(f'| {level} | {cell(rule["acceptedEvidence"])} | {cell(rule["insufficientEvidence"])} | {cell(rule["example"])} |')
        lines.append('')
    if draft:
        lines += ['## Expert decision record', '',
                  '- Reviewer and date:', '- Reviewed thesis sections / external framework:',
                  '- Indicator changes by code:', '- Boundary examples accepted or rejected:',
                  '- Decision: approve, revise, or reject:', '',
                  'Publish a copied version such as `cv-v1` only after recording this decision in the admin editor.', '']
    else:
        lines += ['## Approval record', '',
                  '- Decision: expert review and approval confirmed by the project owner in conversation on 2026-10-12.',
                  '- Reviewer name and review date: not supplied.',
                  '- Basis: proposed indicators and rubric reviewed against the supplied thesis v8.1 §1.2 and §2.3.',
                  '- Validation of annotation agreement remains a Phase 4 pilot task.', '']
    return '\n'.join(lines)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true', help='Fail when a packet differs from its seed')
    args = parser.parse_args()
    for version, output_name in [('cv-v1-draft', 'appendix-a-b-draft.md'), ('cv-v1', 'appendix-a-b-v1.md')]:
        output = ROOT / 'docs' / output_name
        content = render(version)
        if args.check:
            if not output.exists() or output.read_text(encoding='utf-8') != content:
                parser.error(f'{output} is out of date; regenerate it with this script')
        else:
            output.write_text(content, encoding='utf-8')
