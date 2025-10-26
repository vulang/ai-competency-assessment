import json
from types import SimpleNamespace
from urllib import error, request

try:
    from openai import OpenAI as _OpenAI
except ImportError:
    _OpenAI = None


def _to_namespace(obj):
    if isinstance(obj, dict):
        return SimpleNamespace(**{k: _to_namespace(v) for k, v in obj.items()})
    if isinstance(obj, list):
        return [_to_namespace(item) for item in obj]
    return obj


class _ChatCompletions:
    def __init__(self, client):
        self._client = client

    def create(self, **payload):
        base = self._client.base_url.rstrip("/")
        endpoint = base + "/chat/completions"
        body = json.dumps(payload).encode("utf-8")
        headers = {"Content-Type": "application/json"}
        if self._client.api_key:
            headers["Authorization"] = f"Bearer {self._client.api_key}"
        req = request.Request(endpoint, data=body, headers=headers, method="POST")
        try:
            with request.urlopen(req, timeout=self._client.timeout) as resp:
                data = resp.read().decode("utf-8")
        except error.HTTPError as exc:
            detail = exc.read().decode("utf-8", "ignore")
            msg = f"OpenAI request failed ({exc.code}): {detail}"
            raise RuntimeError(msg) from exc
        except error.URLError as exc:
            raise RuntimeError(f"OpenAI request failed: {exc}") from exc
        payload = json.loads(data)
        return _to_namespace(payload)


class SimpleOpenAI:
    def __init__(self, base_url, api_key, timeout=60):
        if base_url.endswith("/"):
            base_url = base_url[:-1]
        self.base_url = base_url
        self.api_key = api_key
        self.timeout = timeout
        self.chat = SimpleNamespace(completions=_ChatCompletions(self))


def create_openai_client(base_url, api_key, timeout=60):
    if _OpenAI is not None:
        return _OpenAI(base_url=base_url, api_key=api_key, timeout=timeout)
    return SimpleOpenAI(base_url=base_url, api_key=api_key, timeout=timeout)
