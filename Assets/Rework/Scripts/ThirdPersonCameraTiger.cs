using UnityEngine;

public class ThirdPersonCameraTiger : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target; // Target yang akan diikuti (Harimau)
    public float distance = 5.0f;
    public float height = 2.0f;
    public float smoothSpeed = 10f;
    
    [Header("Mouse Settings")]
    public float mouseSensitivity = 3.0f;
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 60f;

    private float currentX = 0f;
    private float currentY = 20f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("Target kamera belum diatur!");
            return;
        }

        // Input mouse
        currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
        currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);

        // Hitung posisi dan rotasi kamera
        Vector3 direction = new Vector3(0, 0, -distance);
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 desiredPosition = target.position + rotation * direction + Vector3.up * height;

        // Haluskan pergerakan kamera
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * height);
    }
}
