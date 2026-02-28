using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 2f;
    public float patrolDistance = 3f;

    private Vector3 startPos;
    private int direction = 1;
    private SpriteRenderer sr;

    void Awake()
    {
        startPos = transform.position;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

        if (transform.position.x > startPos.x + patrolDistance) direction = -1;
        if (transform.position.x < startPos.x - patrolDistance) direction = 1;

        sr.flipX = direction < 0;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        PlayerController player = col.gameObject.GetComponent<PlayerController>();
        if (player == null) return;

        bool stomped = false;
        foreach (var contact in col.contacts)
        {
            if (contact.normal.y > 0.5f) { stomped = true; break; }
        }

        if (stomped)
        {
            player.BounceUp();
            ScoreManager.Instance?.AddScore(100);
            Destroy(gameObject);
        }
        else
        {
            player.TakeDamage();
        }
    }
}
