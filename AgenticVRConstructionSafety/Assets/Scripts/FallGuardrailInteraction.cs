using UnityEngine;

public class FallGuardrailInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 8f;

    [Header("UI")]
    [SerializeField] private InstructionUI instructionUI;

    private bool warningShown = false;


    private void Start()
    {
        if (instructionUI == null)
        {
            instructionUI =
                FindObjectOfType<InstructionUI>();
        }
    }


    private void Update()
    {
        Camera cam = Camera.main;

        if (cam == null)
            return;


        // ==========================================
        // CHECK DISTANCE FROM PLAYER
        // ==========================================

        float distance =
            Vector3.Distance(
                cam.transform.position,
                transform.position
            );


        // ==========================================
        // PLAYER IS TOO FAR
        // ==========================================

        if (distance > interactionDistance)
        {
            warningShown = false;
            return;
        }


        // ==========================================
        // FIND CURRENT HAZARD
        // ==========================================

        HazardSpawner spawner =
            FindObjectOfType<HazardSpawner>();

        if (spawner == null)
            return;

        GameObject currentHazard =
            spawner.GetCurrentHazard();

        if (currentHazard == null)
            return;


        FallHazard fallHazard =
            currentHazard.GetComponent<FallHazard>();

        if (fallHazard == null)
            return;


        // ==========================================
        // SHOW WARNING
        // ==========================================

        if (!warningShown)
        {
            warningShown = true;

            instructionUI?.ShowPersistent(
                "FALL HAZARD DETECTED\n\n" +
                "WARNING: Fall protection is required.\n\n" +
                "Move close to the guardrail and\n" +
                "LEFT CLICK to install protection."
            );
        }


        // ==========================================
        // INSTALL GUARDRAIL
        // ==========================================

        if (!Input.GetMouseButtonDown(0))
            return;


        Debug.Log(
            "GUARDRAIL INTERACTION DETECTED"
        );


        instructionUI?.Show(
            "FALL PROTECTION INSTALLED\n\n" +
            "Hazard resolved successfully."
        );


        fallHazard.InstallProtection();

        warningShown = false;
    }
}