from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict


LOG_DIR = Path(__file__).resolve().parent.parent / "logs"
LOG_FILE = LOG_DIR / "training_events.jsonl"


def _ensure_log_directory() -> None:
    LOG_DIR.mkdir(parents=True, exist_ok=True)


def log_event(event: Dict[str, Any]) -> None:
    """
    Append one training event to the session log.

    JSONL is used so every training event is stored as one
    independent JSON record.
    """

    _ensure_log_directory()

    record = {
        "timestamp": datetime.now(timezone.utc).isoformat(),
        **event,
    }

    with LOG_FILE.open(
        "a",
        encoding="utf-8",
    ) as file:
        file.write(
            json.dumps(
                record,
                ensure_ascii=False,
            )
            + "\n"
        )


def read_events() -> list[Dict[str, Any]]:
    """
    Read all recorded training events.
    """

    if not LOG_FILE.exists():
        return []

    events: list[Dict[str, Any]] = []

    with LOG_FILE.open(
        "r",
        encoding="utf-8",
    ) as file:

        for line in file:
            line = line.strip()

            if not line:
                continue

            try:
                events.append(json.loads(line))
            except json.JSONDecodeError:
                continue

    return events


def clear_events() -> None:
    """
    Clear the current training-event log.
    """

    if LOG_FILE.exists():
        LOG_FILE.unlink()