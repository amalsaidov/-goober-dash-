using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

/// <summary>
/// UI Toolkit drop-in for UIManager.
/// Extends UIManager → UIManager.Instance points here.
/// All game code (RaceManager, PauseManager, etc.) works without modification.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UIToolkitManager : UIManager
{
    // ── Panels ────────────────────────────────────────────────────────────
    VisualElement _onboarding;
    VisualElement _mainMenu, _difficulty, _settings, _lobby, _pause, _end;
    VisualElement[] _allPanels; // all full-screen swappable panels

    // ── HUD ───────────────────────────────────────────────────────────────
    VisualElement _hudLayer;
    Label _hudPlayers, _hudRound, _hudTimer, _hudPos;

    // ── Countdown ─────────────────────────────────────────────────────────
    VisualElement _countdownOverlay;
    Label _countdownNumber;

    // ── Message ───────────────────────────────────────────────────────────
    VisualElement _messageBanner;
    Label _messageText;
    Coroutine _hideMsg;

    // ── End ───────────────────────────────────────────────────────────────
    Label _endTitle, _endSub;

    // ── Lobby sub-views ───────────────────────────────────────────────────
    VisualElement _connectView, _roomView, _hostSection;
    Label _ipAddress, _clientWait;
    ScrollView _playerList, _serverList;
    Label _serverSearchText;

    // ── Root ──────────────────────────────────────────────────────────────
    VisualElement _root;
    bool _splashDone;

    // ─────────────────────────────────────────────────────────────────────
    // AWAKE — sets UIManager.Instance = this via base.Awake()
    // ─────────────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake(); // UIManager.Instance = this

        var doc = GetComponent<UIDocument>();
        _root = doc.rootVisualElement;
        _root.pickingMode = PickingMode.Ignore;

        // ── Cache panels ─────────────────────────────────────────────────
        _onboarding = _root.Q("OnboardingPanel");
        _mainMenu   = _root.Q("MainMenuPanel");
        _difficulty = _root.Q("DifficultyPanel");
        _settings   = _root.Q("SettingsPanel");
        _lobby      = _root.Q("LobbyPanel");
        _pause      = _root.Q("PausePanel");
        _end        = _root.Q("EndPanel");

        _allPanels = new[] { _onboarding, _mainMenu, _difficulty, _settings, _lobby, _end };

        // ── Cache HUD ────────────────────────────────────────────────────
        _hudLayer   = _root.Q("HUDLayer");
        _hudPlayers = _root.Q<Label>("hud-players");
        _hudRound   = _root.Q<Label>("hud-round");
        _hudTimer   = _root.Q<Label>("hud-timer");
        _hudPos     = _root.Q<Label>("hud-pos");

        // ── Cache Countdown ──────────────────────────────────────────────
        _countdownOverlay = _root.Q("CountdownOverlay");
        _countdownNumber  = _root.Q<Label>("countdown-number");

        // ── Cache Message ────────────────────────────────────────────────
        _messageBanner = _root.Q("MessageBanner");
        _messageText   = _root.Q<Label>("message-text");

        // ── Cache End ────────────────────────────────────────────────────
        _endTitle = _root.Q<Label>("end-title");
        _endSub   = _root.Q<Label>("end-sub");

        // ── Cache Lobby sub-views ─────────────────────────────────────────
        _connectView      = _root.Q("connect-view");
        _roomView         = _root.Q("room-view");
        _hostSection      = _root.Q("host-section");
        _ipAddress        = _root.Q<Label>("ip-address");
        _clientWait       = _root.Q<Label>("client-wait");
        _playerList       = _root.Q<ScrollView>("player-list");
        _serverList       = _root.Q<ScrollView>("server-list");
        _serverSearchText = _root.Q<Label>("server-search-text");

        // ── Initial state: all panels hidden ─────────────────────────────
        foreach (var p in _allPanels)
        {
            if (p == null) continue;
            p.style.display = DisplayStyle.None;
            p.pickingMode   = PickingMode.Ignore;
        }
        HideHUD();

        // Pause overlay starts hidden
        if (_pause != null) { _pause.style.display = DisplayStyle.None; _pause.pickingMode = PickingMode.Ignore; }

        // ── Wire buttons ──────────────────────────────────────────────────
        WireButtons();

        // Onboarding: tap anywhere to dismiss
        if (_onboarding != null)
            _onboarding.RegisterCallback<PointerDownEvent>(_ => DismissOnboarding());

        // Show onboarding splash on startup
        ShowOnboarding();
    }

    // ─────────────────────────────────────────────────────────────────────
    // ONBOARDING
    // ─────────────────────────────────────────────────────────────────────

    void ShowOnboarding()
    {
        _splashDone = false;
        PanelIn(_onboarding);
    }

    void DismissOnboarding()
    {
        if (_splashDone) return;
        _splashDone = true;
        PanelOut(_onboarding, () => ShowMainMenu());
    }

    // Called by SplashController (legacy path)
    public override void OnSplashDismissed()
    {
        if (_splashDone) return;
        _splashDone = true;
        PanelOut(_onboarding, null);
        ShowMainMenu();
    }

    // ─────────────────────────────────────────────────────────────────────
    // ANIMATED PANEL HELPERS
    // ─────────────────────────────────────────────────────────────────────

    // Animate in: display flex → next frame → add panel--visible (triggers CSS transition)
    void PanelIn(VisualElement el)
    {
        if (el == null) return;
        el.style.display = DisplayStyle.Flex;
        el.pickingMode   = PickingMode.Position;
        // Apply visible class after 1 frame so the CSS transition fires
        el.schedule.Execute(() => el.AddToClassList("panel--visible")).ExecuteLater(20);
    }

    // Animate out: remove panel--visible → after transition → display none
    void PanelOut(VisualElement el, System.Action onDone)
    {
        if (el == null) { onDone?.Invoke(); return; }
        el.RemoveFromClassList("panel--visible");
        el.pickingMode = PickingMode.Ignore;
        el.schedule.Execute(() =>
        {
            if (!el.ClassListContains("panel--visible"))
                el.style.display = DisplayStyle.None;
            onDone?.Invoke();
        }).ExecuteLater(260); // after 0.22s transition + buffer
    }

    void SwitchPanel(VisualElement target)
    {
        _splashDone = true; // any panel switch means splash is past
        foreach (var p in _allPanels)
            if (p != target) PanelOut(p, null);
        PanelIn(target);
    }

    // ─────────────────────────────────────────────────────────────────────
    // UIManager OVERRIDES — all game code routes here
    // ─────────────────────────────────────────────────────────────────────

    public override void ShowMainMenu()
    {
        // Guard: if onboarding hasn't been tapped yet, don't skip splash.
        // DismissOnboarding() will call ShowMainMenu() once the player taps.
        if (!_splashDone) return;
        SwitchPanel(_mainMenu);
    }
    public override void HideMainMenu()        => PanelOut(_mainMenu, null);
    public override void ShowDifficultyPanel() => SwitchPanel(_difficulty);
    public override void HideDifficultyPanel() => PanelOut(_difficulty, null);
    public override void ShowSettings()        => SwitchPanel(_settings);
    public override void HideSettings()        => PanelOut(_settings, null);
    public override void HideEndScreen()       => PanelOut(_end, null);

    public override void ShowLobbyPanel()
    {
        SwitchPanel(_lobby);
        ShowConnectView();
        LanDiscovery.Instance?.StartListening();
    }

    public override void HideLobbyPanel()
    {
        PanelOut(_lobby, null);
        LanDiscovery.Instance?.StopAll();
    }

    public override void ShowPauseMenu()
    {
        if (_pause == null) return;
        _pause.style.display = DisplayStyle.Flex;
        _pause.pickingMode   = PickingMode.Position;
        _pause.schedule.Execute(() => _pause.AddToClassList("overlay-dim--visible")).ExecuteLater(20);
    }

    public override void HidePauseMenu(System.Action onDone = null)
    {
        if (_pause == null) { onDone?.Invoke(); return; }
        _pause.RemoveFromClassList("overlay-dim--visible");
        _pause.pickingMode = PickingMode.Ignore;
        _pause.schedule.Execute(() =>
        {
            if (!_pause.ClassListContains("overlay-dim--visible"))
                _pause.style.display = DisplayStyle.None;
            onDone?.Invoke();
        }).ExecuteLater(260);
    }

    public override void ShowEndScreen(string title, string sub, Color col)
    {
        if (_endTitle != null) { _endTitle.text = title; _endTitle.style.color = new StyleColor(col); }
        if (_endSub   != null)   _endSub.text = sub;
        SwitchPanel(_end);
    }

    // ── HUD ───────────────────────────────────────────────────────────────

    public override void ShowHUD()
    {
        if (_hudLayer == null) return;
        _hudLayer.style.display = DisplayStyle.Flex;
        _hudLayer.schedule.Execute(() => _hudLayer.AddToClassList("hud-layer--visible")).ExecuteLater(20);
    }

    public override void HideHUD()
    {
        if (_hudLayer == null) return;
        _hudLayer.RemoveFromClassList("hud-layer--visible");
        _hudLayer.schedule.Execute(() =>
        {
            if (!_hudLayer.ClassListContains("hud-layer--visible"))
                _hudLayer.style.display = DisplayStyle.None;
        }).ExecuteLater(260);
    }

    public override void UpdateTimer(float time)
    {
        if (_hudTimer != null) _hudTimer.text = Mathf.CeilToInt(time) + "s";
    }

    public override void UpdatePlayerCount(int count)
    {
        if (_hudPlayers != null)
            _hudPlayers.text = string.Format(
                LocalizationManager.Instance?.Get("hud.players.fmt") ?? "Players: {0}", count);
    }

    public override void ShowRoundText(string text)
    {
        if (_hudRound != null) _hudRound.text = text;
    }

    static readonly string[] _suf = { "", "st", "nd", "rd", "th", "th", "th", "th", "th" };
    public override void UpdatePosition(int pos, int total)
    {
        if (_hudPos == null) return;
        bool ru  = LocalizationManager.Instance?.Current == LocalizationManager.Lang.Russian;
        string s = (!ru && pos < _suf.Length) ? _suf[pos] : "";
        _hudPos.text = pos + s + " / " + total;
        _hudPos.style.color = new StyleColor(
            pos == 1 ? new Color(1f, 0.85f, 0.1f) :
            pos == 2 ? new Color(0.8f, 0.8f, 0.8f) :
            pos == 3 ? new Color(0.8f, 0.5f, 0.2f) : Color.white);
    }

    // ── Countdown ─────────────────────────────────────────────────────────

    public override void ShowCountdown(string text)
    {
        if (_countdownNumber != null)
        {
            _countdownNumber.text = text;
            _countdownNumber.EnableInClassList("countdown-number--go", text == "GO!");
        }
        if (_countdownOverlay != null)
        {
            _countdownOverlay.style.display = DisplayStyle.Flex;
            _countdownOverlay.schedule.Execute(
                () => _countdownOverlay.AddToClassList("countdown-overlay--visible")).ExecuteLater(20);
        }
    }

    public override void HideCountdown()
    {
        if (_countdownOverlay == null) return;
        _countdownOverlay.RemoveFromClassList("countdown-overlay--visible");
        _countdownOverlay.schedule.Execute(() =>
        {
            if (!_countdownOverlay.ClassListContains("countdown-overlay--visible"))
                _countdownOverlay.style.display = DisplayStyle.None;
        }).ExecuteLater(260);
    }

    // ── Message ───────────────────────────────────────────────────────────

    public override void ShowMessage(string msg, Color color)
    {
        if (_messageText != null) { _messageText.text = msg; _messageText.style.color = new StyleColor(color); }
        if (_messageBanner != null)
        {
            _messageBanner.style.display = DisplayStyle.Flex;
            _messageBanner.schedule.Execute(
                () => _messageBanner.AddToClassList("message-banner--visible")).ExecuteLater(20);
        }
        if (_hideMsg != null) StopCoroutine(_hideMsg);
        _hideMsg = StartCoroutine(AutoHideMsg(3f));
    }

    public override void HideMessage()
    {
        if (_messageBanner == null) return;
        _messageBanner.RemoveFromClassList("message-banner--visible");
        _messageBanner.schedule.Execute(() =>
        {
            if (!_messageBanner.ClassListContains("message-banner--visible"))
                _messageBanner.style.display = DisplayStyle.None;
        }).ExecuteLater(260);
    }

    IEnumerator AutoHideMsg(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideMessage();
    }

    // ─────────────────────────────────────────────────────────────────────
    // LOBBY — sub-view management
    // ─────────────────────────────────────────────────────────────────────

    public void ShowConnectView()
    {
        if (_connectView != null) _connectView.style.display = DisplayStyle.Flex;
        if (_roomView    != null) _roomView.style.display    = DisplayStyle.None;

        // Pre-fill nickname
        var nf = _root?.Q<TextField>("nickname-field");
        if (nf != null) nf.value = LobbyPanelController.GetLocalNickname();

        RefreshLobbyServers();
    }

    public override void ShowLobbyRoomView(bool isHost)
    {
        if (_connectView != null) _connectView.style.display = DisplayStyle.None;
        if (_roomView    != null) _roomView.style.display    = DisplayStyle.Flex;

        if (_hostSection != null)
            _hostSection.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;

        if (isHost && _ipAddress != null && NetworkLobbyManager.Instance != null)
            _ipAddress.text = NetworkLobbyManager.Instance.GetLocalIP();

        if (_clientWait != null)
            _clientWait.style.display = isHost ? DisplayStyle.None : DisplayStyle.Flex;

        // Bots section — only host
        var botsSection = _root?.Q("bots-section");
        if (botsSection != null)
            botsSection.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;

        // Start button — only host
        var startBtn = _root?.Q<Button>("btn-start");
        if (startBtn != null)
            startBtn.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;

        RefreshLobbyPlayers();
    }

    public override void RefreshLobby(int playerCount, bool botsOn, bool isHost)
    {
        // Ensure room view is showing
        if (_roomView != null && _roomView.style.display == DisplayStyle.None)
            ShowLobbyRoomView(isHost);

        // Update bots toggle text
        var botsBtn = _root?.Q<Button>("btn-bots-toggle");
        if (botsBtn != null) botsBtn.text = botsOn ? "ON" : "OFF";

        RefreshLobbyPlayers();
    }

    public override void RefreshLobbyPlayers()
    {
        if (_playerList == null) return;
        _playerList.Clear();

        var mgr = NetworkLobbyManager.Instance;
        if (mgr == null) return;

        var list = mgr.GetPlayers();
        for (int i = 0; i < list.Count; i++)
            AddPlayerRow(list[i]);
    }

    void AddPlayerRow(LobbyPlayerData data)
    {
        Color col = LobbyPanelController.PlayerColors[
            data.colorIndex % LobbyPanelController.PlayerColors.Length];

        var row = new VisualElement();
        row.AddToClassList("player-row");

        var dot = new VisualElement();
        dot.AddToClassList("player-dot");
        dot.style.backgroundColor = new StyleColor(col);

        var nameLabel = new Label(data.nickname.ToString());
        nameLabel.AddToClassList("player-name");
        nameLabel.style.color = new StyleColor(col);

        row.Add(dot);
        row.Add(nameLabel);

        bool isLocal = NetworkManager.Singleton != null &&
                       data.clientId == NetworkManager.Singleton.LocalClientId;
        string tag = data.isHost ? "HOST" : (isLocal ? "YOU" : "");
        if (!string.IsNullOrEmpty(tag))
        {
            var badge = new Label(tag);
            badge.AddToClassList("player-badge");
            if (!data.isHost) badge.style.color = new StyleColor(new Color(0.4f, 1f, 0.5f));
            row.Add(badge);
        }

        _playerList.Add(row);
    }

    // ── LAN server browser ────────────────────────────────────────────────

    public override void RefreshLobbyServers()
    {
        if (_serverList == null) return;
        _serverList.Clear();

        var servers   = LanDiscovery.Instance?.GetServers();
        bool hasAny   = servers != null && servers.Count > 0;

        if (_serverSearchText != null)
            _serverSearchText.style.display = hasAny ? DisplayStyle.None : DisplayStyle.Flex;

        if (!hasAny) return;

        foreach (var s in servers)
        {
            var row = new VisualElement();
            row.AddToClassList("server-row");

            var ip = new Label(s.ip);
            ip.AddToClassList("server-row__ip");

            var cnt = new Label($"{s.playerCount}/{s.maxPlayers}");
            cnt.AddToClassList("server-row__count");

            var btn = new Button();
            btn.AddToClassList("server-row__join");
            btn.text = "JOIN";
            string capturedIp = s.ip;
            btn.clicked += () => JoinServer(capturedIp);

            row.Add(ip);
            row.Add(cnt);
            row.Add(btn);
            _serverList.Add(row);
        }
    }

    void JoinServer(string ip)
    {
        SaveNickname();
        LanDiscovery.Instance?.StopListening();
        NetworkLobbyManager.Instance?.JoinGame(ip);
        ShowLobbyRoomView(false);
    }

    void SaveNickname()
    {
        var nf = _root?.Q<TextField>("nickname-field");
        string nick = nf?.value ?? LobbyPanelController.GetLocalNickname();
        if (!string.IsNullOrWhiteSpace(nick))
        { PlayerPrefs.SetString("PlayerNickname", nick.Trim()); PlayerPrefs.Save(); }
    }

    // ─────────────────────────────────────────────────────────────────────
    // BUTTON WIRING
    // ─────────────────────────────────────────────────────────────────────

    void WireButtons()
    {
        // Main Menu
        Btn("btn-play",        () => RaceManager.Instance?.OnPlayPressed());
        Btn("btn-multiplayer", () => ShowLobbyPanel());
        Btn("btn-settings",    () => ShowSettings());

        // Difficulty (back wired per-panel to avoid ambiguity)
        Btn("btn-easy",   () => DifficultyManager.Instance?.Select(DifficultyManager.Diff.Easy));
        Btn("btn-normal", () => DifficultyManager.Instance?.Select(DifficultyManager.Diff.Normal));
        Btn("btn-hard",   () => DifficultyManager.Instance?.Select(DifficultyManager.Diff.Hard));
        Btn("btn-ultra",  () => DifficultyManager.Instance?.Select(DifficultyManager.Diff.Ultra));
        BtnIn(_difficulty, "btn-back", () => ShowMainMenu());

        // Settings
        BtnIn(_settings, "btn-back", () => ShowMainMenu());
        WireSettingsToggles();

        // Pause
        Btn("btn-resume",  () => PauseManager.Instance?.Resume());
        Btn("btn-restart", () => { PauseManager.Instance?.Resume(); RaceManager.Instance?.BeginRace(); });
        BtnIn(_pause, "btn-menu", () =>
            { PauseManager.Instance?.Resume(); RaceManager.Instance?.ReturnToMainMenu(); });

        // End
        Btn("btn-again", () => RaceManager.Instance?.PlayAgain());
        BtnIn(_end, "btn-menu", () => RaceManager.Instance?.ReturnToMainMenu());

        // Lobby connect view
        Btn("btn-host", () =>
        {
            SaveNickname();
            LanDiscovery.Instance?.StopListening();
            NetworkLobbyManager.Instance?.Host();
            ShowLobbyRoomView(true);
        });
        BtnIn(_lobby, "btn-back", () =>
        {
            NetworkLobbyManager.Instance?.Leave();
            LanDiscovery.Instance?.StopAll();
            ShowMainMenu();
        });

        // Lobby room view
        Btn("btn-start",       () => NetworkLobbyManager.Instance?.StartGame());
        Btn("btn-leave",       () => { NetworkLobbyManager.Instance?.Leave(); ShowMainMenu(); });
        Btn("btn-bots-toggle", () => NetworkLobbyManager.Instance?.ToggleBots());
    }

    void Btn(string name, System.Action cb)
    {
        var btn = _root?.Q<Button>(name);
        if (btn != null) btn.clicked += cb;
    }

    void BtnIn(VisualElement panel, string name, System.Action cb)
    {
        var btn = panel?.Q<Button>(name);
        if (btn != null) btn.clicked += cb;
    }

    // ─────────────────────────────────────────────────────────────────────
    // SETTINGS TOGGLES
    // ─────────────────────────────────────────────────────────────────────

    void WireSettingsToggles()
    {
        WireToggle(new[] { "q-low", "q-med", "q-high", "q-ultra" },
            PlayerPrefs.GetInt("GS_quality", 2),
            i => { QualitySettings.SetQualityLevel(i); PlayerPrefs.SetInt("GS_quality", i); });

        WireToggle(new[] { "t-std", "t-bw" },
            PlayerPrefs.GetInt("GS_theme", 0),
            i => { WorldThemeManager.Instance?.Apply(i); PlayerPrefs.SetInt("GS_theme", i); });

        WireToggle(new[] { "rt-45", "rt-60", "rt-75" },
            PlayerPrefs.GetInt("GS_roundIdx", 2),
            i => { int[] t = {45, 60, 75}; if (RaceManager.Instance) RaceManager.Instance.roundDuration = t[i]; PlayerPrefs.SetInt("GS_roundIdx", i); });

        WireToggle(new[] { "el-1", "el-2", "el-3" },
            PlayerPrefs.GetInt("GS_elimIdx", 1),
            i => { if (RaceManager.Instance) RaceManager.Instance.eliminatePerRound = i + 1; PlayerPrefs.SetInt("GS_elimIdx", i); });

        WireToggle(new[] { "sh-on", "sh-off" },
            PlayerPrefs.GetInt("GS_camShake", 1) == 1 ? 0 : 1,
            i => { bool on = i == 0; if (CameraFollow.Instance) CameraFollow.Instance.shakeEnabled = on; PlayerPrefs.SetInt("GS_camShake", on ? 1 : 0); });

        WireToggle(new[] { "tr-on", "tr-off" },
            PlayerPrefs.GetInt("GS_trails", 1) == 1 ? 0 : 1,
            i => PlayerPrefs.SetInt("GS_trails", i == 0 ? 1 : 0));

        WireToggle(new[] { "l-en", "l-ru" },
            PlayerPrefs.GetInt("GS_lang", 0),
            i => LocalizationManager.Instance?.SetLanguage(
                i == 0 ? LocalizationManager.Lang.English : LocalizationManager.Lang.Russian));

        BuildColorSwatches();
    }

    void WireToggle(string[] names, int active, System.Action<int> onChange)
    {
        var btns = new Button[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            int idx = i;
            btns[i] = _root?.Q<Button>(names[i]);
            if (btns[i] == null) continue;
            btns[i].EnableInClassList("toggle-btn--active", i == active);
            btns[i].clicked += () =>
            {
                foreach (var b in btns) b?.RemoveFromClassList("toggle-btn--active");
                btns[idx]?.AddToClassList("toggle-btn--active");
                onChange(idx);
            };
        }
    }

    void BuildColorSwatches()
    {
        var row = _settings?.Q("swatch-row");
        if (row == null) return;

        Color[] colors = LobbyPanelController.PlayerColors;
        int sel = PlayerPrefs.GetInt("GS_colorIndex", 0);

        for (int i = 0; i < colors.Length; i++)
        {
            int idx    = i;
            var swatch = new Button();
            swatch.AddToClassList("swatch");
            swatch.style.backgroundColor = new StyleColor(colors[i]);
            swatch.EnableInClassList("swatch--selected", i == sel);
            swatch.clicked += () =>
            {
                row.Query<Button>(className: "swatch").ForEach(s => s.RemoveFromClassList("swatch--selected"));
                swatch.AddToClassList("swatch--selected");
                PlayerPrefs.SetInt("GS_colorIndex", idx);
            };
            row.Add(swatch);
        }
    }
}
