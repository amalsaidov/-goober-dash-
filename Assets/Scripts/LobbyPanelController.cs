using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Controls the lobby panel UI.
/// ConnectView — nickname input + HOST / JOIN buttons
/// RoomView    — player list with names/colors, bot toggle, START button
/// </summary>
public class LobbyPanelController : MonoBehaviour
{
    public static LobbyPanelController Instance;

    [Header("Connect view")]
    public GameObject connectView;
    public InputField  ipInput;
    public InputField  nicknameInput;

    [Header("Room view")]
    public GameObject roomView;
    public Text        ipDisplayText;
    public Text        playerCountText;
    public Text        botsStatusText;
    public Button      toggleBotsButton;
    public Text        clientWaitText;
    public Button      startButton;
    public Transform   playerListContainer;

    [Header("Server browser (connect view)")]
    public Transform serverListContainer;
    public Text      serverSearchText;

    // ── Player color palette (10 colors; indices 0-9) ──────────────────────
    public static readonly Color[] PlayerColors =
    {
        new Color(.20f, .55f, 1f),    // 0  Blue
        new Color(.95f, .22f, .22f),  // 1  Red
        new Color(.22f, .85f, .22f),  // 2  Green
        new Color(1.0f, .60f, .10f),  // 3  Orange
        new Color(.72f, .18f, .88f),  // 4  Purple
        new Color(.15f, .85f, .85f),  // 5  Cyan
        new Color(1.0f, .35f, .70f),  // 6  Pink
        new Color(1.0f, .90f, .18f),  // 7  Yellow
        new Color(.00f, .75f, .65f),  // 8  Teal
        new Color(.65f, .95f, .10f),  // 9  Lime
    };

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        ShowConnectView();
        // Pre-fill nickname from saved value
        if (nicknameInput != null)
            nicknameInput.text = GetLocalNickname();
    }

    // ── Static nickname helpers ───────────────────────────────────────────

    public static string GetLocalNickname()
    {
        string nick = PlayerPrefs.GetString("PlayerNickname", "");
        if (string.IsNullOrWhiteSpace(nick))
            nick = "Player" + Random.Range(100, 999);
        return nick;
    }

    /// <summary>Returns the color index saved in Settings (0-9, default 0).</summary>
    public static int GetLocalColorIndex()
        => PlayerPrefs.GetInt("GS_colorIndex", 0);

    static void SaveNickname(string nick)
    {
        if (!string.IsNullOrWhiteSpace(nick))
        {
            PlayerPrefs.SetString("PlayerNickname", nick.Trim());
            PlayerPrefs.Save();
        }
    }

    void CommitNicknameInput()
    {
        if (nicknameInput != null && !string.IsNullOrWhiteSpace(nicknameInput.text))
            SaveNickname(nicknameInput.text);
    }

    // ── Views ─────────────────────────────────────────────────────────────

    public void ShowConnectView()
    {
        if (connectView) connectView.SetActive(true);
        if (roomView)    roomView.SetActive(false);
        LanDiscovery.Instance?.StartListening();
        RefreshServerList();
    }

    public void ShowRoomView(bool isHost)
    {
        if (connectView) connectView.SetActive(false);
        if (roomView)    roomView.SetActive(true);

        if (ipDisplayText)
        {
            var loc = LocalizationManager.Instance;
            if (isHost && NetworkLobbyManager.Instance != null)
                ipDisplayText.text =
                    (loc?.Get("lobby.yourip") ?? "Your IP:  ") +
                    NetworkLobbyManager.Instance.GetLocalIP();
            else
                ipDisplayText.text = loc?.Get("lobby.connected") ?? "Connected to host";
        }

        // Host-only controls
        if (botsStatusText)   botsStatusText.gameObject.SetActive(isHost);
        if (toggleBotsButton) toggleBotsButton.gameObject.SetActive(isHost);
        if (startButton)      startButton.gameObject.SetActive(isHost);

        // Client-only status
        if (clientWaitText)   clientWaitText.gameObject.SetActive(!isHost);
    }

    // ── Update display ────────────────────────────────────────────────────

    public void Refresh(int playerCount, bool botsOn, bool isHost)
    {
        if (roomView != null && !roomView.activeSelf)
            ShowRoomView(isHost);

        if (playerCountText)
            playerCountText.text = string.Format(
                LocalizationManager.Instance?.Get("lobby.players.fmt") ?? "Players: {0} / 8",
                playerCount);

        if (botsStatusText)
        {
            botsStatusText.gameObject.SetActive(isHost);
            if (isHost)
                botsStatusText.text = LocalizationManager.Instance?.Get(
                    botsOn ? "lobby.bots.on" : "lobby.bots.off") ??
                    (botsOn ? "Bots: ON" : "Bots: OFF");
        }

        if (toggleBotsButton) toggleBotsButton.gameObject.SetActive(isHost);
        if (clientWaitText)   clientWaitText.gameObject.SetActive(!isHost);
        if (startButton)      startButton.gameObject.SetActive(isHost);

        RefreshPlayerList();
    }

    // ── Server browser ────────────────────────────────────────────────────

    public void RefreshServerList()
    {
        if (serverListContainer == null) return;

        foreach (Transform child in serverListContainer)
            Destroy(child.gameObject);

        var servers = LanDiscovery.Instance?.GetServers();
        bool hasServers = servers != null && servers.Count > 0;

        if (serverSearchText)
            serverSearchText.gameObject.SetActive(!hasServers);

        if (!hasServers) return;

        for (int i = 0; i < servers.Count; i++)
            AddServerRow(servers[i], i);
    }

    void AddServerRow(LanDiscovery.ServerInfo info, int index)
    {
        var row = new GameObject("SvrRow_" + index);
        row.transform.SetParent(serverListContainer, false);

        var rt = row.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 1);
        rt.anchorMax        = new Vector2(1, 1);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.offsetMin        = rt.offsetMax = Vector2.zero;
        rt.sizeDelta        = new Vector2(0, 52);
        rt.anchoredPosition = new Vector2(0, -index * 56f);

        row.AddComponent<Image>().color = new Color(1, 1, 1, index % 2 == 0 ? 0.07f : 0.02f);

        // IP text
        var ipGO  = new GameObject("IP");
        ipGO.transform.SetParent(row.transform, false);
        var ipTxt = ipGO.AddComponent<Text>();
        ipTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ipTxt.text      = info.ip;
        ipTxt.fontSize  = 22;
        ipTxt.fontStyle = FontStyle.Bold;
        ipTxt.color     = new Color(0.92f, 0.97f, 1f);
        var ipRt = ipGO.GetComponent<RectTransform>();
        ipRt.anchorMin = new Vector2(0, 0);
        ipRt.anchorMax = new Vector2(1, 1);
        ipRt.offsetMin = new Vector2(14, 0);
        ipRt.offsetMax = new Vector2(-150, 0);

        // Player count
        var cntGO  = new GameObject("Cnt");
        cntGO.transform.SetParent(row.transform, false);
        var cntTxt = cntGO.AddComponent<Text>();
        cntTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cntTxt.text      = $"{info.playerCount}/{info.maxPlayers}";
        cntTxt.fontSize  = 18;
        cntTxt.color     = new Color(0.42f, 0.58f, 0.80f);
        cntTxt.alignment = TextAnchor.MiddleCenter;
        var cntRt = cntGO.GetComponent<RectTransform>();
        cntRt.anchorMin        = new Vector2(1, 0);
        cntRt.anchorMax        = new Vector2(1, 1);
        cntRt.pivot            = new Vector2(1, 0.5f);
        cntRt.sizeDelta        = new Vector2(60, 0);
        cntRt.anchoredPosition = new Vector2(-82, 0);

        // JOIN button
        var btnGO  = new GameObject("JoinBtn");
        btnGO.transform.SetParent(row.transform, false);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.20f, 0.58f, 1.00f, 0.90f);
        var btnRt = btnGO.GetComponent<RectTransform>();
        btnRt.anchorMin        = new Vector2(1, 0.5f);
        btnRt.anchorMax        = new Vector2(1, 0.5f);
        btnRt.pivot            = new Vector2(1, 0.5f);
        btnRt.sizeDelta        = new Vector2(72, 36);
        btnRt.anchoredPosition = new Vector2(-6, 0);

        var lblGO  = new GameObject("Lbl");
        lblGO.transform.SetParent(btnGO.transform, false);
        var lblTxt = lblGO.AddComponent<Text>();
        lblTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lblTxt.text      = "JOIN";
        lblTxt.fontSize  = 15;
        lblTxt.fontStyle = FontStyle.Bold;
        lblTxt.color     = Color.white;
        lblTxt.alignment = TextAnchor.MiddleCenter;
        var lblRt = lblGO.GetComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero;
        lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;

        var btn = btnGO.AddComponent<Button>();
        string capturedIp = info.ip;
        btn.onClick.AddListener(() => OnJoinServerClicked(capturedIp));
    }

    void OnJoinServerClicked(string ip)
    {
        CommitNicknameInput();
        LanDiscovery.Instance?.StopListening();
        NetworkLobbyManager.Instance?.JoinGame(ip);
        ShowRoomView(false);
    }

    // ── Player list ───────────────────────────────────────────────────────

    public void RefreshPlayerList()
    {
        if (playerListContainer == null) return;
        var mgr = NetworkLobbyManager.Instance;
        if (mgr == null) return;

        // Remove stale rows
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        var list = mgr.GetPlayers();
        for (int i = 0; i < list.Count; i++)
            AddPlayerRow(list[i], i);
    }

    void AddPlayerRow(LobbyPlayerData data, int index)
    {
        var row = new GameObject("Row_" + index);
        row.transform.SetParent(playerListContainer, false);

        // Row rect — stacks downward from container top
        var rt = row.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 1);
        rt.anchorMax        = new Vector2(1, 1);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.offsetMin        = rt.offsetMax = Vector2.zero;
        rt.sizeDelta        = new Vector2(0, 44);
        rt.anchoredPosition = new Vector2(0, -index * 48f);

        // Alternating row background
        var bg = row.AddComponent<Image>();
        bg.color = new Color(1, 1, 1, index % 2 == 0 ? 0.05f : 0.02f);

        Color col = PlayerColors[data.colorIndex % PlayerColors.Length];

        // Left color bar
        var bar = new GameObject("Bar");
        bar.transform.SetParent(row.transform, false);
        bar.AddComponent<Image>().color = col;
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0, 0);
        barRt.anchorMax = new Vector2(0, 1);
        barRt.pivot     = new Vector2(0, 0.5f);
        barRt.sizeDelta        = new Vector2(6, 0);
        barRt.anchoredPosition = Vector2.zero;

        // Nickname text
        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(row.transform, false);
        var nameTxt = nameGO.AddComponent<Text>();
        nameTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameTxt.fontSize  = 24;
        nameTxt.fontStyle = FontStyle.Bold;
        nameTxt.color     = col;
        nameTxt.text      = data.nickname.ToString();
        var nameRt = nameGO.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 0);
        nameRt.anchorMax = new Vector2(1, 1);
        nameRt.offsetMin = new Vector2(16, 0);
        nameRt.offsetMax = new Vector2(-90, 0);

        // Tag: HOST (gold) or YOU (green)
        bool isLocal = NetworkManager.Singleton != null &&
                       data.clientId == NetworkManager.Singleton.LocalClientId;
        string tag   = data.isHost ? "HOST" : (isLocal ? "YOU" : "");

        if (!string.IsNullOrEmpty(tag))
        {
            var tagGO = new GameObject("Tag");
            tagGO.transform.SetParent(row.transform, false);
            var tagTxt = tagGO.AddComponent<Text>();
            tagTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tagTxt.fontSize  = 18;
            tagTxt.fontStyle = FontStyle.Bold;
            tagTxt.color     = data.isHost
                ? new Color(1f, 0.82f, 0.2f)   // gold  — HOST
                : new Color(0.4f, 1f, 0.5f);    // green — YOU
            tagTxt.text      = tag;
            tagTxt.alignment = TextAnchor.MiddleRight;
            var tagRt = tagGO.GetComponent<RectTransform>();
            tagRt.anchorMin        = new Vector2(1, 0);
            tagRt.anchorMax        = new Vector2(1, 1);
            tagRt.pivot            = new Vector2(1, 0.5f);
            tagRt.sizeDelta        = new Vector2(80, 0);
            tagRt.anchoredPosition = new Vector2(-10, 0);
        }
    }

    // ── Button callbacks ──────────────────────────────────────────────────

    public void OnHostClicked()
    {
        CommitNicknameInput();
        LanDiscovery.Instance?.StopListening();
        NetworkLobbyManager.Instance?.Host();
        ShowRoomView(true);
    }

    public void OnJoinClicked()
    {
        CommitNicknameInput();
        LanDiscovery.Instance?.StopListening();
        string ip = ipInput != null ? ipInput.text : "127.0.0.1";
        NetworkLobbyManager.Instance?.JoinGame(ip);
        ShowRoomView(false);
    }

    public void OnStartClicked()      => NetworkLobbyManager.Instance?.StartGame();
    public void OnToggleBotsClicked() => NetworkLobbyManager.Instance?.ToggleBots();
    public void OnBackClicked()       => NetworkLobbyManager.Instance?.Leave();
}
