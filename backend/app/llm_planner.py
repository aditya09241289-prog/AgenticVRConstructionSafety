from __future__ import annotations

import os
from typing import Optional

from pydantic import BaseModel, Field

from .learner_model import LearnerState
from .models import (
    AgentAction,
    AgentPlan,
    ExperimentCondition,
    HazardType,
    LearnerEvent,
)


# ============================================================
# LLM CONFIGURATION
# ============================================================

def llm_enabled() -> bool:
    return (
        os.getenv(
            "ENABLE_LLM_PLANNER",
            "false",
        ).lower()
        == "true"
    )


# ============================================================
# STRUCTURED LLM OUTPUT
# ============================================================

class LLMProposal(BaseModel):
    action: str = Field(
        description=(
            "One of: spawn_hazard, set_difficulty, "
            "show_instruction, repeat_scenario, "
            "advance_scenario, end_session."
        )
    )

    hazard_type: str = Field(
        description="One of: fall, struck_by, electrical."
    )

    difficulty: int = Field(
        ge=1,
        le=5,
    )

    instruction: Optional[str] = None

    reason: str

    score: float = Field(
        ge=0.0,
        le=1.0,
    )


# ============================================================
# SYSTEM PROMPT
# ============================================================

SYSTEM_PROMPT = """
You are the pedagogical reasoning agent for a
construction safety training simulator.

Recommend the next training action based on:

- learner skill
- recent success/failure
- hazard-specific performance
- latest learner event
- current training condition

Available hazards:
fall
struck_by
electrical

Available actions:
spawn_hazard
set_difficulty
show_instruction
repeat_scenario
advance_scenario
end_session

Rules:

- Difficulty must remain between 1 and 5.
- Never invent hazards.
- After failure, prefer remediation, repetition,
  or reduced difficulty.
- Repeated success can justify increased difficulty.
- High-severity failures require immediate remediation.
- Never recommend unsafe real-world behavior.
- Return only the requested structured recommendation.
"""


# ============================================================
# BUILD GEMINI CLIENT
# ============================================================

def _build_client():
    from google import genai

    api_key = os.getenv("GEMINI_API_KEY")

    if not api_key:
        raise RuntimeError(
            "GEMINI_API_KEY is not configured."
        )

    return genai.Client(
        api_key=api_key
    )


# ============================================================
# LLM PROPOSAL
# ============================================================

def propose_with_llm(
    state: LearnerState,
    event: LearnerEvent,
    condition: ExperimentCondition,
) -> Optional[AgentPlan]:

    if not llm_enabled():
        return None

    # Fixed condition remains deterministic.
    if condition == ExperimentCondition.FIXED:
        return None

    try:

        client = _build_client()

        model_name = os.getenv(
            "LLM_MODEL",
            "gemini-2.5-flash",
        )

        context = _build_context(
            state,
            event,
            condition,
        )

        prompt = (
            SYSTEM_PROMPT
            + "\n\n"
            + context
            + "\n\n"
            "Return a JSON object containing exactly these "
            "fields:\n"
            "action, hazard_type, difficulty, instruction, "
            "reason, score."
        )

        response = client.models.generate_content(
            model=model_name,
            contents=prompt,
            config={
                "response_mime_type": "application/json",
                "response_schema": LLMProposal,
            },
        )

        proposal = response.parsed

        if proposal is None:

            print(
                "Gemini planner returned no structured response."
            )

            return None

        return _safety_gate(
            proposal,
            state,
            event,
            condition,
        )

    except Exception as exc:

        print(
            f"Gemini planner unavailable: {exc}"
        )

        return None


# ============================================================
# BUILD CONTEXT
# ============================================================

def _build_context(
    state: LearnerState,
    event: LearnerEvent,
    condition: ExperimentCondition,
) -> str:

    fall_accuracy = state.hazard_accuracy(
        HazardType.FALL
    )

    struck_accuracy = state.hazard_accuracy(
        HazardType.STRUCK_BY
    )

    electrical_accuracy = state.hazard_accuracy(
        HazardType.ELECTRICAL
    )

    latest_hazard = (
        event.hazard_type.value
        if event.hazard_type
        else "none"
    )

    return f"""
Training condition:
{condition.value}

Current difficulty:
{state.difficulty}

Events observed:
{state.events_seen}

Current skill score:
{state.skill_score():.3f}

Engagement proxy:
{state.engagement_proxy():.3f}

Success streak:
{state.recent_success_streak}

Failure streak:
{state.recent_failure_streak}

Hazard mastery:

fall:
{fall_accuracy:.3f}

struck_by:
{struck_accuracy:.3f}

electrical:
{electrical_accuracy:.3f}

Latest learner event:

event_type:
{event.event_type.value}

hazard_type:
{latest_hazard}

success:
{event.success}

response_time_ms:
{event.response_time_ms}

severity:
{event.severity}

Recommend the next pedagogical action.
"""


# ============================================================
# DETERMINISTIC SAFETY GATE
# ============================================================

def _safety_gate(
    proposal: LLMProposal,
    state: LearnerState,
    event: LearnerEvent,
    condition: ExperimentCondition,
) -> Optional[AgentPlan]:

    # --------------------------------------------------------
    # Valid actions
    # --------------------------------------------------------

    valid_actions = {
        AgentAction.SPAWN_HAZARD.value,
        AgentAction.SET_DIFFICULTY.value,
        AgentAction.SHOW_INSTRUCTION.value,
        AgentAction.REPEAT_SCENARIO.value,
        AgentAction.ADVANCE_SCENARIO.value,
        AgentAction.END_SESSION.value,
    }

    if proposal.action not in valid_actions:
        return None

    # --------------------------------------------------------
    # Valid hazards
    # --------------------------------------------------------

    valid_hazards = {
        HazardType.FALL.value:
            HazardType.FALL,

        HazardType.STRUCK_BY.value:
            HazardType.STRUCK_BY,

        HazardType.ELECTRICAL.value:
            HazardType.ELECTRICAL,
    }

    if proposal.hazard_type not in valid_hazards:
        return None

    hazard = valid_hazards[
        proposal.hazard_type
    ]

    # --------------------------------------------------------
    # Bound difficulty
    # --------------------------------------------------------

    difficulty = max(
        1,
        min(
            5,
            proposal.difficulty,
        ),
    )

    # --------------------------------------------------------
    # Never increase difficulty after failure
    # --------------------------------------------------------

    if event.success is False:

        difficulty = min(
            difficulty,
            state.difficulty,
        )

    # --------------------------------------------------------
    # High-severity failure
    # --------------------------------------------------------

    if (
        event.success is False
        and event.severity >= 4
    ):

        return AgentPlan(
            action=AgentAction.SHOW_INSTRUCTION,

            hazard_type=(
                event.hazard_type
                or hazard
            ),

            difficulty=max(
                1,
                state.difficulty - 1,
            ),

            instruction=(
                "Stop and reassess the hazard. "
                "Review the correct safety procedure "
                "before attempting the scenario again."
            ),

            reason=(
                "Safety gate converted the Gemini proposal "
                "into mandatory remediation after a "
                "high-severity failure."
            ),

            score=proposal.score,
        )

    # --------------------------------------------------------
    # Failure + difficulty increase
    # --------------------------------------------------------

    if (
        event.success is False
        and proposal.action
        == AgentAction.SET_DIFFICULTY.value
    ):

        return AgentPlan(
            action=AgentAction.SHOW_INSTRUCTION,

            hazard_type=(
                event.hazard_type
                or hazard
            ),

            difficulty=max(
                1,
                state.difficulty - 1,
            ),

            instruction=(
                "Review the hazard response and try "
                "the scenario again."
            ),

            reason=(
                "Safety gate prevented a difficulty increase "
                "after an unsuccessful response."
            ),

            score=proposal.score,
        )

    # --------------------------------------------------------
    # Safe fallback instruction
    # --------------------------------------------------------

    instruction = proposal.instruction

    if (
        event.success is False
        and not instruction
    ):

        instruction = (
            "Review the hazard and choose the safest "
            "available response before continuing."
        )

    # --------------------------------------------------------
    # Final validated plan
    # --------------------------------------------------------

    try:

        action = AgentAction(
            proposal.action
        )

    except ValueError:

        return None

    return AgentPlan(
        action=action,

        hazard_type=hazard,

        difficulty=difficulty,

        instruction=instruction,

        reason=(
            "Gemini pedagogical recommendation accepted "
            "by the deterministic safety gate. "
            + proposal.reason
        ),

        score=max(
            0.0,
            min(
                1.0,
                proposal.score,
            ),
        ),
    )