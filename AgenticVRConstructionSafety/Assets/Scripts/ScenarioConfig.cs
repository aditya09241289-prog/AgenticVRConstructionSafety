using UnityEngine;

[CreateAssetMenu(fileName = "ScenarioConfig", menuName = "Safety Training/Scenario Config")]
public class ScenarioConfig : ScriptableObject
{
    public string scenarioId = "scenario-01";
    public string hazardType = "fall";
    [Range(1, 5)] public int baseSeverity = 3;
    [TextArea] public string learningObjective;
    [TextArea] public string correctProcedure;
}
