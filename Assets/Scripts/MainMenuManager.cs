using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ── PLAY button ───────────────────────────────────────────────────────
    public void OnPlay()
    {
        var ui = UIManager.Instance;
        if (ui == null) return;

        if (MenuAnimator.Instance != null)
            MenuAnimator.Instance.Transition(ui.mainMenuPanel, ui.difficultyPanel);
        else
        {
            ui.HideMainMenu();
            ui.ShowDifficultyPanel();
        }
    }

    // ── SETTINGS button ───────────────────────────────────────────────────
    public void OnSettings()
    {
        // Settings slides in on top; main menu stays visible behind
        UIManager.Instance?.ShowSettings();
    }

    // ── BACK button inside settings ───────────────────────────────────────
    public void OnSettingsBack()
    {
        UIManager.Instance?.HideSettings();
    }

    // ── PLAY AGAIN on end screen ──────────────────────────────────────────
    public void OnPlayAgain()
    {
        RaceManager.Instance?.PlayAgain();
    }

    // ── MAIN MENU on end screen ───────────────────────────────────────────
    public void OnGoToMainMenu()
    {
        RaceManager.Instance?.ReturnToMainMenu();
    }
}
