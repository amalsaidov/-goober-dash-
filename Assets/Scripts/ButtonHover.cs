using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to any menu button to get smooth hover + press micro-animations.
/// Works with Time.unscaledDeltaTime so it runs while the game is paused.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler,  IPointerUpHandler
{
    const float HOVER_SCALE  = 1.045f;
    const float HOVER_SHIFT  = 7f;      // shift right on hover (px)
    const float PRESS_SCALE  = 0.968f;
    const float PRESS_SHIFT  = -2f;     // shift down on press (px)
    const float LERP_SPEED   = 18f;

    RectTransform rt;
    Vector2 restPos;
    Vector3 targetScale  = Vector3.one;
    Vector2 targetPos;
    bool    isHovered, isPressed;

    void Awake()
    {
        rt       = GetComponent<RectTransform>();
        restPos  = rt.anchoredPosition;
        targetPos = restPos;
    }

    void OnDisable()
    {
        // Instantly reset so re-showing the panel looks clean
        isHovered = isPressed = false;
        rt.localScale        = Vector3.one;
        rt.anchoredPosition  = restPos;
        targetScale          = Vector3.one;
        targetPos            = restPos;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime * LERP_SPEED;
        rt.localScale       = Vector3.Lerp(rt.localScale,       targetScale, dt);
        rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPos,   dt);
    }

    public void OnPointerEnter(PointerEventData _) { isHovered = true;  Refresh(); }
    public void OnPointerExit (PointerEventData _) { isHovered = isPressed = false; Refresh(); }
    public void OnPointerDown (PointerEventData _) { isPressed = true;  Refresh(); }
    public void OnPointerUp   (PointerEventData _) { isPressed = false; Refresh(); }

    void Refresh()
    {
        if (isPressed)
        {
            targetScale = new Vector3(PRESS_SCALE, PRESS_SCALE, 1f);
            targetPos   = restPos + new Vector2(0, PRESS_SHIFT);
        }
        else if (isHovered)
        {
            targetScale = new Vector3(HOVER_SCALE, HOVER_SCALE, 1f);
            targetPos   = restPos + new Vector2(HOVER_SHIFT, 0);
        }
        else
        {
            targetScale = Vector3.one;
            targetPos   = restPos;
        }
    }
}
