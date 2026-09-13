using System.Collections;
using TMPro;
using UnityEngine;

public class InstructionUI : MonoBehaviour
{
    [Header("Main UI")]
    [SerializeField] private TMP_Text instructionText;

    [Header("Display")]
    [SerializeField] private float visibleSeconds = 8f;

    private Coroutine hideRoutine;


    // =====================================================
    // NORMAL TEMPORARY MESSAGE
    // =====================================================

    public void Show(string message)
    {
        if (
            instructionText == null ||
            string.IsNullOrWhiteSpace(message)
        )
        {
            return;
        }

        StopCurrentHideTimer();

        instructionText.text = message;
        instructionText.gameObject.SetActive(true);

        RestartHideTimer();
    }


    // =====================================================
    // PERSISTENT MESSAGE
    // =====================================================

    public void ShowPersistent(string message)
    {
        if (
            instructionText == null ||
            string.IsNullOrWhiteSpace(message)
        )
        {
            return;
        }

        StopCurrentHideTimer();

        instructionText.text = message;
        instructionText.gameObject.SetActive(true);
    }


    // =====================================================
    // HIDE MESSAGE
    // =====================================================

    public void Hide()
    {
        StopCurrentHideTimer();

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
        }
    }


    // =====================================================
    // SHOW SCENARIO
    // =====================================================

    public void ShowScenario(
        string hazardType,
        int difficulty,
        int severity,
        string instruction,
        string agentStatus = null
    )
    {
        if (instructionText == null)
            return;

        StopCurrentHideTimer();

        string hazardName =
            FormatHazardName(hazardType);

        string severityBar =
            BuildSeverityBar(severity);

        string message =
            "CONSTRUCTION SAFETY TRAINING\n\n" +

            "HAZARD\n" +
            hazardName +
            "\n\n" +

            "DIFFICULTY\n" +
            $"LEVEL {difficulty} / 5\n\n" +

            "SEVERITY\n" +
            $"{severityBar}  {severity} / 5\n\n" +

            "INSTRUCTION\n" +
            instruction;

        if (!string.IsNullOrWhiteSpace(agentStatus))
        {
            message +=
                "\n\n" +
                "AI TRAINING AGENT\n" +
                agentStatus;
        }

        instructionText.text = message;
        instructionText.gameObject.SetActive(true);

        RestartHideTimer();
    }


    // =====================================================
    // SHOW AGENT DECISION
    // =====================================================

    public void ShowAgentDecision(
        string hazardType,
        int difficulty,
        int severity,
        string instruction,
        string reason
    )
    {
        if (instructionText == null)
            return;

        StopCurrentHideTimer();

        string hazardName =
            FormatHazardName(hazardType);

        string severityBar =
            BuildSeverityBar(severity);

        string message =
            "CONSTRUCTION SAFETY TRAINING\n\n" +

            "HAZARD\n" +
            hazardName +
            "\n\n" +

            "DIFFICULTY\n" +
            $"LEVEL {difficulty} / 5\n\n" +

            "SEVERITY\n" +
            $"{severityBar}  {severity} / 5\n\n" +

            "AI TRAINING AGENT\n" +
            instruction;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            message +=
                "\n\n" +
                "ADAPTATION REASON\n" +
                reason;
        }

        instructionText.text = message;
        instructionText.gameObject.SetActive(true);

        RestartHideTimer();
    }


    // =====================================================
    // MULTIMODAL FALL DETECTION RESULT
    // =====================================================

    public void ShowFallDetection(
        bool fallDetected,
        float confidence,
        string agentAction
    )
    {
        if (instructionText == null)
            return;

        // Stop any previous timer.
        StopCurrentHideTimer();

        string result =
            fallDetected
                ? "⚠ FALL DETECTED"
                : "✓ NO FALL DETECTED";

        string riskLevel;

        if (confidence >= 0.80f)
        {
            riskLevel = "HIGH";
        }
        else if (confidence >= 0.50f)
        {
            riskLevel = "MODERATE";
        }
        else
        {
            riskLevel = "LOW";
        }

        string message =
            "MULTIMODAL FALL DETECTION\n\n" +

            "STATUS\n" +
            result +
            "\n\n" +

            "FINAL FALL SCORE\n" +
            $"{confidence * 100f:F1}%\n\n" +

            "RISK LEVEL\n" +
            riskLevel +
            "\n\n" +

            "SAFETY AGENT ACTION\n" +
            agentAction;

        if (fallDetected)
        {
            message +=
                "\n\n" +

                "EMERGENCY RESPONSE\n" +

                "A potential fall event has been detected. " +
                "The AI safety agent has triggered the appropriate safety response.";
        }
        else
        {
            message +=
                "\n\n" +

                "TRAINING STATUS\n" +

                "No fall event detected. Continue the safety training scenario.";
        }

        // Display the result.
        instructionText.text = message;

        instructionText.gameObject.SetActive(true);

        // IMPORTANT:
        // No RestartHideTimer() here.
        //
        // The fall detection result remains visible
        // until another UI message replaces it.
    }


    // =====================================================
    // SEVERITY BAR
    // =====================================================

    private string BuildSeverityBar(int severity)
    {
        severity =
            Mathf.Clamp(
                severity,
                1,
                5
            );

        string bar = "";

        for (int i = 0; i < 5; i++)
        {
            if (i < severity)
            {
                bar += "■ ";
            }
            else
            {
                bar += "□ ";
            }
        }

        return bar;
    }


    // =====================================================
    // HAZARD NAME
    // =====================================================

    private string FormatHazardName(
        string hazardType
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                hazardType
            )
        )
        {
            return "UNKNOWN";
        }

        switch (
            hazardType.ToLower()
        )
        {
            case "fall":
                return "FALL HAZARD";

            case "struck_by":
                return "STRUCK-BY HAZARD";

            case "electrical":
                return "ELECTRICAL HAZARD";

            default:
                return hazardType.ToUpper();
        }
    }


    // =====================================================
    // HIDE TIMER
    // =====================================================

    private void RestartHideTimer()
    {
        StopCurrentHideTimer();

        hideRoutine =
            StartCoroutine(
                HideLater()
            );
    }


    private void StopCurrentHideTimer()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(
                hideRoutine
            );

            hideRoutine = null;
        }
    }


    private IEnumerator HideLater()
    {
        yield return new WaitForSeconds(
            visibleSeconds
        );

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(
                false
            );
        }

        hideRoutine = null;
    }
}