using System.Collections;
using UnityEngine;

/// <summary>
/// Core UI animation engine.
/// All timings use Time.unscaledDeltaTime — animations work while paused.
/// </summary>
public class MenuAnimator : MonoBehaviour
{
    public static MenuAnimator Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// Fade + spring scale in from 0.94
    public void PanelIn(GameObject panel)  => InternalIn(panel, 0.94f, 6f);

    /// Fade + spring scale in from 0.85 (tighter, faster — for pause modal)
    public void PauseIn(GameObject panel)  => InternalIn(panel, 0.85f, 9f);

    /// Fade + scale out, then invoke onDone
    public void PanelOut(GameObject panel, System.Action onDone = null)
    {
        if (panel == null || !panel.activeSelf) { onDone?.Invoke(); return; }
        var cg = GetOrAdd<CanvasGroup>(panel);
        cg.blocksRaycasts = false;
        StartCoroutine(CoOut(panel, cg, onDone));
    }

    /// Alias for pause panel (same out animation)
    public void PauseOut(GameObject panel, System.Action onDone = null)
        => PanelOut(panel, onDone);

    /// Cross-fade: fade from out, then fade to in
    public void Transition(GameObject from, GameObject to)
        => PanelOut(from, () => PanelIn(to));

    // ── Implementation ────────────────────────────────────────────────────

    void InternalIn(GameObject panel, float startScale, float speed)
    {
        if (panel == null) return;
        var cg = GetOrAdd<CanvasGroup>(panel);
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        var rt = panel.GetComponent<RectTransform>();
        rt.localScale = new Vector3(startScale, startScale, 1f);
        panel.SetActive(true);
        StartCoroutine(CoIn(cg, rt, startScale, speed));
    }

    IEnumerator CoIn(CanvasGroup cg, RectTransform rt, float startScale, float speed)
    {
        var from = new Vector3(startScale, startScale, 1f);
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(t + Time.unscaledDeltaTime * speed, 1f);
            cg.alpha       = Mathf.Clamp01(t * 3f);
            rt.localScale  = Vector3.LerpUnclamped(from, Vector3.one, EaseOutBack(t));
            yield return null;
        }
        cg.alpha      = 1f;
        rt.localScale = Vector3.one;
        cg.blocksRaycasts = true;
    }

    IEnumerator CoOut(GameObject panel, CanvasGroup cg, System.Action onDone)
    {
        var rt = panel.GetComponent<RectTransform>();
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(t + Time.unscaledDeltaTime * 8f, 1f);
            cg.alpha      = 1f - t;
            rt.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.96f, 0.96f, 1f), t * t);
            yield return null;
        }
        panel.SetActive(false);
        cg.alpha      = 1f;
        rt.localScale = Vector3.one;
        onDone?.Invoke();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    // Overshoots by ~10 % then settles at 1
    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }
}
