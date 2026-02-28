using System.Collections;
using UnityEngine;

public class RacePlayer : MonoBehaviour
{
    public bool isHuman = false;
    public bool hasFinished = false;

    private Vector3 spawnPoint;
    private Color   originalColor;

    void Start()
    {
        spawnPoint    = transform.position;
        var sr        = GetComponent<SpriteRenderer>();
        if (sr) originalColor = sr.color;
    }

    public void OnRaceStart()
    {
        var ai = GetComponent<AIPlayer>();
        if (ai) ai.canMove = true;

        var pc = GetComponent<PlayerController>();
        if (pc) pc.canControl = true;
    }

    public void Eliminate()
    {
        var ai = GetComponent<AIPlayer>();
        if (ai) ai.canMove = false;

        var pc = GetComponent<PlayerController>();
        if (pc) pc.canControl = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);

        if (isHuman)
            UIManager.Instance?.ShowMessage(
                LocalizationManager.Instance?.Get("msg.youreout") ?? "YOU'RE OUT!", Color.red);

        StartCoroutine(FadeAndDisable());
    }

    IEnumerator FadeAndDisable()
    {
        yield return new WaitForSeconds(2f);
        gameObject.SetActive(false);
    }

    // Revive for Play Again / Return to Main Menu
    public void Revive()
    {
        StopAllCoroutines();
        gameObject.SetActive(true);
        hasFinished = false;
        transform.position = spawnPoint;

        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = originalColor;

        var ai = GetComponent<AIPlayer>();
        if (ai) { ai.canMove = false; ai.ResetWaypoint(); }

        var pc = GetComponent<PlayerController>();
        if (pc) pc.canControl = false;
    }

    /// <summary>Teleport to last known spawn point and zero velocity (used by KillZone).</summary>
    public void Respawn()
    {
        transform.position = spawnPoint;
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;
    }

    public void ResetForRound()
    {
        transform.position = spawnPoint;
        hasFinished = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = isHuman ? new Color(0.2f, 0.55f, 1f) : originalColor;

        var ai = GetComponent<AIPlayer>();
        if (ai) ai.ResetWaypoint();

        var pc = GetComponent<PlayerController>();
        if (pc) pc.canControl = false;
        if (ai) ai.canMove = false;
    }
}
