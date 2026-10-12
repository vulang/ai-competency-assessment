"""Canonical question-bank taxonomy; labels are read from the shared framework seed."""
import json
from pathlib import Path

FRAMEWORK_PATH = Path(__file__).resolve().parents[3] / "data" / "frameworks" / "cv-v1-draft.yaml"
FRAMEWORK = json.loads(FRAMEWORK_PATH.read_text(encoding="utf-8"))
GROUPS = {group["code"]: group for group in FRAMEWORK["groups"]}
DISPLAY_NAMES = {
    "FOUNDATION": "Fundamental", "DATA": "Data", "CRITICAL_THINKING": "Critical Thinking",
    "APPLICATION": "AI Use Cases", "ETHICS": "AI Ethics", "TOOLS": "AI Tools",
    "FUTURE_OF_WORK": "Future of Work",
}
LEVELS = {level["nameEn"]: level["ordinal"] for level in FRAMEWORK["levels"] if level["ordinal"] > 0}


def group_code(value: str) -> str:
    if value in GROUPS:
        return value
    for code, display in DISPLAY_NAMES.items():
        if value == display:
            return code
    raise ValueError(f"Unknown competency group: {value}")


def level_name(value: str) -> str:
    if value in LEVELS:
        return value
    name = next((name for name, ordinal in LEVELS.items() if value == name.upper()), None)
    if name:
        return name
    raise ValueError(f"Unknown positive competency level: {value}")
