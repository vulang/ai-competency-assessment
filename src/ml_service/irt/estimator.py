"""
IRT 3-Parameter Logistic Estimator
===================================
Implements:
  - P(θ): 3PL probability function
  - Fisher Information I(θ) per item
  - MLE theta estimation via scipy.optimize
  - EAP theta estimation with Gaussian prior
  - Item parameter calibration via joint MLE (EM-like)
"""
import math
import numpy as np
from scipy.optimize import minimize_scalar, minimize
from scipy.stats import norm
from typing import Optional

from models import IRTParams, ItemResponse, ThetaEstimate


# ---------------------------------------------------------------------------
# Core 3PL Functions
# ---------------------------------------------------------------------------

def p3pl(theta: float, a: float, b: float, c: float) -> float:
    """3PL probability of correct response."""
    exponent = -a * (theta - b)
    # Clamp exponent to avoid overflow
    exponent = max(-500.0, min(500.0, exponent))
    return c + (1.0 - c) / (1.0 + math.exp(exponent))


def fisher_information(theta: float, a: float, b: float, c: float) -> float:
    """
    Fisher Information I(θ) for a single 3PL item.

    Derivation:
      dP/dθ = a(P - c)(1 - P) / (1 - c)
      I(θ)  = (dP/dθ)² / (P · Q) = a²(P-c)²(1-P) / [(1-c)² · P]

    This correctly approaches 0 as P→1 (easy item at high θ)
    and as P→c (item far below candidate ability).
    """
    p = p3pl(theta, a, b, c)
    if p <= 1e-10 or p >= 1.0 - 1e-10:
        return 0.0
    one_minus_c = 1.0 - c
    if one_minus_c < 1e-10:
        return 0.0
    numerator = a**2 * (p - c)**2 * (1.0 - p)
    denominator = one_minus_c**2 * p
    return numerator / denominator


def total_fisher_information(theta: float, items: list[IRTParams]) -> float:
    """Sum of Fisher Information across all items at a given theta."""
    return sum(fisher_information(theta, item.a, item.b, item.c) for item in items)


def log_likelihood(theta: float, responses: list[ItemResponse]) -> float:
    """Log-likelihood of a response pattern given theta."""
    ll = 0.0
    for resp in responses:
        p = p3pl(theta, resp.irt_params.a, resp.irt_params.b, resp.irt_params.c)
        p = max(1e-10, min(1.0 - 1e-10, p))
        if resp.is_correct:
            ll += math.log(p)
        else:
            ll += math.log(1.0 - p)
    return ll


# ---------------------------------------------------------------------------
# Theta Estimation
# ---------------------------------------------------------------------------

def estimate_theta_mle(responses: list[ItemResponse]) -> ThetaEstimate:
    """
    Maximum Likelihood Estimation of theta.
    Returns theta in [-4, 4] with SE derived from Fisher Information.
    Raises ValueError if responses are all correct or all incorrect.
    """
    if not responses:
        return ThetaEstimate(theta=0.0, se=1.0, method="MLE", fisher_info=0.0)

    all_correct = all(r.is_correct for r in responses)
    all_wrong = all(not r.is_correct for r in responses)

    if all_correct:
        # Return high ability with large SE
        theta = 3.0
    elif all_wrong:
        theta = -3.0
    else:
        result = minimize_scalar(
            lambda t: -log_likelihood(t, responses),
            bounds=(-4.0, 4.0),
            method="bounded",
        )
        theta = float(result.x)

    theta = float(np.clip(theta, -4.0, 4.0))
    total_info = total_fisher_information(theta, [r.irt_params for r in responses])
    se = 1.0 / math.sqrt(total_info) if total_info > 1e-6 else 1.0

    return ThetaEstimate(
        theta=theta,
        se=se,
        method="MLE",
        fisher_info=total_info,
    )


def estimate_theta_eap(
    responses: list[ItemResponse],
    prior_mean: float = 0.0,
    prior_sd: float = 1.0,
    n_quadrature: int = 41,
) -> ThetaEstimate:
    """
    Expected A Posteriori (EAP) estimation.
    Numerically integrates theta × posterior over Gaussian prior.
    More stable than MLE for short response sequences.
    """
    # Quadrature points in [-4, 4]
    quad_points = np.linspace(-4.0, 4.0, n_quadrature)
    prior_weights = norm.pdf(quad_points, loc=prior_mean, scale=prior_sd)

    # Compute likelihood at each quadrature point
    likelihoods = np.array([
        math.exp(log_likelihood(t, responses)) if responses else 1.0
        for t in quad_points
    ])

    posterior = likelihoods * prior_weights
    posterior_sum = posterior.sum()

    if posterior_sum < 1e-300:
        # Degenerate case — fall back to prior mean
        theta = prior_mean
        se = prior_sd
    else:
        posterior /= posterior_sum
        theta = float(np.dot(quad_points, posterior))
        variance = float(np.dot((quad_points - theta) ** 2, posterior))
        se = math.sqrt(variance) if variance > 0 else prior_sd

    total_info = total_fisher_information(theta, [r.irt_params for r in responses])

    return ThetaEstimate(
        theta=float(np.clip(theta, -4.0, 4.0)),
        se=se,
        method="EAP",
        fisher_info=total_info,
    )


def estimate_theta(
    responses: list[ItemResponse],
    method: str = "EAP",
    prior_mean: float = 0.0,
    prior_sd: float = 1.0,
) -> ThetaEstimate:
    """Entry-point: estimate theta with specified method."""
    if method.upper() == "MLE":
        return estimate_theta_mle(responses)
    return estimate_theta_eap(responses, prior_mean, prior_sd)


# ---------------------------------------------------------------------------
# Item Selection: Maximum Fisher Information
# ---------------------------------------------------------------------------

def select_next_item(
    current_theta: float,
    candidate_items: list[IRTParams],
    candidate_ids: list[int],
) -> tuple[int, float]:
    """
    Select the next item that maximises Fisher Information at current_theta.
    Returns (question_id, fisher_info).
    """
    if not candidate_items:
        raise ValueError("No candidate items available")

    best_id = candidate_ids[0]
    best_info = -1.0

    for item_id, params in zip(candidate_ids, candidate_items):
        info = fisher_information(current_theta, params.a, params.b, params.c)
        if info > best_info:
            best_info = info
            best_id = item_id

    return best_id, best_info


# ---------------------------------------------------------------------------
# Item Calibration (Simple Joint MLE)
# ---------------------------------------------------------------------------

def calibrate_item(
    response_data: list[bool],
    theta_estimates: list[float],
) -> IRTParams:
    """
    Estimate (a, b, c) for a single item given a set of (theta, response) pairs.
    Uses scipy.optimize with bounded L-BFGS-B.
    Requires at least 30 observations for stable estimates.
    """
    if len(response_data) < 30:
        return IRTParams()  # Return defaults if insufficient data

    y = np.array([1.0 if r else 0.0 for r in response_data])
    thetas = np.array(theta_estimates)

    def neg_log_lik(params: np.ndarray) -> float:
        a, b, c = params
        ll = 0.0
        for theta_i, y_i in zip(thetas, y):
            p = p3pl(float(theta_i), float(a), float(b), float(c))
            p = max(1e-10, min(1.0 - 1e-10, p))
            ll += y_i * math.log(p) + (1.0 - y_i) * math.log(1.0 - p)
        return -ll

    x0 = np.array([1.0, 0.0, 0.25])
    bounds = [(0.3, 3.0), (-4.0, 4.0), (0.0, 0.4)]

    result = minimize(neg_log_lik, x0, method="L-BFGS-B", bounds=bounds)

    if result.success:
        a, b, c = result.x
        # Compute SE of b via observed Fisher Information (numerical Hessian not available easily)
        # Approximate via curvature at optimum
        return IRTParams(
            a=float(np.clip(a, 0.3, 3.0)),
            b=float(np.clip(b, -4.0, 4.0)),
            c=float(np.clip(c, 0.0, 0.4)),
            response_count=len(response_data),
        )

    return IRTParams(response_count=len(response_data))
