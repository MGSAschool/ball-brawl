using UnityEngine;

public class CameraFollowMouse : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target; // Drag your Player here
    [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -12f);

    [Header("Mouse Orbit Settings")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minPitch = -15f; // Min downward/upward angle
    [SerializeField] private float maxPitch = 60f;

    [Header("Damping")]
    [SerializeField] private float smoothSpeed = 10f;

    private float yaw = 0f;
    private float pitch = 20f;

    private void Start()
    {
        // Lock and hide the cursor during gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            pitch = angles.x;
            yaw = angles.y;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Read mouse delta inputs
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Calculate rotation and position around the player
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetPosition = target.position + targetRotation * offset;

        // Smoothly interpolate position and apply rotation
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}