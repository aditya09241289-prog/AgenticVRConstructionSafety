using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class StartSessionRequest
{
    public string participant_id;
    public string condition;
}

[Serializable]
public class AgentPlan
{
    public string action;
    public string hazard_type;
    public int difficulty;
    public string instruction;
    public string reason;
    public float score;
}

[Serializable]
public class StartSessionResponse
{
    public string session_id;
    public string condition;
    public AgentPlan first_plan;
}

[Serializable]
public class LearnerEventPayload
{
    public string session_id;
    public string event_type;
    public string hazard_type;
    public bool success;
    public int response_time_ms;
    public int severity;
}

[Serializable]
public class FallDetectionResponse
{
    public string event_type;

    public float wearable_probability;
    public float environment_probability;
    public float vision_probability;
    public float final_score;

    public bool fall_detected;

    public string agent_action;
    public string reason;
}

public class AgentApiClient : MonoBehaviour
{
    private const string BaseUrl =
        "http://127.0.0.1:8001";

    private const string HealthUrl =
        BaseUrl + "/health";

    private const float BackendWaitTimeout = 30f;

    private bool backendReady = false;

    public IEnumerator StartSession(
        string participantId,
        string condition,
        Action<StartSessionResponse> onSuccess,
        Action<string> onError)
    {
        yield return WaitForBackendReady(
            success =>
            {
                backendReady = success;
            }
        );

        if (!backendReady)
        {
            onError?.Invoke(
                "Construction Safety backend did not become ready on port 8001."
            );

            yield break;
        }

        var payload = new StartSessionRequest
        {
            participant_id = participantId,
            condition = condition
        };

        string json =
            JsonUtility.ToJson(payload);

        yield return PostJson(
            "/sessions/start",
            json,
            response =>
            {
                try
                {
                    StartSessionResponse result =
                        JsonUtility.FromJson<StartSessionResponse>(
                            response
                        );

                    if (result == null)
                    {
                        onError?.Invoke(
                            "Start session returned an empty response."
                        );

                        return;
                    }

                    onSuccess?.Invoke(result);
                }
                catch (Exception ex)
                {
                    onError?.Invoke(
                        "Failed to parse start-session response: " +
                        ex.Message
                    );
                }
            },
            onError
        );
    }

    public IEnumerator SendEvent(
        LearnerEventPayload payload,
        Action<AgentPlan> onSuccess,
        Action<string> onError)
    {
        yield return WaitForBackendReady(
            success =>
            {
                backendReady = success;
            }
        );

        if (!backendReady)
        {
            onError?.Invoke(
                "Construction Safety backend is unavailable on port 8001."
            );

            yield break;
        }

        string json =
            JsonUtility.ToJson(payload);

        yield return PostJson(
            "/agent/event",
            json,
            response =>
            {
                try
                {
                    AgentPlan result =
                        JsonUtility.FromJson<AgentPlan>(
                            response
                        );

                    if (result == null)
                    {
                        onError?.Invoke(
                            "Agent event returned an empty response."
                        );

                        return;
                    }

                    onSuccess?.Invoke(result);
                }
                catch (Exception ex)
                {
                    onError?.Invoke(
                        "Failed to parse agent-event response: " +
                        ex.Message
                    );
                }
            },
            onError
        );
    }

    public IEnumerator RunFallDetection(
        Action<FallDetectionResponse> onSuccess,
        Action<string> onError)
    {
        UnityEngine.Debug.Log(
            "Waiting for Construction Safety backend..."
        );

        yield return WaitForBackendReady(
            success =>
            {
                backendReady = success;
            }
        );

        if (!backendReady)
        {
            onError?.Invoke(
                "Construction Safety backend could not be reached on port 8001."
            );

            yield break;
        }

        UnityEngine.Debug.Log(
            "Construction Safety backend is ready."
        );

        UnityEngine.Debug.Log(
            "Requesting multimodal fall detection..."
        );

        yield return PostJson(
            "/agent/fall-detection",
            "{}",
            response =>
            {
                try
                {
                    FallDetectionResponse result =
                        JsonUtility.FromJson<FallDetectionResponse>(
                            response
                        );

                    if (result == null)
                    {
                        onError?.Invoke(
                            "Fall detection returned an empty response."
                        );

                        return;
                    }

                    UnityEngine.Debug.Log(
                        "========================================"
                    );

                    UnityEngine.Debug.Log(
                        "MULTIMODAL FALL DETECTION"
                    );

                    UnityEngine.Debug.Log(
                        $"Wearable: " +
                        $"{result.wearable_probability:P2}"
                    );

                    UnityEngine.Debug.Log(
                        $"Environment: " +
                        $"{result.environment_probability:P2}"
                    );

                    UnityEngine.Debug.Log(
                        $"Vision: " +
                        $"{result.vision_probability:P2}"
                    );

                    UnityEngine.Debug.Log(
                        $"Final Score: " +
                        $"{result.final_score:P2}"
                    );

                    UnityEngine.Debug.Log(
                        $"Fall Detected: " +
                        result.fall_detected
                    );

                    UnityEngine.Debug.Log(
                        $"Agent Action: " +
                        result.agent_action
                    );

                    UnityEngine.Debug.Log(
                        $"Reason: " +
                        result.reason
                    );

                    UnityEngine.Debug.Log(
                        "========================================"
                    );

                    onSuccess?.Invoke(result);
                }
                catch (Exception ex)
                {
                    onError?.Invoke(
                        "Failed to parse fall-detection response: " +
                        ex.Message
                    );
                }
            },
            onError
        );
    }

    private IEnumerator WaitForBackendReady(
        Action<bool> result)
    {
        if (backendReady)
        {
            result?.Invoke(true);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < BackendWaitTimeout)
        {
            using (
                UnityWebRequest request =
                    UnityWebRequest.Get(HealthUrl)
            )
            {
                request.timeout = 2;

                yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER

                if (
                    request.result ==
                    UnityWebRequest.Result.Success
                )

#else

                if (
                    !request.isNetworkError &&
                    !request.isHttpError
                )

#endif
                {
                    result?.Invoke(true);
                    yield break;
                }
            }

            elapsed += 0.5f;

            yield return new WaitForSeconds(0.5f);
        }

        UnityEngine.Debug.LogError(
            "Construction Safety backend did not respond at:\n" +
            HealthUrl
        );

        result?.Invoke(false);
    }

    private IEnumerator PostJson(
        string route,
        string json,
        Action<string> onSuccess,
        Action<string> onError)
    {
        string fullUrl =
            BaseUrl + route;

        byte[] body =
            Encoding.UTF8.GetBytes(json);

        UnityEngine.Debug.Log(
            "POST " + fullUrl
        );

        using (
            UnityWebRequest request =
                new UnityWebRequest(
                    fullUrl,
                    "POST"
                )
        )
        {
            request.uploadHandler =
                new UploadHandlerRaw(body);

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            request.timeout = 180;

            yield return request.SendWebRequest();

            if (
                request.result !=
                UnityWebRequest.Result.Success
            )
            {
                string responseText =
                    request.downloadHandler != null
                        ? request.downloadHandler.text
                        : "";

                string message =
                    $"Agent API error: " +
                    $"{request.responseCode} " +
                    $"{request.error}\n" +
                    $"URL: {fullUrl}\n" +
                    $"Response: {responseText}";

                UnityEngine.Debug.LogError(
                    message
                );

                onError?.Invoke(message);

                yield break;
            }

            onSuccess?.Invoke(
                request.downloadHandler.text
            );
        }
    }
}