
# Agentic AI for VR Construction Safety

An agentic artificial intelligence system for adaptive construction-safety training in an immersive virtual environment.

The project combines **Unity-based VR training**, **agentic decision-making**, **learner telemetry**, **adaptive scenario generation**, and **multimodal fall detection** to create a safety-training environment that can respond dynamically to learner behaviour.

---

## Overview

Traditional safety-training systems often present the same scenarios to every learner. This project explores an adaptive alternative in which an AI agent observes learner interactions, evaluates performance, and selects subsequent training actions based on the learner's behaviour.


### Adaptive AI Training

The training environment communicates with an agentic backend that can determine:

* Which hazard should appear next
* Training difficulty
* Learner instructions
* Adaptive responses based on previous performance
* Reasons behind agent decisions

### Construction Safety Scenarios

The current VR environment includes multiple hazard scenarios:

* **Fall hazards**
* **Struck-by / line-of-fire hazards**
* **Electrical hazards**

Learners must respond to hazards using appropriate safety actions.

### Learner Telemetry

The system records training events such as:

* Hazard type
* Success/failure
* Response time
* Severity
* Session information

This information is used by the agent to adapt subsequent training scenarios.

### Multimodal Fall Detection

The system integrates three information sources:

```text
Wearable
Environment
Vision
```

These are processed independently and combined through decision-level fusion.

The current implementation uses:

```text
Final Score =
0.40 × Wearable Probability
+ 0.30 × Environment Probability
+ 0.30 × Vision Probability
```

A configurable threshold is then used to determine whether a fall has been detected.

### Automatic Backend Startup

The standalone application includes a backend launcher so the local safety-training API can be started automatically when the application runs.

## Technology Stack

### Frontend / Simulation

* Unity
* C#
* Unity Physics
* TextMesh Pro
* Interactive VR-style training environment

### Backend

* Python
* FastAPI
* Uvicorn
* REST APIs

### AI / Decision Layer

* Learner modelling
* Rule/policy-based decision making
* Adaptive scenario planning
* LLM-assisted planning components
* Multimodal decision fusion

### Development

* Git
* GitHub
* PyInstaller
* PowerShell


## Safety Scenarios

### 1. Fall Hazard

The learner encounters a fall-risk situation and must identify the hazard and take the appropriate protective action.

The system can provide warnings and detect whether the learner has successfully resolved the situation.

### 2. Struck-By Hazard

The learner is exposed to a moving hazard or line-of-fire scenario.

The system evaluates whether the learner chooses a safe route or remains in an unsafe area.

### 3. Electrical Hazard

The learner enters an electrical hazard zone and receives a warning.

The scenario can be resolved through an appropriate electrical-safety action, such as isolating the power.

---

## API

The backend exposes a local REST API.

Example health endpoint:

```text
GET /health
```

Example agent interaction:

```text
POST /sessions/start
POST /agent/event
POST /agent/fall-detection
```

Interactive API documentation is available through FastAPI/Swagger when the backend is running:

```text
http://127.0.0.1:8001/docs
```

---

## Running the Project

### Backend

From the `backend` directory:

```powershell
.\venv\Scripts\Activate.ps1
python -m uvicorn app.main:app --host 127.0.0.1 --port 8001
```

The backend should then be available at:

```text
http://127.0.0.1:8001
```

Health check:

```text
http://127.0.0.1:8001/health
```

### Unity

Open:

```text
AgenticVRConstructionSafety/
```

in Unity and open:

```text
Assets/scenes/ConstructionSafetyTraining.unity
```

The standalone application can use the included backend launcher to start the local backend automatically.

---

## Research Motivation

This project explores how **agentic AI can be used to make immersive construction-safety training adaptive rather than static**.

The central idea is:

```text
Observe learner behaviour
        ↓
Model learner performance
        ↓
Select next training action
        ↓
Adapt scenario difficulty
        ↓
Improve training experience
```

This architecture can potentially support personalized safety training by adjusting scenarios according to the learner's demonstrated behaviour rather than presenting identical exercises to every participant.

---

## Research Questions

The project can be extended to investigate questions such as:

1. Can agent-driven scenario selection produce more adaptive safety-training experiences?
2. Can learner telemetry be used to estimate training performance and select appropriate difficulty?
3. Can multimodal sensing improve automated detection of safety-critical events?
4. Can immersive adaptive training improve hazard-response performance compared with static training?

---

## Current Status

The prototype currently demonstrates:

* Interactive Unity construction-safety environment
* Multiple safety hazard scenarios
* Learner event telemetry
* Agentic backend
* Adaptive scenario planning
* Multimodal fall-detection integration
* Local REST API
* Automatic backend launching for standalone execution
* Metrics and pilot-study support

---

## Future Work

Potential future extensions include:

* Real VR headset integration
* Real wearable sensor integration
* Larger-scale learner studies
* More sophisticated learner models
* Reinforcement-learning-based policy optimization
* Improved computer-vision safety perception
* Personalized training curricula
* Multi-agent construction-site simulation
* Cloud deployment and remote analytics
* Real-world safety-training validation

---

## Result
<img width="1917" height="1198" alt="Screenshot 2026-09-14 001632" src="https://github.com/user-attachments/assets/75a91913-8da0-4b4c-a826-a6d675a23d5b" />
<img width="1917" height="1198" alt="Screenshot 2026-09-14 001638" src="https://github.com/user-attachments/assets/3faacc22-3989-4fdd-8982-ee1667368299" />
<img width="1887" height="971" alt="Screenshot 2026-09-14 001717" src="https://github.com/user-attachments/assets/e961d536-bb8d-4fb7-9adf-1f6bda28c39d" />
<img width="1890" height="973" alt="Screenshot 2026-09-14 001705" src="https://github.com/user-attachments/assets/570bc3af-0810-4178-8706-9ca5fff8ee21" />






## Disclaimer

This repository represents a research prototype for educational and experimental purposes.

The system should not be treated as a replacement for certified occupational-safety procedures, professional safety training, or real-world emergency systems.

---

## Author

**Aditya Pawar**

