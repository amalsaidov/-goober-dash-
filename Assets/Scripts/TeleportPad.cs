using UnityEngine;
using System.Collections;

/// <summary>
/// Teleports any player/bot that steps on it to exitPoint.
/// Visual: pulsing cyan/purple portal circle. Cooldown prevents re-trigger.
/// </summary>
public class TeleportPad : MonoBehaviour
{
    /// <summary>World-space destination. Set by SceneSetup.</summary>
    public Vector3 exitPoint;

    SpriteRenderer _sr;
    Color          _baseColor;
    bool           _ready = true;

    static readonly Color COL_READY  = new Color(0.4f, 0.9f, 1.0f, 0.85f);
    static readonly Color COL_USED   = new Color(0.3f, 0.3f, 0.5f, 0.40f);

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _baseColor = COL_READY;
        StartCoroutine(Pulse());
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_ready) return;
        var rb = other.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // Teleport
        other.transform.position = exitPoint + Vector3.up * 0.6f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, 0f);

        StartCoroutine(Cooldown());
    }

    IEnumerator Cooldown()
    {
        _ready = false;
        if (_sr) _sr.color = COL_USED;
        yield return new WaitForSeconds(1.5f);
        _ready = true;
        if (_sr) _sr.color = COL_READY;
    }

    IEnumerator Pulse()
    {
        while (true)
        {
            if (_ready && _sr)
            {
                float t = Mathf.PingPong(Time.time * 1.8f, 1f);
                _sr.color = Color.Lerp(COL_READY, new Color(0.7f, 0.4f, 1f, 0.9f), t);
            }
            yield return null;
        }
    }
}
