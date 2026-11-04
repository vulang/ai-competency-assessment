import sys
from pathlib import Path

import numpy as np
import pytest
from fastapi.testclient import TestClient

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from ctt.analysis import (ItemSpec, analyze, cronbach_alpha, omega_total, one_factor_loadings,
                          upper_lower)
from main import app

# Ten respondents, five items of increasing difficulty with a clear common factor.
SCORES = np.array([
    [1, 1, 1, 1, 1],
    [1, 1, 1, 1, 0],
    [1, 1, 1, 0, 1],
    [1, 1, 1, 0, 0],
    [1, 1, 0, 1, 0],
    [1, 1, 0, 0, 0],
    [1, 0, 1, 0, 0],
    [1, 0, 0, 0, 0],
    [1, 1, 0, 0, 0],
    [1, 0, 0, 0, 0],
], dtype=float)


def test_alpha_matches_covariance_form_and_kr20():
    k = SCORES.shape[1]
    cov = np.cov(SCORES, rowvar=False)
    covariance_form = k / (k - 1) * (1 - np.trace(cov) / cov.sum())
    p = SCORES.mean(axis=0)
    kr20 = k / (k - 1) * (1 - np.sum(p * (1 - p)) / np.var(SCORES.sum(axis=1)))
    assert cronbach_alpha(SCORES) == pytest.approx(covariance_form)
    assert cronbach_alpha(SCORES) == pytest.approx(kr20)


def test_alpha_is_undefined_without_total_variance():
    assert cronbach_alpha(np.ones((4, 3))) is None
    assert cronbach_alpha(np.array([[1.0, 0.0]])) is None


def test_one_factor_recovers_exact_loadings_and_omega():
    loadings = np.array([0.8, 0.7, 0.6, 0.5])
    corr = np.outer(loadings, loadings)
    np.fill_diagonal(corr, 1.0)
    fitted, heywood = one_factor_loadings(corr)
    assert not heywood
    assert fitted == pytest.approx(loadings, abs=1e-4)
    expected = loadings.sum() ** 2 / (loadings.sum() ** 2 + np.sum(1 - loadings ** 2))
    assert omega_total(fitted) == pytest.approx(expected, abs=1e-4)


def test_omega_equals_standardized_alpha_for_equal_loadings():
    k, loading = 5, 0.6
    corr = np.full((k, k), loading ** 2)
    np.fill_diagonal(corr, 1.0)
    fitted, _ = one_factor_loadings(corr)
    mean_r = loading ** 2
    standardized_alpha = k * mean_r / (1 + (k - 1) * mean_r)
    assert omega_total(fitted) == pytest.approx(standardized_alpha, abs=1e-4)


def test_upper_lower_groups_use_27_percent():
    upper, lower = upper_lower(np.arange(10.0))
    assert sorted(upper.tolist()) == [7, 8, 9]
    assert sorted(lower.tolist()) == [0, 1, 2]


def test_item_statistics_flags_and_distractors():
    items = [ItemSpec(f"q{i}", "DATA" if i < 3 else "ETHICS", 3, (0,)) for i in range(5)]
    # Correct answers pick option 0. Wrong answers on q4 come from low scorers choosing option 1;
    # option 2 is never chosen.
    selections = [[[0] if SCORES[r, i] else [1] for i in range(5)] for r in range(10)]
    report = analyze(items, SCORES, selections)
    by_id = {item["item_id"]: item for item in report["items"]}

    assert report["n_respondents"] == 10 and report["n_items"] == 5
    assert by_id["q0"]["p"] == 1.0
    assert {"zero_variance", "too_easy"} <= set(by_id["q0"]["flags"])
    assert by_id["q0"]["item_rest_r"] is None
    assert by_id["q4"]["p"] == pytest.approx(0.2)
    rest = SCORES.sum(axis=1) - SCORES[:, 2]
    assert by_id["q2"]["item_rest_r"] == pytest.approx(np.corrcoef(SCORES[:, 2], rest)[0, 1])
    group_rest = SCORES[:, :3].sum(axis=1) - SCORES[:, 2]
    assert by_id["q2"]["item_rest_r_group"] == pytest.approx(np.corrcoef(SCORES[:, 2], group_rest)[0, 1])

    distractors = {d["option"]: d for d in by_id["q3"]["distractors"]}
    assert distractors[0]["is_key"] and distractors[0]["proportion"] == pytest.approx(0.3)
    assert "nonfunctional" in distractors[2]["flags"]
    assert "nonfunctional_distractor" in by_id["q3"]["flags"]
    assert by_id["q3"]["omit_rate"] == 0

    # q0 has no variance, so overall reliability uses the remaining four items.
    assert report["reliability"]["excluded_items"] == ["q0"]
    assert report["reliability"]["alpha"] == pytest.approx(cronbach_alpha(SCORES[:, 1:]))
    assert report["reliability"]["omega"] is not None
    groups = {g["group"]: g for g in report["groups"]}
    assert groups["DATA"]["n_items"] == 3 and groups["ETHICS"]["n_items"] == 2
    assert groups["ETHICS"]["omega"] is None
    assert sum(b["count"] for b in report["total"]["histogram"]) == 10


def test_empty_form_reports_without_statistics():
    report = analyze([ItemSpec("q1")], np.zeros((0, 1)), None)
    assert report["n_respondents"] == 0
    assert report["reliability"]["alpha"] is None
    assert report["items"][0]["p"] is None


def test_ctt_http_contract():
    items = [{"item_id": f"q{i}", "group": "DATA", "option_count": 2, "key": [0]} for i in range(5)]
    respondents = [{"respondent_id": f"r{r}", "scores": SCORES[r].tolist(),
                    "selections": [[0] if s else [1] for s in SCORES[r]]} for r in range(10)]
    with TestClient(app) as client:
        response = client.post("/ctt/analyze", json={"items": items, "respondents": respondents})
        assert response.status_code == 200
        body = response.json()
        assert body["reliability"]["alpha"] == pytest.approx(cronbach_alpha(SCORES[:, 1:]))
        assert len(body["items"][1]["distractors"]) == 2
        bad = client.post("/ctt/analyze", json={"items": items, "respondents": [
            {"respondent_id": "x", "scores": [1, 0]}]})
        assert bad.status_code == 422
        bad_key = client.post("/ctt/analyze", json={"items": [{"item_id": "q", "option_count": 2, "key": [3]}],
                                                    "respondents": []})
        assert bad_key.status_code == 422
