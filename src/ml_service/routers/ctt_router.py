import numpy as np
from fastapi import APIRouter, HTTPException

from ctt.analysis import ItemSpec, Thresholds, analyze
from models import CttAnalyzeRequest, CttAnalyzeResponse

router = APIRouter(prefix="/ctt", tags=["CTT"])


@router.post("/analyze", response_model=CttAnalyzeResponse)
async def analyze_items(request: CttAnalyzeRequest) -> CttAnalyzeResponse:
    """
    Classical Test Theory report for one fixed test form: difficulty, corrected item-total
    correlation, distractor analysis, Cronbach's alpha and McDonald's omega (overall and per group).
    """
    k = len(request.items)
    if len({item.item_id for item in request.items}) != k:
        raise HTTPException(status_code=422, detail="Item IDs must be unique.")
    for respondent in request.respondents:
        if len(respondent.scores) != k or any(s not in (0, 1) for s in respondent.scores):
            raise HTTPException(status_code=422, detail="Each respondent needs one 0/1 score per item.")
        if respondent.selections is not None and len(respondent.selections) != k:
            raise HTTPException(status_code=422, detail="Selections must align with items.")
    for item in request.items:
        if item.option_count is not None and any(not 0 <= key < item.option_count for key in item.key):
            raise HTTPException(status_code=422, detail=f"Item {item.item_id} has a key outside its options.")

    scores = np.array([r.scores for r in request.respondents], dtype=float).reshape(len(request.respondents), k)
    selections = None
    if request.respondents and all(r.selections is not None for r in request.respondents):
        selections = [r.selections for r in request.respondents]
    items = [ItemSpec(i.item_id, i.group, i.option_count, tuple(i.key)) for i in request.items]
    report = analyze(items, scores, selections, Thresholds(**request.thresholds.model_dump()))
    return CttAnalyzeResponse(**report)
