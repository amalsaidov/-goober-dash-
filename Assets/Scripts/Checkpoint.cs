using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private bool used = false;
    private SpriteRenderer sr;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            used = true;
            pc.UpdateSpawnPoint(transform.position + Vector3.up * 2f);
            if (sr) sr.color = new Color(0.2f, 1f, 0.4f, 0.6f);
        }
    }
}
