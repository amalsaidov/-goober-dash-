using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Activated when the local player finishes the race.
/// Freezes the human player in place, then cycles the camera
/// through ALL remaining RacePlayer objects (bots + finished humans).
/// Shows "👁 [name]  X/N" label at the bottom of the screen.
/// </summary>
public class SpectatorController : MonoBehaviour
{
    public static SpectatorController Instance { get; private set; }

    // ── UI refs (wired by SceneSetup) ─────────────────────────────────────
    public GameObject panel;
    public Text       watchLabel;
    public Button     prevBtn;
    public Button     nextBtn;

    // ── State ─────────────────────────────────────────────────────────────
    bool           _active;
    RacePlayer[]   _targets;
    int            _idx;

    // local human — frozen during spectating
    PlayerController _localPC;
    Rigidbody2D      _localRb;
    float            _savedGravity;

    void Awake()
    {
        Instance = this;
        if (panel) panel.SetActive(false);
    }

    void Update()
    {
        if (!_active) return;

        var kb = Keyboard.current;
        if (kb != null && kb.leftArrowKey.wasPressedThisFrame)  Step(-1);
        if (kb != null && kb.rightArrowKey.wasPressedThisFrame) Step( 1);
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void Activate()
    {
        // Collect ALL race players (bots + humans, finished or not)
        _targets = Object.FindObjectsByType<RacePlayer>(FindObjectsSortMode.None);
        if (_targets == null || _targets.Length == 0) return;

        // Freeze the local human player so they don't fall / dash / die
        foreach (var rp in _targets)
        {
            if (!rp.isHuman) continue;
            _localPC = rp.GetComponent<PlayerController>();
            _localRb = rp.GetComponent<Rigidbody2D>();
            if (_localPC != null)
                _localPC.canControl = false;   // blocks ALL input incl. Shift/dash
            if (_localRb != null)
            {
                _savedGravity          = _localRb.gravityScale;
                _localRb.gravityScale  = 0f;
                _localRb.linearVelocity = Vector2.zero;
            }
            break;
        }

        _active = true;

        // Start on a bot if available, otherwise index 0
        _idx = 0;
        for (int i = 0; i < _targets.Length; i++)
        {
            if (!_targets[i].isHuman) { _idx = i; break; }
        }

        FocusTarget();
        if (panel) panel.SetActive(true);
    }

    public void Deactivate()
    {
        // Restore human player when race/round ends
        if (_localPC != null) _localPC.canControl = true;
        if (_localRb  != null) _localRb.gravityScale = _savedGravity;
        _localPC = null;
        _localRb  = null;

        _active = false;
        if (panel) panel.SetActive(false);
    }

    // Called by touch ◀▶ buttons
    public void OnPrevPressed() => Step(-1);
    public void OnNextPressed() => Step( 1);

    // ── Internals ─────────────────────────────────────────────────────────

    void Step(int dir)
    {
        if (_targets == null || _targets.Length == 0) return;
        _idx = (_idx + dir + _targets.Length) % _targets.Length;
        FocusTarget();
    }

    void FocusTarget()
    {
        if (_targets == null || _idx < 0 || _idx >= _targets.Length) return;
        var rp = _targets[_idx];
        if (rp == null) return;

        // Point camera at this player
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.target = rp.transform;

        // Build name: try PlayerNameTag TextMesh, fall back to object name
        string name = rp.gameObject.name;
        var pnt = rp.GetComponentInChildren<PlayerNameTag>();
        if (pnt != null)
        {
            var tm = pnt.GetComponent<TextMesh>();
            if (tm != null && !string.IsNullOrEmpty(tm.text)) name = tm.text;
        }

        // "👁 Bot 2   2 / 4"
        if (watchLabel)
            watchLabel.text = $"\U0001f441  {name}     {_idx + 1} / {_targets.Length}";
    }
}
