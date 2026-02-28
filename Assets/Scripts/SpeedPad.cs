using System.Collections;
using UnityEngine;

public class SpeedPad : MonoBehaviour
{
    // Additive flat boost — no multiplicative stacking
    public float boostAmount = 4f;
    public float duration = 3f;
    public float maxBoostedSpeed = 13f;

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null && !pc.isBoosted) StartCoroutine(BoostPlayer(pc));

        AIPlayer ai = other.GetComponent<AIPlayer>();
        if (ai != null && !ai.isBoosted) StartCoroutine(BoostAI(ai));
    }

    IEnumerator BoostPlayer(PlayerController pc)
    {
        pc.isBoosted = true;
        float orig = pc.moveSpeed;
        pc.moveSpeed = Mathf.Min(pc.moveSpeed + boostAmount, maxBoostedSpeed);

        var sr = pc.GetComponent<SpriteRenderer>();
        if (sr) sr.color = new Color(1f, 0.85f, 0.1f);

        yield return new WaitForSeconds(duration);

        pc.moveSpeed = orig;
        if (sr) sr.color = Color.white;
        pc.isBoosted = false;
    }

    IEnumerator BoostAI(AIPlayer ai)
    {
        ai.isBoosted = true;
        float orig = ai.speed;
        ai.speed = Mathf.Min(ai.speed + boostAmount, maxBoostedSpeed);

        var sr = ai.GetComponent<SpriteRenderer>();
        if (sr) sr.color = new Color(1f, 0.85f, 0.1f); // same golden glow as player

        yield return new WaitForSeconds(duration);

        ai.speed = orig;
        if (sr) sr.color = Color.white; // sprite colour is baked into texture — white = original
        ai.isBoosted = false;
    }
}
