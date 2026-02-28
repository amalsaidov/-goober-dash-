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

    void OnCollisionEnter2D(Collision2D col)
    {
        Rigidbody2D rb = col.gameObject.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // Only bounce from above
        foreach (var c in col.contacts)
        {
            if (c.normal.y < -0.5f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
                CameraFollow.Instance?.Shake(0.4f, 0.18f);
                StartCoroutine(SquashAnim());
                return;
            }
        }
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
