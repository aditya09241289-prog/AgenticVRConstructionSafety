using UnityEngine;

public class MultimodalTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    private InstructionUI instructionUI;


    // =====================================================
    // INITIALIZATION
    // =====================================================

    private void Start()
    {
        instructionUI =
            FindFirstObjectByType<InstructionUI>();

        if (instructionUI == null)
        {
            Debug.LogWarning(
                "InstructionUI not found in the scene!"
            );
        }
    }


    // =====================================================
    // TRIGGER DETECTION
    // =====================================================

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        hasTriggered = true;

        Debug.Log(
            "Player entered monitored zone. Starting multimodal fall detection..."
        );


        // =============================================
        // SHOW ANALYZING MESSAGE
        // =============================================

        if (instructionUI != null)
        {
            instructionUI.ShowPersistent(
                "MULTIMODAL SAFETY ANALYSIS\n\n" +
                "Analyzing wearable sensor data...\n" +
                "Analyzing environment conditions...\n" +
                "Analyzing vision data...\n\n" +
                "AI SAFETY AGENT PROCESSING..."
            );
        }


        // =============================================
        // FIND API CLIENT
        // =============================================

        AgentApiClient apiClient =
            FindFirstObjectByType<AgentApiClient>();


        if (apiClient == null)
        {
            Debug.LogError(
                "AgentApiClient not found in the scene!"
            );

            if (instructionUI != null)
            {
                instructionUI.Show(
                    "SAFETY SYSTEM ERROR\n\n" +
                    "Unable to connect to AI Safety Agent."
                );
            }

            return;
        }


        // =============================================
        // START FALL DETECTION
        // =============================================

        StartCoroutine(
            apiClient.RunFallDetection(
                OnDetectionSuccess,
                OnDetectionError
            )
        );
    }


    // =====================================================
    // DETECTION SUCCESS
    // =====================================================

    private void OnDetectionSuccess(
        FallDetectionResponse result
    )
    {
        Debug.Log(
            "===================================="
        );

        Debug.Log(
            "MULTIMODAL FALL ANALYSIS COMPLETE"
        );

        Debug.Log(
            "===================================="
        );


        // =============================================
        // LOG WEARABLE RESULT
        // =============================================

        Debug.Log(
            $"Wearable Probability: " +
            $"{result.wearable_probability:P2}"
        );


        // =============================================
        // LOG ENVIRONMENT RESULT
        // =============================================

        Debug.Log(
            $"Environment Probability: " +
            $"{result.environment_probability:P2}"
        );


        // =============================================
        // LOG VISION RESULT
        // =============================================

        Debug.Log(
            $"Vision Probability: " +
            $"{result.vision_probability:P2}"
        );


        // =============================================
        // LOG FINAL SCORE
        // =============================================

        Debug.Log(
            $"Final Fall Score: " +
            $"{result.final_score:P2}"
        );


        // =============================================
        // LOG FALL RESULT
        // =============================================

        Debug.Log(
            $"Fall Detected: " +
            $"{result.fall_detected}"
        );


        // =============================================
        // LOG AGENT ACTION
        // =============================================

        Debug.Log(
            $"Agent Action: " +
            $"{result.agent_action}"
        );


        // =============================================
        // LOG AI REASON
        // =============================================

        Debug.Log(
            $"Reason: " +
            $"{result.reason}"
        );


        // =============================================
        // SHOW RESULT ON SCREEN
        // =============================================

        if (instructionUI == null)
        {
            instructionUI =
                FindFirstObjectByType<InstructionUI>();
        }


        if (instructionUI != null)
        {
            instructionUI.ShowFallDetection(
                result.fall_detected,
                result.final_score,
                result.agent_action
            );
        }
        else
        {
            Debug.LogError(
                "InstructionUI not found. Cannot display fall detection result."
            );
        }
    }


    // =====================================================
    // DETECTION ERROR
    // =====================================================

    private void OnDetectionError(
        string error
    )
    {
        Debug.LogError(
            "Multimodal fall detection failed: " +
            error
        );


        if (instructionUI == null)
        {
            instructionUI =
                FindFirstObjectByType<InstructionUI>();
        }


        if (instructionUI != null)
        {
            instructionUI.Show(
                "MULTIMODAL SAFETY SYSTEM ERROR\n\n" +
                error
            );
        }
    }


    // =====================================================
    // RESET TRIGGER WHEN PLAYER LEAVES
    // =====================================================

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            hasTriggered = false;

            Debug.Log(
                "Player left monitored zone."
            );
        }
    }
}