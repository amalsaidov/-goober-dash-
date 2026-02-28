using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A single color swatch in the Settings → PERSONALIZE panel.
/// Clicking saves the chosen colorIndex to PlayerPrefs and refreshes all swatches.
/// </summary>
public class ColorSwatchButton : MonoBehaviour
{
    public int colorIndex;

    void Start()
    {
        GetComponent<Button>()?.onClick.AddListener(OnClick);
        RefreshBorder();
    }

    void OnClick()
    {
        PlayerPrefs.SetInt("GS_colorIndex", colorIndex);
        PlayerPrefs.Save();
        foreach (var sb in FindObjectsByType<ColorSwatchButton>(FindObjectsSortMode.None))
            sb.RefreshBorder();
    }

    public void RefreshBorder()
    {
        var ol = GetComponent<Outline>();
        if (ol == null) return;
        bool selected = PlayerPrefs.GetInt("GS_colorIndex", 0) == colorIndex;
        ol.effectColor    = selected ? Color.white : new Color(1f, 1f, 1f, 0.12f);
        ol.effectDistance = selected ? new Vector2(3, -3) : new Vector2(1, -1);
    }
}
