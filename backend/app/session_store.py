from __future__ import annotations

import json
import uuid
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Dict

from .learner_model import LearnerState
from .models import ExperimentCondition, LearnerEvent


@dataclass
class SessionRecord:
    participant_id: str
    condition: ExperimentCondition
    state: LearnerState


class SessionStore:
    def __init__(self, log_dir: str = "data") -> None:
        self.sessions: Dict[str, SessionRecord] = {}

        self.log_dir = Path(log_dir)
        self.log_dir.mkdir(
            parents=True,
            exist_ok=True,
        )

    # ---------------------------------------------------------
    # CREATE SESSION
    # ---------------------------------------------------------

    def create(
        self,
        participant_id: str,
        condition: ExperimentCondition,
    ) -> str:

        session_id = str(uuid.uuid4())

        self.sessions[session_id] = SessionRecord(
            participant_id=participant_id,
            condition=condition,
            state=LearnerState(),
        )

        self._append(
            session_id,
            {
                "type": "session_start",
                "participant_id": participant_id,
                "condition": condition.value,
                "initial_difficulty": 1,
            },
        )

        return session_id

    # ---------------------------------------------------------
    # GET SESSION
    # ---------------------------------------------------------

    def get(
        self,
        session_id: str,
    ) -> SessionRecord:

        if session_id not in self.sessions:
            raise KeyError(session_id)

        return self.sessions[session_id]

    # ---------------------------------------------------------
    # LOG LEARNER EVENT
    # ---------------------------------------------------------

    def log_event(
        self,
        event: LearnerEvent,
    ) -> None:

        session = self.get(event.session_id)

        # Record the event itself.
        event_payload = {
            "type": "learner_event",
            **event.model_dump(mode="json"),
        }

        self._append(
            event.session_id,
            event_payload,
        )

        # Record the learner's updated state after
        # processing the event.
        self.log_state(
            event.session_id,
            session.state,
        )

    # ---------------------------------------------------------
    # LOG AGENT PLAN
    # ---------------------------------------------------------

    def log_plan(
        self,
        session_id: str,
        plan: dict,
    ) -> None:

        session = self.get(session_id)

        payload = {
            "type": "agent_plan",
            **plan,
            "learner_difficulty": session.state.difficulty,
            "learner_skill_score": session.state.skill_score(),
            "engagement_proxy": session.state.engagement_proxy(),
        }

        self._append(
            session_id,
            payload,
        )

    # ---------------------------------------------------------
    # LOG LEARNER STATE
    # ---------------------------------------------------------

    def log_state(
        self,
        session_id: str,
        state: LearnerState,
    ) -> None:

        self._append(
            session_id,
            {
                "type": "learner_state",
                **state.learner_summary(),
            },
        )

    # ---------------------------------------------------------
    # LOG EXPERIMENT METRIC
    # ---------------------------------------------------------

    def log_metric(
        self,
        session_id: str,
        name: str,
        value: float,
    ) -> None:

        session = self.get(session_id)

        self._append(
            session_id,
            {
                "type": "metric",
                "name": name,
                "value": value,
                "condition": session.condition.value,
            },
        )

    # ---------------------------------------------------------
    # LOG SESSION END
    # ---------------------------------------------------------

    def log_session_end(
        self,
        session_id: str,
    ) -> None:

        session = self.get(session_id)

        self._append(
            session_id,
            {
                "type": "session_end",
                "participant_id": session.participant_id,
                "condition": session.condition.value,
                "final_difficulty": session.state.difficulty,
                "final_skill_score": session.state.skill_score(),
                "engagement_proxy": session.state.engagement_proxy(),
                "events_seen": session.state.events_seen,
            },
        )

    # ---------------------------------------------------------
    # LOW-LEVEL JSONL LOGGER
    # ---------------------------------------------------------

    def _append(
        self,
        session_id: str,
        payload: dict,
    ) -> None:

        payload = {
            "timestamp": datetime.now(
                timezone.utc
            ).isoformat(),

            "session_id": session_id,

            **payload,
        }

        log_file = (
            self.log_dir
            / f"{session_id}.jsonl"
        )

        with log_file.open(
            "a",
            encoding="utf-8",
        ) as f:

            f.write(
                json.dumps(
                    payload,
                    ensure_ascii=False,
                )
                + "\n"
            )