using System.Collections.Generic;
using UnityEngine;

public class LearnerTelemetry : MonoBehaviour
{
    [SerializeField] private TrainingSessionManager sessionManager;

    private readonly Dictionary<int, float> exposureStart = new Dictionary<int, float>();

    public void BeginExposure(HazardBase hazard)
    {
        if (hazard == null) return;
        exposureStart[hazard.GetInstanceID()] = Time.realtimeSinceStartup;
    }

    public void RecordResponse(HazardBase hazard, bool success)
    {
        if (hazard == null || sessionManager == null) return;

        int id = hazard.GetInstanceID();
        float start = exposureStart.TryGetValue(id, out float t) ? t : Time.realtimeSinceStartup;
        int responseMs = Mathf.RoundToInt((Time.realtimeSinceStartup - start) * 1000f);
        exposureStart.Remove(id);

        sessionManager.ReportHazardResponse(
            hazard.HazardType,
            success,
            responseMs,
            hazard.Severity
        );
    }
}
