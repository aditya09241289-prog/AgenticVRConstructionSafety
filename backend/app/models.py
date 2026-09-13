from __future__ import annotations

from enum import Enum
from typing import Any, Dict, Optional
from pydantic import BaseModel, Field


class HazardType(str, Enum):
    FALL = "fall"
    STRUCK_BY = "struck_by"
    ELECTRICAL = "electrical"


class ExperimentCondition(str, Enum):
    ADAPTIVE = "adaptive"
    FIXED = "fixed"


class EventType(str, Enum):
    HAZARD_RESPONSE = "hazard_response"
    HAZARD_EXPOSURE = "hazard_exposure"
    INSTRUCTION_ACK = "instruction_ack"
    SCENARIO_COMPLETE = "scenario_complete"
    SESSION_END = "session_end"


class AgentAction(str, Enum):
    SPAWN_HAZARD = "spawn_hazard"
    SET_DIFFICULTY = "set_difficulty"
    SHOW_INSTRUCTION = "show_instruction"
    REPEAT_SCENARIO = "repeat_scenario"
    ADVANCE_SCENARIO = "advance_scenario"
    END_SESSION = "end_session"


class StartSessionRequest(BaseModel):
    participant_id: str = Field(min_length=1, max_length=100)
    condition: ExperimentCondition = ExperimentCondition.ADAPTIVE


class StartSessionResponse(BaseModel):
    session_id: str
    condition: ExperimentCondition
    first_plan: "AgentPlan"


class LearnerEvent(BaseModel):
    session_id: str
    event_type: EventType
    hazard_type: Optional[HazardType] = None
    success: Optional[bool] = None
    response_time_ms: Optional[int] = Field(default=None, ge=0)
    severity: int = Field(default=1, ge=1, le=5)
    metadata: Dict[str, Any] = Field(default_factory=dict)


class AgentPlan(BaseModel):
    action: AgentAction
    hazard_type: Optional[HazardType] = None
    difficulty: int = Field(default=1, ge=1, le=5)
    instruction: Optional[str] = None
    reason: str
    score: float = Field(ge=0.0, le=1.0)


class SessionSnapshot(BaseModel):
    session_id: str
    participant_id: str
    condition: ExperimentCondition
    difficulty: int
    events_seen: int
    skill_score: float
    engagement_proxy: float
    hazard_accuracy: Dict[str, float]
