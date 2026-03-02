using System.Collections;
using UnityEngine;

/// <summary>
/// Floating orb — touching it charges the player's next dash with 1.7× force.
/// Bobs up and down while available; grays out for respawnDelay seconds
/// after being picked up, then reappears.
/// Effect is consumed on the next dash; clears automatically if unused.
/// </summary>
public class DashBoost : MonoBehaviour
{
    public float respawnDelay = 5f;

    bool           _available = true;
    SpriteRenderer _sr;
    Color          _activeColor;
    Vector3        _baseLocalPos;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr) _activeColor = _sr.color;
        _baseLocalPos = transform.localPosition;
    }

    void Update()
    {
        float bob = Mathf.Sin(Time.time * 2.0f + transform.position.x * 0.7f) * 0.13f;
        transform.localPosition = new Vector3(_baseLocalPos.x, _baseLocalPos.y + bob, _baseLocalPos.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_available) return;
        var pc = other.GetComponent<PlayerController>();
        if (pc == null) return;
        pc.ActivateDashBoost();
        _available = false;
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        RefreshColor();
        yield return new WaitForSeconds(respawnDelay);
        _available = true;
        RefreshColor();
    }

    void RefreshColor()
    {
        if (_sr == null) return;
        var c = _activeColor;
        c.a = _available ? _activeColor.a : 0.08f;
        _sr.color = c;
    }
}
