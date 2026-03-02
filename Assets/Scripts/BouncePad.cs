using UnityEngine;

public class BouncePad : MonoBehaviour
{
    public float bounceForce = 24f;

    private SpriteRenderer sr;
    private Vector3 originalScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    // Trigger-based: bounce pad is non-solid so players never snag on its edges.
    // Velocity check replaces contact-normal check — only bounce when approaching
    // from the side or above (vy ≤ 0.5), not when jumping up through it.
    void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb == null) return;
        if (rb.linearVelocity.y > 0.5f) return; // jumping up through pad — ignore
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
        CameraFollow.Instance?.Shake(0.08f, 0.12f);
        StartCoroutine(SquashAnim());
    }

    System.Collections.IEnumerator SquashAnim()
    {
        transform.localScale = new Vector3(originalScale.x * 1.5f, originalScale.y * 0.4f, 1f);
        yield return new WaitForSeconds(0.1f);
        float t = 0;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, t / 0.2f);
            yield return null;
        }
        transform.localScale = originalScale;
    }
}
