using UnityEngine;

public class HazardSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Transform spawnPoint;

    private GameObject currentHazard;
    private int lastSpawnFrame = -1;

    public GameObject Spawn(string hazardType, int difficulty)
    {
        return SpawnHazard(hazardType, difficulty);
    }

    public GameObject SpawnHazard(string hazardType, int difficulty)
    {
        if (lastSpawnFrame == Time.frameCount)
        {
            Debug.Log("Duplicate hazard spawn request ignored.");
            return currentHazard;
        }

        lastSpawnFrame = Time.frameCount;

        ClearCurrent();

        if (string.IsNullOrWhiteSpace(hazardType))
        {
            Debug.LogError("Hazard type is empty.");
            return null;
        }

        Vector3 position = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        Quaternion rotation = spawnPoint != null
            ? spawnPoint.rotation
            : Quaternion.identity;

        GameObject hazard = GameObject.CreatePrimitive(
            PrimitiveType.Cube
        );

        hazard.transform.position = position;
        hazard.transform.rotation = rotation;
        hazard.transform.localScale = new Vector3(3f, 0.5f, 3f);

        hazard.name =
            hazardType +
            "_Hazard_Difficulty" +
            difficulty;

        BoxCollider collider =
            hazard.GetComponent<BoxCollider>();

        collider.isTrigger = true;

        Renderer renderer =
            hazard.GetComponent<Renderer>();

        if (renderer != null)
        {
            Material material =
                new Material(Shader.Find("Standard"));

            material.color = Color.red;
            renderer.material = material;
        }

        HazardBase hazardComponent = null;

        switch (hazardType.ToLower())
        {
            case "fall":
                hazardComponent =
                    hazard.AddComponent<FallHazard>();

                hazardComponent.InitializeHazard(
                    "fall",
                    4
                );

                break;

            case "struck_by":
                hazardComponent =
                    hazard.AddComponent<StruckByHazard>();

                hazardComponent.InitializeHazard(
                    "struck_by",
                    4
                );

                break;

            case "electrical":
                hazardComponent =
                    hazard.AddComponent<ElectricalHazard>();

                hazardComponent.InitializeHazard(
                    "electrical",
                    5
                );

                break;

            default:
                Debug.LogError(
                    "Unknown hazard type: " + hazardType
                );

                Destroy(hazard);
                return null;
        }

        currentHazard = hazard;

        Debug.Log(
            "HAZARD SPAWNED: " +
            currentHazard.name +
            " | Severity: " +
            hazardComponent.Severity
        );

        return currentHazard;
    }

    public void ClearCurrent()
    {
        if (currentHazard != null)
        {
            Destroy(currentHazard);
            currentHazard = null;
        }
    }

    public GameObject GetCurrentHazard()
    {
        return currentHazard;
    }
}   