#!/usr/bin/env python3
"""Render the proposed Appendix A/B reviewer packet from the versioned seed."""
import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'data/frameworks/cv-v1-draft.yaml'
OUTPUT = ROOT / 'docs/appendix-a-b-draft.md'
LEVELS = {1: 'Basic', 2: 'Intermediate', 3: 'Advanced'}


def cell(value: str) -> str:
    return value.replace('|', '\\|').replace('\n', ' ')


def render() -> str:
    data = json.loads(SOURCE.read_text(encoding='utf-8'))
    lines = [
        '# Proposed Appendices A and B — expert review copy', '',
        f'Framework version: `{data["version"]}` · Status: {data["status"]}', '',
        'The supplied thesis v8.1 names Appendices A/B under “PHỤ LỤC DỰ KIẾN” but does not contain their full text.',
        'These indicators are proposals aligned to §1.2; rubric boundaries follow §2.3. They require expert review.', '',
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
    lines += ['## Expert decision record', '',
              '- Reviewer and date:', '- Reviewed thesis sections / external framework:',
              '- Indicator changes by code:', '- Boundary examples accepted or rejected:',
              '- Decision: approve, revise, or reject:', '',
              'Publish a copied version such as `cv-v1` only after recording this decision in the admin editor.', '']
    return '\n'.join(lines)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true', help='Fail when the reviewer packet differs from the seed')
    args = parser.parse_args()
    content = render()
    if args.check:
        if not OUTPUT.exists() or OUTPUT.read_text(encoding='utf-8') != content:
            parser.error(f'{OUTPUT} is out of date; regenerate it with this script')
    else:
        OUTPUT.write_text(content, encoding='utf-8')
