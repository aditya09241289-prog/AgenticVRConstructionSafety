using UnityEngine;

public class FallHazard : HazardBase
{
    [Header("Fall-risk example")]
    [SerializeField] private GameObject guardrail;

    private bool playerInHazard = false;
    private bool warningShown = false;

    private void Reset()
    {
        hazardType = "fall";
        severity = 4;
    }

    protected override void Start()
    {
        base.Start();

        if (guardrail != null)
        {
            guardrail.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInHazard = true;

        Debug.Log(
            "FALL HAZARD TRIGGERED - Player entered unsafe zone!"
        );

        if (!warningShown)
        {
            warningShown = true;

            Debug.Log(
                "FALL HAZARD WARNING - Install fall protection before proceeding."
            );
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInHazard = false;

        Debug.Log(
            "Player left the fall hazard zone."
        );
    }

    public void InstallProtection()
    {
        if (resolved)
            return;

        // The learner does NOT need to remain inside
        // the danger zone to install protection.
        // The interaction script handles the distance
        // check to the guardrail.

        if (guardrail != null)
        {
            guardrail.SetActive(true);
        }

        Debug.Log(
            "FALL HAZARD - CORRECT RESPONSE"
        );

        Debug.Log(
            "Fall hazard resolved correctly."
        );

        playerInHazard = false;

        Collider hazardCollider =
            GetComponent<Collider>();

        if (hazardCollider != null)
        {
            hazardCollider.enabled = false;
        }

        ResolveCorrectly();
    }

    public void EnterUnsafeZone()
    {
        if (resolved)
            return;

        Debug.Log(
            "FALL HAZARD - UNSAFE RESPONSE"
        );

        ResolveIncorrectly();
    }
}