using UnityEngine;
using System.Collections;

/// <summary>
/// Platform that shakes then disappears 0.4s after a player lands on it.
/// Reappears after 3 seconds.
/// </summary>
public class CrumblingPlatform : MonoBehaviour
{
    bool _triggered;

    SpriteRenderer _sr;
    BoxCollider2D  _col;
    Color          _baseColor;
    Vector3        _originLocalPos;

    void Awake()
    {
        _sr           = GetComponent<SpriteRenderer>();
        _col          = GetComponent<BoxCollider2D>();
        _baseColor    = _sr ? _sr.color : Color.white;
        _originLocalPos = transform.localPosition;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (_triggered) return;
        // Only trigger when something lands on TOP (contact normal points up)
        foreach (var c in col.contacts)
            if (c.normal.y < -0.5f) return;
        _triggered = true;
        StartCoroutine(Crumble());
    }

    IEnumerator Crumble()
    {
        // Shake + flash red for 0.4s
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float shake = (1f - t / 0.4f) * 0.08f;
            transform.localPosition = _originLocalPos + new Vector3(Random.Range(-shake, shake), 0f, 0f);
            if (_sr) _sr.color = Color.Lerp(_baseColor, new Color(1f, 0.25f, 0.1f), t / 0.4f);
            yield return null;
        }
        transform.localPosition = _originLocalPos;

        // Disappear — disable collider so players fall through, fade out
        if (_col) _col.enabled = false;
        t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            if (_sr) _sr.color = new Color(_sr.color.r, _sr.color.g, _sr.color.b,
                                           Mathf.Lerp(1f, 0f, t / 0.25f));
            yield return null;
        }
        if (_sr) _sr.enabled = false;

        // Stay hidden for 3 seconds
        yield return new WaitForSeconds(3f);

        // Reappear
        if (_sr) { _sr.enabled = true; _sr.color = _baseColor; }
        if (_col) _col.enabled = true;
        _triggered = false;
    }
}
