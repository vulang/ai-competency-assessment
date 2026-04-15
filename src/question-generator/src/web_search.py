"""
web_search.py — WebSearchTool

Provider adapter for internet search fallback when RAG confidence is low.
Supported providers:
  - duckduckgo  : free, no API key (unofficial — may be rate-limited)
  - serper      : ~$0.30/1K queries, needs SERPER_API_KEY
  - serpapi     : ~$75+/month, needs SERPAPI_API_KEY
  - tavily      : ~$20/10K queries, needs TAVILY_API_KEY
"""
from __future__ import annotations

import json
import os
import urllib.request
import urllib.parse
from abc import ABC, abstractmethod
from dataclasses import dataclass
from typing import Optional


@dataclass
class SearchResult:
    title: str
    snippet: str
    url: str


class WebSearchProvider(ABC):
    @abstractmethod
    def search(self, query: str, max_results: int, timeout: int) -> list[SearchResult]:
        ...


# ── DuckDuckGo ───────────────────────────────────────────────────────────────
class DuckDuckGoProvider(WebSearchProvider):
    def search(self, query: str, max_results: int, timeout: int) -> list[SearchResult]:
        try:
            from duckduckgo_search import DDGS  # type: ignore

            results: list[SearchResult] = []
            with DDGS(timeout=timeout) as ddgs:
                for r in ddgs.text(query, max_results=max_results):
                    results.append(
                        SearchResult(
                            title=r.get("title", ""),
                            snippet=r.get("body", ""),
                            url=r.get("href", ""),
                        )
                    )
            return results
        except Exception as e:
            print(f"[web_search] DuckDuckGo error: {e}")
            return []


# ── Serper.dev ───────────────────────────────────────────────────────────────
class SerperProvider(WebSearchProvider):
    def __init__(self, api_key: str):
        self.api_key = api_key

    def search(self, query: str, max_results: int, timeout: int) -> list[SearchResult]:
        try:
            payload = json.dumps({"q": query, "num": max_results}).encode()
            req = urllib.request.Request(
                "https://google.serper.dev/search",
                data=payload,
                headers={
                    "X-API-KEY": self.api_key,
                    "Content-Type": "application/json",
                },
                method="POST",
            )
            with urllib.request.urlopen(req, timeout=timeout) as resp:
                data = json.loads(resp.read())
            results: list[SearchResult] = []
            for item in data.get("organic", [])[:max_results]:
                results.append(
                    SearchResult(
                        title=item.get("title", ""),
                        snippet=item.get("snippet", ""),
                        url=item.get("link", ""),
                    )
                )
            return results
        except Exception as e:
            print(f"[web_search] Serper error: {e}")
            return []


# ── SerpAPI ───────────────────────────────────────────────────────────────────
class SerpApiProvider(WebSearchProvider):
    def __init__(self, api_key: str):
        self.api_key = api_key

    def search(self, query: str, max_results: int, timeout: int) -> list[SearchResult]:
        try:
            params = urllib.parse.urlencode({
                "q": query,
                "api_key": self.api_key,
                "num": max_results,
                "engine": "google",
            })
            url = f"https://serpapi.com/search.json?{params}"
            req = urllib.request.Request(url)
            with urllib.request.urlopen(req, timeout=timeout) as resp:
                data = json.loads(resp.read())
            results: list[SearchResult] = []
            for item in data.get("organic_results", [])[:max_results]:
                results.append(
                    SearchResult(
                        title=item.get("title", ""),
                        snippet=item.get("snippet", ""),
                        url=item.get("link", ""),
                    )
                )
            return results
        except Exception as e:
            print(f"[web_search] SerpAPI error: {e}")
            return []


# ── Tavily ────────────────────────────────────────────────────────────────────
class TavilyProvider(WebSearchProvider):
    def __init__(self, api_key: str):
        self.api_key = api_key

    def search(self, query: str, max_results: int, timeout: int) -> list[SearchResult]:
        try:
            from tavily import TavilyClient  # type: ignore

            client = TavilyClient(api_key=self.api_key)
            resp = client.search(query=query, max_results=max_results)
            results: list[SearchResult] = []
            for item in resp.get("results", [])[:max_results]:
                results.append(
                    SearchResult(
                        title=item.get("title", ""),
                        snippet=item.get("content", ""),
                        url=item.get("url", ""),
                    )
                )
            return results
        except Exception as e:
            print(f"[web_search] Tavily error: {e}")
            return []


# ── Factory & Tool ────────────────────────────────────────────────────────────
def _create_provider(provider_name: str) -> WebSearchProvider:
    p = provider_name.lower()
    if p == "duckduckgo":
        return DuckDuckGoProvider()
    if p == "serper":
        key = os.getenv("SERPER_API_KEY", "")
        if not key:
            print("[web_search] SERPER_API_KEY not set, falling back to DuckDuckGo.")
            return DuckDuckGoProvider()
        return SerperProvider(api_key=key)
    if p == "serpapi":
        key = os.getenv("SERPAPI_API_KEY", "")
        if not key:
            print("[web_search] SERPAPI_API_KEY not set, falling back to DuckDuckGo.")
            return DuckDuckGoProvider()
        return SerpApiProvider(api_key=key)
    if p == "tavily":
        key = os.getenv("TAVILY_API_KEY", "")
        if not key:
            print("[web_search] TAVILY_API_KEY not set, falling back to DuckDuckGo.")
            return DuckDuckGoProvider()
        return TavilyProvider(api_key=key)
    print(f"[web_search] Unknown provider '{provider_name}', using DuckDuckGo.")
    return DuckDuckGoProvider()


class WebSearchTool:
    def __init__(self, provider: str = "duckduckgo", max_results: int = 3, timeout: int = 10, enabled: bool = True):
        self.enabled = enabled
        self.max_results = max_results
        self.timeout = timeout
        self._provider: Optional[WebSearchProvider] = None
        if enabled:
            self._provider = _create_provider(provider)

    def search(self, query: str) -> list[SearchResult]:
        if not self.enabled or self._provider is None:
            return []
        return self._provider.search(query, self.max_results, self.timeout)

    def format_context(self, results: list[SearchResult]) -> str:
        if not results:
            return "(Không tìm thấy thông tin bổ sung từ web.)"
        lines: list[str] = []
        for i, r in enumerate(results, 1):
            lines.append(f"[Web {i}] {r.title}\n{r.snippet}\nNguồn: {r.url}")
        return "\n\n".join(lines)
