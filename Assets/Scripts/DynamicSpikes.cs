using System.Collections;
using UnityEngine;

/// <summary>
/// Animated hazard — spikes cycle between a lethal active phase and a safe
/// hidden phase.  When active and touched, the player is respawned at their
/// last checkpoint.
/// Use initialDelay to stagger multiple spike groups so they don't all pulse
/// in sync.
/// </summary>
public class DynamicSpikes : MonoBehaviour
{
    public float onDuration   = 1.0f;  // seconds visible + lethal
    public float offDuration  = 3.5f;  // seconds hidden + safe
    public float initialDelay = 0f;    // stagger before first cycle (seconds)

    bool           _active = false;
    SpriteRenderer _sr;
    BoxCollider2D  _col;
    Color          _baseColor;

    void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _col = GetComponent<BoxCollider2D>();
        if (_sr) _baseColor = _sr.color;
    }

    void Start()
    {
        SetPhase(false);               // start hidden
        StartCoroutine(Cycle());
    }

    IEnumerator Cycle()
    {
        if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);
        while (true)
        {
            SetPhase(true);
            yield return new WaitForSeconds(onDuration);
            SetPhase(false);
            yield return new WaitForSeconds(offDuration);
        }
    }

    void SetPhase(bool on)
    {
        _active = on;
        if (_col) _col.enabled = on;

        float alpha = on ? _baseColor.a : 0.06f;

        // Main body
        if (_sr)
        {
            var c = _baseColor; c.a = alpha;
            _sr.color = c;
        }

        // Child tip renderers (orange triangles in SpawnSpk helper)
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            if (sr == _sr) continue;
            var c = sr.color; c.a = alpha;
            sr.color = c;
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!_active) return;
        col.gameObject.GetComponent<RacePlayer>()?.Respawn();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_active) return;
        other.GetComponent<RacePlayer>()?.Respawn();
    }
}
