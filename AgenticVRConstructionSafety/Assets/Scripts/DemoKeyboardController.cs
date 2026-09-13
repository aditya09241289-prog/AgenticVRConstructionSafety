using UnityEngine;

public class DemoKeyboardController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Third Person Camera")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float cameraDistance = 5f;
    [SerializeField] private float cameraHeight = 2.4f;
    [SerializeField] private float cameraLookHeight = 1.25f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minPitch = -15f;
    [SerializeField] private float maxPitch = 55f;

    private float cameraYaw;
    private float cameraPitch = 12f;

    private Vector3 movementDirection;

    private void Awake()
    {
        if (playerCamera == null)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                playerCamera = mainCamera.transform;
            }
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                "DemoKeyboardController: Main Camera not found."
            );
        }
    }

    private void Start()
    {
        if (playerCamera != null)
        {
            Vector3 cameraEuler =
                playerCamera.eulerAngles;

            cameraYaw = cameraEuler.y;
            cameraPitch = 12f;

            SetCameraPosition();
        }

        LockCursor();

        Debug.Log(
            "Third-person controller started. " +
            "Mouse look is active."
        );
    }

    private void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandlePlayerRotation();
        HandleCursor();
    }

    private void LateUpdate()
    {
        SetCameraPosition();
    }

    private void HandleMovement()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A))
        {
            horizontal -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            horizontal += 1f;
        }

        if (Input.GetKey(KeyCode.W))
        {
            vertical += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            vertical -= 1f;
        }

        Quaternion yawRotation =
            Quaternion.Euler(
                0f,
                cameraYaw,
                0f
            );

        Vector3 forward =
            yawRotation * Vector3.forward;

        Vector3 right =
            yawRotation * Vector3.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        movementDirection =
            forward * vertical +
            right * horizontal;

        if (movementDirection.sqrMagnitude > 1f)
        {
            movementDirection.Normalize();
        }

        float speed =
            Input.GetKey(KeyCode.LeftShift)
                ? sprintSpeed
                : moveSpeed;

        transform.position +=
            movementDirection *
            speed *
            Time.deltaTime;
    }

    private void HandleMouseLook()
    {
        if (playerCamera == null)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        float mouseX =
            Input.GetAxisRaw("Mouse X") *
            mouseSensitivity;

        float mouseY =
            Input.GetAxisRaw("Mouse Y") *
            mouseSensitivity;

        cameraYaw += mouseX;

        cameraPitch -= mouseY;

        cameraPitch =
            Mathf.Clamp(
                cameraPitch,
                minPitch,
                maxPitch
            );
    }

    private void HandlePlayerRotation()
    {
        if (movementDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector3 direction =
            movementDirection;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );
    }

    private void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            LockCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;
    }

    private void SetCameraPosition()
    {
        if (playerCamera == null)
        {
            return;
        }

        Vector3 target =
            transform.position +
            Vector3.up *
            cameraLookHeight;

        Quaternion rotation =
            Quaternion.Euler(
                cameraPitch,
                cameraYaw,
                0f
            );

        Vector3 position =
            target -
            rotation *
            Vector3.forward *
            cameraDistance;

        position.y +=
            cameraHeight -
            cameraLookHeight;

        playerCamera.position =
            position;

        playerCamera.rotation =
            rotation;
    }
}