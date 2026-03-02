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
        RacePlayer rp = other.GetComponent<RacePlayer>();
        if (pc == null && rp == null) return;
        used = true;
        var pos = transform.position + Vector3.up * 2f;
        // Keep both spawn points in sync so KillZone (RacePlayer.Respawn)
        // and the y<-35 fallback (PlayerController.Respawn) both use the checkpoint
        if (pc != null) pc.UpdateSpawnPoint(pos);
        if (rp != null) rp.SetSpawnPoint(pos);
        if (sr) sr.color = new Color(0.2f, 1f, 0.4f, 0.18f);
    }
}
