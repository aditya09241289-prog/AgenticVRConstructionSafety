using UnityEngine;

public class StruckByHazard : HazardBase
{
    [Header("Struck-by Hazard")]
    [SerializeField] private Transform movingEquipment;

    [SerializeField] private float speed = 1.5f;

    [SerializeField]
    private Vector3 localTravel =
        new Vector3(4f, 0f, 0f);

    [Header("Safe Interaction")]
    [SerializeField] private float interactionDistance = 12f;

    [Header("Unsafe Response")]
    [SerializeField] private float unsafeResponseDelay = 5f;

    private Vector3 start;

    private bool playerInLineOfFire = false;

    private bool warningShown = false;

    private float timeEnteredHazard = -1f;


    // =====================================================
    // RESET
    // =====================================================

    private void Reset()
    {
        hazardType = "struck_by";
        severity = 4;
    }


    // =====================================================
    // START
    // =====================================================

    protected override void Start()
    {
        base.Start();

        if (movingEquipment != null)
        {
            start =
                movingEquipment.localPosition;
        }
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        // -------------------------------------------------
        // Animate moving equipment if available.
        // -------------------------------------------------

        if (
            movingEquipment != null &&
            !resolved
        )
        {
            float phase =
                (Mathf.Sin(
                    Time.time * speed
                ) + 1f) * 0.5f;

            movingEquipment.localPosition =
                Vector3.Lerp(
                    start,
                    start + localTravel,
                    phase
                );
        }


        // -------------------------------------------------
        // LEFT CLICK = SAFE ROUTE
        // -------------------------------------------------

        if (
            !resolved &&
            Input.GetMouseButtonDown(0)
        )
        {
            TryChooseSafeRoute();
        }


        // -------------------------------------------------
        // UNSAFE RESPONSE TIMER
        // -------------------------------------------------

        if (
            playerInLineOfFire &&
            !resolved &&
            timeEnteredHazard >= 0f
        )
        {
            float elapsed =
                Time.time - timeEnteredHazard;

            if (
                elapsed >= unsafeResponseDelay
            )
            {
                EnterLineOfFire();
            }
        }
    }


    // =====================================================
    // PLAYER ENTERS LINE OF FIRE
    // =====================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        if (!other.CompareTag("Player"))
            return;

        if (resolved)
            return;

        playerInLineOfFire = true;

        timeEnteredHazard =
            Time.time;

        Debug.Log(
            "STRUCK-BY HAZARD TRIGGERED - " +
            "Player entered line of fire!"
        );

        if (!warningShown)
        {
            warningShown = true;

            Debug.Log(
                "STRUCK-BY WARNING - " +
                "Move outside the line of fire."
            );
        }
    }


    // =====================================================
    // PLAYER LEAVES LINE OF FIRE
    // =====================================================

    private void OnTriggerExit(
        Collider other
    )
    {
        if (!other.CompareTag("Player"))
            return;

        playerInLineOfFire = false;

        timeEnteredHazard = -1f;

        Debug.Log(
            "Player left the struck-by hazard zone."
        );
    }


    // =====================================================
    // SAFE ROUTE INTERACTION
    // =====================================================

    private void TryChooseSafeRoute()
    {
        if (resolved)
            return;

        Camera cam =
            Camera.main;

        if (cam == null)
        {
            Debug.LogWarning(
                "Cannot choose safe route: " +
                "Main Camera not found."
            );

            return;
        }

        float distance =
            Vector3.Distance(
                cam.transform.position,
                transform.position
            );

        if (
            distance >
            interactionDistance
        )
        {
            Debug.Log(
                "Move closer to the struck-by hazard " +
                "to choose the safe route."
            );

            return;
        }

        Debug.Log(
            "SAFE ROUTE SELECTED"
        );

        ChooseSafeRoute();
    }


    // =====================================================
    // CORRECT RESPONSE
    // =====================================================

    public void ChooseSafeRoute()
    {
        if (resolved)
            return;

        Debug.Log(
            "STRUCK-BY HAZARD - CORRECT RESPONSE"
        );

        Debug.Log(
            "Learner selected a safe route " +
            "outside the line of fire."
        );

        playerInLineOfFire = false;

        timeEnteredHazard = -1f;

        Collider hazardCollider =
            GetComponent<Collider>();

        if (hazardCollider != null)
        {
            hazardCollider.enabled = false;
        }

        ResolveCorrectly();
    }


    // =====================================================
    // INCORRECT RESPONSE
    // =====================================================

    public void EnterLineOfFire()
    {
        if (resolved)
            return;

        Debug.Log(
            "STRUCK-BY HAZARD - UNSAFE RESPONSE"
        );

        Debug.Log(
            "Learner remained in the line of fire too long."
        );

        playerInLineOfFire = false;

        timeEnteredHazard = -1f;

        ResolveIncorrectly();
    }
}