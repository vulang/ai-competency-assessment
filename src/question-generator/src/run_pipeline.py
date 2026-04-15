"""
run_pipeline.py — Agentic Question Generation CLI

Usage:
  # Agentic pipeline (recommended):
  python -m src.run_pipeline \
      --plan configs/plan_example.yaml \
      --output output/questions.jsonl \
      --use-agents [--debug] [--concurrency 4]

  # Legacy pass-through (backward-compatible):
  python -m src.run_pipeline \
      --topic "AI Ethics" --count 5 --output output/out.jsonl
"""
from __future__ import annotations

import argparse
import asyncio
import json
import os
import sys
import time
from typing import Any

from dotenv import load_dotenv

load_dotenv()

# ── path setup ────────────────────────────────────────────────────────────────
current_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(current_dir)
sys.path.insert(0, project_root)   # question-generator/
sys.path.insert(0, current_dir)    # question-generator/src/ — needed for `import agents`


# ── helpers ───────────────────────────────────────────────────────────────────
def _load_config() -> dict:
    import yaml  # type: ignore
    cfg_path = os.path.join(project_root, "configs", "model.yaml")
    with open(cfg_path, "r", encoding="utf-8") as f:
        return yaml.safe_load(f)


def _load_prompt(filename: str) -> str:
    path = os.path.join(project_root, "prompts", filename)
    with open(path, "r", encoding="utf-8") as f:
        return f.read()


def _load_plan(plan_path: str) -> dict:
    import yaml  # type: ignore
    with open(plan_path, "r", encoding="utf-8") as f:
        return yaml.safe_load(f)


def _expand_plan(plan: dict) -> list[dict]:
    try:
        from src.plan import expand_plan as _ep
    except ImportError:
        from plan import expand_plan as _ep  # type: ignore
    return _ep(plan)


def _write_output(questions: list[dict], output_path: str):
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    with open(output_path, "w", encoding="utf-8") as f:
        for q in questions:
            f.write(json.dumps(q, ensure_ascii=False) + "\n")
    print(f"\n✅ Wrote {len(questions)} questions → {output_path}")


def _write_audit(job_results: list[Any], output_path: str):
    """Write full audit trail to a companion .audit.jsonl file."""
    audit_path = output_path.replace(".jsonl", ".audit.jsonl")
    with open(audit_path, "w", encoding="utf-8") as f:
        for jr in job_results:
            record = {
                "job_id": jr.job_id,
                "accepted": jr.accepted,
                "final_judge_score": jr.final_judge_score,
                "reject_reason": jr.reject_reason,
                "attempts": [
                    {
                        "attempt": a.attempt,
                        "question_id": a.question.get("id") if a.question else None,
                        "judge_score": a.judge.score if a.judge else None,
                        "judge_dims": a.judge.dimensions if a.judge else {},
                        "judge_passed": a.judge.passed if a.judge else None,
                        "rag_sources": a.rag_sources,
                        "web_search_used": a.web_search_used,
                        "elapsed_sec": a.elapsed_sec,
                    }
                    for a in jr.attempts
                ],
            }
            f.write(json.dumps(record, ensure_ascii=False) + "\n")
    print(f"📋 Audit trail → {audit_path}")


# ── agentic pipeline entry point ──────────────────────────────────────────────
async def run_agentic(
    plan_path: str,
    output_path: str,
    debug: bool = False,
    concurrency: int = 4,
):
    try:
        from agents import build_orchestrator_from_config  # type: ignore
    except ImportError:
        from src.agents import build_orchestrator_from_config  # type: ignore

    print("🚀 Agentic Question Generator starting…")
    config = _load_config()
    generator_prompt = _load_prompt("generator_vn.txt")
    judge_prompt = _load_prompt("judge_vn.txt")
    plan = _load_plan(plan_path)
    jobs = _expand_plan(plan)

    data_root = os.path.join(project_root, "data")
    index_path = os.path.join(project_root, ".cache", "rag_index")

    orchestrator = build_orchestrator_from_config(
        config=config,
        prompt_tmpl=generator_prompt,
        judge_prompt_tmpl=judge_prompt,
        data_root=data_root,
        index_path=index_path,
    )

    t0 = time.perf_counter()
    result = await orchestrator.run(jobs, debug=debug, concurrency=concurrency)
    elapsed = time.perf_counter() - t0

    # Summary
    print(f"\n─────────────────────────────")
    print(f"  Jobs total : {result.total_jobs}")
    print(f"  Accepted   : {result.accepted} ✅")
    print(f"  Rejected   : {result.rejected} ❌")
    print(f"  Attempts   : {result.total_attempts}")
    print(f"  Time       : {elapsed:.1f}s")
    print(f"─────────────────────────────")

    _write_output(result.questions, output_path)
    _write_audit(result.job_results, output_path)


# ── legacy entry point (backward compat) ──────────────────────────────────────
async def run_legacy(topic: str, count: int, output_path: str):
    """Simple passthrough for backward-compatible --topic / --count usage."""
    # Build a minimal plan and run through agentic pipeline
    plan = {
        "total": count,
        "mix": [
            {
                "group": "NhanThucAI",
                "level": "NenTang",
                "type": "mcq_single",
                "difficulty": [1, 2],
                "topic": topic,
                "count": count,
            }
        ],
    }
    config = _load_config()
    generator_prompt = _load_prompt("generator_vn.txt")
    judge_prompt = _load_prompt("judge_vn.txt")

    try:
        from agents import build_orchestrator_from_config  # type: ignore
    except ImportError:
        from src.agents import build_orchestrator_from_config  # type: ignore

    data_root = os.path.join(project_root, "data")
    index_path = os.path.join(project_root, ".cache", "rag_index")

    orchestrator = build_orchestrator_from_config(
        config=config,
        prompt_tmpl=generator_prompt,
        judge_prompt_tmpl=judge_prompt,
        data_root=data_root,
        index_path=index_path,
    )

    def _expand_minimal(plan):
        jobs = []
        for item in plan.get("mix", []):
            for i in range(item.get("count", 1)):
                j = dict(item)
                j["id"] = f"job_{i+1}"
                j.pop("count", None)
                jobs.append(j)
        return jobs

    jobs = _expand_minimal(plan)
    result = await orchestrator.run(jobs, debug=False, concurrency=min(4, count))
    _write_output(result.questions, output_path)


# ── CLI ───────────────────────────────────────────────────────────────────────
async def main():
    parser = argparse.ArgumentParser(description="Agentic Question Generation Pipeline")

    # Agentic mode
    parser.add_argument("--plan", type=str, help="YAML plan file (agentic mode)")

    # Legacy mode / simple overrides
    parser.add_argument("--topic", type=str, help="Topic (legacy simple mode)")
    parser.add_argument("--count", type=int, default=5, help="Number of questions (legacy)")

    # Shared
    parser.add_argument("--output", type=str, required=True, help="Output JSONL file path")
    parser.add_argument("--use-agents", action="store_true", default=True,
                        help="Use agentic pipeline (default: True)")
    parser.add_argument("--debug", action="store_true", help="Verbose debug output")
    parser.add_argument("--concurrency", type=int, default=4,
                        help="Max concurrent jobs (default: 4)")

    args = parser.parse_args()

    if args.plan:
        await run_agentic(
            plan_path=args.plan,
            output_path=args.output,
            debug=args.debug,
            concurrency=args.concurrency,
        )
    elif args.topic:
        print(f"[legacy mode] Generating {args.count} questions for: {args.topic}")
        await run_legacy(topic=args.topic, count=args.count, output_path=args.output)
    else:
        parser.print_help()
        sys.exit(1)


if __name__ == "__main__":
    asyncio.run(main())
