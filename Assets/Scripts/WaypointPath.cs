using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    public static WaypointPath Instance;
    public Vector3[] points;

    void Awake() { Instance = this; }
}
