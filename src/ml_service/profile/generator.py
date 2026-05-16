"""
Competency Profile Generator
==============================
Combines IRT theta + BKT mastery per skill + domain weights
to produce a 7-dimension competency profile (0–100 per domain).
"""
import math
from models import (
    GenerateProfileRequest,
    CompetencyProfileResponse,
    DomainScore,
    SkillMastery,
)


# IRT theta → overall ability boost (small adjustment, keeps domain scores meaningful)
def _irt_boost(theta: float) -> float:
    """Maps theta [-4, 4] to a multiplicative factor [0.7, 1.3]."""
    return 1.0 + 0.075 * theta


def _classify_level(theta: float) -> str:
    """Classify overall competency level from IRT theta."""
    if theta < -1.0:
        return "Foundation"
    elif theta < 1.0:
        return "Apply"
    else:
        return "Create"


def generate_profile(request: GenerateProfileRequest) -> CompetencyProfileResponse:
    """
    Aggregate skill masteries into domain scores and overall profile.

    Algorithm:
      1. Group skills by domain
      2. domain_raw = weighted average of skill mastery probabilities
      3. domain_score = clip(domain_raw * irt_boost * 100, 0, 100)
      4. overall_score = weighted average of domain scores by domain skill count
    """
    boost = _irt_boost(request.theta)

    # Group by domain
    domains: dict[int, list[SkillMastery]] = {}
    for sm in request.skill_masteries:
        domains.setdefault(sm.domain_id, []).append(sm)

    domain_scores: list[DomainScore] = []
    weighted_total = 0.0
    total_weight = 0.0

    for domain_id, skills in domains.items():
        domain_name = skills[0].domain_name

        # Weighted average mastery in this domain
        total_skill_weight = sum(s.weight for s in skills)
        if total_skill_weight < 1e-9:
            continue

        weighted_mastery = sum(s.mastery_prob * s.weight for s in skills) / total_skill_weight
        raw_score = weighted_mastery * boost * 100.0
        score = float(min(100.0, max(0.0, raw_score)))

        mastery_count = sum(1 for s in skills if s.mastery_prob >= 0.95)

        ds = DomainScore(
            domain_id=domain_id,
            domain_name=domain_name,
            score=round(score, 1),
            mastery_count=mastery_count,
            total_skills=len(skills),
        )
        domain_scores.append(ds)

        # Weight overall score by number of skills in domain
        weighted_total += score * len(skills)
        total_weight += len(skills)

    overall_score = (weighted_total / total_weight) if total_weight > 0 else 0.0

    return CompetencyProfileResponse(
        user_id=request.user_id,
        session_id=request.session_id,
        theta=round(request.theta, 4),
        overall_level=_classify_level(request.theta),
        domain_scores=sorted(domain_scores, key=lambda d: d.domain_id),
        overall_score=round(overall_score, 1),
    )
