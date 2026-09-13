UNITY QUICK SETUP

1. Open this folder in Unity 2022.3 LTS or later.
2. Install TextMeshPro essentials if prompted.
3. Make a scene named ConstructionTraining.
4. Create an empty TrainingSystem object and attach:
   - AgentApiClient
   - TrainingSessionManager
   - LearnerTelemetry
   - HazardSpawner
   - DemoKeyboardController (optional for laptop demo)
5. Create three simple hazard prefabs and attach:
   - FallHazard
   - StruckByHazard
   - ElectricalHazard
6. Assign all prefabs to HazardSpawner.
7. Add a Canvas -> TextMeshPro text element -> InstructionUI and wire its reference.
8. Assign AgentApiClient, HazardSpawner, InstructionUI references to TrainingSessionManager.
9. Run the Python backend.
10. Press Play. Press 1 for a successful response, 2 for a failed response.

For real VR interaction, install Unity XR Interaction Toolkit and wire controller/button/trigger
interactions to the public methods in each hazard script. The adaptive backend does not depend
on a specific headset.
