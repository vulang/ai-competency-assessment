import importlib.util
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[1]


def load_module(name):
    spec = importlib.util.spec_from_file_location(name, ROOT / 'src' / f'{name}.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def test_expanded_plan_preserves_group_level_and_independent_difficulty(monkeypatch):
    module = load_module('plan')
    monkeypatch.setattr(module.random, 'choice', lambda choices: choices[0])
    plan = {'mix': [{'group': 'Data', 'level': 'Advanced', 'type': 'mcq_single', 'difficulty': [1], 'count': 2}]}
    jobs = module.expand_plan(plan)
    assert len(jobs) == 2
    assert len({job['id'] for job in jobs}) == 2
    assert all(job['level'] == 'Advanced' and job['difficulty'] == 1 for job in jobs)
    assert plan['mix'][0]['count'] == 2


def test_empty_plan_creates_no_jobs():
    assert load_module('plan').expand_plan({'mix': []}) == []


def test_framework_alignment_checks_supported_group_and_evidence():
    module = load_module('align_framework')
    mapping = yaml.safe_load((ROOT / 'data' / 'framework_mapping.yaml').read_text())
    assert len(mapping) == 7
    assert module.align_with_framework({'group': 'AI Ethics', 'stem': 'Nhận diện thiên kiến và bias', 'options': []}, mapping)
    assert not module.align_with_framework({'group': 'unknown', 'stem': 'bias'}, mapping)
    assert not module.align_with_framework({'group': 'AI Ethics', 'stem': 'unrelated text'}, mapping)
