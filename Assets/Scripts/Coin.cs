using UnityEngine;

public class Coin : MonoBehaviour
{
    public int value = 10;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Bob up and down
        float y = startPos.y + Mathf.Sin(Time.time * 3f) * 0.15f;
        transform.position = new Vector3(startPos.x, y, startPos.z);

        // Spin
        transform.Rotate(0, 0, 120f * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            ScoreManager.Instance?.AddScore(value);
            Destroy(gameObject);
        }
    }
}
