"""
agents.py — Agentic Question Generation Pipeline

Three components:
  - GeneratorAgent  : enriches context via RAG + web search, then calls LLM to produce a question
  - JudgeAgent      : evaluates the question against 4 scored dimensions (LLM-as-a-Judge)
  - AgenticOrchestrator : drives the per-job retry loop with full audit trail

Domain ID prefix mapping (from Confluence Question Bank spec):
  NhanThucAI       → QF-
  KyThuatUngDung   → QP-
  DanhGiaChatLuong → QE-
  DaoDucQuanTri    → QP-ETH-
  ThietKeHeThong   → QW-
"""
from __future__ import annotations

import asyncio
import json
import os
import re
import time
import traceback
from dataclasses import dataclass, field
from typing import Any, Optional

from dotenv import load_dotenv

load_dotenv()

# ── Try importing OpenAI async client ─────────────────────────────────────────
try:
    from openai import AsyncOpenAI
    OPENAI_ASYNC_AVAILABLE = True
except ImportError:
    OPENAI_ASYNC_AVAILABLE = False

try:
    from .retriever import ContextRetriever, RetrievedChunk
    from .web_search import WebSearchTool, SearchResult
except ImportError:
    from retriever import ContextRetriever, RetrievedChunk
    from web_search import WebSearchTool, SearchResult


# ── Domain ID prefix mapping ──────────────────────────────────────────────────
DOMAIN_ID_PREFIX: dict[str, str] = {
    "NhanThucAI":       "QF-",
    "KyThuatUngDung":   "QP-",
    "DanhGiaChatLuong": "QE-",
    "DaoDucQuanTri":    "QP-ETH-",
    "ThietKeHeThong":   "QW-",
}


def _make_question_id(group: str, counter: int) -> str:
    prefix = DOMAIN_ID_PREFIX.get(group, "Q-")
    return f"{prefix}{counter:03d}"


# ── Data models ───────────────────────────────────────────────────────────────
@dataclass
class JudgeResult:
    passed: bool
    score: float
    dimensions: dict[str, float]
    feedback: str


@dataclass
class QuestionAttempt:
    attempt: int
    question: dict[str, Any]
    judge: Optional[JudgeResult]
    rag_sources: list[dict]
    web_search_used: bool
    elapsed_sec: float


@dataclass
class JobResult:
    job_id: str
    accepted: bool
    question: Optional[dict[str, Any]]
    attempts: list[QuestionAttempt] = field(default_factory=list)
    final_judge_score: float = 0.0
    reject_reason: str = ""


@dataclass
class PipelineResult:
    questions: list[dict[str, Any]]
    job_results: list[JobResult]
    total_jobs: int
    accepted: int
    rejected: int
    total_attempts: int
    elapsed_sec: float


# ── Helper: create async OpenAI client ───────────────────────────────────────
def _make_async_client(base_url: str, api_key: str) -> "AsyncOpenAI":
    if not OPENAI_ASYNC_AVAILABLE:
        raise RuntimeError(
            "openai>=1.0.0 with async support is required. pip install openai"
        )
    url = base_url if base_url.endswith("/v1") else (base_url.rstrip("/") + "/v1")
    return AsyncOpenAI(api_key=api_key, base_url=url)


def _extract_json(text: str) -> Optional[dict]:
    """Extract the first JSON object from an LLM response string."""
    # Try direct parse first
    try:
        return json.loads(text.strip())
    except Exception:
        pass
    # Try extracting from markdown code blocks
    m = re.search(r"```(?:json)?\s*(\{.*?\})\s*```", text, re.S)
    if m:
        try:
            return json.loads(m.group(1))
        except Exception:
            pass
    # Greedy search for any JSON object
    m = re.search(r"\{.*\}", text, re.S)
    if m:
        try:
            return json.loads(m.group(0))
        except Exception:
            pass
    return None


# ── GeneratorAgent ────────────────────────────────────────────────────────────
class GeneratorAgent:
    def __init__(
        self,
        client: "AsyncOpenAI",
        model: str,
        prompt_tmpl: str,
        retriever: Optional[ContextRetriever],
        web_search: Optional[WebSearchTool],
        rag_top_k: int = 5,
        rag_min_score: float = 0.25,
        temperature: float = 0.8,
        max_tokens: int = 900,
    ):
        self.client = client
        self.model = model
        self.prompt_tmpl = prompt_tmpl
        self.retriever = retriever
        self.web_search = web_search
        self.rag_top_k = rag_top_k
        self.rag_min_score = rag_min_score
        self.temperature = temperature
        self.max_tokens = max_tokens

    async def generate(
        self,
        job: dict,
        question_id: str,
        feedback: Optional[str] = None,
        debug: bool = False,
    ) -> tuple[Optional[dict], list[dict], bool]:
        """
        Returns: (question_dict_or_None, rag_sources, web_search_used)
        """
        # Step 1 — RAG retrieval
        rag_chunks: list[RetrievedChunk] = []
        rag_sources: list[dict] = []
        rag_context = "(Không có ngữ cảnh nội bộ.)"
        web_context = "(Không có thông tin bổ sung từ web.)"
        web_used = False

        if self.retriever:
            query = f"{job.get('group', '')} {job.get('topic', '')} {job.get('level', '')}"
            rag_chunks = self.retriever.retrieve(query, top_k=self.rag_top_k)
            if rag_chunks:
                rag_context = self.retriever.format_context(rag_chunks)
                rag_sources = [
                    {
                        "source": c.source,
                        "doc_type": c.doc_type,
                        "page": c.page,
                        "score": round(c.score, 3),
                    }
                    for c in rag_chunks
                ]

        # Step 2 — Web search fallback
        best_rag_score = max((c.score for c in rag_chunks), default=0.0)
        if self.web_search and self.web_search.enabled and best_rag_score < self.rag_min_score:
            search_query = f"{job.get('topic', '')} AI competency {job.get('group', '')}"
            results = self.web_search.search(search_query)
            if results:
                web_context = self.web_search.format_context(results)
                web_used = True
                if debug:
                    print(f"[generator] Web search triggered (RAG score={best_rag_score:.2f}): {search_query}")

        # Step 3 — Build feedback section (only on retries)
        feedback_section = ""
        if feedback:
            feedback_section = (
                f"\n## Phản hồi từ Giám khảo (lần thử trước chưa đạt)\n"
                f"Vui lòng cải thiện câu hỏi theo hướng dẫn sau:\n{feedback}\n"
                f"QUAN TRỌNG: Hãy tạo lại câu hỏi, không chỉ sửa nhỏ."
            )

        # Step 4 — Fill prompt template
        difficulty = job.get("difficulty", 1)
        if isinstance(difficulty, list):
            import random
            difficulty = random.choice(difficulty)

        prompt = self.prompt_tmpl
        for key, value in {
            "group": job.get("group", ""),
            "level": job.get("level", ""),
            "type": job.get("type", "mcq_single"),
            "difficulty": str(difficulty),
            "topic": job.get("topic", ""),
            "question_id": question_id,
            "rag_context": rag_context,
            "web_context": web_context,
            "feedback_section": feedback_section,
        }.items():
            prompt = prompt.replace("{" + key + "}", str(value))

        messages = [
            {
                "role": "system",
                "content": "Bạn là chuyên gia khảo thí AI, sinh câu hỏi theo schema JSON được yêu cầu.",
            },
            {"role": "user", "content": prompt},
        ]

        # Step 5 — Call LLM
        try:
            resp = await self.client.chat.completions.create(
                model=self.model,
                messages=messages,
                temperature=self.temperature,
                max_tokens=self.max_tokens,
            )
            raw = resp.choices[0].message.content or ""
            if debug:
                print(f"[generator] RAW ({len(raw)} chars):", raw[:500])
            obj = _extract_json(raw)
            if not obj:
                print(f"[generator] {question_id} — no valid JSON in response.")
                return None, rag_sources, web_used

            # Enforce required fields
            obj["id"] = question_id
            obj["status"] = "draft"
            obj.setdefault("lang", "vi")
            obj.setdefault("group", job.get("group", ""))
            obj.setdefault("level", job.get("level", ""))
            obj.setdefault("type", job.get("type", "mcq_single"))
            obj.setdefault("difficulty", difficulty)
            obj.setdefault("options", [])
            obj.setdefault("answer", [])

            # Normalise answer to list
            if isinstance(obj["answer"], str):
                obj["answer"] = [obj["answer"]]

            # MCQ cleanup — strip full-text from answers, keep only letter keys
            if obj["type"] in ("mcq_single", "mcq_multi"):
                cleaned = []
                for a in obj["answer"]:
                    a_str = str(a).strip()
                    m = re.match(r"^([A-D])[.\s:]", a_str, re.IGNORECASE)
                    if m:
                        cleaned.append(m.group(1).upper())
                    elif re.match(r"^[A-D]$", a_str, re.IGNORECASE):
                        cleaned.append(a_str.upper())
                    else:
                        cleaned.append(a_str)
                # Deduplicate
                seen: set[str] = set()
                dedup = [x for x in cleaned if not (x in seen or seen.add(x))]  # type: ignore
                obj["answer"] = dedup
                if obj["type"] == "mcq_single" and len(obj["answer"]) > 1:
                    obj["answer"] = [obj["answer"][0]]

            # Attach source info from RAG
            if rag_sources:
                obj["source_context"] = rag_sources[0]["source"]

            return obj, rag_sources, web_used

        except Exception as e:
            print(f"[generator] {question_id} error: {e}")
            if debug:
                traceback.print_exc()
            return None, rag_sources, web_used


# ── JudgeAgent ────────────────────────────────────────────────────────────────
class JudgeAgent:
    def __init__(
        self,
        client: "AsyncOpenAI",
        model: str,
        judge_prompt_tmpl: str,
        pass_threshold: float = 0.75,
        temperature: float = 0.2,
        max_tokens: int = 700,
    ):
        self.client = client
        self.model = model
        self.judge_prompt_tmpl = judge_prompt_tmpl
        self.pass_threshold = pass_threshold
        self.temperature = temperature
        self.max_tokens = max_tokens

    async def evaluate(
        self,
        question: dict,
        job: dict,
        debug: bool = False,
    ) -> JudgeResult:
        prompt = self.judge_prompt_tmpl.replace(
            "{pass_threshold}", str(self.pass_threshold)
        ).replace(
            "{job_json}", json.dumps(job, ensure_ascii=False, indent=2)
        ).replace(
            "{question_json}", json.dumps(question, ensure_ascii=False, indent=2)
        )

        messages = [
            {
                "role": "system",
                "content": "Bạn là giám khảo chuyên nghiệp. Chỉ trả về JSON đánh giá theo yêu cầu.",
            },
            {"role": "user", "content": prompt},
        ]

        try:
            resp = await self.client.chat.completions.create(
                model=self.model,
                messages=messages,
                temperature=self.temperature,
                max_tokens=self.max_tokens,
            )
            raw = resp.choices[0].message.content or ""
            if debug:
                print(f"[judge] RAW:", raw[:500])
            obj = _extract_json(raw)
            if not obj:
                print(f"[judge] No valid JSON in judge response.")
                return JudgeResult(passed=False, score=0.0,
                                   dimensions={}, feedback="Phản hồi giám khảo không hợp lệ.")

            score = float(obj.get("score", 0.0))
            dims = obj.get("dimensions", {})
            # Recompute score from dimensions if present
            if dims:
                valid_scores = [float(v) for v in dims.values() if isinstance(v, (int, float))]
                if valid_scores:
                    score = sum(valid_scores) / len(valid_scores)

            passed = score >= self.pass_threshold
            return JudgeResult(
                passed=passed,
                score=round(score, 3),
                dimensions={k: round(float(v), 3) for k, v in dims.items()},
                feedback=obj.get("feedback", ""),
            )

        except Exception as e:
            print(f"[judge] evaluation error: {e}")
            if debug:
                traceback.print_exc()
            return JudgeResult(passed=False, score=0.0,
                               dimensions={}, feedback=f"Lỗi giám khảo: {e}")


# ── AgenticOrchestrator ───────────────────────────────────────────────────────
class AgenticOrchestrator:
    def __init__(
        self,
        generator: GeneratorAgent,
        judge: JudgeAgent,
        max_retries: int = 3,
    ):
        self.generator = generator
        self.judge = judge
        self.max_retries = max_retries
        # Per-domain ID counters
        self._id_counters: dict[str, int] = {}

    def _next_id(self, group: str) -> str:
        self._id_counters[group] = self._id_counters.get(group, 0) + 1
        return _make_question_id(group, self._id_counters[group])

    async def _process_job(self, job: dict, debug: bool = False) -> JobResult:
        job_id = job.get("id", "unknown")
        group = job.get("group", "")
        question_id = self._next_id(group)
        result = JobResult(job_id=job_id, accepted=False, question=None)

        feedback: Optional[str] = None
        for attempt in range(1, self.max_retries + 2):  # max_retries+1 total attempts
            t0 = time.perf_counter()
            if debug:
                print(f"\n[orchestrator] Job {job_id} | attempt {attempt}/{self.max_retries+1}")

            # Generate
            question, rag_sources, web_used = await self.generator.generate(
                job, question_id, feedback=feedback, debug=debug
            )

            # Judge
            judge_result: Optional[JudgeResult] = None
            if question:
                judge_result = await self.judge.evaluate(question, job, debug=debug)

            elapsed = time.perf_counter() - t0
            result.attempts.append(
                QuestionAttempt(
                    attempt=attempt,
                    question=question or {},
                    judge=judge_result,
                    rag_sources=rag_sources,
                    web_search_used=web_used,
                    elapsed_sec=round(elapsed, 2),
                )
            )

            if question and judge_result and judge_result.passed:
                # Promote status to "reviewed"
                question["status"] = "reviewed"
                result.accepted = True
                result.question = question
                result.final_judge_score = judge_result.score
                if debug:
                    print(f"[orchestrator] ✅ {question_id} accepted (score={judge_result.score:.3f})")
                break

            # Prepare feedback for next attempt
            if judge_result and judge_result.feedback:
                feedback = judge_result.feedback
                if debug:
                    print(f"[orchestrator] ❌ score={judge_result.score:.3f} — retrying with feedback")
            elif not question:
                feedback = "Câu hỏi trước không sinh được JSON hợp lệ. Hãy tuân thủ nghiêm ngặt schema."
                if debug:
                    print(f"[orchestrator] ❌ No valid question — retrying")

        if not result.accepted:
            last_score = result.attempts[-1].judge.score if result.attempts[-1].judge else 0.0
            result.reject_reason = (
                f"Vượt quá số lần thử ({self.max_retries + 1}). "
                f"Điểm cuối: {last_score:.3f}"
            )
            if debug:
                print(f"[orchestrator] ❌ {question_id} rejected: {result.reject_reason}")

        return result

    async def run(
        self,
        jobs: list[dict],
        debug: bool = False,
        concurrency: int = 4,
    ) -> PipelineResult:
        t0 = time.perf_counter()
        semaphore = asyncio.Semaphore(concurrency)

        async def _bounded(job):
            async with semaphore:
                return await self._process_job(job, debug=debug)

        job_results = await asyncio.gather(*[_bounded(j) for j in jobs])
        elapsed = time.perf_counter() - t0

        accepted = [r for r in job_results if r.accepted]
        questions = [r.question for r in accepted if r.question]
        total_attempts = sum(len(r.attempts) for r in job_results)

        return PipelineResult(
            questions=questions,
            job_results=list(job_results),
            total_jobs=len(jobs),
            accepted=len(accepted),
            rejected=len(jobs) - len(accepted),
            total_attempts=total_attempts,
            elapsed_sec=round(elapsed, 2),
        )


# ── Factory: build orchestrator from config ──────────────────────────────────
def build_orchestrator_from_config(
    config: dict,
    prompt_tmpl: str,
    judge_prompt_tmpl: str,
    data_root: str,
    index_path: str,
) -> AgenticOrchestrator:
    gen_cfg = config.get("generator", {})
    judge_cfg = config.get("judge", {})
    rag_cfg = config.get("rag", {})
    ws_cfg = config.get("web_search", {})
    loop_cfg = config.get("agentic", {})

    # Generator client
    gen_base = gen_cfg.get("base_url") or os.getenv("OPENAI_BASE_URL", "https://api.openai.com/v1")
    gen_key = os.getenv(gen_cfg.get("api_key_env", "OPENAI_API_KEY"), "EMPTY")
    gen_client = _make_async_client(gen_base, gen_key)

    # Judge client (may be same or different provider)
    judge_base_raw = judge_cfg.get("base_url") or ""
    judge_base = judge_base_raw if judge_base_raw else gen_base
    judge_key = os.getenv(judge_cfg.get("api_key_env", "OPENAI_API_KEY"), gen_key)
    judge_client = _make_async_client(judge_base, judge_key)

    # Retriever
    retriever: Optional[ContextRetriever] = None
    if rag_cfg.get("enabled", True):
        try:
            retriever = ContextRetriever(
                data_root=data_root,
                index_path=index_path,
                embedding_model=rag_cfg.get("embedding_model", "text-embedding-3-small"),
                chunk_size=rag_cfg.get("chunk_size", 400),
                chunk_overlap=rag_cfg.get("chunk_overlap", 80),
                max_pdf_pages=rag_cfg.get("max_pdf_pages", 100),
                api_key=gen_key,
                base_url=gen_base,
            )
        except Exception as e:
            print(f"[agents] WARNING: Could not initialise retriever: {e}. Proceeding without RAG.")

    # Web search
    web_search = WebSearchTool(
        provider=ws_cfg.get("provider", "duckduckgo"),
        max_results=ws_cfg.get("max_results", 3),
        timeout=ws_cfg.get("timeout_sec", 10),
        enabled=ws_cfg.get("enabled", True),
    )

    generator = GeneratorAgent(
        client=gen_client,
        model=gen_cfg.get("model", "gpt-4o-mini"),
        prompt_tmpl=prompt_tmpl,
        retriever=retriever,
        web_search=web_search,
        rag_top_k=rag_cfg.get("top_k", 5),
        rag_min_score=rag_cfg.get("min_score", 0.25),
        temperature=gen_cfg.get("temperature", 0.8),
        max_tokens=gen_cfg.get("max_tokens", 900),
    )

    judge = JudgeAgent(
        client=judge_client,
        model=judge_cfg.get("model", "gpt-4o"),
        judge_prompt_tmpl=judge_prompt_tmpl,
        pass_threshold=loop_cfg.get("pass_threshold", 0.75),
        temperature=judge_cfg.get("temperature", 0.2),
        max_tokens=judge_cfg.get("max_tokens", 700),
    )

    return AgenticOrchestrator(
        generator=generator,
        judge=judge,
        max_retries=loop_cfg.get("max_retries", 3),
    )
