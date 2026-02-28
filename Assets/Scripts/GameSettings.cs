using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance;

    // ── Display ───────────────────────────────────────────────
    public int  qualityLevel  = 2;   // 0=Low 1=Med 2=High 3=Ultra
    public int  worldTheme    = 0;   // 0=Standard (color)  1=B&W

    // ── Experience ────────────────────────────────────────────
    public bool cameraShake   = true;
    public bool playerTrails  = true;
    public bool debugOverlay  = true;

    // ── Gameplay ──────────────────────────────────────────────
    public int  roundTimeIdx  = 2;   // 0=45s  1=60s  2=75s
    public int  elimPerRound  = 1;   // 0=1    1=2    2=3

    public static readonly float[] RoundTimes  = { 45f, 60f, 75f };
    public static readonly int[]   ElimCounts  = { 1, 2, 3 };

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) { Instance = this; Load(); }
        else { Destroy(gameObject); return; }

        // Apply quality immediately (doesn't depend on other managers)
        QualitySettings.SetQualityLevel(qualityLevel);
    }

    void Start()
    {
        // All other singletons exist now — safe to apply everything
        Apply();
    }

    void Load()
    {
        qualityLevel  = PlayerPrefs.GetInt  ("GS_quality",  2);
        cameraShake   = PlayerPrefs.GetInt  ("GS_camShake", 1) == 1;
        playerTrails  = PlayerPrefs.GetInt  ("GS_trails",   1) == 1;
        debugOverlay  = PlayerPrefs.GetInt  ("GS_dbgOverlay", 1) == 1;
        roundTimeIdx  = PlayerPrefs.GetInt  ("GS_roundIdx", 2);
        elimPerRound  = PlayerPrefs.GetInt  ("GS_elimIdx",  1);
        worldTheme    = PlayerPrefs.GetInt  ("GS_theme",    0);
    }

    public void Apply()
    {
        QualitySettings.SetQualityLevel(qualityLevel);

        if (CameraFollow.Instance != null)
            CameraFollow.Instance.shakeEnabled = cameraShake;

        foreach (var t in Object.FindObjectsByType<PlayerTrail>(FindObjectsSortMode.None))
            t.enabled = playerTrails;

        var dbg = Object.FindFirstObjectByType<DebugOverlay>();
        if (dbg != null) dbg.SetEnabledBySettings(debugOverlay);

        WorldThemeManager.Instance?.Apply(worldTheme);

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.roundDuration     = RoundTimes[roundTimeIdx];
            RaceManager.Instance.eliminatePerRound = ElimCounts[elimPerRound];
        }
    }

    public void Save()
    {
        PlayerPrefs.SetInt("GS_quality",  qualityLevel);
        PlayerPrefs.SetInt("GS_camShake",    cameraShake   ? 1 : 0);
        PlayerPrefs.SetInt("GS_trails",      playerTrails  ? 1 : 0);
        PlayerPrefs.SetInt("GS_dbgOverlay",  debugOverlay  ? 1 : 0);
        PlayerPrefs.SetInt("GS_roundIdx", roundTimeIdx);
        PlayerPrefs.SetInt("GS_elimIdx",  elimPerRound);
        PlayerPrefs.SetInt("GS_theme",    worldTheme);
        PlayerPrefs.Save();
    }
}
