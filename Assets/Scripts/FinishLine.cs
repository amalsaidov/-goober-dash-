using UnityEngine;

public class FinishLine : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        RacePlayer rp = other.GetComponent<RacePlayer>();
        if (rp != null && !rp.hasFinished)
        {
            rp.hasFinished = true;
            RaceManager.Instance?.PlayerFinished(rp);
        }
    }
}
