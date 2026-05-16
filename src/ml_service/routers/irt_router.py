from fastapi import APIRouter
from models import (
    EstimateThetaRequest,
    EstimateThetaResponse,
    CalibrateItemRequest,
    CalibrateItemResponse,
    NextItemRequest,
    NextItemResponse,
)
from irt.estimator import estimate_theta, calibrate_item, select_next_item

router = APIRouter(prefix="/irt", tags=["IRT"])


@router.post("/estimate", response_model=EstimateThetaResponse)
async def estimate_ability(request: EstimateThetaRequest) -> EstimateThetaResponse:
    """
    Estimate candidate ability (theta) from a list of item responses.
    Supports MLE and EAP methods.
    """
    theta_est = estimate_theta(
        responses=request.responses,
        method=request.method,
        prior_mean=request.prior_mean,
        prior_sd=request.prior_sd,
    )
    return EstimateThetaResponse(estimate=theta_est)


@router.post("/calibrate", response_model=CalibrateItemResponse)
async def calibrate_item_params(request: CalibrateItemRequest) -> CalibrateItemResponse:
    """
    Calibrate IRT (a, b, c) parameters for a single question using
    observed responses and corresponding theta estimates.
    Requires >= 30 observations for stable estimates.
    """
    params = calibrate_item(
        response_data=request.response_data,
        theta_estimates=request.theta_estimates,
    )
    return CalibrateItemResponse(question_id=request.question_id, params=params)


@router.post("/next-item", response_model=NextItemResponse)
async def get_next_item(request: NextItemRequest) -> NextItemResponse:
    """
    Select the optimal next item for CAT using Maximum Fisher Information.
    Returns the question_id that maximises information at current_theta.
    """
    selected_id, fisher_info = select_next_item(
        current_theta=request.current_theta,
        candidate_items=request.candidate_items,
        candidate_ids=request.candidate_item_ids,
    )
    return NextItemResponse(
        selected_item_id=selected_id,
        fisher_info_at_theta=round(fisher_info, 6),
    )
