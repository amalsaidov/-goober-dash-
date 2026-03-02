using UnityEngine;

public class FinishLine : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        RacePlayer rp = other.GetComponent<RacePlayer>();
        if (rp != null && !rp.hasFinished)
        {
            rp.hasFinished = true;
            // Only update PlayerController's in-race respawn (kill zone recovery).
            // Do NOT touch RacePlayer.spawnPoint — that is used by ResetForRound/Revive
            // and must stay at the original start position for Play Again.
            var pc = rp.GetComponent<PlayerController>();
            if (pc) pc.UpdateSpawnPoint(transform.position + Vector3.up * 1f);
            RaceManager.Instance?.PlayerFinished(rp);
        }
    }
}
