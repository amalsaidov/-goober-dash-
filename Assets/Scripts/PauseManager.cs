using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ESC to pause / resume.  Only pauses during an active race.
/// Uses Time.unscaledDeltaTime so the pause UI still animates while paused.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    public bool IsPaused { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        // ESC on keyboard (null on iPad — mobile uses the ⏸ touch button)
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    /// <summary>Called by the ⏸ touch button and by keyboard ESC.</summary>
    public void TogglePause()
    {
        if (IsPaused)
            Resume();
        else if (RaceManager.Instance != null && RaceManager.Instance.IsRacing)
            Pause();
    }

    // ── Pause ─────────────────────────────────────────────────────────────

    void Pause()
    {
        IsPaused        = true;
        Time.timeScale  = 0f;
        UIManager.Instance?.ShowPauseMenu();
    }

    // ── Public actions (wired via MenuButton) ────────────────────────────

    public void Resume()
    {
        IsPaused       = false;
        Time.timeScale = 1f;
        UIManager.Instance?.HidePauseMenu();
    }

    public void RestartGame()
    {
        IsPaused       = false;
        Time.timeScale = 1f;
        UIManager.Instance?.HidePauseMenu(null);
        RaceManager.Instance?.PlayAgain();
    }

    public void GoToMainMenu()
    {
        IsPaused       = false;
        Time.timeScale = 1f;
        UIManager.Instance?.HidePauseMenu(null);
        RaceManager.Instance?.ReturnToMainMenu();
    }
}
