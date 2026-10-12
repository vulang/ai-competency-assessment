import csv
import importlib.util
import sys
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "src"))


def load_module(name):
    spec = importlib.util.spec_from_file_location(name, ROOT / 'src' / f'{name}.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def test_expanded_plan_preserves_group_level_and_independent_difficulty(monkeypatch):
    module = load_module('plan')
    monkeypatch.setattr(module.random, 'choice', lambda choices: choices[0])
    plan = {'mix': [{'group': 'DATA', 'level': 'Advanced', 'type': 'mcq_single', 'difficulty': [1], 'count': 2}]}
    jobs = module.expand_plan(plan)
    assert len(jobs) == 2
    assert len({job['id'] for job in jobs}) == 2
    assert all(job['group'] == 'DATA' and job['level'] == 'Advanced' and job['difficulty'] == 1 for job in jobs)
    assert plan['mix'][0]['count'] == 2


def test_empty_plan_creates_no_jobs():
    assert load_module('plan').expand_plan({'mix': []}) == []


def test_framework_alignment_checks_supported_group_and_evidence():
    module = load_module('align_framework')
    mapping = yaml.safe_load((ROOT / 'data' / 'framework_mapping.yaml').read_text())
    taxonomy = load_module('taxonomy')
    assert set(mapping) == set(taxonomy.GROUPS)
    with (ROOT / 'data' / 'syllabus.csv').open(newline='', encoding='utf-8') as source:
        assert {row['group'] for row in csv.DictReader(source)} == set(taxonomy.GROUPS)
    assert module.align_with_framework({'group': 'AI Ethics', 'stem': 'Nhận diện thiên kiến và bias', 'options': []}, mapping)
    assert not module.align_with_framework({'group': 'unknown', 'stem': 'bias'}, mapping)
    assert not module.align_with_framework({'group': 'AI Ethics', 'stem': 'unrelated text'}, mapping)
