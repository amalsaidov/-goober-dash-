using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit replacement for UIManager.
/// Attach to a GameObject that also has a UIDocument component.
/// Assign GameRoot.uxml as the UIDocument's SourceAsset.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UIToolkitManager : MonoBehaviour
{
    public static UIToolkitManager Instance;

    // ── Cached element refs ───────────────────────────────────────────────
    VisualElement _root;

    // Panels
    VisualElement _mainMenu, _difficulty, _settings, _lobby, _pause, _end;

    // HUD
    VisualElement _hudLayer;
    Label _hudPlayers, _hudRound, _hudTimer, _hudPos;

    // Countdown
    VisualElement _countdownOverlay;
    Label _countdownNumber;

    // Message
    VisualElement _messageBanner;
    Label _messageText;
    Coroutine _hideMessageCoroutine;

    // End
    Label _endTitle, _endSub;

    // Onboarding
    VisualElement _onboarding;
    bool _splashDone;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        var doc = GetComponent<UIDocument>();
        _root = doc.rootVisualElement;

        // Root must NOT block legacy UGUI or game input on transparent areas.
        // Buttons/interactive children keep their default PickingMode.Position.
        _root.pickingMode = PickingMode.Ignore;

        // Onboarding
        _onboarding = _root.Q("OnboardingPanel");

        // Panels
        _mainMenu   = _root.Q("MainMenuPanel");
        _difficulty = _root.Q("DifficultyPanel");
        _settings   = _root.Q("SettingsPanel");
        _lobby      = _root.Q("LobbyPanel");
        _pause      = _root.Q("PausePanel");
        _end        = _root.Q("EndPanel");

        // HUD
        _hudLayer   = _root.Q("HUDLayer");
        _hudPlayers = _root.Q<Label>("hud-players");
        _hudRound   = _root.Q<Label>("hud-round");
        _hudTimer   = _root.Q<Label>("hud-timer");
        _hudPos     = _root.Q<Label>("hud-pos");

        // Countdown
        _countdownOverlay = _root.Q("CountdownOverlay");
        _countdownNumber  = _root.Q<Label>("countdown-number");

        // Message
        _messageBanner = _root.Q("MessageBanner");
        _messageText   = _root.Q<Label>("message-text");

        // End
        _endTitle = _root.Q<Label>("end-title");
        _endSub   = _root.Q<Label>("end-sub");

        WireButtons();

        // Onboarding: tap/click anywhere dismisses splash
        if (_onboarding != null)
        {
            _onboarding.pickingMode = PickingMode.Position;
            _onboarding.RegisterCallback<PointerDownEvent>(_ => DismissOnboarding());
        }
    }

    void DismissOnboarding()
    {
        if (_splashDone) return;
        _splashDone = true;
        _onboarding?.RemoveFromClassList("panel--visible");
        ShowMainMenu();
    }

    // Called by SplashController when legacy splash is dismissed
    public void OnSplashDismissed()
    {
        if (_splashDone) return;
        _splashDone = true;
        _onboarding?.RemoveFromClassList("panel--visible");
        ShowMainMenu();
    }

    // ── Button wiring ─────────────────────────────────────────────────────

    void WireButtons()
    {
        // Main Menu
        Btn("btn-play",        () => RaceManager.Instance?.OnPlayPressed());
        Btn("btn-multiplayer", () => ShowLobbyPanel());
        Btn("btn-settings",    () => ShowPanel(_settings));

        // Difficulty
        Btn("btn-easy",   () => DifficultyManager.Instance?.Select(DifficultyManager.Diff.Easy));
        Btn("btn-normal", () => DifficultyManager.Instance?.Select(DifficultyManager.Diff.Normal));
        Btn("btn-hard",   () => DifficultyManager.Instance?.Select(DifficultyManager.Diff.Hard));
        Btn("btn-ultra",  () => DifficultyManager.Instance?.Select(DifficultyManager.Diff.Ultra));
        BtnInPanel(_difficulty, "btn-back", () => ShowPanel(_mainMenu));

        // Settings
        BtnInPanel(_settings, "btn-back", () => ShowPanel(_mainMenu));
        WireSettingsToggles();

        // Pause
        Btn("btn-resume",  () => PauseManager.Instance?.Resume());
        Btn("btn-restart", () => { PauseManager.Instance?.Resume(); RaceManager.Instance?.BeginRace(); });
        Btn("btn-menu",    () => { PauseManager.Instance?.Resume(); RaceManager.Instance?.ReturnToMainMenu(); });

        // End
        Btn("btn-again", () => RaceManager.Instance?.PlayAgain());
        BtnInPanel(_end, "btn-menu", () => RaceManager.Instance?.ReturnToMainMenu());

        // Lobby
        Btn("btn-host", () => NetworkLobbyManager.Instance?.Host());
        Btn("btn-join", () =>
        {
            var tf = _root.Q<TextField>("nickname-field");
            NetworkLobbyManager.Instance?.JoinGame(tf?.value ?? "");
        });
        BtnInPanel(_lobby, "btn-back", () => ShowPanel(_mainMenu));
        Btn("btn-start",      () => NetworkLobbyManager.Instance?.StartGame());
        Btn("btn-leave",      () => NetworkLobbyManager.Instance?.Leave());
        Btn("btn-bots-toggle",() => NetworkLobbyManager.Instance?.ToggleBots());
    }

    void Btn(string name, System.Action cb)
    {
        var btn = _root.Q<Button>(name);
        if (btn != null) btn.clicked += cb;
    }

    void BtnInPanel(VisualElement panel, string name, System.Action cb)
    {
        var btn = panel?.Q<Button>(name);
        if (btn != null) btn.clicked += cb;
    }

    // ── Panel show/hide ───────────────────────────────────────────────────

    void ShowPanel(VisualElement panel)
    {
        // Hide all panels, then show the requested one
        foreach (var p in new[] { _mainMenu, _difficulty, _settings, _lobby, _pause, _end })
            p?.RemoveFromClassList("panel--visible");
        panel?.AddToClassList("panel--visible");
    }

    public void ShowMainMenu()    => ShowPanel(_mainMenu);
    public void HideMainMenu()    => _mainMenu?.RemoveFromClassList("panel--visible");

    public void ShowDifficultyPanel() => ShowPanel(_difficulty);
    public void HideDifficultyPanel() => _difficulty?.RemoveFromClassList("panel--visible");

    public void ShowSettings()    => ShowPanel(_settings);
    public void HideSettings()    => _settings?.RemoveFromClassList("panel--visible");

    public void ShowLobbyPanel()  => ShowPanel(_lobby);
    public void HideLobbyPanel()  => _lobby?.RemoveFromClassList("panel--visible");

    public void ShowPauseMenu()   => _pause?.AddToClassList("panel--visible");
    public void HidePauseMenu(System.Action onDone = null)
    {
        _pause?.RemoveFromClassList("panel--visible");
        onDone?.Invoke();
    }

    public void ShowEndScreen(string title, string sub, Color col)
    {
        if (_endTitle != null)
        {
            _endTitle.text  = title;
            _endTitle.style.color = new StyleColor(col);
        }
        if (_endSub != null) _endSub.text = sub;
        ShowPanel(_end);
    }

    public void HideEndScreen() => _end?.RemoveFromClassList("panel--visible");

    // ── HUD ───────────────────────────────────────────────────────────────

    public void ShowHUD() => _hudLayer?.AddToClassList("panel--visible");
    public void HideHUD() => _hudLayer?.RemoveFromClassList("panel--visible");

    public void UpdateTimer(float time)
    {
        if (_hudTimer != null) _hudTimer.text = Mathf.CeilToInt(time) + "s";
    }

    public void UpdatePlayerCount(int count)
    {
        if (_hudPlayers != null)
            _hudPlayers.text = string.Format(
                LocalizationManager.Instance?.Get("hud.players.fmt") ?? "Players: {0}", count);
    }

    public void ShowRoundText(string text)
    {
        if (_hudRound != null) _hudRound.text = text;
    }

    static readonly string[] _suffixes = { "", "st", "nd", "rd", "th", "th", "th", "th", "th" };
    public void UpdatePosition(int pos, int total)
    {
        if (_hudPos == null) return;
        bool ru = LocalizationManager.Instance?.Current == LocalizationManager.Lang.Russian;
        string suf = (!ru && pos < _suffixes.Length) ? _suffixes[pos] : "";
        _hudPos.text  = pos + suf + " / " + total;
        _hudPos.style.color = new StyleColor(
            pos == 1 ? new Color(1f, 0.85f, 0.1f) :
            pos == 2 ? new Color(0.8f, 0.8f, 0.8f) :
            pos == 3 ? new Color(0.8f, 0.5f, 0.2f) : Color.white);
    }

    // ── Countdown ─────────────────────────────────────────────────────────

    public void ShowCountdown(string text)
    {
        if (_countdownNumber != null)
        {
            _countdownNumber.text = text;
            _countdownNumber.EnableInClassList("countdown-number--go", text == "GO!");
        }
        _countdownOverlay?.AddToClassList("countdown-overlay--visible");
    }

    public void HideCountdown()
        => _countdownOverlay?.RemoveFromClassList("countdown-overlay--visible");

    // ── Message ───────────────────────────────────────────────────────────

    public void ShowMessage(string msg, Color color)
    {
        if (_messageText != null)
        {
            _messageText.text = msg;
            _messageText.style.color = new StyleColor(color);
        }
        _messageBanner?.AddToClassList("message-banner--visible");

        if (_hideMessageCoroutine != null) StopCoroutine(_hideMessageCoroutine);
        _hideMessageCoroutine = StartCoroutine(AutoHide(3f));
    }

    public void HideMessage()
        => _messageBanner?.RemoveFromClassList("message-banner--visible");

    IEnumerator AutoHide(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideMessage();
    }

    // ── Settings toggles ──────────────────────────────────────────────────

    void WireSettingsToggles()
    {
        // Quality
        WireToggleGroup(_settings, new[] { "q-low", "q-med", "q-high", "q-ultra" },
            PlayerPrefs.GetInt("GS_quality", 2),
            i => { QualitySettings.SetQualityLevel(i); PlayerPrefs.SetInt("GS_quality", i); });

        // Theme
        WireToggleGroup(_settings, new[] { "t-std", "t-bw" },
            PlayerPrefs.GetInt("GS_theme", 0),
            i => { WorldThemeManager.Instance?.Apply(i); PlayerPrefs.SetInt("GS_theme", i); });

        // Round Time
        WireToggleGroup(_settings, new[] { "rt-45", "rt-60", "rt-75" },
            PlayerPrefs.GetInt("GS_roundIdx", 2),
            i =>
            {
                int[] times = { 45, 60, 75 };
                if (RaceManager.Instance) RaceManager.Instance.roundDuration = times[i];
                PlayerPrefs.SetInt("GS_roundIdx", i);
            });

        // Eliminate
        WireToggleGroup(_settings, new[] { "el-1", "el-2", "el-3" },
            PlayerPrefs.GetInt("GS_elimIdx", 1),
            i =>
            {
                if (RaceManager.Instance) RaceManager.Instance.eliminatePerRound = i + 1;
                PlayerPrefs.SetInt("GS_elimIdx", i);
            });

        // Camera Shake
        WireToggleGroup(_settings, new[] { "sh-on", "sh-off" },
            PlayerPrefs.GetInt("GS_camShake", 1) == 1 ? 0 : 1,
            i =>
            {
                bool on = i == 0;
                if (CameraFollow.Instance) CameraFollow.Instance.shakeEnabled = on;
                PlayerPrefs.SetInt("GS_camShake", on ? 1 : 0);
            });

        // Trails
        WireToggleGroup(_settings, new[] { "tr-on", "tr-off" },
            PlayerPrefs.GetInt("GS_trails", 1) == 1 ? 0 : 1,
            i => PlayerPrefs.SetInt("GS_trails", i == 0 ? 1 : 0));

        // Language
        WireToggleGroup(_settings, new[] { "l-en", "l-ru" },
            PlayerPrefs.GetInt("GS_lang", 0),
            i => LocalizationManager.Instance?.SetLanguage(
                i == 0 ? LocalizationManager.Lang.English
                       : LocalizationManager.Lang.Russian));

        // Color swatches
        BuildColorSwatches();
    }

    void WireToggleGroup(VisualElement parent, string[] names, int activeIndex,
                         System.Action<int> onChange)
    {
        var buttons = new Button[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            int idx = i;
            buttons[i] = parent.Q<Button>(names[i]);
            if (buttons[i] == null) continue;

            buttons[i].EnableInClassList("toggle-btn--active", i == activeIndex);
            buttons[i].clicked += () =>
            {
                foreach (var b in buttons) b?.RemoveFromClassList("toggle-btn--active");
                buttons[idx]?.AddToClassList("toggle-btn--active");
                onChange(idx);
            };
        }
    }

    void BuildColorSwatches()
    {
        var row = _settings?.Q("swatch-row");
        if (row == null) return;

        Color[] colors = LobbyPanelController.PlayerColors;
        int selected   = PlayerPrefs.GetInt("GS_colorIndex", 0);

        for (int i = 0; i < colors.Length; i++)
        {
            int idx    = i;
            var swatch = new Button();
            swatch.AddToClassList("swatch");
            swatch.style.backgroundColor = new StyleColor(colors[i]);
            swatch.EnableInClassList("swatch--selected", i == selected);

            swatch.clicked += () =>
            {
                row.Query<Button>(className: "swatch").ForEach(
                    s => s.RemoveFromClassList("swatch--selected"));
                swatch.AddToClassList("swatch--selected");
                PlayerPrefs.SetInt("GS_colorIndex", idx);
            };
            row.Add(swatch);
        }
    }
}
