using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;

    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 2f, -10f);

    [Header("Shake")]
    public bool  shakeEnabled = true;
    public float shakeDecay = 8f;

    [Header("Dynamic Zoom")]
    public float baseSize = 6f;
    public float sprintZoomOut = 1.5f;

    private float shakeIntensity;
    private Camera cam;
    private Rigidbody2D targetRb;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (targetRb == null)
            targetRb = target.GetComponent<Rigidbody2D>();

        // Follow
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);

        // Shake
        if (shakeIntensity > 0.01f)
        {
            transform.position += (Vector3)Random.insideUnitCircle * shakeIntensity;
            shakeIntensity = Mathf.Lerp(shakeIntensity, 0, shakeDecay * Time.deltaTime);
        }

        // Dynamic zoom — zoom out when moving fast
        if (cam != null && targetRb != null)
        {
            float speed = Mathf.Abs(targetRb.linearVelocity.x);
            float targetSize = baseSize + Mathf.Clamp(speed * 0.1f, 0, sprintZoomOut);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * 3f);
        }
    }

    public void Shake(float intensity = 0.3f, float duration = 0.2f)
    {
        if (!shakeEnabled) return;
        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
    }
}
