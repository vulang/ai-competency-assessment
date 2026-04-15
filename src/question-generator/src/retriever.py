"""
retriever.py — ContextRetriever

Builds and queries a local FAISS vector index over:
  - data/contexts/*.md   (existing competency context files)
  - data/documents/*.pdf (drop folder for curriculum / regulatory PDFs)

The index is persisted to .cache/rag_index/ and rebuilt automatically when
any source file changes (mtime-based invalidation).
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import pickle
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional

from dotenv import load_dotenv

load_dotenv()

# ── optional heavy deps ──────────────────────────────────────────────────────
try:
    import numpy as np
    import faiss  # type: ignore
    FAISS_AVAILABLE = True
except ImportError:
    FAISS_AVAILABLE = False

try:
    import tiktoken
    _enc = tiktoken.get_encoding("cl100k_base")

    def _token_len(text: str) -> int:
        return len(_enc.encode(text))
except ImportError:
    def _token_len(text: str) -> int:  # type: ignore[misc]
        return len(text) // 4  # rough fallback

try:
    import pymupdf4llm  # type: ignore
    PYMUPDF4LLM_AVAILABLE = True
except ImportError:
    PYMUPDF4LLM_AVAILABLE = False
    try:
        import fitz  # type: ignore  (PyMuPDF raw)
        FITZ_AVAILABLE = True
    except ImportError:
        FITZ_AVAILABLE = False

try:
    from openai import OpenAI as _OpenAI
    OPENAI_AVAILABLE = True
except ImportError:
    OPENAI_AVAILABLE = False


# ── data structures ──────────────────────────────────────────────────────────
@dataclass
class Chunk:
    text: str
    source: str          # relative file path
    doc_type: str        # "md" | "pdf"
    page: Optional[int]  # PDF page number (1-indexed), None for md


@dataclass
class RetrievedChunk:
    text: str
    score: float         # cosine similarity 0-1 (higher = more relevant)
    source: str
    doc_type: str
    page: Optional[int]


# ── text extraction ───────────────────────────────────────────────────────────
def _extract_md(path: str) -> list[Chunk]:
    with open(path, "r", encoding="utf-8") as f:
        text = f.read()
    return [Chunk(text=text, source=path, doc_type="md", page=None)]


def _extract_pdf(path: str, max_pages: int = 100) -> list[Chunk]:
    chunks: list[Chunk] = []
    if PYMUPDF4LLM_AVAILABLE:
        try:
            pages = pymupdf4llm.to_markdown(path, show_progress=False)
            # pymupdf4llm returns a single string with page separators
            # Split by form-feed or by page marker
            page_texts = pages.split("\f") if "\f" in pages else [pages]
            for i, pt in enumerate(page_texts):
                if max_pages and i >= max_pages:
                    print(f"[retriever] PDF truncated at page {max_pages}: {path}")
                    break
                if pt.strip():
                    chunks.append(Chunk(text=pt, source=path, doc_type="pdf", page=i + 1))
        except Exception as e:
            print(f"[retriever] pymupdf4llm failed for {path}: {e}. Falling back to raw fitz.")
            chunks = _extract_pdf_raw(path, max_pages)
    elif FITZ_AVAILABLE:
        chunks = _extract_pdf_raw(path, max_pages)
    else:
        print(f"[retriever] WARNING: no PDF parser available for {path}. Install pymupdf4llm.")
    return chunks


def _extract_pdf_raw(path: str, max_pages: int = 100) -> list[Chunk]:
    import fitz  # type: ignore
    chunks: list[Chunk] = []
    doc = fitz.open(path)
    for i, page in enumerate(doc):
        if max_pages and i >= max_pages:
            print(f"[retriever] PDF truncated at page {max_pages}: {path}")
            break
        text = page.get_text("text")
        if text.strip():
            chunks.append(Chunk(text=text, source=path, doc_type="pdf", page=i + 1))
    doc.close()
    return chunks


# ── chunking ─────────────────────────────────────────────────────────────────
def _split_chunks(raw_chunks: list[Chunk], chunk_size: int, chunk_overlap: int) -> list[Chunk]:
    """Split text into overlapping token-window chunks."""
    out: list[Chunk] = []
    for rc in raw_chunks:
        words = rc.text.split()
        # Approximate by words when tiktoken unavailable
        pos = 0
        while pos < len(words):
            window = words[pos:pos + chunk_size * 3]  # over-fetch, trim by tokens
            segment = " ".join(window)
            # Trim to chunk_size tokens
            while _token_len(segment) > chunk_size and len(window) > 1:
                window = window[:-1]
                segment = " ".join(window)
            if segment.strip():
                out.append(Chunk(text=segment.strip(), source=rc.source,
                                 doc_type=rc.doc_type, page=rc.page))
            # Advance by (chunk_size - chunk_overlap) words (rough)
            step = max(1, len(window) - chunk_overlap)
            pos += step
    return out


# ── embedding ────────────────────────────────────────────────────────────────
def _embed(texts: list[str], client, model: str = "text-embedding-3-small") -> "np.ndarray":
    """Batch-embed texts and return float32 numpy array (N x dim)."""
    if not OPENAI_AVAILABLE:
        raise RuntimeError("[retriever] openai package not installed.")
    # OpenAI allows up to 2048 inputs per call; batch if needed
    vectors = []
    batch_size = 512
    for i in range(0, len(texts), batch_size):
        batch = texts[i: i + batch_size]
        resp = client.embeddings.create(model=model, input=batch)
        vectors.extend([d.embedding for d in resp.data])
    return np.array(vectors, dtype=np.float32)


# ── mtime fingerprint ─────────────────────────────────────────────────────────
def _file_sig(paths: list[str]) -> str:
    """Return a digest of all file mtimes to detect changes."""
    h = hashlib.md5()
    for p in sorted(paths):
        try:
            h.update(f"{p}:{os.path.getmtime(p):.3f}".encode())
        except FileNotFoundError:
            pass
    return h.hexdigest()


# ── ContextRetriever ─────────────────────────────────────────────────────────
class ContextRetriever:
    def __init__(
        self,
        data_root: str,
        index_path: str,
        embedding_model: str = "text-embedding-3-small",
        chunk_size: int = 400,
        chunk_overlap: int = 80,
        max_pdf_pages: int = 100,
        api_key: str | None = None,
        base_url: str | None = None,
    ):
        if not FAISS_AVAILABLE:
            raise RuntimeError(
                "[retriever] faiss-cpu and numpy are required. "
                "Install with: pip install faiss-cpu numpy"
            )
        self.data_root = data_root
        self.index_path = index_path
        self.embedding_model = embedding_model
        self.chunk_size = chunk_size
        self.chunk_overlap = chunk_overlap
        self.max_pdf_pages = max_pdf_pages

        _api_key = api_key or os.getenv("OPENAI_API_KEY", "EMPTY")
        _base_url = base_url or os.getenv("OPENAI_BASE_URL", "https://api.openai.com/v1")
        if not _base_url.endswith("/v1"):
            _base_url = _base_url.rstrip("/") + "/v1"
        self._client = _OpenAI(api_key=_api_key, base_url=_base_url)

        self._index: Optional["faiss.IndexFlatIP"] = None
        self._chunks: list[Chunk] = []
        self._ensure_index()

    # ── source discovery ──────────────────────────────────────────────────────
    def _source_paths(self) -> list[str]:
        paths: list[str] = []
        ctx_dir = os.path.join(self.data_root, "contexts")
        doc_dir = os.path.join(self.data_root, "documents")
        if os.path.isdir(ctx_dir):
            paths += [str(p) for p in Path(ctx_dir).glob("*.md")]
        if os.path.isdir(doc_dir):
            paths += [str(p) for p in Path(doc_dir).glob("*.pdf")]
        return paths

    # ── index lifecycle ───────────────────────────────────────────────────────
    def _sig_path(self) -> str:
        return os.path.join(self.index_path, "sig.txt")

    def _chunks_path(self) -> str:
        return os.path.join(self.index_path, "chunks.pkl")

    def _faiss_path(self) -> str:
        return os.path.join(self.index_path, "index.faiss")

    def _ensure_index(self):
        sources = self._source_paths()
        current_sig = _file_sig(sources)

        # Load cached index if signature matches
        if (
            os.path.exists(self._sig_path())
            and os.path.exists(self._faiss_path())
            and os.path.exists(self._chunks_path())
        ):
            with open(self._sig_path()) as f:
                cached_sig = f.read().strip()
            if cached_sig == current_sig:
                self._index = faiss.read_index(self._faiss_path())
                with open(self._chunks_path(), "rb") as f:
                    self._chunks = pickle.load(f)
                print(f"[retriever] Loaded cached index ({len(self._chunks)} chunks).")
                return

        # Build fresh index
        print(f"[retriever] Building index from {len(sources)} source file(s)…")
        raw: list[Chunk] = []
        for src in sources:
            ext = os.path.splitext(src)[1].lower()
            if ext == ".md":
                raw += _extract_md(src)
            elif ext == ".pdf":
                raw += _extract_pdf(src, self.max_pdf_pages)

        self._chunks = _split_chunks(raw, self.chunk_size, self.chunk_overlap)
        if not self._chunks:
            print("[retriever] WARNING: No content found to index.")
            self._index = faiss.IndexFlatIP(1536)  # dummy empty index
            return

        texts = [c.text for c in self._chunks]
        print(f"[retriever] Embedding {len(texts)} chunks…")
        vectors = _embed(texts, self._client, self.embedding_model)

        # L2-normalise for cosine similarity via inner product
        norms = np.linalg.norm(vectors, axis=1, keepdims=True)
        norms[norms == 0] = 1
        vectors = vectors / norms

        dim = vectors.shape[1]
        self._index = faiss.IndexFlatIP(dim)
        self._index.add(vectors)

        # Persist
        os.makedirs(self.index_path, exist_ok=True)
        faiss.write_index(self._index, self._faiss_path())
        with open(self._chunks_path(), "wb") as f:
            pickle.dump(self._chunks, f)
        with open(self._sig_path(), "w") as f:
            f.write(current_sig)
        print(f"[retriever] Index built and saved ({len(self._chunks)} chunks).")

    # ── public API ────────────────────────────────────────────────────────────
    def retrieve(self, query: str, top_k: int = 5) -> list[RetrievedChunk]:
        if self._index is None or self._index.ntotal == 0:
            return []
        q_vec = _embed([query], self._client, self.embedding_model)
        norm = np.linalg.norm(q_vec)
        if norm > 0:
            q_vec = q_vec / norm
        scores, indices = self._index.search(q_vec, min(top_k, self._index.ntotal))
        results: list[RetrievedChunk] = []
        for score, idx in zip(scores[0], indices[0]):
            if idx < 0:
                continue
            c = self._chunks[idx]
            results.append(
                RetrievedChunk(
                    text=c.text,
                    score=float(score),
                    source=c.source,
                    doc_type=c.doc_type,
                    page=c.page,
                )
            )
        return results

    def format_context(self, chunks: list[RetrievedChunk]) -> str:
        """Format chunks into a readable context block for the LLM prompt."""
        if not chunks:
            return "(Không có ngữ cảnh nội bộ phù hợp.)"
        lines: list[str] = []
        for i, c in enumerate(chunks, 1):
            label = c.source
            if c.page:
                label += f" (trang {c.page})"
            lines.append(f"[{i}] {label} (score={c.score:.2f}):\n{c.text.strip()}")
        return "\n\n".join(lines)


# ── CLI ───────────────────────────────────────────────────────────────────────
def main():
    ap = argparse.ArgumentParser(description="Test RAG retrieval")
    ap.add_argument("--query", required=True, help="Search query")
    ap.add_argument("--top-k", type=int, default=5)
    ap.add_argument("--data-root", default=None)
    ap.add_argument("--index-path", default=None)
    args = ap.parse_args()

    root_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    data_root = args.data_root or os.path.join(root_dir, "data")
    index_path = args.index_path or os.path.join(root_dir, ".cache", "rag_index")

    retriever = ContextRetriever(data_root=data_root, index_path=index_path)
    results = retriever.retrieve(args.query, top_k=args.top_k)

    if not results:
        print("No results found.")
        return
    for r in results:
        label = r.source
        if r.page:
            label += f" (p.{r.page})"
        print(f"\n[{r.score:.3f}] {label}")
        print(r.text[:300] + ("…" if len(r.text) > 300 else ""))


if __name__ == "__main__":
    main()
