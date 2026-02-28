using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Attached to the OnboardingPanel by SceneSetup.
/// On first touch / tap / key press → fades out the splash and tells
/// UIManager to show the Main Menu.
/// </summary>
public class SplashController : MonoBehaviour
{
    // "TAP TO START" text pulses to draw attention
    Text _tapText;
    CanvasGroup _cg;
    bool _done;

    void Awake()
    {
        // Add a CanvasGroup so we can fade the whole panel out
        _cg = gameObject.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        _cg.alpha = 1f;

        // Find the tap-to-start label
        var t = transform.Find("OB_Tap");
        if (t != null) _tapText = t.GetComponent<Text>();
    }

    void Start()
    {
        StartCoroutine(PulseTapText());
    }

    void Update()
    {
        if (_done) return;

        bool pressed = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                    || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
        if (pressed)
            StartCoroutine(DismissSplash());
    }

    // Gentle alpha pulse on "TAP TO START" text
    IEnumerator PulseTapText()
    {
        while (!_done)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 1.2f;
                if (_tapText)
                    _tapText.color = new Color(_tapText.color.r, _tapText.color.g,
                                               _tapText.color.b, Mathf.Lerp(0.30f, 0.90f, t));
                yield return null;
            }
            t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 1.2f;
                if (_tapText)
                    _tapText.color = new Color(_tapText.color.r, _tapText.color.g,
                                               _tapText.color.b, Mathf.Lerp(0.90f, 0.30f, t));
                yield return null;
            }
        }
    }

    IEnumerator DismissSplash()
    {
        _done = true;

        // Fade out the onboarding panel
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 2.5f;
            _cg.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        // Show main menu via UIManager
        UIManager.Instance?.ShowMainMenu();
        gameObject.SetActive(false);
    }
}
