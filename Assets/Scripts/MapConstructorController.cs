using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// In-game map editor. Activated from Main Menu → CONSTRUCT.
/// WASD/arrows move camera. Mouse click places/erases blocks.
/// Save/Load via PlayerPrefs JSON. TEST runs the race with custom blocks.
/// </summary>
public class MapConstructorController : MonoBehaviour
{
    public static MapConstructorController Instance;

    [Header("UI References (wired by SceneSetup)")]
    public GameObject panel;
    public Text       modeLabel;
    public Text       coordsLabel;

    // ── State ───────────────────────────────────────────────────────────────
    bool              _active;
    ConstructBlockType _selected = ConstructBlockType.Floor;
    bool              _eraseMode;

    // ── Scene objects ────────────────────────────────────────────────────────
    GameObject        _ghost;
    SpriteRenderer    _ghostSr;
    readonly List<GameObject>   _placed = new List<GameObject>();
    readonly CustomMapData      _data   = new CustomMapData();

    // ── Camera ───────────────────────────────────────────────────────────────
    CameraFollow      _cf;
    Transform         _savedTarget;
    Camera            _cam;

    // ── Frozen player state ──────────────────────────────────────────────────
    readonly List<(PlayerController pc, Rigidbody2D rb, float grav)> _frozen
        = new List<(PlayerController, Rigidbody2D, float)>();

    // ── Constants ────────────────────────────────────────────────────────────
    const float GRID      = 0.5f;
    const float CAM_SPEED = 14f;
    const string PREFS_KEY = "GM_CustomMap";

    // Width × Height for each block type
    static readonly (float w, float h)[] SIZES =
    {
        (3.0f, 0.50f),  // Floor
        (2.0f, 0.40f),  // Platform
        (1.5f, 0.25f),  // ThinPlatform
        (0.5f, 2.50f),  // Wall
        (1.0f, 0.35f),  // BouncePad
        (1.5f, 0.30f),  // SpeedPad
        (1.5f, 0.30f),  // ConveyorLeft
        (1.5f, 0.30f),  // ConveyorRight
        (3.0f, 1.50f),  // LowGravZone
        (0.4f, 2.00f),  // Checkpoint
        (0.4f, 3.00f),  // FinishLine
    };

    static readonly Color[] COLORS =
    {
        new Color(0.72f, 0.72f, 0.72f),  // Floor
        new Color(0.60f, 0.60f, 0.60f),  // Platform
        new Color(0.50f, 0.50f, 0.50f),  // ThinPlatform
        new Color(0.40f, 0.40f, 0.40f),  // Wall
        new Color(1.00f, 0.45f, 0.08f),  // BouncePad
        new Color(0.08f, 0.92f, 0.38f),  // SpeedPad
        new Color(0.20f, 0.58f, 1.00f),  // ConveyorLeft
        new Color(0.20f, 0.58f, 1.00f),  // ConveyorRight
        new Color(0.55f, 0.20f, 0.85f),  // LowGravZone
        new Color(1.00f, 0.78f, 0.08f),  // Checkpoint
        new Color(0.10f, 0.95f, 0.52f),  // FinishLine
    };

    // ── Lifecycle ────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Open the constructor editor panel.</summary>
    public void Activate()
    {
        _active = true;
        panel?.SetActive(true);
        _cam = Camera.main;

        // Freeze all players
        _frozen.Clear();
        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            var rb = pc.GetComponent<Rigidbody2D>();
            float grav = rb ? rb.gravityScale : 3.6f;
            _frozen.Add((pc, rb, grav));
            pc.canControl = false;
            if (rb) { rb.gravityScale = 0f; rb.linearVelocity = Vector2.zero; }
        }

        // Free the camera from its target
        _cf = CameraFollow.Instance;
        if (_cf) { _savedTarget = _cf.target; _cf.target = null; }

        // Ghost block
        _ghost  = new GameObject("MCGhost");
        _ghostSr = _ghost.AddComponent<SpriteRenderer>();
        _ghostSr.sprite = RuntimeWhiteSprite();
        _ghostSr.sortingOrder = 30;
        UpdateGhostVisual();

        RefreshModeLabel();
    }

    /// <summary>Close the editor and restore everything.</summary>
    public void Deactivate()
    {
        _active = false;
        panel?.SetActive(false);

        // Restore camera
        if (_cf && _savedTarget) _cf.target = _savedTarget;

        // Restore players
        foreach (var (pc, rb, grav) in _frozen)
        {
            if (pc) pc.canControl = true;
            if (rb) rb.gravityScale = grav;
        }
        _frozen.Clear();

        // Destroy ghost
        if (_ghost) { Destroy(_ghost); _ghost = null; }
    }

    // ── Toolbar callbacks ─────────────────────────────────────────────────────

    public void OnBackPressed()
    {
        ClearCustomBlocks();
        Deactivate();
        UIManager.Instance?.HideConstructorPanel();
        UIManager.Instance?.ShowMainMenu();
    }

    public void OnSavePressed()
    {
        string json = JsonUtility.ToJson(_data, true);
        PlayerPrefs.SetString(PREFS_KEY, json);
        PlayerPrefs.Save();
        UIManager.Instance?.ShowMessage("Map saved!", new Color(0.1f, 0.95f, 0.52f));
    }

    public void OnLoadPressed()
    {
        string json = PlayerPrefs.GetString(PREFS_KEY, "");
        if (string.IsNullOrEmpty(json))
        {
            UIManager.Instance?.ShowMessage("No saved map.", new Color(1f, 0.78f, 0.08f));
            return;
        }
        ClearCustomBlocks();
        var loaded = JsonUtility.FromJson<CustomMapData>(json);
        _data.blocks.Clear();
        _data.mapName = loaded.mapName;
        foreach (var b in loaded.blocks)
        {
            PlaceBlock(b.type, new Vector3(b.x, b.y, 0f), record: false);
            _data.blocks.Add(new ConstructBlock { type = b.type, x = b.x, y = b.y });
        }
        UIManager.Instance?.ShowMessage($"Loaded {_placed.Count} blocks.", new Color(0f, 0.86f, 0.96f));
    }

    public void OnClearPressed()
    {
        ClearCustomBlocks();
        UIManager.Instance?.ShowMessage("Canvas cleared.", new Color(0.92f, 0.97f, 1f));
    }

    public void OnTestPressed()
    {
        // Keep placed blocks, close editor UI, restore player control, start race
        panel?.SetActive(false);
        _active = false;

        if (_cf && _savedTarget) _cf.target = _savedTarget;
        if (_ghost) { Destroy(_ghost); _ghost = null; }

        foreach (var (pc, rb, grav) in _frozen)
        {
            if (pc) pc.canControl = true;
            if (rb) rb.gravityScale = grav;
        }
        _frozen.Clear();

        // Start race with whatever is in the scene now
        RaceManager.Instance?.BeginRace();
    }

    // ── Palette callbacks ─────────────────────────────────────────────────────

    public void SelectBlock(int typeIndex)
    {
        _eraseMode = false;
        _selected  = (ConstructBlockType)typeIndex;
        UpdateGhostVisual();
        RefreshModeLabel();
    }

    public void ToggleErase()
    {
        _eraseMode = !_eraseMode;
        UpdateGhostVisual();
        RefreshModeLabel();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (!_active) return;
        MoveCameraWithInput();
        UpdateGhostPosition();
        HandlePlacementInput();
        UpdateCoordsLabel();
    }

    void MoveCameraWithInput()
    {
        var kb = Keyboard.current;
        if (kb == null || _cam == null) return;

        float speed = CAM_SPEED * Time.deltaTime;
        Vector3 move = Vector3.zero;
        if (kb.leftArrowKey.isPressed  || kb.aKey.isPressed) move.x -= speed;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) move.x += speed;
        if (kb.upArrowKey.isPressed    || kb.wKey.isPressed) move.y += speed;
        if (kb.downArrowKey.isPressed  || kb.sKey.isPressed) move.y -= speed;
        _cam.transform.position += move;
    }

    // ── Ghost block ───────────────────────────────────────────────────────────

    void UpdateGhostPosition()
    {
        if (_ghost == null || _cam == null) return;
        bool overUI = UnityEngine.EventSystems.EventSystem.current?
            .IsPointerOverGameObject() ?? false;
        _ghost.SetActive(!overUI);
        if (!overUI)
            _ghost.transform.position = SnapToGrid(MouseWorldPos());
    }

    void UpdateGhostVisual()
    {
        if (_ghostSr == null) return;
        if (_eraseMode)
        {
            _ghost.transform.localScale = new Vector3(1f, 1f, 1f);
            _ghostSr.color = new Color(1f, 0.2f, 0.2f, 0.45f);
        }
        else
        {
            var (w, h) = SIZES[(int)_selected];
            _ghost.transform.localScale = new Vector3(w, h, 1f);
            var c = COLORS[(int)_selected];
            _ghostSr.color = new Color(c.r, c.g, c.b, 0.45f);
        }
    }

    // ── Placement ─────────────────────────────────────────────────────────────

    void HandlePlacementInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        bool overUI = UnityEngine.EventSystems.EventSystem.current?
            .IsPointerOverGameObject() ?? false;
        if (overUI) return;

        var wp = SnapToGrid(MouseWorldPos());

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (_eraseMode) EraseAt(wp);
            else            PlaceBlock(_selected, wp, record: true);
        }
        else if (mouse.rightButton.wasPressedThisFrame)
        {
            EraseAt(wp);
        }
    }

    void PlaceBlock(ConstructBlockType type, Vector3 pos, bool record)
    {
        // Prevent placing two blocks at the exact same position
        foreach (var existing in _placed)
            if (existing && Vector3.Distance(existing.transform.position, pos) < 0.25f) return;

        var (w, h) = SIZES[(int)type];
        var col    = COLORS[(int)type];
        int groundL = LayerMask.NameToLayer("Ground");

        var go = new GameObject($"CM_{type}@{pos.x:F1}_{pos.y:F1}");
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(w, h, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = RuntimeWhiteSprite();
        sr.color        = col;
        sr.sortingOrder = 3;

        bool isTrigger = type == ConstructBlockType.SpeedPad      ||
                         type == ConstructBlockType.ConveyorLeft   ||
                         type == ConstructBlockType.ConveyorRight  ||
                         type == ConstructBlockType.LowGravZone    ||
                         type == ConstructBlockType.Checkpoint     ||
                         type == ConstructBlockType.FinishLine;

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = isTrigger;

        // BouncePad also needs a trigger (players fall through it rather than land on it)
        if (type == ConstructBlockType.BouncePad)
        {
            box.isTrigger = true;
        }
        else if (!isTrigger)
        {
            go.layer = groundL;
        }

        // Attach behavior scripts
        switch (type)
        {
            case ConstructBlockType.BouncePad:
                go.AddComponent<BouncePad>();
                break;
            case ConstructBlockType.SpeedPad:
                go.AddComponent<SpeedPad>();
                break;
            case ConstructBlockType.ConveyorLeft:
                go.AddComponent<ConveyorBelt>().speed = -5f;
                break;
            case ConstructBlockType.ConveyorRight:
                go.AddComponent<ConveyorBelt>().speed = 5f;
                break;
            case ConstructBlockType.LowGravZone:
                go.AddComponent<LowGravityZone>();
                break;
            case ConstructBlockType.Checkpoint:
                go.AddComponent<Checkpoint>();
                break;
            case ConstructBlockType.FinishLine:
                go.AddComponent<FinishLine>();
                break;
        }

        _placed.Add(go);

        if (record)
            _data.blocks.Add(new ConstructBlock { type = type, x = pos.x, y = pos.y });
    }

    void EraseAt(Vector3 pos)
    {
        float bestDist = float.MaxValue;
        GameObject toRemove = null;

        foreach (var go in _placed)
        {
            if (!go) continue;
            float d = Vector2.Distance(go.transform.position, pos);
            if (d < bestDist) { bestDist = d; toRemove = go; }
        }

        if (toRemove != null && bestDist < 1.5f)
        {
            float ex = toRemove.transform.position.x;
            float ey = toRemove.transform.position.y;
            _data.blocks.RemoveAll(b =>
                Mathf.Approximately(b.x, ex) && Mathf.Approximately(b.y, ey));
            _placed.Remove(toRemove);
            Destroy(toRemove);
        }
    }

    public void ClearCustomBlocks()
    {
        foreach (var go in _placed)
            if (go) Destroy(go);
        _placed.Clear();
        _data.blocks.Clear();
    }

    // ── Labels ────────────────────────────────────────────────────────────────

    void RefreshModeLabel()
    {
        if (modeLabel == null) return;
        modeLabel.text = _eraseMode
            ? "\u274c ERASE"
            : $"\u270f PLACE  [{_selected}]";
    }

    void UpdateCoordsLabel()
    {
        if (coordsLabel == null || _cam == null) return;
        var snap = SnapToGrid(MouseWorldPos());
        coordsLabel.text = $"X: {snap.x:F1}  Y: {snap.y:F1}   blocks: {_placed.Count}   |   WASD/arrows: pan  \u00b7  LMB: place  \u00b7  RMB: erase";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    Vector3 MouseWorldPos()
    {
        if (_cam == null) return Vector3.zero;
        var mp = Mouse.current?.position.ReadValue() ?? Vector2.zero;
        var wp = _cam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, 10f));
        return new Vector3(wp.x, wp.y, 0f);
    }

    static Vector3 SnapToGrid(Vector3 pos)
        => new Vector3(
            Mathf.Round(pos.x / GRID) * GRID,
            Mathf.Round(pos.y / GRID) * GRID,
            0f);

    // Runtime-generated 4×4 white sprite (no asset files needed)
    static Sprite _rtSprite;
    static Sprite RuntimeWhiteSprite()
    {
        if (_rtSprite != null) return _rtSprite;
        var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
        var px  = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        _rtSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        return _rtSprite;
    }
}
