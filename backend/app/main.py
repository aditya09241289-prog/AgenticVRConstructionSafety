from __future__ import annotations

from fastapi import FastAPI, HTTPException

from .models import (
    AgentPlan,
    HazardType,
    LearnerEvent,
    SessionSnapshot,
    StartSessionRequest,
    StartSessionResponse,
)

from .policy import PedagogicalPolicy
from .session_store import SessionStore
from .fall_detection_service import run_fall_detection
from .llm_planner import (
    llm_enabled,
    propose_with_llm,
)
from .session_logger import log_event


app = FastAPI(
    title="Agentic VR Construction Safety Backend",
    version="0.1.0",
    description="Adaptive agent backend for Unity construction-safety training.",
)


store = SessionStore()
policy = PedagogicalPolicy()


@app.get("/health")
def health() -> dict:
    return {
        "status": "ok"
    }


# =========================================================
# START TRAINING SESSION
# =========================================================

@app.post(
    "/sessions/start",
    response_model=StartSessionResponse,
)
def start_session(
    req: StartSessionRequest,
) -> StartSessionResponse:

    session_id = store.create(
        req.participant_id,
        req.condition,
    )

    plan = policy.first_plan(
        req.condition
    )

    store.log_plan(
        session_id,
        plan.model_dump(mode="json"),
    )

    # -----------------------------------------------------
    # Research logging
    # -----------------------------------------------------

    log_event(
        {
            "event_type": "session_start",
            "session_id": session_id,
            "participant_id": req.participant_id,
            "condition": req.condition.value,
            "initial_difficulty": plan.difficulty,
            "initial_hazard": (
                plan.hazard_type.value
                if plan.hazard_type
                else None
            ),
        }
    )

    return StartSessionResponse(
        session_id=session_id,
        condition=req.condition,
        first_plan=plan,
    )


# =========================================================
# OBSERVE LEARNER EVENT
# =========================================================

@app.post(
    "/agent/event",
    response_model=AgentPlan,
)
def observe_event(
    event: LearnerEvent,
) -> AgentPlan:

    try:

        session = store.get(
            event.session_id
        )

    except KeyError:

        raise HTTPException(
            status_code=404,
            detail="Unknown session_id",
        )

    # -----------------------------------------------------
    # Update learner model first.
    # -----------------------------------------------------

    session.state.observe(event)

    store.log_event(event)

    # -----------------------------------------------------
    # Research logging:
    # learner response
    # -----------------------------------------------------

    log_event(
        {
            "event_type": "learner_event",
            "session_id": event.session_id,
            "hazard_type": (
                event.hazard_type.value
                if event.hazard_type
                else None
            ),
            "success": event.success,
            "response_time_ms": event.response_time_ms,
            "severity": event.severity,
            "difficulty": session.state.difficulty,
            "events_seen": session.state.events_seen,
        }
    )

    # -----------------------------------------------------
    # Fixed condition ALWAYS uses deterministic policy.
    # -----------------------------------------------------

    if session.condition.value == "fixed":

        plan = policy.next_plan(
            session.state,
            event,
            session.condition,
        )

        print(
            "AGENT MODE: deterministic fixed-condition policy"
        )

        planner_mode = "deterministic_fixed"

    # -----------------------------------------------------
    # Adaptive condition:
    #
    # 1. Try LLM planner.
    # 2. If unavailable/fails, use deterministic policy.
    # -----------------------------------------------------

    else:

        plan = None

        if llm_enabled():

            print(
                "AGENT MODE: LLM pedagogical planner"
            )

            plan = propose_with_llm(
                session.state,
                event,
                session.condition,
            )

            planner_mode = (
                "llm"
                if plan is not None
                else "deterministic_fallback"
            )

        else:

            planner_mode = "deterministic_fallback"

        if plan is None:

            print(
                "AGENT MODE: deterministic fallback policy"
            )

            plan = policy.next_plan(
                session.state,
                event,
                session.condition,
            )

    # -----------------------------------------------------
    # Store agent decision.
    # -----------------------------------------------------

    store.log_plan(
        event.session_id,
        plan.model_dump(mode="json"),
    )

    # -----------------------------------------------------
    # Research logging:
    # agent decision
    # -----------------------------------------------------

    log_event(
        {
            "event_type": "agent_decision",
            "session_id": event.session_id,
            "condition": session.condition.value,
            "planner_mode": planner_mode,
            "action": plan.action.value,
            "hazard_type": (
                plan.hazard_type.value
                if plan.hazard_type
                else None
            ),
            "difficulty": plan.difficulty,
            "instruction": plan.instruction,
            "reason": plan.reason,
            "score": plan.score,
        }
    )

    # -----------------------------------------------------
    # End-session bookkeeping.
    # -----------------------------------------------------

    if event.event_type.value == "session_end":

        store.log_session_end(
            event.session_id
        )

        log_event(
            {
                "event_type": "session_end",
                "session_id": event.session_id,
                "condition": session.condition.value,
                "final_difficulty": session.state.difficulty,
                "events_seen": session.state.events_seen,
                "skill_score": session.state.skill_score(),
                "engagement_proxy": (
                    session.state.engagement_proxy()
                ),
            }
        )

    return plan


# =========================================================
# MULTIMODAL FALL DETECTION
# =========================================================

@app.post(
    "/agent/fall-detection",
)
def fall_detection_event() -> dict:

    try:

        detection = run_fall_detection()

    except Exception as exc:

        raise HTTPException(
            status_code=500,
            detail=f"Fall detection failed: {exc}",
        )

    # -----------------------------------------------------
    # Agent response to detected fall.
    # -----------------------------------------------------

    if detection["fall_detected"]:

        agent_action = "emergency_response"

        reason = (
            "High-confidence multimodal fall detected. "
            "The safety agent should trigger an emergency "
            "response and prioritize fall-related remediation."
        )

    else:

        agent_action = "continue_training"

        reason = (
            "No fall detected by the multimodal detector. "
            "The learner can continue the training scenario."
        )

    # -----------------------------------------------------
    # Research logging:
    # multimodal fall detection
    # -----------------------------------------------------

    log_event(
        {
            "event_type": "fall_detection",
            "wearable_probability": (
                detection["wearable_probability"]
            ),
            "environment_probability": (
                detection["environment_probability"]
            ),
            "vision_probability": (
                detection["vision_probability"]
            ),
            "final_score": (
                detection["final_score"]
            ),
            "fall_detected": (
                detection["fall_detected"]
            ),
            "agent_action": agent_action,
        }
    )

    return {

        "event_type":
            "fall_detection",

        "wearable_probability":
            detection[
                "wearable_probability"
            ],

        "environment_probability":
            detection[
                "environment_probability"
            ],

        "vision_probability":
            detection[
                "vision_probability"
            ],

        "final_score":
            detection[
                "final_score"
            ],

        "fall_detected":
            detection[
                "fall_detected"
            ],

        "agent_action":
            agent_action,

        "reason":
            reason,
    }


# =========================================================
# SESSION SNAPSHOT
# =========================================================

@app.get(
    "/sessions/{session_id}",
    response_model=SessionSnapshot,
)
def session_snapshot(
    session_id: str,
) -> SessionSnapshot:

    try:

        session = store.get(
            session_id
        )

    except KeyError:

        raise HTTPException(
            status_code=404,
            detail="Unknown session_id",
        )

    accuracy = {

        hazard.value:
            session.state.hazard_accuracy(
                hazard
            )

        for hazard in (

            HazardType.FALL,

            HazardType.STRUCK_BY,

            HazardType.ELECTRICAL,

        )
    }

    return SessionSnapshot(

        session_id=session_id,

        participant_id=
            session.participant_id,

        condition=
            session.condition,

        difficulty=
            session.state.difficulty,

        events_seen=
            session.state.events_seen,

        skill_score=
            session.state.skill_score(),

        engagement_proxy=
            session.state.engagement_proxy(),

        hazard_accuracy=
            accuracy,
    )