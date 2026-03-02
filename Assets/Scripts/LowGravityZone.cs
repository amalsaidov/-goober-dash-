using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Trigger zone that reduces gravity for anything inside it.
/// Restores original gravity on exit. Handles multiple players simultaneously.
/// </summary>
public class LowGravityZone : MonoBehaviour
{
    public float gravityScale = 0.35f;

    readonly Dictionary<Rigidbody2D, float> _saved = new Dictionary<Rigidbody2D, float>();

    void OnTriggerEnter2D(Collider2D other)
    {
        var rb = other.GetComponent<Rigidbody2D>();
        if (rb == null) return;
        if (rb.gravityScale < 0.05f) return; // already in fly/ghost mode — don't interfere
        if (!_saved.ContainsKey(rb))
            _saved[rb] = rb.gravityScale;
        rb.gravityScale = gravityScale;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var rb = other.GetComponent<Rigidbody2D>();
        if (rb == null) return;
        if (_saved.TryGetValue(rb, out float orig))
        {
            rb.gravityScale = orig;
            _saved.Remove(rb);
        }
    }

    // Safety: if object is destroyed while inside zone
    void OnDisable()
    {
        foreach (var kv in _saved)
            if (kv.Key != null) kv.Key.gravityScale = kv.Value;
        _saved.Clear();
    }
}
