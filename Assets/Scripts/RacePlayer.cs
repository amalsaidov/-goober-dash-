using System.Collections;
using UnityEngine;

public class RacePlayer : MonoBehaviour
{
    public bool isHuman = false;
    public bool hasFinished = false;

    private Vector3 spawnPoint;
    private Color   originalColor;
    private float   _lastBump;

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

    /// <summary>Override the stored spawn point (used by MapManager on map switch).</summary>
    public void SetSpawnPoint(Vector3 p) { spawnPoint = p; }

    /// <summary>Returns the current stored spawn point.</summary>
    public Vector3 GetSpawnPoint() => spawnPoint;

    /// <summary>Teleport to last known spawn point and zero velocity (used by KillZone).</summary>
    public void Respawn()
    {
        transform.position = spawnPoint;
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;
    }

    /// <summary>Apply and store color — survives round resets.</summary>
    public void SetPlayerColor(Color col)
    {
        originalColor = col;
        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = col;
    }

    // ── Player-to-player bump ─────────────────────────────────────────────
    void OnCollisionEnter2D(Collision2D col)
    {
        if (Time.time - _lastBump < 0.5f) return;
        if (RaceManager.Instance == null || !RaceManager.Instance.IsRacing) return;
        if (col.gameObject.layer != gameObject.layer) return;  // only other players
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;
        float dir = Mathf.Sign(transform.position.x - col.transform.position.x);
        if (dir == 0f) dir = 1f;
        // Horizontal push only — no upward force (prevents bots launching into sky)
        rb.AddForce(new Vector2(dir * 5f, 0f), ForceMode2D.Impulse);
        _lastBump = Time.time;
    }

    public void ResetForRound()
    {
        transform.position = spawnPoint;
        hasFinished = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = originalColor;  // use stored color (updated by SetPlayerColor)

        var ai = GetComponent<AIPlayer>();
        if (ai) ai.ResetWaypoint();

        var pc = GetComponent<PlayerController>();
        if (pc) pc.canControl = false;
        if (ai) ai.canMove = false;
    }
}
