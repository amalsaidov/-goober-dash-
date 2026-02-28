using UnityEngine;

/// <summary>
/// Switches the game world between Standard (color) and B&W modes at runtime.
///
/// On Start it caches every SpriteRenderer's original color — those are the
/// Standard colors baked in by SceneSetup.  When B&W is selected it converts
/// each cached color to luminance-weighted greyscale.  Switching back to
/// Standard restores the originals.
///
/// The camera background is handled separately so the sky stays pure black
/// in B&W mode instead of showing the blue fill color.
/// </summary>
public class WorldThemeManager : MonoBehaviour
{
    public static WorldThemeManager Instance;

    // ── Cached sprite state ───────────────────────────────────────────────────
    struct SrEntry
    {
        public SpriteRenderer sr;
        public Color          original;
    }

    SrEntry[] _cache;
    Camera    _cam;
    Color     _camOriginal;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // Cache every world SpriteRenderer that exists right now
        var all = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        _cache = new SrEntry[all.Length];
        for (int i = 0; i < all.Length; i++)
            _cache[i] = new SrEntry { sr = all[i], original = all[i].color };

        // Cache the camera background color
        _cam = Camera.main;
        if (_cam) _camOriginal = _cam.backgroundColor;

        // Apply the saved theme (GameSettings.Instance may already exist)
        int saved = GameSettings.Instance != null ? GameSettings.Instance.worldTheme : 0;
        Apply(saved);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// Returns the theme-adjusted version of a color (grayscale if B&W mode is active)
    public Color GetThemedColor(Color original)
    {
        int theme = GameSettings.Instance != null ? GameSettings.Instance.worldTheme : 0;
        if (theme != 1) return original;
        float g = original.r * 0.299f + original.g * 0.587f + original.b * 0.114f;
        return new Color(g, g, g, original.a);
    }

    /// <summary>0 = Standard (color)  |  1 = B&W</summary>
    public void Apply(int theme)
    {
        if (_cache == null) return;

        bool bw = theme == 1;

        foreach (var entry in _cache)
        {
            if (entry.sr == null) continue;

            if (bw)
            {
                Color c = entry.original;
                // Luminance-weighted greyscale (perceptual)
                float g = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
                entry.sr.color = new Color(g, g, g, c.a);
            }
            else
            {
                entry.sr.color = entry.original;
            }
        }

        // Camera background
        if (_cam)
            _cam.backgroundColor = bw ? Color.black : _camOriginal;
    }
}
