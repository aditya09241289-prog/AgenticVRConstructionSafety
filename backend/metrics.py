from __future__ import annotations

import json
from pathlib import Path
from collections import defaultdict


# =========================================================
# CONFIGURATION
# =========================================================

# Only analyze the most recent session that actually
# contains learner events.
LATEST_SESSION_ONLY = True

LOG_FILE = (
    Path(__file__).resolve().parent
    / "logs"
    / "training_events.jsonl"
)

OUTPUT_DIR = (
    Path(__file__).resolve().parent
    / "metrics_output"
)


# =========================================================
# LOAD TRAINING EVENTS
# =========================================================

def load_events():
    if not LOG_FILE.exists():
        raise FileNotFoundError(
            f"Training log not found: {LOG_FILE}"
        )

    events = []

    with LOG_FILE.open(
        "r",
        encoding="utf-8",
    ) as file:

        for line in file:
            line = line.strip()

            if not line:
                continue

            try:
                events.append(
                    json.loads(line)
                )
            except json.JSONDecodeError:
                print(
                    "Warning: skipped invalid JSON line."
                )

    return events


# =========================================================
# CALCULATE METRICS
# =========================================================

def calculate_metrics(events):

    learner_events = [
        e
        for e in events
        if e.get("event_type") == "learner_event"
        and e.get("success") is not None
    ]

    agent_events = [
        e
        for e in events
        if e.get("event_type") == "agent_decision"
    ]

    if not learner_events:
        raise RuntimeError(
            "No learner events found in the selected session."
        )

    total = len(learner_events)

    successes = sum(
        1
        for e in learner_events
        if e["success"] is True
    )

    failures = total - successes

    success_rate = successes / total

    # -----------------------------------------------------
    # Response time
    # -----------------------------------------------------

    response_times = [
        e["response_time_ms"]
        for e in learner_events
        if e.get("response_time_ms") is not None
    ]

    average_response_time = (
        sum(response_times) / len(response_times)
        if response_times
        else 0
    )

    # -----------------------------------------------------
    # Hazard-specific metrics
    # -----------------------------------------------------

    hazard_data = defaultdict(list)

    for event in learner_events:

        hazard = event.get(
            "hazard_type",
            "unknown"
        )

        hazard_data[hazard].append(event)

    hazard_metrics = {}

    for hazard, hazard_events in hazard_data.items():

        attempts = len(hazard_events)

        correct = sum(
            1
            for e in hazard_events
            if e["success"] is True
        )

        times = [
            e["response_time_ms"]
            for e in hazard_events
            if e.get("response_time_ms") is not None
        ]

        hazard_metrics[hazard] = {
            "attempts": attempts,
            "successes": correct,
            "failures": attempts - correct,
            "accuracy": (
                correct / attempts
                if attempts
                else 0
            ),
            "average_response_time_ms": (
                sum(times) / len(times)
                if times
                else 0
            ),
        }

    # -----------------------------------------------------
    # Difficulty
    # -----------------------------------------------------

    difficulties = [
        e["difficulty"]
        for e in learner_events
        if e.get("difficulty") is not None
    ]

    average_difficulty = (
        sum(difficulties) / len(difficulties)
        if difficulties
        else 0
    )

    maximum_difficulty = (
        max(difficulties)
        if difficulties
        else 0
    )

    initial_difficulty = (
        difficulties[0]
        if difficulties
        else 0
    )

    # -----------------------------------------------------
    # Difficulty progression
    # -----------------------------------------------------

    difficulty_progression = []

    for index, event in enumerate(
        learner_events,
        start=1
    ):

        difficulty_progression.append(
            {
                "event_number": index,
                "timestamp": event.get(
                    "timestamp"
                ),
                "hazard_type": event.get(
                    "hazard_type"
                ),
                "difficulty": event.get(
                    "difficulty"
                ),
                "success": event.get(
                    "success"
                ),
            }
        )

    # -----------------------------------------------------
    # Agent behaviour
    # -----------------------------------------------------

    agent_actions = defaultdict(int)

    for event in agent_events:

        action = event.get(
            "action",
            "unknown"
        )

        agent_actions[action] += 1

    adaptive_decisions = sum(
        1
        for e in agent_events
        if e.get("condition") == "adaptive"
    )

    remediation_count = sum(
        1
        for e in agent_events
        if e.get("action") == "show_instruction"
    )

    difficulty_increases = sum(
        1
        for e in agent_events
        if e.get("action") == "set_difficulty"
    )

    scenario_advances = sum(
        1
        for e in agent_events
        if e.get("action")
        in {
            "advance_scenario",
            "spawn_hazard",
            "repeat_scenario",
        }
    )

    # -----------------------------------------------------
    # Sessions
    # -----------------------------------------------------

    sessions = {
        e.get("session_id")
        for e in events
        if e.get("session_id")
    }

    # -----------------------------------------------------
    # Return metrics
    # -----------------------------------------------------

    return {

        "total_learner_events":
            total,

        "successful_responses":
            successes,

        "failed_responses":
            failures,

        "overall_success_rate":
            success_rate,

        "average_response_time_ms":
            average_response_time,

        "initial_difficulty":
            initial_difficulty,

        "average_difficulty":
            average_difficulty,

        "maximum_difficulty":
            maximum_difficulty,

        "number_of_sessions":
            len(sessions),

        "adaptive_agent_decisions":
            adaptive_decisions,

        "remediation_instructions":
            remediation_count,

        "difficulty_increase_decisions":
            difficulty_increases,

        "scenario_advances":
            scenario_advances,

        "agent_actions":
            dict(agent_actions),

        "hazard_metrics":
            hazard_metrics,

        "difficulty_progression":
            difficulty_progression,
    }


# =========================================================
# SAVE RESULTS
# =========================================================

def save_results(metrics):

    OUTPUT_DIR.mkdir(
        parents=True,
        exist_ok=True,
    )

    output_file = (
        OUTPUT_DIR
        / "training_metrics.json"
    )

    with output_file.open(
        "w",
        encoding="utf-8",
    ) as file:

        json.dump(
            metrics,
            file,
            indent=2,
        )

    return output_file


# =========================================================
# PRINT REPORT
# =========================================================

def print_report(metrics):

    print()

    print("=" * 60)
    print(
        "CONSTRUCTION SAFETY TRAINING METRICS"
    )
    print("=" * 60)

    print(
        f"Sessions              : "
        f"{metrics['number_of_sessions']}"
    )

    print(
        f"Learner events        : "
        f"{metrics['total_learner_events']}"
    )

    print(
        f"Successful responses  : "
        f"{metrics['successful_responses']}"
    )

    print(
        f"Failed responses      : "
        f"{metrics['failed_responses']}"
    )

    print(
        f"Overall success rate  : "
        f"{metrics['overall_success_rate']:.2%}"
    )

    print(
        f"Average response time : "
        f"{metrics['average_response_time_ms']:.1f} ms"
    )

    print(
        f"Initial difficulty    : "
        f"{metrics['initial_difficulty']}"
    )

    print(
        f"Average difficulty    : "
        f"{metrics['average_difficulty']:.2f}"
    )

    print(
        f"Maximum difficulty    : "
        f"{metrics['maximum_difficulty']}"
    )

    print()
    print("HAZARD PERFORMANCE")
    print("-" * 60)

    for hazard, data in metrics[
        "hazard_metrics"
    ].items():

        print(
            f"{hazard:12s} | "
            f"Accuracy: "
            f"{data['accuracy']:.2%} | "
            f"Attempts: "
            f"{data['attempts']} | "
            f"Avg response: "
            f"{data['average_response_time_ms']:.1f} ms"
        )

    print()
    print("AGENT BEHAVIOUR")
    print("-" * 60)

    print(
        f"Adaptive decisions       : "
        f"{metrics['adaptive_agent_decisions']}"
    )

    print(
        f"Remediation instructions : "
        f"{metrics['remediation_instructions']}"
    )

    print(
        f"Difficulty increases     : "
        f"{metrics['difficulty_increase_decisions']}"
    )

    print(
        f"Scenario advances        : "
        f"{metrics['scenario_advances']}"
    )

    print()
    print("Agent actions:")

    for action, count in metrics[
        "agent_actions"
    ].items():

        print(
            f"  {action:22s}: {count}"
        )

    print("=" * 60)


# =========================================================
# MAIN
# =========================================================

def main():

    events = load_events()

    if not events:
        raise RuntimeError(
            "Training log is empty."
        )

    # -----------------------------------------------------
    # Find the latest session that actually contains
    # learner events.
    # -----------------------------------------------------

    if LATEST_SESSION_ONLY:

        learner_session_ids = {
            e.get("session_id")
            for e in events
            if e.get("event_type")
            == "learner_event"
            and e.get("session_id")
        }

        if not learner_session_ids:
            raise RuntimeError(
                "No learner events found in the training log."
            )

        session_starts = [
            e
            for e in events
            if e.get("event_type")
            == "session_start"
            and e.get("session_id")
            in learner_session_ids
        ]

        # -------------------------------------------------
        # If the session_start record is missing, use the
        # latest learner event's session directly.
        # -------------------------------------------------

        if session_starts:

            latest_session = max(
                session_starts,
                key=lambda e:
                    e.get("timestamp", "")
            )

            session_id = (
                latest_session["session_id"]
            )

        else:

            learner_events = [
                e
                for e in events
                if e.get("event_type")
                == "learner_event"
                and e.get("session_id")
            ]

            latest_event = max(
                learner_events,
                key=lambda e:
                    e.get("timestamp", "")
            )

            session_id = (
                latest_event["session_id"]
            )

        # -------------------------------------------------
        # Keep only this session.
        # -------------------------------------------------

        events = [
            e
            for e in events
            if e.get("session_id")
            == session_id
        ]

        print()

        print(
            "Using latest session with learner data:"
        )

        print(
            session_id
        )

    # -----------------------------------------------------
    # Calculate and save metrics.
    # -----------------------------------------------------

    metrics = calculate_metrics(
        events
    )

    output_file = save_results(
        metrics
    )

    print_report(
        metrics
    )

    print()

    print(
        "Saved metrics to:"
    )

    print(
        output_file
    )


if __name__ == "__main__":
    main()