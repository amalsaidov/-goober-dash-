using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Builds an on-screen virtual gamepad (Left / Right / Jump / Dash) and writes
/// the pressed state into the TouchInput static class each frame.
///
/// Placed in the scene by SceneSetup.CreateUI().
/// Runs at execution order -50 so TouchInput is populated before
/// PlayerController.Update() (default order 0) reads it.
/// </summary>
[DefaultExecutionOrder(-50)]
public class TouchControlsOverlay : MonoBehaviour
{
    // ── Raw button state (set by EventTrigger callbacks) ──────────────────
    bool _moveLeft, _moveRight;
    bool _jumpHeld;
    bool _jumpDownBuf, _jumpUpBuf, _dashDownBuf;
    bool _prevJumpHeld;

    // ── Fly mode button ───────────────────────────────────────────────────
    Image _flyBtnImg;
    bool  _flyActive;
    static readonly Color FLY_OFF   = new Color(0f,    0f,    0f,    0.35f);
    static readonly Color FLY_ON    = new Color(0.35f, 1f,    1f,    0.55f);

    // ── Ghost mode button ─────────────────────────────────────────────────
    Image _ghostBtnImg;
    bool  _ghostActive;
    static readonly Color GHOST_OFF = new Color(0f,    0f,    0f,    0.35f);
    static readonly Color GHOST_ON  = new Color(0.8f,  0.85f, 1f,    0.55f);

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        BuildCanvas();
    }

    void Update()
    {
        // Detect jump-release transition for short-hop support
        if (_prevJumpHeld && !_jumpHeld)
            _jumpUpBuf = true;
        _prevJumpHeld = _jumpHeld;

        // Publish to static state so PlayerController can read it
        TouchInput.moveLeft  = _moveLeft;
        TouchInput.moveRight = _moveRight;
        TouchInput.jumpHeld  = _jumpHeld;
        TouchInput.jumpDown  = _jumpDownBuf;
        TouchInput.jumpUp    = _jumpUpBuf;
        TouchInput.dashDown  = _dashDownBuf;

        // Clear one-frame signals (PlayerController already read them above)
        _jumpDownBuf = false;
        _jumpUpBuf   = false;
        _dashDownBuf = false;
    }

    // ── Canvas + button construction ──────────────────────────────────────

    void BuildCanvas()
    {
        var cgo = new GameObject("TouchCanvas");
        cgo.transform.SetParent(transform, false);

        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // above all game UI

        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        cgo.AddComponent<GraphicRaycaster>();

        // ── Left ──────────────────────────────────────────────────────────
        MakeBtn(cgo.transform, "◀", "Left",
            anchorMin: new Vector2(0, 0), anchorMax: new Vector2(0, 0),
            size: new Vector2(140, 140),
            pos: new Vector2(110, 120),
            bg: new Color(1f, 1f, 1f, 0.20f),
            onDown: () => _moveLeft = true,
            onUp:   () => _moveLeft = false);

        // ── Right ─────────────────────────────────────────────────────────
        MakeBtn(cgo.transform, "▶", "Right",
            anchorMin: new Vector2(0, 0), anchorMax: new Vector2(0, 0),
            size: new Vector2(140, 140),
            pos: new Vector2(275, 120),
            bg: new Color(1f, 1f, 1f, 0.20f),
            onDown: () => _moveRight = true,
            onUp:   () => _moveRight = false);

        // ── Jump ──────────────────────────────────────────────────────────
        MakeBtn(cgo.transform, "▲", "Jump",
            anchorMin: new Vector2(1, 0), anchorMax: new Vector2(1, 0),
            size: new Vector2(160, 160),
            pos: new Vector2(-110, 120),
            bg: new Color(0.25f, 0.75f, 1f, 0.25f),
            onDown: () => { _jumpHeld = true;  _jumpDownBuf = true; },
            onUp:   () =>   _jumpHeld = false);

        // ── Dash ──────────────────────────────────────────────────────────
        MakeBtn(cgo.transform, "⚡", "Dash",
            anchorMin: new Vector2(1, 0), anchorMax: new Vector2(1, 0),
            size: new Vector2(130, 130),
            pos: new Vector2(-285, 120),
            bg: new Color(1f, 0.85f, 0.15f, 0.22f),
            onDown: () => _dashDownBuf = true,
            onUp:   () => { });

        // ── Pause / Menu  (top-right, always visible) ─────────────────────
        MakeBtn(cgo.transform, "⏸", "Pause",
            anchorMin: new Vector2(1, 1), anchorMax: new Vector2(1, 1),
            size: new Vector2(96, 60),
            pos: new Vector2(-58, -38),
            bg: new Color(0f, 0f, 0f, 0.35f),
            onDown: () => PauseManager.Instance?.TogglePause(),
            onUp:   () => { });

        // ── Fly mode toggle (top-left, next to pause) ──────────────────────
        _flyBtnImg = MakeBtn(cgo.transform, "✈", "Fly",
            anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1),
            size: new Vector2(96, 60),
            pos: new Vector2(55, -38),
            bg: FLY_OFF,
            onDown: () =>
            {
                _flyActive = !_flyActive;
                if (_flyActive && _ghostActive) { _ghostActive = false; if (_ghostBtnImg) _ghostBtnImg.color = GHOST_OFF; }
                var pcs = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
                foreach (var pc in pcs) { if (pc.canControl) { pc.ToggleFlyMode(); break; } }
                if (_flyBtnImg != null) _flyBtnImg.color = _flyActive ? FLY_ON : FLY_OFF;
            },
            onUp: () => { });

        // ── Ghost mode toggle (top-left, next to fly) ─────────────────────
        _ghostBtnImg = MakeBtn(cgo.transform, "👻", "Ghost",
            anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1),
            size: new Vector2(96, 60),
            pos: new Vector2(160, -38),
            bg: GHOST_OFF,
            onDown: () =>
            {
                _ghostActive = !_ghostActive;
                if (_ghostActive && _flyActive) { _flyActive = false; if (_flyBtnImg) _flyBtnImg.color = FLY_OFF; }
                var pcs = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
                foreach (var pc in pcs) { if (pc.canControl) { pc.ToggleGhostMode(); break; } }
                if (_ghostBtnImg != null) _ghostBtnImg.color = _ghostActive ? GHOST_ON : GHOST_OFF;
            },
            onUp: () => { });
    }

    Image MakeBtn(Transform parent, string icon, string objName,
                  Vector2 anchorMin, Vector2 anchorMax,
                  Vector2 size, Vector2 pos,
                  Color bg,
                  System.Action onDown, System.Action onUp)
    {
        var go = new GameObject("TC_" + objName);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color         = bg;
        img.raycastTarget = true;

        var trigger = go.AddComponent<EventTrigger>();

        var downE = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        downE.callback.AddListener(_ => onDown());
        trigger.triggers.Add(downE);

        var upE = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        upE.callback.AddListener(_ => onUp());
        trigger.triggers.Add(upE);

        // Safety: finger slides off button → treat as release
        var exitE = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitE.callback.AddListener(_ => onUp());
        trigger.triggers.Add(exitE);

        // Icon label
        var lgo = new GameObject("Lbl");
        lgo.transform.SetParent(go.transform, false);
        var txt = lgo.AddComponent<Text>();
        txt.text      = icon;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize  = 52;
        txt.color     = new Color(1f, 1f, 1f, 0.75f);
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var trt = lgo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        return img;
    }
}
