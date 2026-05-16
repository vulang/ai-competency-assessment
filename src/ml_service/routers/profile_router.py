from fastapi import APIRouter
from models import GenerateProfileRequest, CompetencyProfileResponse
from profile.generator import generate_profile

router = APIRouter(prefix="/profile", tags=["Competency Profile"])


@router.post("/generate", response_model=CompetencyProfileResponse)
async def generate_competency_profile(
    request: GenerateProfileRequest,
) -> CompetencyProfileResponse:
    """
    Generate a multi-dimensional competency profile for a candidate.
    Combines IRT theta + BKT mastery per skill + domain weights.
    Returns scores per domain (0–100) and overall competency level.
    """
    return generate_profile(request)
