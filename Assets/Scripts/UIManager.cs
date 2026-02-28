using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HUD")]
    public Text scoreText;
    public Text livesText;
    public Text timerText;
    public Text playerCountText;
    public Text roundText;
    public Text positionText;

    [Header("Countdown")]
    public GameObject countdownPanel;
    public Text countdownText;

    [Header("Message")]
    public GameObject messagePanel;
    public Text messageText;

    [Header("Main Menu")]
    public GameObject mainMenuPanel;

    [Header("Settings")]
    public GameObject settingsPanel;

    [Header("Difficulty")]
    public GameObject difficultyPanel;

    [Header("End Screen")]
    public GameObject endPanel;
    public Text endTitleText;
    public Text endSubText;

    [Header("Pause")]
    public GameObject pausePanel;

    [Header("Lobby")]
    public GameObject lobbyPanel;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ── HUD ────────────────────────────────────────────────────────────────

    public void UpdateScore(int score)
    {
        if (scoreText) scoreText.text = "Score: " + score;
    }

    public void UpdateLives(int lives) { }

    public void UpdateTimer(float time)
    {
        if (timerText) timerText.text = Mathf.CeilToInt(time) + "s";
    }

    public void UpdatePlayerCount(int count)
    {
        if (playerCountText)
            playerCountText.text = string.Format(
                LocalizationManager.Instance?.Get("hud.players.fmt") ?? "Players: {0}", count);
    }

    public void ShowRoundText(string text)
    {
        if (roundText) roundText.text = text;
    }

    // ── Countdown ──────────────────────────────────────────────────────────

    public void ShowCountdown(string text)
    {
        if (countdownPanel) countdownPanel.SetActive(true);
        if (countdownText)
        {
            countdownText.text  = text;
            countdownText.color = text == "GO!" ? Color.green : Color.white;
        }
    }

    public void HideCountdown()
    {
        if (countdownPanel) countdownPanel.SetActive(false);
    }

    // ── Message ────────────────────────────────────────────────────────────

    public void ShowMessage(string msg, Color color)
    {
        if (messagePanel) messagePanel.SetActive(true);
        if (messageText)
        {
            messageText.text  = msg;
            messageText.color = color;
        }
        StartCoroutine(AutoHideMessage(3f));
    }

    IEnumerator AutoHideMessage(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideMessage();
    }

    public void HideMessage()
    {
        if (messagePanel) messagePanel.SetActive(false);
    }

    // ── Main Menu ──────────────────────────────────────────────────────────

    public void ShowMainMenu()
    {
        if (MenuAnimator.Instance) MenuAnimator.Instance.PanelIn(mainMenuPanel);
        else mainMenuPanel?.SetActive(true);
    }

    public void HideMainMenu()
    {
        if (MenuAnimator.Instance) MenuAnimator.Instance.PanelOut(mainMenuPanel);
        else mainMenuPanel?.SetActive(false);
    }

    // ── Settings ───────────────────────────────────────────────────────────

    public void ShowSettings()
    {
        if (MenuAnimator.Instance) MenuAnimator.Instance.PanelIn(settingsPanel);
        else settingsPanel?.SetActive(true);
    }

    public void HideSettings()
    {
        if (MenuAnimator.Instance) MenuAnimator.Instance.PanelOut(settingsPanel);
        else settingsPanel?.SetActive(false);
    }

    // ── Difficulty ─────────────────────────────────────────────────────────

    public void ShowDifficultyPanel()
    {
        if (MenuAnimator.Instance) MenuAnimator.Instance.PanelIn(difficultyPanel);
        else difficultyPanel?.SetActive(true);
    }

    public void HideDifficultyPanel()
    {
        if (MenuAnimator.Instance) MenuAnimator.Instance.PanelOut(difficultyPanel);
        else difficultyPanel?.SetActive(false);
    }

    // ── End Screen ─────────────────────────────────────────────────────────

    public void ShowEndScreen(string title, string sub, Color color)
    {
        if (endTitleText) { endTitleText.text = title; endTitleText.color = color; }
        if (endSubText)   endSubText.text = sub;
        if (MenuAnimator.Instance) MenuAnimator.Instance.PanelIn(endPanel);
        else endPanel?.SetActive(true);
    }

    public void HideEndScreen()
    {
        if (MenuAnimator.Instance) MenuAnimator.Instance.PanelOut(endPanel);
        else endPanel?.SetActive(false);
    }

    // ── Pause Menu ─────────────────────────────────────────────────────────

    public void ShowPauseMenu()
    {
        if (MenuAnimator.Instance) MenuAnimator.Instance.PauseIn(pausePanel);
        else pausePanel?.SetActive(true);
    }

    public void HidePauseMenu(System.Action onDone = null)
    {
        if (MenuAnimator.Instance) MenuAnimator.Instance.PauseOut(pausePanel, onDone);
        else { pausePanel?.SetActive(false); onDone?.Invoke(); }
    }

    // ── Lobby ──────────────────────────────────────────────────────────────

    public void ShowLobbyPanel()
    {
        if (MenuAnimator.Instance) MenuAnimator.Instance.PanelIn(lobbyPanel);
        else lobbyPanel?.SetActive(true);
    }

    public void HideLobbyPanel()
    {
        if (MenuAnimator.Instance) MenuAnimator.Instance.PanelOut(lobbyPanel);
        else lobbyPanel?.SetActive(false);
    }

    // ── Position ───────────────────────────────────────────────────────────

    static readonly string[] suffixes = { "", "st", "nd", "rd", "th", "th", "th", "th", "th" };
    public void UpdatePosition(int pos, int total)
    {
        if (!positionText) return;
        bool isRussian = LocalizationManager.Instance?.Current == LocalizationManager.Lang.Russian;
        string suf = (!isRussian && pos < suffixes.Length) ? suffixes[pos] : "";
        positionText.text = pos + suf + " / " + total;
        positionText.color = pos == 1 ? new Color(1f, 0.85f, 0.1f) :
                             pos == 2 ? new Color(0.8f, 0.8f, 0.8f) :
                             pos == 3 ? new Color(0.8f, 0.5f, 0.2f) : Color.white;
    }
}
