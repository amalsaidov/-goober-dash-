using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to any UI Text to auto-update its content when the language changes.
/// Set <see cref="key"/> to one of the keys defined in <see cref="LocalizationManager"/>.
/// </summary>
[RequireComponent(typeof(Text))]
public class LocalizedText : MonoBehaviour
{
    public string key;

    Text _text;

    void Awake() => _text = GetComponent<Text>();

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += Refresh;
        Refresh();
    }

    void OnDisable() => LocalizationManager.OnLanguageChanged -= Refresh;

    void Refresh()
    {
        if (_text == null) _text = GetComponent<Text>();
        if (_text == null || LocalizationManager.Instance == null) return;
        _text.text = LocalizationManager.Instance.Get(key);
    }
}
