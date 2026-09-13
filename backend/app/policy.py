from __future__ import annotations

from typing import Iterable

from .learner_model import LearnerState
from .models import (
    AgentAction,
    AgentPlan,
    ExperimentCondition,
    HazardType,
    LearnerEvent,
)


# =========================================================
# FIXED CONDITION
# =========================================================

FIXED_SEQUENCE = [
    HazardType.FALL,
    HazardType.STRUCK_BY,
    HazardType.ELECTRICAL,
    HazardType.FALL,
    HazardType.ELECTRICAL,
    HazardType.STRUCK_BY,
]


# =========================================================
# SAFETY INSTRUCTIONS
# =========================================================

INSTRUCTIONS = {
    HazardType.FALL:
        "Check guardrails, floor openings, and fall-protection equipment before entering the work area.",

    HazardType.STRUCK_BY:
        "Scan for moving equipment and suspended loads, then keep outside the line of fire.",

    HazardType.ELECTRICAL:
        "Identify energized sources, maintain clearance, and verify isolation before approaching electrical equipment.",
}


# =========================================================
# PEDAGOGICAL POLICY
# =========================================================

class PedagogicalPolicy:

    # -----------------------------------------------------
    # First scenario
    # -----------------------------------------------------

    def first_plan(
        self,
        condition: ExperimentCondition
    ) -> AgentPlan:

        return AgentPlan(
            action=AgentAction.SPAWN_HAZARD,

            hazard_type=HazardType.FALL,

            difficulty=1,

            instruction=
                "Identify the hazard and take the safest available action.",

            reason=
                f"Starting {condition.value} training with a baseline fall-risk scenario.",

            score=0.5,
        )


    # -----------------------------------------------------
    # Main decision function
    # -----------------------------------------------------

    def next_plan(
        self,
        state: LearnerState,
        event: LearnerEvent,
        condition: ExperimentCondition
    ) -> AgentPlan:

        # -------------------------------------------------
        # End session
        # -------------------------------------------------

        if event.event_type.value == "session_end":

            return AgentPlan(
                action=AgentAction.END_SESSION,

                difficulty=state.difficulty,

                reason=
                    "The learner ended the session.",

                score=state.skill_score(),
            )


        # -------------------------------------------------
        # Fixed experimental condition
        # -------------------------------------------------

        if condition == ExperimentCondition.FIXED:

            return self._fixed_plan(state)


        # -------------------------------------------------
        # Adaptive experimental condition
        # -------------------------------------------------

        return self._adaptive_plan(
            state,
            event
        )


    # =====================================================
    # FIXED CONDITION
    # =====================================================

    def _fixed_plan(
        self,
        state: LearnerState
    ) -> AgentPlan:

        idx = (
            state.events_seen
            % len(FIXED_SEQUENCE)
        )

        hazard = FIXED_SEQUENCE[idx]

        difficulty = min(
            5,
            1 + state.events_seen // 3
        )

        state.difficulty = difficulty

        return AgentPlan(
            action=AgentAction.ADVANCE_SCENARIO,

            hazard_type=hazard,

            difficulty=difficulty,

            instruction=None,

            reason=
                "Fixed control condition: advancing according to the predefined scenario sequence.",

            score=state.skill_score(),
        )


    # =====================================================
    # ADAPTIVE CONDITION
    # =====================================================

    def _adaptive_plan(
        self,
        state: LearnerState,
        event: LearnerEvent
    ) -> AgentPlan:

        score = state.skill_score()


        # -------------------------------------------------
        # FAILURE
        # -------------------------------------------------
        #
        # If the learner fails:
        #
        # 1. Reduce difficulty
        # 2. Give targeted remediation
        # 3. Repeat the relevant hazard
        #
        # -------------------------------------------------

        if event.success is False:

            if event.hazard_type:

                state.difficulty = max(
                    1,
                    state.difficulty - 1
                )

                return AgentPlan(
                    action=AgentAction.SHOW_INSTRUCTION,

                    hazard_type=event.hazard_type,

                    difficulty=state.difficulty,

                    instruction=
                        INSTRUCTIONS[event.hazard_type],

                    reason=
                        self._failure_reason(event),

                    score=score,
                )


        # -------------------------------------------------
        # SUCCESS
        # -------------------------------------------------
        #
        # IMPORTANT:
        #
        # A successful response should NEVER result in
        # another show_instruction for the same scenario.
        #
        # The agent should advance.
        #
        # -------------------------------------------------

        if event.success is True:

            # ---------------------------------------------
            # Two consecutive successes
            # ---------------------------------------------

            if state.recent_success_streak >= 2:

                state.difficulty = min(
                    5,
                    state.difficulty + 1
                )

                hazard = self._next_hazard(
                    state,
                    event
                )

                return AgentPlan(
                    action=AgentAction.SET_DIFFICULTY,

                    hazard_type=hazard,

                    difficulty=state.difficulty,

                    instruction=
                        "Good performance. The next scenario will include less obvious cues and tighter response demands.",

                    reason=
                        "Two consecutive successful responses indicate readiness for a harder scenario.",

                    score=score,
                )


            # ---------------------------------------------
            # Normal successful response
            # ---------------------------------------------

            hazard = self._next_hazard(
                state,
                event
            )

            return AgentPlan(
                action=AgentAction.ADVANCE_SCENARIO,

                hazard_type=hazard,

                difficulty=state.difficulty,

                instruction=None,

                reason=
                    "The learner completed the scenario successfully, so the agent is advancing to the next training hazard.",

                score=score,
            )


        # -------------------------------------------------
        # Fallback
        # -------------------------------------------------
        #
        # Used for events without a success/failure result.
        #
        # -------------------------------------------------

        hazard = self._weakest_hazard(
            state
        )

        return AgentPlan(
            action=AgentAction.ADVANCE_SCENARIO,

            hazard_type=hazard,

            difficulty=state.difficulty,

            instruction=None,

            reason=
                f"Selecting {hazard.value} based on the learner's current demonstrated mastery.",

            score=score,
        )


    # =====================================================
    # SELECT NEXT HAZARD
    # =====================================================

    def _next_hazard(
        self,
        state: LearnerState,
        event: LearnerEvent
    ) -> HazardType:

        hazards = [
            HazardType.FALL,
            HazardType.STRUCK_BY,
            HazardType.ELECTRICAL,
        ]

        # -------------------------------------------------
        # Prefer a hazard that has not been attempted yet.
        # -------------------------------------------------

        untested = [
            hazard
            for hazard in hazards
            if (
                not state.hazard_stats.get(hazard)
                or state.hazard_stats[hazard].attempts == 0
            )
        ]

        if untested:

            # Don't immediately repeat the same hazard
            # that was just completed.

            for hazard in untested:

                if (
                    event.hazard_type is None
                    or hazard != event.hazard_type
                ):
                    return hazard

            return untested[0]


        # -------------------------------------------------
        # Once all hazards have been tested, select the
        # weakest demonstrated hazard.
        # -------------------------------------------------

        return self._weakest_hazard(
            state
        )


    # =====================================================
    # FAILURE REASON
    # =====================================================

    @staticmethod
    def _failure_reason(
        event: LearnerEvent
    ) -> str:

        slow = (
            event.response_time_ms is not None
            and event.response_time_ms > 5000
        )

        if (
            slow
            and event.severity >= 3
        ):

            return (
                "The learner missed a high-severity "
                "hazard and responded slowly, so "
                "targeted remediation is appropriate."
            )

        if event.severity >= 3:

            return (
                "The learner missed a high-severity "
                "hazard, so the agent is providing "
                "immediate corrective guidance."
            )

        return (
            "The learner missed the hazard, so the "
            "agent is reinforcing the relevant "
            "safety procedure."
        )


    # =====================================================
    # WEAKEST HAZARD
    # =====================================================

    @staticmethod
    def _weakest_hazard(
        state: LearnerState
    ) -> HazardType:

        hazards: Iterable[HazardType] = (
            HazardType.FALL,
            HazardType.STRUCK_BY,
            HazardType.ELECTRICAL,
        )

        return min(
            hazards,

            key=lambda h: (
                state.hazard_stats.get(h).attempts
                if state.hazard_stats.get(h)
                else 0,

                state.hazard_accuracy(h),
            ),
        )