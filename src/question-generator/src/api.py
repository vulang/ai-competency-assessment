"""
api.py — FastAPI Question Generator API

Endpoints:
  POST /generate-from-plan   — legacy synchronous generation from plan
  POST /generate             — simple legacy endpoint
  POST /generate-agentic     — new agentic pipeline with RAG, judge, feedback loop
  GET  /health               — health check
"""
from __future__ import annotations

import asyncio
import json
import os
import sys
import time
from typing import Any, Dict, List, Optional

# Ensure both question-generator/ and question-generator/src/ are on sys.path
_current_file_dir = os.path.dirname(os.path.abspath(__file__))
_pkg_root = os.path.dirname(_current_file_dir)
sys.path.insert(0, _pkg_root)       # question-generator/
sys.path.insert(0, _current_file_dir)  # question-generator/src/

from fastapi import FastAPI, HTTPException, BackgroundTasks
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from dotenv import load_dotenv

load_dotenv()

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

try:
    from .plan import expand_plan
    from .generate import generate_questions
except ImportError:
    from plan import expand_plan      # type: ignore
    from generate import generate_questions  # type: ignore

app = FastAPI(
    title="AI Competency Question Generator API",
    description="Agentic question generation with RAG enrichment and LLM-as-a-Judge feedback loop",
    version="2.0.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:4200", "http://localhost:3000"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ── Helpers ───────────────────────────────────────────────────────────────────
_current_dir = os.path.dirname(os.path.abspath(__file__))
_project_root = os.path.dirname(_current_dir)


def _load_yaml(path: str) -> dict:
    import yaml  # type: ignore
    with open(path, "r", encoding="utf-8") as f:
        return yaml.safe_load(f)


def _load_prompt(filename: str) -> str:
    path = os.path.join(_project_root, "prompts", filename)
    with open(path, "r", encoding="utf-8") as f:
        return f.read()


def _load_config() -> dict:
    return _load_yaml(os.path.join(_project_root, "configs", "model.yaml"))


# ── Request / Response models ─────────────────────────────────────────────────
class MixItem(BaseModel):
    group: str
    level: str
    type: str
    difficulty: List[int]
    topic: str = ""
    count: int = 1


class GenerationPlanRequest(BaseModel):
    total: int = 100
    mix: List[MixItem]


class GenerationRequest(BaseModel):
    topic: str
    count: int = 5
    difficulty: Optional[str] = "medium"


class AgenticRequest(BaseModel):
    """Request body for the agentic endpoint."""
    mix: List[MixItem]
    total: Optional[int] = None     # optional override; defaults to sum of mix counts
    debug: bool = False
    concurrency: int = 4


class JudgeDimensions(BaseModel):
    clarity: float = 0.0
    correctness: float = 0.0
    group_alignment: float = 0.0
    application_focus: float = 0.0


class AttemptSummary(BaseModel):
    attempt: int
    question_id: Optional[str]
    judge_score: Optional[float]
    judge_dims: Dict[str, float]
    judge_passed: Optional[bool]
    rag_sources: List[Dict[str, Any]]
    web_search_used: bool
    elapsed_sec: float


class JobResultSummary(BaseModel):
    job_id: str
    accepted: bool
    final_judge_score: float
    reject_reason: str
    attempts: List[AttemptSummary]


class AgenticResponse(BaseModel):
    questions: List[Dict[str, Any]]
    summary: Dict[str, Any]
    audit: List[JobResultSummary]


# ── Legacy endpoints ──────────────────────────────────────────────────────────
@app.post("/generate-from-plan", response_model=List[Dict[str, Any]])
async def generate_from_plan(request: GenerationPlanRequest):
    try:
        prompt_path = os.path.join(_project_root, "prompts", "generator_vn.txt")
        if not os.path.exists(prompt_path):
            raise HTTPException(status_code=500, detail=f"Prompt not found: {prompt_path}")
        with open(prompt_path, "r", encoding="utf-8") as f:
            prompt_tmpl = f.read()
        plan_dict = request.model_dump()
        jobs = expand_plan(plan_dict)
        questions, skipped = generate_questions(jobs, prompt_tmpl)
        if not questions and skipped > 0:
            raise HTTPException(status_code=500, detail="All questions skipped. Check logs.")
        return questions
    except HTTPException:
        raise
    except Exception as e:
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/generate")
async def generate_questions_simple(request: GenerationRequest):
    diff_map = {"easy": [1], "medium": [2], "hard": [3]}
    difficulty = diff_map.get(request.difficulty or "medium", [2])
    mix_item = MixItem(
        group="NhanThucAI",
        level="NenTang",
        type="mcq_single",
        difficulty=difficulty,
        topic=request.topic,
        count=request.count,
    )
    plan = GenerationPlanRequest(total=request.count, mix=[mix_item])
    return await generate_from_plan(plan)


# ── Agentic endpoint ──────────────────────────────────────────────────────────
@app.post("/generate-agentic", response_model=AgenticResponse)
async def generate_agentic(request: AgenticRequest):
    """
    Full agentic pipeline:
    - GeneratorAgent enriches context via RAG + optional web search
    - JudgeAgent scores each question on 4 dimensions
    - Feedback loop retries failing questions up to max_retries times
    - Returns accepted questions + full audit trail
    """
    try:
        try:
            from agents import build_orchestrator_from_config  # type: ignore
        except ImportError:
            try:
                from src.agents import build_orchestrator_from_config  # type: ignore
            except ImportError:
                raise HTTPException(
                    status_code=500,
                    detail="agents module not found. Ensure all dependencies are installed.",
            )

        config = _load_config()
        generator_prompt = _load_prompt("generator_vn.txt")
        judge_prompt = _load_prompt("judge_vn.txt")

        # Build jobs from mix
        jobs: list[dict] = []
        for item in request.mix:
            for i in range(item.count):
                j = item.model_dump()
                j.pop("count", None)
                j["id"] = f"{item.group}_{item.level}_{item.type}_{i+1}"
                jobs.append(j)

        data_root = os.path.join(_project_root, "data")
        index_path = os.path.join(_project_root, ".cache", "rag_index")

        orchestrator = build_orchestrator_from_config(
            config=config,
            prompt_tmpl=generator_prompt,
            judge_prompt_tmpl=judge_prompt,
            data_root=data_root,
            index_path=index_path,
        )

        t0 = time.perf_counter()
        result = await orchestrator.run(
            jobs, debug=request.debug, concurrency=request.concurrency
        )
        elapsed = time.perf_counter() - t0

        # Build audit list
        audit: list[JobResultSummary] = []
        for jr in result.job_results:
            audit.append(
                JobResultSummary(
                    job_id=jr.job_id,
                    accepted=jr.accepted,
                    final_judge_score=jr.final_judge_score,
                    reject_reason=jr.reject_reason,
                    attempts=[
                        AttemptSummary(
                            attempt=a.attempt,
                            question_id=a.question.get("id") if a.question else None,
                            judge_score=a.judge.score if a.judge else None,
                            judge_dims=a.judge.dimensions if a.judge else {},
                            judge_passed=a.judge.passed if a.judge else None,
                            rag_sources=a.rag_sources,
                            web_search_used=a.web_search_used,
                            elapsed_sec=a.elapsed_sec,
                        )
                        for a in jr.attempts
                    ],
                )
            )

        return AgenticResponse(
            questions=result.questions,
            summary={
                "total_jobs": result.total_jobs,
                "accepted": result.accepted,
                "rejected": result.rejected,
                "total_attempts": result.total_attempts,
                "elapsed_sec": round(elapsed, 2),
            },
            audit=audit,
        )

    except HTTPException:
        raise
    except Exception as e:
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))


# ── Health ────────────────────────────────────────────────────────────────────
@app.get("/health")
def health_check():
    return {"status": "healthy", "version": "2.0.0"}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
