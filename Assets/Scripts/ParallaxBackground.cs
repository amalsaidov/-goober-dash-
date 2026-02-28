using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Range(0f, 1f)]
    public float parallaxFactor = 0.3f;

    private Camera cam;
    private Vector3 lastCamPos;

    void Start()
    {
        cam = Camera.main;
        lastCamPos = cam.transform.position;
    }

    void LateUpdate()
    {
        Vector3 delta = cam.transform.position - lastCamPos;
        transform.position += new Vector3(delta.x * parallaxFactor, delta.y * 0.2f, 0f);
        lastCamPos = cam.transform.position;
    }
}
