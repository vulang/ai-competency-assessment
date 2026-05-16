"""
Bayesian Knowledge Tracing (BKT)
=================================
Hidden Markov Model with 4 parameters:
  P(L0) — prior probability of mastery
  P(T)  — transition (learn) probability
  P(S)  — slip probability (knows but answers wrong)
  P(G)  — guess probability (doesn't know but answers right)

Mastery threshold: P(Ln) >= 0.95
"""

from models import BKTParams

MASTERY_THRESHOLD = 0.95


def _update_mastery(prior: float, is_correct: bool, params: BKTParams) -> float:
    """
    Single Bayesian update step.
    Returns posterior mastery probability after observing one response.
    """
    p_l = prior
    p_t = params.p_t
    p_s = params.p_s
    p_g = params.p_g

    if is_correct:
        # P(L|correct)
        numerator = p_l * (1.0 - p_s)
        denominator = p_l * (1.0 - p_s) + (1.0 - p_l) * p_g
    else:
        # P(L|wrong)
        numerator = p_l * p_s
        denominator = p_l * p_s + (1.0 - p_l) * (1.0 - p_g)

    if denominator < 1e-12:
        posterior = p_l
    else:
        posterior = numerator / denominator

    # Apply learning transition: P(L_{n+1}) = P(Ln) + (1 - P(Ln)) * P(T)
    posterior_with_learn = posterior + (1.0 - posterior) * p_t

    return float(min(1.0, max(0.0, posterior_with_learn)))


def update_mastery(
    prior_mastery: float,
    is_correct: bool,
    params: BKTParams = BKTParams(),
) -> tuple[float, bool]:
    """
    Update mastery probability after a single observation.
    Returns (posterior_mastery, is_mastered).
    """
    posterior = _update_mastery(prior_mastery, is_correct, params)
    return posterior, posterior >= MASTERY_THRESHOLD


def estimate_mastery_sequence(
    responses: list[bool],
    params: BKTParams = BKTParams(),
) -> list[float]:
    """
    Compute the sequence of mastery probabilities for a list of responses.
    Starts from P(L0).
    """
    sequence: list[float] = []
    current = params.p_l0

    for is_correct in responses:
        current = _update_mastery(current, is_correct, params)
        sequence.append(current)

    return sequence


def batch_update_mastery(
    prior_mastery: float,
    responses: list[bool],
    params: BKTParams = BKTParams(),
) -> tuple[float, bool]:
    """
    Apply multiple BKT updates in sequence.
    Returns final (mastery, is_mastered).
    """
    current = prior_mastery
    for is_correct in responses:
        current = _update_mastery(current, is_correct, params)
    return current, current >= MASTERY_THRESHOLD
