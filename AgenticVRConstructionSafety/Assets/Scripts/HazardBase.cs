using UnityEngine;

public abstract class HazardBase : MonoBehaviour
{
    [SerializeField] protected string hazardType = "fall";
    [SerializeField, Range(1, 5)] protected int severity = 2;
    [SerializeField] protected LearnerTelemetry telemetry;

    protected bool resolved;

    public string HazardType => hazardType;
    public int Severity => severity;

    // Used by HazardSpawner for runtime-created hazards.
    public void InitializeHazard(string type, int hazardSeverity)
    {
        hazardType = type;
        severity = Mathf.Clamp(hazardSeverity, 1, 5);
    }

    protected virtual void Start()
    {
        if (telemetry == null)
            telemetry = FindObjectOfType<LearnerTelemetry>();

        telemetry?.BeginExposure(this);
    }

    public virtual void ResolveCorrectly()
    {
        if (resolved) return;

        resolved = true;
        telemetry?.RecordResponse(this, true);
        OnResolved(true);
    }

    public virtual void ResolveIncorrectly()
    {
        if (resolved) return;

        resolved = true;
        telemetry?.RecordResponse(this, false);
        OnResolved(false);
    }

    protected virtual void OnResolved(bool success)
    {
        gameObject.SetActive(false);
    }
}