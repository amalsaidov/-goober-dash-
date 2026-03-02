using UnityEngine;

/// <summary>
/// Pushes players/bots horizontally while they stand on or inside the trigger.
/// speed > 0 = push right, speed < 0 = push left.
/// </summary>
public class ConveyorBelt : MonoBehaviour
{
    public float speed = 5f;

    void OnTriggerStay2D(Collider2D other)
    {
        var rb = other.GetComponent<Rigidbody2D>();
        if (rb == null) return;
        // Gradually push toward target belt speed (feels like friction pull, not teleport)
        float target = rb.linearVelocity.x + speed * 8f * Time.fixedDeltaTime;
        target = Mathf.Clamp(target, -24f, 24f);
        rb.linearVelocity = new Vector2(target, rb.linearVelocity.y);
    }
}
