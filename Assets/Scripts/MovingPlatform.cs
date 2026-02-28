using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public float distance = 3f;
    public float speed = 2f;
    public bool vertical = false;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * distance;
        if (vertical)
            transform.position = startPos + new Vector3(0, offset, 0);
        else
            transform.position = startPos + new Vector3(offset, 0, 0);
    }
}
