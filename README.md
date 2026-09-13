# Agentic AI for VR-Based Construction Safety Training and Hazard Response

Mitacs-oriented research prototype for adaptive construction-safety training in Unity.

## What this prototype implements

- Unity-side learner telemetry and hazard interactions (falls, struck-by, electrical).
- Python/FastAPI agent backend that receives learner events and maintains a learner model.
- Adaptive pedagogical policy that chooses the next scenario difficulty, hazard, and instructional cue.
- Optional LLM planning hook (disabled by default) for future LangGraph/OpenAI integration.
- Session logging to JSONL/CSV-ready records for a pilot study comparing adaptive vs fixed training.
- Fixed-scripted control mode to support the research comparison described in the project abstract.

## Architecture

Unity (C#) -> HTTP JSON -> FastAPI Agent -> Learner Model -> Pedagogical Policy -> Action Plan -> Unity

The agent can issue actions such as:

- spawn_hazard
- set_difficulty
- show_instruction
- repeat_scenario
- advance_scenario
- end_session

## Repository layout

```
AgenticVRConstructionSafety/
  Assets/
    Scripts/
      AgentApiClient.cs
      TrainingSessionManager.cs
      LearnerTelemetry.cs
      HazardBase.cs
      FallHazard.cs
      StruckByHazard.cs
      ElectricalHazard.cs
      HazardSpawner.cs
      InstructionUI.cs
      ScenarioConfig.cs
  ProjectSettings/
backend/
  app/
    main.py
    models.py
    learner_model.py
    policy.py
    session_store.py
    llm_planner.py
  tests/
    test_policy.py
  requirements.txt
  .env.example
scripts/
  run_backend.bat
  run_backend.sh
study/
  pilot_protocol.md
  metrics.md
```

## 1. Run the Python backend

Requires Python 3.10+.

```bash
cd backend
python -m venv .venv
# Windows
.venv\\Scripts\\activate
# macOS/Linux
source .venv/bin/activate

pip install -r requirements.txt
uvicorn app.main:app --reload --host 127.0.0.1 --port 8000
```

Open http://127.0.0.1:8000/docs to inspect the API.

## 2. Test the backend

```bash
cd backend
pytest -q
```

## 3. Open the Unity project

Use Unity Hub and add the `AgenticVRConstructionSafety` directory as a project. Recommended editor: Unity 2022.3 LTS or newer.

The repository contains scripts rather than large binary scene/assets so it stays easy to submit, review, and version-control. Create a basic construction-site scene using Unity primitives or free assets and attach the provided components.

### Minimal scene wiring

1. Create an empty GameObject named `TrainingSessionManager`.
2. Attach `TrainingSessionManager.cs`, `AgentApiClient.cs`, `LearnerTelemetry.cs`, and `HazardSpawner.cs`.
3. Create a Canvas with a Text/TMP field and attach `InstructionUI.cs`.
4. Create prefabs for each hazard type and attach the matching hazard script.
5. Assign the prefabs and UI references in the Inspector.
6. Set the backend base URL to `http://127.0.0.1:8000`.
7. Start the backend, then press Play in Unity.

## Research modes

The backend supports two experiment conditions:

- `adaptive`: the agent adjusts difficulty, hazards, and cues using learner performance.
- `fixed`: hazards follow a predefined progression and do not adapt to the learner.

Start a session with either condition. This makes it possible to run a small within- or between-subject pilot aligned with the proposal.

## Example learner event

```json
{
  "session_id": "demo-001",
  "event_type": "hazard_response",
  "hazard_type": "electrical",
  "success": false,
  "response_time_ms": 6200,
  "severity": 3,
  "metadata": {"attempt": 1}
}
```

## Example agent response

```json
{
  "action": "show_instruction",
  "hazard_type": "electrical",
  "difficulty": 2,
  "instruction": "Before approaching the panel, identify the energized source and isolate it.",
  "reason": "The learner missed a high-severity electrical hazard and responded slowly.",
  "score": 0.42
}
```

## Why the default agent is not purely LLM-based

For a research prototype, repeatability matters. The default adaptation policy is deterministic and auditable, which makes experimental comparison easier. `llm_planner.py` provides an extension point where an LLM/LangGraph planner can be added to propose actions, while safety constraints and experiment rules remain enforced by the deterministic policy layer.

## Suggested Mitacs demo

A strong short demo is:

1. Learner correctly identifies a fall hazard -> system increases difficulty.
2. Learner misses an electrical hazard -> agent gives a targeted cue and repeats a related scenario.
3. Backend dashboard/log shows performance score changing over time.
4. Toggle to `fixed` mode -> same learner events no longer alter scenario selection.

This directly demonstrates the central research question: adaptive agent-driven training versus fixed scripted VR safety training.
