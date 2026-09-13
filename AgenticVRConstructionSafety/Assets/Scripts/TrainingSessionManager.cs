using System.Collections;
using UnityEngine;

public class TrainingSessionManager : MonoBehaviour
{
    [Header("Experiment")]
    [SerializeField] private string participantId = "pilot-001";
    [SerializeField] private bool adaptiveCondition = true;

    [Header("References")]
    [SerializeField] private AgentApiClient apiClient;
    [SerializeField] private HazardSpawner hazardSpawner;
    [SerializeField] private InstructionUI instructionUI;

    [Header("Scenario Transition")]
    [SerializeField] private float minimumDistanceBeforeRespawn = 4f;
    [SerializeField] private float remediationDelay = 3f;

    public string SessionId { get; private set; }

    public int CurrentDifficulty { get; private set; } = 1;

    public bool SessionReady =>
        !string.IsNullOrEmpty(SessionId);

    private bool fallDetectionRunning = false;

    private int planGeneration = 0;

    private Coroutine pendingRemediationRoutine;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        if (apiClient == null)
            apiClient = FindObjectOfType<AgentApiClient>();

        if (hazardSpawner == null)
            hazardSpawner = FindObjectOfType<HazardSpawner>();

        if (instructionUI == null)
            instructionUI = FindObjectOfType<InstructionUI>();

        if (apiClient == null)
        {
            Debug.LogError(
                "TrainingSessionManager: AgentApiClient was not found."
            );

            return;
        }

        if (hazardSpawner == null)
        {
            Debug.LogWarning(
                "TrainingSessionManager: HazardSpawner was not found."
            );
        }

        if (instructionUI == null)
        {
            Debug.LogWarning(
                "TrainingSessionManager: InstructionUI was not found."
            );
        }

        StartCoroutine(BeginSession());
    }


    // =====================================================
    // AUTOMATIC FALL DETECTION
    // =====================================================

    public void StartAutomaticFallDetection()
    {
        if (fallDetectionRunning)
        {
            Debug.Log(
                "Fall detection already running."
            );

            return;
        }

        StartCoroutine(
            RunFallDetection()
        );
    }


    // =====================================================
    // BEGIN SESSION
    // =====================================================

    private IEnumerator BeginSession()
    {
        string condition =
            adaptiveCondition
                ? "adaptive"
                : "fixed";

        yield return apiClient.StartSession(
            participantId,
            condition,

            response =>
            {
                if (response == null)
                {
                    Debug.LogError(
                        "TrainingSessionManager: Backend returned an empty response."
                    );

                    return;
                }

                SessionId =
                    response.session_id;

                Debug.Log(
                    $"Training session started: " +
                    $"{SessionId} ({condition})"
                );

                ApplyPlan(
                    response.first_plan
                );
            },

            error =>
            {
                Debug.LogError(
                    $"Agent backend error: {error}"
                );

                instructionUI?.Show(
                    "Could not connect to AI training backend."
                );
            }
        );
    }


    // =====================================================
    // FALL DETECTION
    // =====================================================

    private IEnumerator RunFallDetection()
    {
        if (apiClient == null)
        {
            Debug.LogError(
                "Cannot run fall detection: AgentApiClient is missing."
            );

            yield break;
        }

        fallDetectionRunning = true;

        Debug.Log(
            "========================================"
        );

        Debug.Log(
            "STARTING AUTOMATIC MULTIMODAL FALL DETECTION"
        );

        Debug.Log(
            "========================================"
        );

        instructionUI?.Show(
            "⚠ ANALYSING SAFETY RISK..."
        );

        yield return apiClient.RunFallDetection(
            response =>
            {
                if (response == null)
                {
                    Debug.LogError(
                        "Fall detection returned an empty response."
                    );

                    instructionUI?.Show(
                        "Fall detection returned no result."
                    );

                    return;
                }

                Debug.Log(
                    $"WEARABLE     : " +
                    $"{response.wearable_probability:P2}"
                );

                Debug.Log(
                    $"ENVIRONMENT  : " +
                    $"{response.environment_probability:P2}"
                );

                Debug.Log(
                    $"VISION       : " +
                    $"{response.vision_probability:P2}"
                );

                Debug.Log(
                    $"FUSION       : " +
                    $"{response.final_score:P2}"
                );

                Debug.Log(
                    $"FALL DETECTED: " +
                    $"{response.fall_detected}"
                );

                Debug.Log(
                    $"AGENT ACTION : " +
                    $"{response.agent_action}"
                );

                instructionUI?.ShowFallDetection(
                    response.fall_detected,
                    response.final_score,
                    response.agent_action
                );

                if (response.fall_detected)
                {
                    Debug.Log(
                        "SAFETY AGENT: Emergency response triggered."
                    );
                }
                else
                {
                    Debug.Log(
                        "SAFETY AGENT: Continue training."
                    );
                }
            },

            error =>
            {
                Debug.LogError(
                    $"Fall detection error: {error}"
                );

                instructionUI?.Show(
                    "Fall detection unavailable."
                );
            }
        );

        fallDetectionRunning = false;
    }


    // =====================================================
    // REPORT LEARNER RESPONSE
    // =====================================================

    public void ReportHazardResponse(
        string hazardType,
        bool success,
        int responseTimeMs,
        int severity)
    {
        if (!SessionReady)
        {
            Debug.LogWarning(
                "Cannot report hazard response: session is not ready."
            );

            return;
        }

        var payload =
            new LearnerEventPayload
            {
                session_id = SessionId,
                event_type = "hazard_response",
                hazard_type = hazardType,
                success = success,
                response_time_ms = responseTimeMs,
                severity = Mathf.Clamp(
                    severity,
                    1,
                    5
                )
            };

        StartCoroutine(
            apiClient.SendEvent(
                payload,
                ApplyPlan,

                error =>
                {
                    Debug.LogError(
                        $"Agent unavailable: {error}"
                    );

                    instructionUI?.Show(
                        "Agent unavailable. Continue with the current scenario."
                    );
                }
            )
        );
    }


    // =====================================================
    // APPLY AGENT PLAN
    // =====================================================

    public void ApplyPlan(
        AgentPlan plan)
    {
        if (plan == null)
        {
            Debug.LogWarning(
                "TrainingSessionManager received an empty agent plan."
            );

            return;
        }

        planGeneration++;

        int currentGeneration =
            planGeneration;

        CancelPendingRemediation();

        CurrentDifficulty =
            Mathf.Clamp(
                plan.difficulty,
                1,
                5
            );

        Debug.Log(
            $"Agent plan: {plan.action}, " +
            $"hazard={plan.hazard_type}, " +
            $"difficulty={plan.difficulty}, " +
            $"reason={plan.reason}"
        );

        switch (plan.action)
        {
            case "spawn_hazard":

            case "advance_scenario":

            case "set_difficulty":

                SpawnScenarioAndDisplay(plan);

                break;


            case "show_instruction":

                ShowCurrentScenarioInstruction(plan);

                pendingRemediationRoutine =
                    StartCoroutine(
                        RepeatAfterCue(
                            plan,
                            currentGeneration
                        )
                    );

                break;


            case "repeat_scenario":

                SpawnScenarioAndDisplay(plan);

                break;


            case "end_session":

                instructionUI?.Show(
                    "Training session complete."
                );

                hazardSpawner?.ClearCurrent();

                break;
        }
    }


    // =====================================================
    // SPAWN SCENARIO
    // =====================================================

    private void SpawnScenarioAndDisplay(
        AgentPlan plan)
    {
        if (hazardSpawner == null)
            return;

        GameObject hazard =
            hazardSpawner.Spawn(
                plan.hazard_type,
                CurrentDifficulty
            );

        if (hazard == null)
            return;

        HazardBase hazardComponent =
            hazard.GetComponent<HazardBase>();

        int severity = 1;

        if (hazardComponent != null)
        {
            severity =
                hazardComponent.Severity;
        }

        string instruction =
            string.IsNullOrWhiteSpace(
                plan.instruction
            )
                ? GetDefaultInstruction(
                    plan.hazard_type
                )
                : plan.instruction;

        string agentStatus =
            GetAgentStatus(plan);

        instructionUI?.ShowScenario(
            plan.hazard_type,
            CurrentDifficulty,
            severity,
            instruction,
            agentStatus
        );
    }


    // =====================================================
    // SHOW AGENT INSTRUCTION
    // =====================================================

    private void ShowCurrentScenarioInstruction(
        AgentPlan plan)
    {
        int severity = 1;

        GameObject currentHazard =
            hazardSpawner?.GetCurrentHazard();

        if (currentHazard != null)
        {
            HazardBase hazardComponent =
                currentHazard.GetComponent<HazardBase>();

            if (hazardComponent != null)
            {
                severity =
                    hazardComponent.Severity;
            }
        }

        string instruction =
            string.IsNullOrWhiteSpace(
                plan.instruction
            )
                ? GetDefaultInstruction(
                    plan.hazard_type
                )
                : plan.instruction;

        instructionUI?.ShowAgentDecision(
            plan.hazard_type,
            CurrentDifficulty,
            severity,
            instruction,
            plan.reason
        );
    }


    // =====================================================
    // REMEDIATION / REPEAT
    // =====================================================

    private IEnumerator RepeatAfterCue(
        AgentPlan plan,
        int generation)
    {
        yield return new WaitForSeconds(
            remediationDelay
        );

        if (generation != planGeneration)
            yield break;

        if (plan == null)
            yield break;

        while (
            IsPlayerTooCloseToSpawnPoint()
        )
        {
            if (
                generation != planGeneration
            )
            {
                yield break;
            }

            instructionUI?.Show(
                "Move away from the hazard area, " +
                "then continue the training."
            );

            yield return new WaitForSeconds(
                0.5f
            );
        }

        if (
            generation != planGeneration
        )
        {
            yield break;
        }

        SpawnScenarioAndDisplay(
            plan
        );

        pendingRemediationRoutine =
            null;
    }


    // =====================================================
    // CANCEL OLD REMEDIATION
    // =====================================================

    private void CancelPendingRemediation()
    {
        if (
            pendingRemediationRoutine != null
        )
        {
            StopCoroutine(
                pendingRemediationRoutine
            );

            pendingRemediationRoutine =
                null;
        }
    }


    // =====================================================
    // PLAYER DISTANCE CHECK
    // =====================================================

    private bool IsPlayerTooCloseToSpawnPoint()
    {
        if (hazardSpawner == null)
            return false;

        GameObject currentHazard =
            hazardSpawner.GetCurrentHazard();

        if (currentHazard == null)
            return false;

        Camera playerCamera =
            Camera.main;

        if (playerCamera == null)
            return false;

        float distance =
            Vector3.Distance(
                playerCamera.transform.position,
                currentHazard.transform.position
            );

        return distance <
            minimumDistanceBeforeRespawn;
    }


    // =====================================================
    // AGENT STATUS
    // =====================================================

    private string GetAgentStatus(
        AgentPlan plan)
    {
        switch (plan.action)
        {
            case "set_difficulty":

                return
                    "DIFFICULTY ADAPTED\n" +
                    "The agent increased scenario difficulty.";

            case "advance_scenario":

                return
                    "SCENARIO ADVANCED\n" +
                    "The agent selected the next training scenario.";

            case "repeat_scenario":

                return
                    "SCENARIO REPEATED\n" +
                    "The agent is reinforcing this hazard.";

            case "spawn_hazard":

                return
                    "NEW SCENARIO\n" +
                    "The agent selected this hazard.";

            default:

                return
                    "ADAPTIVE TRAINING ACTIVE";
        }
    }


    // =====================================================
    // DEFAULT INSTRUCTIONS
    // =====================================================

    private string GetDefaultInstruction(
        string hazardType)
    {
        if (
            string.IsNullOrWhiteSpace(
                hazardType
            )
        )
        {
            return
                "Identify the hazard and take the safest available action.";
        }

        switch (
            hazardType.ToLower()
        )
        {
            case "fall":

                return
                    "Check guardrails, floor openings, and fall-protection equipment before entering the work area.";

            case "struck_by":

                return
                    "Scan for moving equipment and suspended loads, then keep outside the line of fire.";

            case "electrical":

                return
                    "Identify energized sources, maintain clearance, and verify isolation before approaching electrical equipment.";

            default:

                return
                    "Identify the hazard and take the safest available action.";
        }
    }
}