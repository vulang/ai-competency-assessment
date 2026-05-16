from fastapi import APIRouter
from models import (
    UpdateMasteryRequest,
    UpdateMasteryResponse,
    MasterySequenceRequest,
    MasterySequenceResponse,
)
from kt.bkt import update_mastery, estimate_mastery_sequence

router = APIRouter(prefix="/kt", tags=["Knowledge Tracing"])


@router.post("/update", response_model=UpdateMasteryResponse)
async def update_skill_mastery(request: UpdateMasteryRequest) -> UpdateMasteryResponse:
    """
    Apply a single Bayesian Knowledge Tracing update.
    Returns updated mastery probability and mastery status.
    """
    posterior, mastered = update_mastery(
        prior_mastery=request.prior_mastery,
        is_correct=request.is_correct,
        params=request.params,
    )
    return UpdateMasteryResponse(
        skill_id=request.skill_id,
        posterior_mastery=round(posterior, 4),
        mastered=mastered,
    )


@router.post("/sequence", response_model=MasterySequenceResponse)
async def mastery_sequence(request: MasterySequenceRequest) -> MasterySequenceResponse:
    """
    Compute the full mastery probability sequence from a list of responses.
    Useful for visualizing learning progression over time.
    """
    sequence = estimate_mastery_sequence(
        responses=request.responses,
        params=request.params,
    )
    final = sequence[-1] if sequence else request.params.p_l0
    return MasterySequenceResponse(
        skill_id=request.skill_id,
        mastery_sequence=[round(p, 4) for p in sequence],
        final_mastery=round(final, 4),
        mastered=final >= 0.95,
    )
