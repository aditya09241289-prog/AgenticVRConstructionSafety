using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class BackendLauncher : MonoBehaviour
{
    public static BackendLauncher Instance { get; private set; }

    [Header("Backend")]
    [SerializeField] private float startupTimeoutSeconds = 30f;
    [SerializeField] private float healthCheckInterval = 0.5f;

    private System.Diagnostics.Process backendProcess;
    private bool backendReady = false;

    public bool IsBackendReady => backendReady;

    // Construction Safety backend uses PORT 8001
    private const string HealthUrl =
        "http://127.0.0.1:8001/health";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        UnityEngine.Debug.Log(
            "CONSTRUCTION SAFETY BACKEND LAUNCHER STARTED"
        );

        StartCoroutine(StartBackendRoutine());
    }

    private IEnumerator StartBackendRoutine()
    {
        string backendPath = Path.GetFullPath(
            Path.Combine(
                Application.dataPath,
                "..",
                "SafetyBackend.exe"
            )
        );

        UnityEngine.Debug.Log(
            "Looking for SafetyBackend.exe at:\n" +
            backendPath
        );

        // --------------------------------------------------
        // CHECK FILE
        // --------------------------------------------------

        if (!File.Exists(backendPath))
        {
            UnityEngine.Debug.LogError(
                "SafetyBackend.exe NOT FOUND.\n\n" +
                "Expected location:\n" +
                backendPath
            );

            yield break;
        }

        UnityEngine.Debug.Log(
            "SafetyBackend.exe FOUND."
        );

        // --------------------------------------------------
        // START BACKEND
        // --------------------------------------------------

        try
        {
            System.Diagnostics.ProcessStartInfo startInfo =
                new System.Diagnostics.ProcessStartInfo();

            startInfo.FileName = backendPath;

            startInfo.WorkingDirectory =
                Path.GetDirectoryName(backendPath);

            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            startInfo.WindowStyle =
                System.Diagnostics.ProcessWindowStyle.Hidden;

            backendProcess =
                System.Diagnostics.Process.Start(startInfo);

            if (backendProcess == null)
            {
                UnityEngine.Debug.LogError(
                    "FAILED TO START SafetyBackend.exe"
                );

                yield break;
            }

            UnityEngine.Debug.Log(
                "SafetyBackend.exe started automatically."
            );
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(
                "BACKEND START ERROR:\n" +
                ex.Message
            );

            yield break;
        }

        // --------------------------------------------------
        // WAIT FOR BACKEND
        // --------------------------------------------------

        float elapsed = 0f;

        while (elapsed < startupTimeoutSeconds)
        {
            if (backendProcess != null)
            {
                try
                {
                    if (backendProcess.HasExited)
                    {
                        UnityEngine.Debug.LogError(
                            "SafetyBackend.exe exited unexpectedly."
                        );

                        yield break;
                    }
                }
                catch
                {
                }
            }

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
                    backendReady = true;

                    UnityEngine.Debug.Log(
                        "========================================"
                    );

                    UnityEngine.Debug.Log(
                        "CONSTRUCTION SAFETY BACKEND READY"
                    );

                    UnityEngine.Debug.Log(
                        "http://127.0.0.1:8001/health"
                    );

                    UnityEngine.Debug.Log(
                        "========================================"
                    );

                    yield break;
                }
            }

            elapsed += healthCheckInterval;

            yield return new WaitForSeconds(
                healthCheckInterval
            );
        }

        backendReady = false;

        UnityEngine.Debug.LogError(
            "========================================"
        );

        UnityEngine.Debug.LogError(
            "BACKEND STARTUP TIMEOUT"
        );

        UnityEngine.Debug.LogError(
            "Backend did not become ready on port 8001."
        );

        UnityEngine.Debug.LogError(
            "========================================"
        );
    }

    private void OnApplicationQuit()
    {
        StopBackend();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            StopBackend();
            Instance = null;
        }
    }

    private void StopBackend()
    {
        if (backendProcess == null)
            return;

        try
        {
            if (!backendProcess.HasExited)
            {
                backendProcess.Kill();

                try
                {
                    backendProcess.WaitForExit(2000);
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        backendProcess = null;
        backendReady = false;
    }
}