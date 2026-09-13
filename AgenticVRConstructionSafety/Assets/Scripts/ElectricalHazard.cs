using UnityEngine;

public class ElectricalHazard : HazardBase
{
    [Header("Electrical Hazard")]
    [SerializeField] private GameObject warningIndicator;
    [SerializeField] private bool energized = true;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 8f;

    private bool playerInHazard = false;
    private bool responseRecorded = false;

    private void Reset()
    {
        hazardType = "electrical";
        severity = 5;
    }

    protected override void Start()
    {
        base.Start();

        energized = true;
        responseRecorded = false;

        if (warningIndicator != null)
        {
            warningIndicator.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (resolved)
            return;

        playerInHazard = true;

        Debug.Log(
            "ELECTRICAL HAZARD TRIGGERED - Energized equipment detected!"
        );

        if (energized)
        {
            Debug.Log(
                "ELECTRICAL WARNING - Do NOT approach or touch equipment before isolation."
            );
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInHazard = false;

        Debug.Log(
            "Player left the electrical hazard zone."
        );
    }

    private void Update()
    {
        if (resolved)
            return;

        if (!playerInHazard)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            IsolatePower();
        }
    }

    public void IsolatePower()
    {
        if (resolved)
            return;

        Camera cam = Camera.main;

        if (cam != null)
        {
            float distance =
                Vector3.Distance(
                    cam.transform.position,
                    transform.position
                );

            if (distance > interactionDistance)
            {
                Debug.Log(
                    "Move closer to the electrical hazard to isolate power."
                );

                return;
            }
        }

        if (!energized)
        {
            Debug.Log(
                "Electrical hazard is already isolated."
            );

            return;
        }

        energized = false;

        if (warningIndicator != null)
        {
            warningIndicator.SetActive(false);
        }

        Debug.Log(
            "ELECTRICAL HAZARD - CORRECT RESPONSE"
        );

        Debug.Log(
            "Power isolated safely."
        );

        playerInHazard = false;

        Collider hazardCollider =
            GetComponent<Collider>();

        if (hazardCollider != null)
        {
            hazardCollider.enabled = false;
        }

        RecordCorrectResponse();
    }

    public void TouchBeforeIsolation()
    {
        if (resolved)
            return;

        if (!energized)
            return;

        if (responseRecorded)
            return;

        responseRecorded = true;

        Debug.Log(
            "ELECTRICAL HAZARD - UNSAFE RESPONSE"
        );

        Debug.Log(
            "Learner attempted to approach energized equipment before isolation."
        );

        ResolveIncorrectly();
    }

    private void RecordCorrectResponse()
    {
        if (resolved)
            return;

        responseRecorded = true;

        ResolveCorrectly();
    }
}