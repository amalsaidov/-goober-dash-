using UnityEngine;

/// <summary>
/// Placed in trigger zones below major gaps.
/// Instantly respawns any player or bot that enters the trigger.
/// </summary>
public class KillZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var rp = other.GetComponent<RacePlayer>();
        if (rp == null) return;
        rp.Respawn();
        // Also snap PlayerController to same point so y<-35 fallback stays in sync
        var pc = other.GetComponent<PlayerController>();
        if (pc != null) pc.UpdateSpawnPoint(rp.GetSpawnPoint());
    }
}
