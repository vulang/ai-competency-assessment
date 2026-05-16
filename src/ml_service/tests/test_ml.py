"""
Unit tests for IRT 3PL estimator and BKT.
Run with: python -m pytest tests/ -v
"""
import math
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from models import IRTParams, ItemResponse, BKTParams
from irt.estimator import (
    p3pl,
    fisher_information,
    estimate_theta_mle,
    estimate_theta_eap,
    select_next_item,
)
from kt.bkt import update_mastery, estimate_mastery_sequence


# ---------------------------------------------------------------------------
# IRT Tests
# ---------------------------------------------------------------------------

def test_p3pl_at_b_equals_theta():
    """P(θ=b) should equal c + (1-c)/2."""
    a, b, c = 1.0, 0.5, 0.25
    result = p3pl(b, a, b, c)
    expected = c + (1 - c) / 2
    assert abs(result - expected) < 1e-6


def test_p3pl_high_theta():
    """High theta → probability approaches 1."""
    assert p3pl(10.0, 1.0, 0.0, 0.25) > 0.99


def test_p3pl_low_theta():
    """Very low theta → probability approaches c (guessing floor)."""
    result = p3pl(-10.0, 1.0, 0.0, 0.25)
    assert abs(result - 0.25) < 0.01


def test_fisher_information_positive():
    """Fisher information should always be non-negative."""
    assert fisher_information(0.0, 1.0, 0.0, 0.25) > 0


def test_fisher_information_max_near_b():
    """Fisher information is maximised near difficulty b."""
    a, b, c = 1.0, 0.5, 0.0
    info_at_b = fisher_information(b, a, b, c)
    info_far = fisher_information(b + 2.0, a, b, c)
    assert info_at_b > info_far


def test_mle_mixed_responses():
    """MLE theta should be within [-4, 4] for mixed responses."""
    params = IRTParams(a=1.0, b=0.0, c=0.25)
    responses = [
        ItemResponse(question_id=i, is_correct=(i % 2 == 0), irt_params=params)
        for i in range(8)
    ]
    est = estimate_theta_mle(responses)
    assert -4.0 <= est.theta <= 4.0
    assert est.se > 0
    assert est.method == "MLE"


def test_eap_all_correct_above_zero():
    """All correct responses → EAP theta should be above 0."""
    params = IRTParams(a=1.0, b=0.0, c=0.25)
    responses = [
        ItemResponse(question_id=i, is_correct=True, irt_params=params)
        for i in range(5)
    ]
    est = estimate_theta_eap(responses)
    assert est.theta > 0.0


def test_eap_all_wrong_below_zero():
    """All wrong responses → EAP theta should be below 0."""
    params = IRTParams(a=1.0, b=0.0, c=0.25)
    responses = [
        ItemResponse(question_id=i, is_correct=False, irt_params=params)
        for i in range(5)
    ]
    est = estimate_theta_eap(responses)
    assert est.theta < 0.0


def test_eap_empty_returns_prior():
    """Empty responses → EAP returns prior mean."""
    est = estimate_theta_eap([], prior_mean=0.0, prior_sd=1.0)
    assert abs(est.theta - 0.0) < 0.01


def test_select_next_item_returns_valid():
    """select_next_item returns a valid item_id with positive Fisher information."""
    items = [
        IRTParams(a=1.0, b=-2.0, c=0.25),
        IRTParams(a=1.0, b=0.0, c=0.25),
        IRTParams(a=1.0, b=2.0, c=0.25),
    ]
    ids = [10, 20, 30]
    selected_id, info = select_next_item(0.0, items, ids)
    assert selected_id in ids, f"Selected id {selected_id} not in candidate ids {ids}"
    assert info > 0, "Fisher information should be positive"


def test_select_next_item_hard_item_preferred_at_high_theta():
    """At theta=3, hard item (b=3) should be preferred over easy item (b=-3)."""
    items = [
        IRTParams(a=1.5, b=-3.0, c=0.25),  # Very easy
        IRTParams(a=1.5, b=3.0, c=0.25),   # Very hard — should win at theta=3
    ]
    ids = [1, 2]
    selected_id, info = select_next_item(3.0, items, ids)
    assert selected_id == 2, f"Hard item should be preferred at theta=3, got {selected_id}"
    assert info > 0


def test_fisher_info_higher_near_matching_difficulty():
    """Fisher info of item with b=theta is higher than same item at theta far away."""
    # A hard item's info at theta=2 (matching b) vs theta=0 (far)
    info_at_match = fisher_information(2.0, 1.0, 2.0, 0.25)
    info_at_far = fisher_information(0.0, 1.0, 2.0, 0.25)
    assert info_at_match > info_at_far, (
        f"Info at b=theta should exceed far: {info_at_match:.4f} vs {info_at_far:.4f}"
    )


# ---------------------------------------------------------------------------
# BKT Tests
# ---------------------------------------------------------------------------

def test_bkt_correct_increases_mastery():
    """Correct response should increase mastery probability."""
    prior = 0.5
    posterior, _ = update_mastery(prior, is_correct=True)
    assert posterior > prior


def test_bkt_wrong_decreases_or_holds_mastery():
    """Wrong response should decrease mastery (accounting for learning)."""
    prior = 0.8
    posterior, _ = update_mastery(prior, is_correct=False)
    # After wrong + learning, mastery should not jump above prior
    assert posterior <= prior + 0.15  # allow small learning gain


def test_bkt_mastery_threshold():
    """Mastery flag is True when posterior >= 0.95."""
    _, mastered = update_mastery(0.96, is_correct=True)
    assert mastered is True


def test_bkt_not_mastered_low_prior():
    """Low prior → not mastered after one correct."""
    _, mastered = update_mastery(0.1, is_correct=True)
    assert mastered is False


def test_bkt_sequence_monotone_for_all_correct():
    """All-correct sequence should have non-decreasing mastery."""
    responses = [True] * 10
    seq = estimate_mastery_sequence(responses)
    for i in range(len(seq) - 1):
        assert seq[i + 1] >= seq[i] - 1e-9  # allow tiny float tolerance


def test_bkt_sequence_length():
    """Sequence length equals number of responses."""
    responses = [True, False, True, True]
    seq = estimate_mastery_sequence(responses)
    assert len(seq) == 4


if __name__ == "__main__":
    # Quick smoke-test without pytest
    test_p3pl_at_b_equals_theta()
    test_mle_mixed_responses()
    test_eap_all_correct_above_zero()
    test_select_next_item_returns_valid()
    test_select_next_item_hard_item_preferred_at_high_theta()
    test_fisher_info_higher_near_matching_difficulty()
    test_bkt_sequence_monotone_for_all_correct()
    print("All tests passed.")
