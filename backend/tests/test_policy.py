from app.learner_model import LearnerState
from app.models import EventType, ExperimentCondition, HazardType, LearnerEvent
from app.policy import PedagogicalPolicy


def event(success=True, hazard=HazardType.FALL, response=2500, severity=2):
    return LearnerEvent(
        session_id="test",
        event_type=EventType.HAZARD_RESPONSE,
        hazard_type=hazard,
        success=success,
        response_time_ms=response,
        severity=severity,
    )


def test_failure_produces_instruction():
    p = PedagogicalPolicy()
    s = LearnerState(difficulty=3)
    e = event(False, HazardType.ELECTRICAL, 7000, 4)
    s.observe(e)
    plan = p.next_plan(s, e, ExperimentCondition.ADAPTIVE)
    assert plan.action.value == "show_instruction"
    assert plan.hazard_type == HazardType.ELECTRICAL
    assert plan.difficulty == 2


def test_success_streak_increases_difficulty():
    p = PedagogicalPolicy()
    s = LearnerState(difficulty=1)
    e1 = event(True)
    s.observe(e1)
    p.next_plan(s, e1, ExperimentCondition.ADAPTIVE)
    e2 = event(True)
    s.observe(e2)
    plan = p.next_plan(s, e2, ExperimentCondition.ADAPTIVE)
    assert plan.action.value == "set_difficulty"
    assert plan.difficulty == 2


def test_fixed_condition_ignores_failure_for_remediation():
    p = PedagogicalPolicy()
    s = LearnerState(difficulty=1)
    e = event(False, HazardType.ELECTRICAL, 9000, 5)
    s.observe(e)
    plan = p.next_plan(s, e, ExperimentCondition.FIXED)
    assert plan.action.value == "advance_scenario"
