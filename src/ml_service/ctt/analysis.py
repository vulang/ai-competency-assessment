"""
Classical Test Theory (CTT) item and reliability analysis (thesis §2.12, Appendix H).

Inputs are dichotomous item scores (respondents × items). Unanswered items are scored 0
by the caller, as is usual for a fixed short form.

- Difficulty p: proportion correct.
- Discrimination: corrected item–total (item–rest) Pearson correlation, against the whole
  test and within the item's competency group; upper/lower 27% index D.
- Distractors: selection rate overall and in the upper/lower 27% groups, and the
  option–rest correlation.
- Reliability: Cronbach's alpha (sample variances), alpha if item deleted, and McDonald's
  omega total from a one-factor model fitted to the inter-item Pearson (phi) correlations by
  iterated principal-axis factoring, which converges to the minres solution.
"""
from __future__ import annotations

import math
from dataclasses import asdict, dataclass, field

import numpy as np

UPPER_LOWER_FRACTION = 0.27
HEYWOOD_LIMIT = 0.995


@dataclass(frozen=True)
class Thresholds:
    min_difficulty: float = 0.2
    max_difficulty: float = 0.9
    min_discrimination: float = 0.2
    min_distractor_rate: float = 0.05


@dataclass(frozen=True)
class ItemSpec:
    item_id: str
    group: str | None = None
    option_count: int | None = None
    key: tuple[int, ...] = ()


@dataclass
class Reliability:
    n_items: int
    alpha: float | None
    omega: float | None
    excluded_items: list[str] = field(default_factory=list)
    notes: list[str] = field(default_factory=list)


def _finite(value: float | None) -> float | None:
    if value is None or not math.isfinite(value):
        return None
    return float(value)


def pearson(x: np.ndarray, y: np.ndarray) -> float | None:
    """Pearson correlation; None when either variable is constant."""
    if len(x) < 2:
        return None
    sx, sy = np.std(x), np.std(y)
    if sx == 0 or sy == 0:
        return None
    return _finite(float(np.mean((x - x.mean()) * (y - y.mean())) / (sx * sy)))


def cronbach_alpha(scores: np.ndarray) -> float | None:
    """alpha = k/(k-1) * (1 - sum(item variances) / total variance)."""
    n, k = scores.shape
    if k < 2 or n < 2:
        return None
    total_var = np.var(scores.sum(axis=1), ddof=1)
    if total_var == 0:
        return None
    return _finite(k / (k - 1) * (1 - np.var(scores, axis=0, ddof=1).sum() / total_var))


def one_factor_loadings(corr: np.ndarray, max_iter: int = 500, tol: float = 1e-8) -> tuple[np.ndarray, bool]:
    """
    Standardized loadings of a one-factor model by iterated principal-axis factoring.
    Returns (loadings, heywood) where heywood marks communalities clipped at HEYWOOD_LIMIT.
    """
    k = corr.shape[0]
    try:
        smc = 1 - 1 / np.diag(np.linalg.inv(corr))
    except np.linalg.LinAlgError:
        smc = np.max(np.abs(corr - np.eye(k)), axis=1)
    communality = np.clip(smc, 0.005, HEYWOOD_LIMIT)
    heywood = False
    loadings = np.zeros(k)
    for _ in range(max_iter):
        reduced = corr.copy()
        np.fill_diagonal(reduced, communality)
        values, vectors = np.linalg.eigh(reduced)
        loadings = vectors[:, -1] * math.sqrt(max(values[-1], 0.0))
        updated = loadings ** 2
        heywood = bool(np.any(updated > HEYWOOD_LIMIT))
        updated = np.clip(updated, 0.0, HEYWOOD_LIMIT)
        converged = np.max(np.abs(updated - communality)) < tol
        communality = updated
        if converged:
            break
    if loadings.sum() < 0:
        loadings = -loadings
    return loadings, heywood


def omega_total(loadings: np.ndarray) -> float | None:
    """omega = (sum lambda)^2 / ((sum lambda)^2 + sum(1 - lambda^2)) for standardized loadings."""
    common = loadings.sum() ** 2
    unique = np.sum(1 - np.clip(loadings ** 2, 0, 1))
    if common + unique == 0:
        return None
    return _finite(common / (common + unique))


def reliability(scores: np.ndarray, item_ids: list[str]) -> Reliability:
    variances = np.var(scores, axis=0, ddof=1) if scores.shape[0] > 1 else np.zeros(scores.shape[1])
    keep = variances > 0
    excluded = [item_ids[i] for i in range(len(item_ids)) if not keep[i]]
    used = scores[:, keep]
    notes: list[str] = []
    if excluded:
        notes.append(f"{len(excluded)} item(s) without score variance were excluded.")
    k = used.shape[1]
    alpha = cronbach_alpha(used)
    omega = None
    if alpha is None:
        notes.append("Alpha needs at least two respondents and two items with score variance.")
    if k < 3 or used.shape[0] < 3:
        notes.append("Omega needs at least three respondents and three items with score variance.")
    else:
        loadings, heywood = one_factor_loadings(np.corrcoef(used, rowvar=False))
        omega = omega_total(loadings)
        if heywood:
            notes.append("A Heywood case occurred; omega is unstable for this sample.")
    return Reliability(n_items=k, alpha=alpha, omega=omega, excluded_items=excluded, notes=notes)


def upper_lower(total: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    """Indexes of the top and bottom 27% by total score (stable for ties)."""
    n = len(total)
    size = max(1, math.ceil(UPPER_LOWER_FRACTION * n)) if n >= 2 else 0
    order = np.argsort(total, kind="stable")
    return order[n - size:], order[:size]


def analyze(items: list[ItemSpec], scores: np.ndarray, selections: list[list[list[int]]] | None,
            thresholds: Thresholds = Thresholds()) -> dict:
    """Full CTT report. scores is respondents × items with values 0/1."""
    n, k = scores.shape
    item_ids = [item.item_id for item in items]
    total = scores.sum(axis=1)
    overall = reliability(scores, item_ids) if n else Reliability(k, None, None, notes=["No responses."])
    upper, lower = upper_lower(total) if n else (np.array([], int), np.array([], int))

    groups: dict[str, list[int]] = {}
    for index, item in enumerate(items):
        if item.group:
            groups.setdefault(item.group, []).append(index)

    alpha_deleted = [None] * k
    if overall.alpha is not None:
        for index in range(k):
            rest = np.delete(scores, index, axis=1)
            alpha_deleted[index] = cronbach_alpha(rest[:, np.var(rest, axis=0, ddof=1) > 0])

    report_items = []
    for index, item in enumerate(items):
        column = scores[:, index]
        p = float(column.mean()) if n else None
        rest_r = pearson(column, total - column)
        group_members = groups.get(item.group or "", [])
        group_total = scores[:, group_members].sum(axis=1) if group_members else total
        group_r = pearson(column, group_total - column) if len(group_members) > 1 else None
        d_index = float(column[upper].mean() - column[lower].mean()) if len(upper) else None
        flags = []
        if n and np.var(column) == 0:
            flags.append("zero_variance")
        if p is not None and p < thresholds.min_difficulty:
            flags.append("too_hard")
        if p is not None and p > thresholds.max_difficulty:
            flags.append("too_easy")
        if rest_r is not None and rest_r < thresholds.min_discrimination:
            flags.append("negative_discrimination" if rest_r < 0 else "low_discrimination")
        if alpha_deleted[index] is not None and overall.alpha is not None and alpha_deleted[index] > overall.alpha:
            flags.append("alpha_increases_if_deleted")
        distractors, omit_rate = _distractors(item, index, scores, total, selections, upper, lower, thresholds)
        if any("nonfunctional" in d["flags"] for d in distractors):
            flags.append("nonfunctional_distractor")
        if any("attracts_high_scorers" in d["flags"] for d in distractors):
            flags.append("distractor_attracts_high_scorers")
        report_items.append({
            "item_id": item.item_id,
            "group": item.group,
            "n": n,
            "p": _finite(p),
            "item_rest_r": rest_r,
            "item_rest_r_group": group_r,
            "discrimination_index": _finite(d_index),
            "alpha_if_deleted": alpha_deleted[index],
            "omit_rate": omit_rate,
            "flags": flags,
            "distractors": distractors,
        })

    report_groups = []
    for code, members in groups.items():
        rel = reliability(scores[:, members], [item_ids[i] for i in members]) if n else Reliability(len(members), None, None)
        report_groups.append({
            "group": code,
            "n_items": len(members),
            "mean_percent": _finite(float(scores[:, members].mean() * 100)) if n else None,
            "alpha": rel.alpha,
            "omega": rel.omega,
            "excluded_items": rel.excluded_items,
            "notes": rel.notes,
        })

    values, counts = np.unique(total, return_counts=True) if n else (np.array([]), np.array([]))
    return {
        "n_respondents": n,
        "n_items": k,
        "total": {
            "mean": _finite(float(total.mean())) if n else None,
            "sd": _finite(float(np.std(total, ddof=1))) if n > 1 else None,
            "min": _finite(float(total.min())) if n else None,
            "max": _finite(float(total.max())) if n else None,
            "histogram": [{"score": float(v), "count": int(c)} for v, c in zip(values, counts)],
        },
        "reliability": {
            "n_items": overall.n_items,
            "alpha": overall.alpha,
            "omega": overall.omega,
            "excluded_items": overall.excluded_items,
            "notes": overall.notes,
        },
        "groups": report_groups,
        "items": report_items,
        "thresholds": asdict(thresholds),
    }


def _distractors(item: ItemSpec, index: int, scores: np.ndarray, total: np.ndarray,
                 selections: list[list[list[int]]] | None, upper: np.ndarray, lower: np.ndarray,
                 thresholds: Thresholds) -> tuple[list[dict], float | None]:
    n = scores.shape[0]
    if not selections or not item.option_count or n == 0:
        return [], None
    chosen = np.zeros((n, item.option_count))
    omitted = 0
    for respondent, picks in enumerate(selections):
        options = [o for o in picks[index] if 0 <= o < item.option_count] if index < len(picks) else []
        if not options:
            omitted += 1
        chosen[respondent, options] = 1
    rest = total - scores[:, index]
    result = []
    for option in range(item.option_count):
        column = chosen[:, option]
        is_key = option in item.key
        proportion = float(column.mean())
        option_r = pearson(column, rest)
        flags = []
        if not is_key and proportion < thresholds.min_distractor_rate:
            flags.append("nonfunctional")
        if not is_key and option_r is not None and option_r > 0:
            flags.append("attracts_high_scorers")
        result.append({
            "option": option,
            "is_key": is_key,
            "proportion": proportion,
            "upper": _finite(float(column[upper].mean())) if len(upper) else None,
            "lower": _finite(float(column[lower].mean())) if len(lower) else None,
            "option_rest_r": option_r,
            "flags": flags,
        })
    return result, omitted / n
