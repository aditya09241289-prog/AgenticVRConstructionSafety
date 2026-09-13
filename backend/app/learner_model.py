from __future__ import annotations

from dataclasses import dataclass, field
from collections import defaultdict
from typing import Dict

from .models import HazardType, LearnerEvent


@dataclass
class HazardStats:
    attempts: int = 0
    successes: int = 0
    avg_response_ms: float = 0.0

    def update(
        self,
        success: bool | None,
        response_time_ms: int | None,
    ) -> None:
        self.attempts += 1

        if success:
            self.successes += 1

        if response_time_ms is not None:
            n = self.attempts
            self.avg_response_ms += (
                response_time_ms - self.avg_response_ms
            ) / n

    @property
    def accuracy(self) -> float:
        if self.attempts == 0:
            return 0.0

        return self.successes / self.attempts


@dataclass
class LearnerState:
    difficulty: int = 1
    events_seen: int = 0

    recent_success_streak: int = 0
    recent_failure_streak: int = 0

    hazard_stats: Dict[HazardType, HazardStats] = field(
        default_factory=lambda: defaultdict(HazardStats)
    )

    def observe(self, event: LearnerEvent) -> None:
        """
        Update the learner model from a new training event.

        Difficulty changes are controlled by PedagogicalPolicy.
        """

        self.events_seen += 1

        # Update statistics for hazard-response and
        # scenario-completion events.
        if (
            event.hazard_type
            and event.event_type.value
            in {"hazard_response", "scenario_complete"}
        ):
            stats = self.hazard_stats[event.hazard_type]

            stats.update(
                event.success,
                event.response_time_ms,
            )

        # Track recent performance streaks.
        if event.success is True:
            self.recent_success_streak += 1
            self.recent_failure_streak = 0

        elif event.success is False:
            self.recent_failure_streak += 1
            self.recent_success_streak = 0

        # Difficulty is intentionally NOT changed here.
        # PedagogicalPolicy controls difficulty adaptation.

    def hazard_accuracy(
        self,
        hazard: HazardType,
    ) -> float:
        """
        Return the learner's observed accuracy for
        a particular hazard type.
        """

        stats = self.hazard_stats.get(hazard)

        if stats is None:
            return 0.0

        return stats.accuracy

    def skill_score(self) -> float:
        """
        Estimate overall learner skill.

        75% comes from accuracy.
        25% comes from response speed.
        """

        if not self.hazard_stats:
            return 0.5

        observed = [
            stats
            for stats in self.hazard_stats.values()
            if stats.attempts
        ]

        if not observed:
            return 0.5

        # Overall accuracy across observed hazard types.
        accuracy = (
            sum(stats.accuracy for stats in observed)
            / len(observed)
        )

        # Response-speed component.
        response_bonus_parts = []

        for stats in observed:
            if stats.avg_response_ms <= 0:
                continue

            bonus = max(
                0.0,
                min(
                    1.0,
                    (10000.0 - stats.avg_response_ms) / 8000.0,
                ),
            )

            response_bonus_parts.append(bonus)

        if response_bonus_parts:
            speed = (
                sum(response_bonus_parts)
                / len(response_bonus_parts)
            )
        else:
            speed = 0.5

        return round(
            0.75 * accuracy + 0.25 * speed,
            3,
        )

    def engagement_proxy(self) -> float:
        """
        Prototype engagement estimate.

        Uses:
        - overall skill
        - successful-response streak
        - failure streak
        """

        if self.events_seen == 0:
            return 0.5

        score = self.skill_score()

        streak_bonus = min(
            0.15,
            self.recent_success_streak * 0.03,
        )

        failure_penalty = min(
            0.15,
            self.recent_failure_streak * 0.03,
        )

        return round(
            max(
                0.0,
                min(
                    1.0,
                    score
                    + streak_bonus
                    - failure_penalty,
                ),
            ),
            3,
        )

    # ---------------------------------------------------------
    # TELEMETRY SUMMARY
    # ---------------------------------------------------------

    def learner_summary(self) -> dict:
        """
        Return a compact, JSON-friendly snapshot of the
        learner's current training state.

        This is used by SessionStore for experiment logging.
        """

        hazard_summary = {}

        for hazard in (
            HazardType.FALL,
            HazardType.STRUCK_BY,
            HazardType.ELECTRICAL,
        ):
            stats = self.hazard_stats.get(hazard)

            if stats is None:
                hazard_summary[hazard.value] = {
                    "attempts": 0,
                    "successes": 0,
                    "accuracy": 0.0,
                    "avg_response_ms": 0.0,
                }

            else:
                hazard_summary[hazard.value] = {
                    "attempts": stats.attempts,
                    "successes": stats.successes,
                    "accuracy": round(
                        stats.accuracy,
                        3,
                    ),
                    "avg_response_ms": round(
                        stats.avg_response_ms,
                        1,
                    ),
                }

        return {
            "difficulty": self.difficulty,
            "events_seen": self.events_seen,
            "recent_success_streak": self.recent_success_streak,
            "recent_failure_streak": self.recent_failure_streak,
            "skill_score": self.skill_score(),
            "engagement_proxy": self.engagement_proxy(),
            "hazards": hazard_summary,
        }