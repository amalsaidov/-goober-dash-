using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button), typeof(Image))]
public class SettingButton : MonoBehaviour
{
    public enum SettingId
    {
        Quality, CameraShake, PlayerTrails, RoundTime, ElimPerRound, Language, Theme, DebugOverlay
    }

    public SettingId setting;
    public int       valueIndex;

    // ── Colors ──────────────────────────────────────────────────
    static readonly Color SEL_BG    = new Color(0.18f, 0.48f, 0.90f, 1f);  // electric blue
    static readonly Color SEL_HI    = new Color(0.28f, 0.60f, 1.00f, 1f);
    static readonly Color UNSEL_BG  = new Color(0.05f, 0.08f, 0.16f, 1f);  // very dark
    static readonly Color UNSEL_HI  = new Color(0.09f, 0.14f, 0.26f, 1f);

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
        Refresh();
    }

    void OnEnable() => Refresh();

    void OnClick()
    {
        if (setting == SettingId.Language)
        {
            LocalizationManager.Instance?.SetLanguage((LocalizationManager.Lang)valueIndex);
            RefreshAll();
            return;
        }

        var gs = GameSettings.Instance;
        if (gs == null) return;

        switch (setting)
        {
            case SettingId.Quality:       gs.qualityLevel  = valueIndex;          break;
            case SettingId.CameraShake:   gs.cameraShake   = valueIndex == 0;     break;
            case SettingId.PlayerTrails:  gs.playerTrails  = valueIndex == 0;     break;
            case SettingId.RoundTime:     gs.roundTimeIdx  = valueIndex;          break;
            case SettingId.ElimPerRound:  gs.elimPerRound  = valueIndex;          break;
            case SettingId.Theme:         gs.worldTheme    = valueIndex;          break;
            case SettingId.DebugOverlay:  gs.debugOverlay  = valueIndex == 0;     break;
        }

        gs.Apply();
        gs.Save();
        RefreshAll();
    }

    public void Refresh()
    {
        // Language buttons work even without GameSettings
        if (setting == SettingId.Language)
        {
            bool langSel = LocalizationManager.Instance != null &&
                           (int)LocalizationManager.Instance.Current == valueIndex;
            ApplyVisual(langSel);
            return;
        }

        var gs = GameSettings.Instance;
        if (gs == null) return;

        bool sel = false;
        switch (setting)
        {
            case SettingId.Quality:       sel = gs.qualityLevel == valueIndex;               break;
            case SettingId.CameraShake:   sel = (valueIndex == 0) == gs.cameraShake;        break;
            case SettingId.PlayerTrails:  sel = (valueIndex == 0) == gs.playerTrails;       break;
            case SettingId.RoundTime:     sel = gs.roundTimeIdx == valueIndex;               break;
            case SettingId.ElimPerRound:  sel = gs.elimPerRound == valueIndex;               break;
            case SettingId.Theme:         sel = gs.worldTheme == valueIndex;                 break;
            case SettingId.DebugOverlay:  sel = (valueIndex == 0) == gs.debugOverlay;       break;
        }

        ApplyVisual(sel);
    }

    void ApplyVisual(bool sel)
    {
        Color bg = sel ? SEL_BG : UNSEL_BG;
        Color hi = sel ? SEL_HI : UNSEL_HI;

        var img = GetComponent<Image>();
        if (img) img.color = bg;

        var btn = GetComponent<Button>();
        if (btn != null)
        {
            var bc = btn.colors;
            bc.normalColor      = bg;
            bc.highlightedColor = hi;
            bc.pressedColor     = Color.Lerp(bg, Color.black, 0.25f);
            bc.colorMultiplier  = 1f;
            btn.colors = bc;
        }

        var lbl = GetComponentInChildren<Text>();
        if (lbl) lbl.color = sel ? Color.white : new Color(0.38f, 0.52f, 0.72f);
    }

    public static void RefreshAll()
    {
        foreach (var sb in Object.FindObjectsByType<SettingButton>(FindObjectsSortMode.None))
            sb.Refresh();
    }
}
