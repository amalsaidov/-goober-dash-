using UnityEngine;

/// <summary>
/// Tornado / wind column — applies constant upward force while any Rigidbody2D
/// is inside the trigger.  The player stays airborne if they stand still in the
/// column; moving horizontally carries them out of the stream.
/// Turbulence adds a subtle sinusoidal horizontal push for visual interest.
/// </summary>
public class WindZone : MonoBehaviour
{
    public float upForce    = 55f;   // upward push — must exceed gravity (scale 3.6 → ~35 N)
    public float turbulence = 1.2f; // horizontal wobble strength (N)

    void OnTriggerStay2D(Collider2D other)
    {
        var rb = other.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // Soft-cap: reduce force as upward speed grows so the player doesn't
        // blast to infinity, but stays floating nicely in the column.
        float vy = rb.linearVelocity.y;
        float fy = Mathf.Max(0f, upForce - vy * 0.8f);
        float fx = Mathf.Sin(Time.time * 3.8f + rb.GetInstanceID() * 0.7f) * turbulence;

        rb.AddForce(new Vector2(fx, fy), ForceMode2D.Force);
    }

}
