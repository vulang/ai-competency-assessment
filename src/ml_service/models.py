from typing import Optional
from pydantic import BaseModel, Field


# ---------------------------------------------------------------------------
# IRT Models
# ---------------------------------------------------------------------------

class IRTParams(BaseModel):
    """3-Parameter Logistic IRT parameters for a single question item."""
    a: float = Field(1.0, description="Discrimination parameter (0.5–2.5)")
    b: float = Field(0.0, description="Difficulty parameter in logit scale (-3 to +3)")
    c: float = Field(0.25, description="Pseudo-guessing parameter (0.0–0.33)")
    se_b: Optional[float] = Field(None, description="Standard error of b estimate")
    response_count: int = Field(0, description="Number of responses used for calibration")


class ItemResponse(BaseModel):
    """A single item response from a candidate."""
    question_id: int
    is_correct: bool
    irt_params: IRTParams


class ThetaEstimate(BaseModel):
    """Estimated ability (theta) for a candidate."""
    theta: float = Field(description="Ability estimate in logit scale")
    se: float = Field(description="Standard error of the estimate")
    method: str = Field(description="Estimation method: MLE or EAP")
    fisher_info: float = Field(description="Total Fisher information at theta")


class EstimateThetaRequest(BaseModel):
    responses: list[ItemResponse]
    method: str = Field("EAP", description="MLE or EAP")
    prior_mean: float = 0.0
    prior_sd: float = 1.0


class EstimateThetaResponse(BaseModel):
    estimate: ThetaEstimate


class CalibrateItemRequest(BaseModel):
    """Request to calibrate IRT parameters for a single item."""
    question_id: int
    response_data: list[bool] = Field(description="List of is_correct for each response")
    theta_estimates: list[float] = Field(description="Corresponding theta estimates per response")


class CalibrateItemResponse(BaseModel):
    question_id: int
    params: IRTParams


class NextItemRequest(BaseModel):
    """Select the optimal next item using Maximum Fisher Information."""
    current_theta: float
    candidate_items: list[IRTParams]
    candidate_item_ids: list[int]


class NextItemResponse(BaseModel):
    selected_item_id: int
    fisher_info_at_theta: float


# ---------------------------------------------------------------------------
# BKT Models
# ---------------------------------------------------------------------------

class BKTParams(BaseModel):
    """Bayesian Knowledge Tracing parameters for a skill."""
    p_l0: float = Field(0.3, description="Prior probability of knowing the skill")
    p_t: float = Field(0.1, description="Probability of learning (transition)")
    p_s: float = Field(0.1, description="Probability of slip (knows but answers wrong)")
    p_g: float = Field(0.25, description="Probability of guess (doesn't know but answers right)")


class UpdateMasteryRequest(BaseModel):
    skill_id: int
    prior_mastery: float = Field(description="Current mastery probability (0–1)")
    is_correct: bool
    params: BKTParams = BKTParams()


class UpdateMasteryResponse(BaseModel):
    skill_id: int
    posterior_mastery: float
    mastered: bool = Field(description="True if mastery_prob >= 0.95")


class MasterySequenceRequest(BaseModel):
    """Estimate mastery probability sequence from a list of responses."""
    skill_id: int
    responses: list[bool]
    params: BKTParams = BKTParams()


class MasterySequenceResponse(BaseModel):
    skill_id: int
    mastery_sequence: list[float]
    final_mastery: float
    mastered: bool


# ---------------------------------------------------------------------------
# Competency Profile Models
# ---------------------------------------------------------------------------

class SkillMastery(BaseModel):
    skill_id: int
    skill_name: str
    domain_id: int
    domain_name: str
    mastery_prob: float
    weight: float = 1.0


class DomainScore(BaseModel):
    domain_id: int
    domain_name: str
    score: float = Field(description="Score 0–100")
    mastery_count: int
    total_skills: int


class GenerateProfileRequest(BaseModel):
    user_id: str
    session_id: str
    theta: float
    se_theta: float
    skill_masteries: list[SkillMastery]


class CompetencyProfileResponse(BaseModel):
    user_id: str
    session_id: str
    theta: float
    overall_level: str = Field(description="Foundation | Apply | Create")
    domain_scores: list[DomainScore]
    overall_score: float = Field(description="Weighted average across all domains, 0–100")


# ---------------------------------------------------------------------------
# CTT Models (thesis §2.12)
# ---------------------------------------------------------------------------

class CttItem(BaseModel):
    item_id: str
    group: Optional[str] = Field(None, description="Competency group code")
    option_count: Optional[int] = Field(None, ge=2, le=26, description="Options shown; omit to skip distractor analysis")
    key: list[int] = Field(default_factory=list, description="Zero-based indexes of the keyed option(s)")


class CttRespondent(BaseModel):
    respondent_id: str
    scores: list[float] = Field(description="Dichotomous item scores (0/1) aligned with items")
    selections: Optional[list[list[int]]] = Field(None, description="Chosen option indexes per item; [] when omitted")


class CttThresholds(BaseModel):
    min_difficulty: float = Field(0.2, ge=0, le=1)
    max_difficulty: float = Field(0.9, ge=0, le=1)
    min_discrimination: float = Field(0.2, ge=-1, le=1)
    min_distractor_rate: float = Field(0.05, ge=0, le=1)


class CttAnalyzeRequest(BaseModel):
    items: list[CttItem] = Field(min_length=1)
    respondents: list[CttRespondent]
    thresholds: CttThresholds = Field(default_factory=CttThresholds)


class CttDistractor(BaseModel):
    option: int
    is_key: bool
    proportion: float
    upper: Optional[float]
    lower: Optional[float]
    option_rest_r: Optional[float]
    flags: list[str]


class CttItemResult(BaseModel):
    item_id: str
    group: Optional[str]
    n: int
    p: Optional[float] = Field(description="Difficulty: proportion correct")
    item_rest_r: Optional[float] = Field(description="Corrected item-total correlation (whole test)")
    item_rest_r_group: Optional[float] = Field(description="Corrected item-total correlation within the group")
    discrimination_index: Optional[float] = Field(description="Upper 27% minus lower 27% proportion correct")
    alpha_if_deleted: Optional[float]
    omit_rate: Optional[float]
    flags: list[str]
    distractors: list[CttDistractor]


class CttReliability(BaseModel):
    n_items: int
    alpha: Optional[float]
    omega: Optional[float]
    excluded_items: list[str]
    notes: list[str]


class CttGroupResult(BaseModel):
    group: str
    n_items: int
    mean_percent: Optional[float]
    alpha: Optional[float]
    omega: Optional[float]
    excluded_items: list[str]
    notes: list[str]


class CttHistogramBin(BaseModel):
    score: float
    count: int


class CttTotal(BaseModel):
    mean: Optional[float]
    sd: Optional[float]
    min: Optional[float]
    max: Optional[float]
    histogram: list[CttHistogramBin]


class CttAnalyzeResponse(BaseModel):
    n_respondents: int
    n_items: int
    total: CttTotal
    reliability: CttReliability
    groups: list[CttGroupResult]
    items: list[CttItemResult]
    thresholds: CttThresholds
