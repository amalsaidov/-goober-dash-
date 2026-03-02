using UnityEngine;

/// <summary>
/// World-space dash-cooldown bar pinned above a player/bot.
/// • Drains (left→right fill shrinks) when dash is used.
/// • Refills over dashCooldown seconds.
/// • Turns purple when a DashBoost pickup is active.
/// Lives as a top-level scene object so player squash/stretch doesn't affect it.
/// </summary>
public class DashBar : MonoBehaviour
{
    const float BAR_W  = 0.72f;   // world units
    const float BAR_H  = 0.055f;
    const float Y_OFFS = 0.88f;   // above player center

    static readonly Color COL_READY   = new Color(0.10f, 0.88f, 1.00f, 0.30f); // faded cyan — ready
    static readonly Color COL_CHARGE  = new Color(0.10f, 0.88f, 1.00f, 0.95f); // bright cyan — refilling
    static readonly Color COL_EMPTY   = new Color(0.04f, 0.22f, 0.45f, 0.70f); // dark — just used
    static readonly Color COL_BOOSTED = new Color(0.72f, 0.18f, 1.00f, 0.95f); // purple — boost active

    [SerializeField] Transform        _target;
    [SerializeField] PlayerController _pc;
    [SerializeField] Transform        _fillTr;
    [SerializeField] SpriteRenderer   _fillSr;

    /// <summary>Called once by SceneSetup to wire up this bar to its player.</summary>
    public void Init(Transform target, PlayerController pc, Transform fillTr, SpriteRenderer fillSr)
    {
        _target = target;
        _pc     = pc;
        _fillTr = fillTr;
        _fillSr = fillSr;
    }

    void LateUpdate()
    {
        if (_target == null) return;
        transform.position = _target.position + new Vector3(0f, Y_OFFS, -0.1f);

        if (_pc == null || _fillTr == null) return;

        float ratio = _pc.DashReadyRatio;               // 0 = just used, 1 = ready
        float w     = Mathf.Max(0.002f, ratio * BAR_W); // left-aligned fill width
        float x     = BAR_W * (ratio - 1f) * 0.5f;     // offset so left edge is fixed

        _fillTr.localPosition = new Vector3(x, 0f, -0.01f);
        _fillTr.localScale    = new Vector3(w, BAR_H * 0.65f, 1f);

        if (_pc.DashIsBoosted)
            _fillSr.color = COL_BOOSTED;
        else if (ratio >= 1f)
            _fillSr.color = COL_READY;
        else
            _fillSr.color = Color.Lerp(COL_EMPTY, COL_CHARGE, ratio);
    }
}
