using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.InputSystem;

/// <summary>
/// Debug HUD — top-left corner, semi-transparent.
/// Toggle: BackQuote (`) key on desktop; persisted between sessions.
/// Shows maximum diagnostic data: perf, network, lobby players,
/// connected client IPs/pings, ALL NetworkObject hashes, LAN servers, build info.
/// </summary>
[DefaultExecutionOrder(-100)]
public class DebugOverlay : MonoBehaviour
{
    // ── Config ──────────────────────────────────────────────────────────────
    [System.Serializable]
    public class Cfg
    {
        [Header("▶ Master")]
        public bool    masterEnable = true;
        public KeyCode toggleKey    = KeyCode.BackQuote; // ` / ~
        [Range(0.05f, 1f)]
        public float   refreshHz = 0.12f;   // seconds between text rebuilds
        [Range(0f, 1f)]
        public float   panelAlpha = 0.62f;  // background transparency
        [Range(0.3f, 1f)]
        public float   textAlpha  = 0.82f;  // text transparency

        [Header("▶ Performance")]
        public bool fps       = true;
        public bool frameMs   = true;
        public bool fpsMinMax = true;
        public bool fpsTarget = true;
        public bool heapMB    = true;
        public bool gcCount   = false;

        [Header("▶ Network")]
        public bool netRole      = true;
        public bool localIp      = true;
        public bool clientId     = true;
        public bool ping         = true;
        public bool playersCount = true;
        public bool netObjCount  = true;
        public bool serverTime   = true;
        public bool transport    = true;

        [Header("▶ Lobby (multiplayer diagnostics)")]
        public bool lobbySection  = true;   // entire lobby block
        public bool lobbyPlayers  = true;   // list all lobby players
        public bool connClients   = true;   // list connected client IDs + pings (host)
        public bool lanServers    = true;   // LAN discovery server list
        public bool allHashes     = true;   // ALL NetworkObject GlobalObjectIdHashes

        [Header("▶ Game")]
        public bool isRacing  = true;
        public bool roundNum  = true;
        public bool gameTimer = true;
        public bool rank      = true;
        public bool score     = true;
        public bool timeScale = false;

        [Header("▶ Player")]
        public bool playerPos    = true;
        public bool playerVel    = true;
        public bool playerSpeed  = true;
        public bool grounded     = true;
        public bool canControl   = true;
        public bool playerState  = true;
        public bool playerColor  = true;

        [Header("▶ Build & Hashes")]
        public bool appVer      = true;
        public bool buildGuid   = true;
        public bool buildType   = true;
        public bool unityVer    = true;
        public bool platform    = true;
        public bool netHashes   = false;   // legacy — now covered by allHashes

        [Header("▶ System")]
        public bool device     = true;
        public bool osVer      = false;
        public bool resolution = true;
        public bool dpi        = false;
        public bool battery    = true;
        public bool uptime     = true;
        public bool sceneName  = true;
    }

    public Cfg config = new Cfg();

    // ── UI ──────────────────────────────────────────────────────────────────
    Text          _text;
    Image         _bgImage;
    CanvasGroup   _canvasGroup;

    // ── FPS ─────────────────────────────────────────────────────────────────
    float _fps;
    float _fpsMin = float.MaxValue, _fpsMax;
    float _fpsAccum; int _fpsCnt; float _fpsWin;
    const float FPS_WINDOW = 3f;

    // ── Throttle ─────────────────────────────────────────────────────────────
    float _rebuildTimer;

    // ── Cached local player ──────────────────────────────────────────────────
    PlayerController _ctrl;
    Rigidbody2D      _rb;
    RacePlayer       _rp;
    float            _playerCd;

    // ── GC baseline ──────────────────────────────────────────────────────────
    int _gcBase;

    // ── String builder ───────────────────────────────────────────────────────
    readonly StringBuilder _sb = new StringBuilder(4096);

    // ═══════════════════════════════════════════════════════════════════════

    void Awake()
    {
        _gcBase = System.GC.CollectionCount(0);
        BuildUI();
        // Always show on startup if masterEnable — GameSettings.Apply() may override later
        PlayerPrefs.SetInt("dbg_vis", 1);
        SetVisible(config.masterEnable);
    }

    void Update()
    {
        // Toggle
        if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
        {
            bool v = !(_text != null && _text.gameObject.activeSelf);
            SetVisible(v);
            PlayerPrefs.SetInt("dbg_vis", v ? 1 : 0);
        }

        if (!config.masterEnable) return;
        if (_text == null || !_text.gameObject.activeSelf) return;

        UpdateFps();
        CachePlayer();

        _rebuildTimer -= Time.unscaledDeltaTime;
        if (_rebuildTimer > 0f) return;
        _rebuildTimer = config.refreshHz;

        _sb.Clear();
        WriteAll();
        _text.text = _sb.ToString();
    }

    void SetVisible(bool v)
    {
        if (_canvasGroup != null) _canvasGroup.alpha = v ? 1f : 0f;
        if (_text != null) _text.gameObject.SetActive(v);
    }

    // Called by GameSettings.Apply() when the Debug Overlay setting changes
    public void SetEnabledBySettings(bool enabled)
    {
        config.masterEnable = enabled;
        if (!enabled)
        {
            SetVisible(false);
        }
        else
        {
            PlayerPrefs.SetInt("dbg_vis", 1);
            SetVisible(true);
        }
    }

    // ── Build UI ─────────────────────────────────────────────────────────────
    void BuildUI()
    {
        // Root canvas
        var cgo = new GameObject("DbgCanvas");
        cgo.transform.SetParent(transform);
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        var cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        // CanvasGroup for overall alpha
        _canvasGroup = cgo.AddComponent<CanvasGroup>();
        _canvasGroup.interactable   = false;
        _canvasGroup.blocksRaycasts = false;

        // Panel — wider to accommodate lobby data
        var panel = new GameObject("DbgPanel");
        panel.transform.SetParent(canvas.transform, false);
        _bgImage       = panel.AddComponent<Image>();
        _bgImage.color = new Color(0.02f, 0.03f, 0.07f, config.panelAlpha);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0, 1);
        prt.anchoredPosition = new Vector2(10, -10);
        prt.sizeDelta        = new Vector2(400, 400); // wider for lobby data

        // Left accent bar
        var bar = new GameObject("Bar");
        bar.transform.SetParent(panel.transform, false);
        bar.AddComponent<Image>().color = new Color(0.22f, 0.52f, 0.95f, 0.75f);
        var brt = bar.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(0, 1);
        brt.pivot = new Vector2(0, 0.5f);
        brt.sizeDelta = new Vector2(3, 0);
        brt.anchoredPosition = Vector2.zero;

        // Text
        var tgo = new GameObject("DbgTxt");
        tgo.transform.SetParent(panel.transform, false);
        _text = tgo.AddComponent<Text>();
        _text.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.fontSize           = 13;
        _text.color              = new Color(1f, 1f, 1f, config.textAlpha);
        _text.supportRichText    = true;
        _text.horizontalOverflow = HorizontalWrapMode.Wrap;
        _text.verticalOverflow   = VerticalWrapMode.Overflow;
        _text.text               = "<b>■ DEBUG</b>\n initializing...";
        var trt = tgo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8, 5);
        trt.offsetMax = new Vector2(-6, -5);

        // Auto-resize panel height
        var le = panel.AddComponent<LayoutElement>();
        le.minHeight = 60f;
        var csf = panel.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // ── FPS ──────────────────────────────────────────────────────────────────
    void UpdateFps()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt < 0.00001f) return;
        _fpsAccum += 1f / dt;
        _fpsCnt++;
        _fpsWin += dt;
        if (_fpsWin >= FPS_WINDOW)
        {
            _fps = _fpsAccum / _fpsCnt;
            if (_fps > 1f)
            {
                if (_fps < _fpsMin) _fpsMin = _fps;
                if (_fps > _fpsMax) _fpsMax = _fps;
            }
            _fpsAccum = 0; _fpsCnt = 0; _fpsWin = 0;
        }
    }

    // ── Cache local player ───────────────────────────────────────────────────
    void CachePlayer()
    {
        _playerCd -= Time.unscaledDeltaTime;
        if (_playerCd > 0f) return;
        _playerCd = 1f;
        _ctrl = null; _rb = null; _rp = null;

        foreach (var rp in Object.FindObjectsByType<RacePlayer>(FindObjectsSortMode.None))
        {
            if (!rp.isHuman) continue;
            var ns = rp.GetComponent<NetworkSync>();
            if (ns != null && !ns.IsLocalController) continue;
            _rp   = rp;
            _ctrl = rp.GetComponent<PlayerController>();
            _rb   = rp.GetComponent<Rigidbody2D>();
            break;
        }
        // Fallback: no network, just take first human
        if (_rp == null)
            foreach (var rp in Object.FindObjectsByType<RacePlayer>(FindObjectsSortMode.None))
                if (rp.isHuman)
                {
                    _rp = rp;
                    _ctrl = rp.GetComponent<PlayerController>();
                    _rb   = rp.GetComponent<Rigidbody2D>();
                    break;
                }
    }

    // ── String helpers ───────────────────────────────────────────────────────
    static string C(string s, string hex) => $"<color={hex}>{s}</color>";
    static string B(string s)             => $"<b>{s}</b>";

    // Section header
    void Sec(string title, string hex)
        => _sb.AppendLine(B(C($"─ {title}", hex)));

    // Key/Value row — label left-padded 10 chars for wider panel
    void Row(string lbl, string val, string valHex = "#C8DCFF")
        => _sb.AppendLine($" {C(lbl.PadRight(10), "#4A6080")} {C(val, valHex)}");

    // ── Write all sections ───────────────────────────────────────────────────
    void WriteAll()
    {
        _sb.AppendLine(B(C("■ DEBUG  [` = toggle]", "#2A3A50")));

        WritePerf();
        WriteNetwork();
        WriteLobby();      // new comprehensive lobby/multiplayer section
        WriteGame();
        WritePlayer();
        WriteBuild();
        WriteSystem();
    }

    // ── PERFORMANCE ──────────────────────────────────────────────────────────
    void WritePerf()
    {
        if (!AnyOf(config.fps, config.frameMs, config.fpsMinMax,
                   config.fpsTarget, config.heapMB, config.gcCount)) return;
        Sec("PERF", "#FF9944");

        if (config.fps)
        {
            string col = _fps >= 55 ? "#44FF88" : _fps >= 30 ? "#FFD744" : "#FF4444";
            Row("FPS", _fps > 0 ? $"{_fps:F0}" : "…", col);
        }
        if (config.frameMs && _fps > 0)
            Row("FRAME", $"{1000f / _fps:F1} ms");
        if (config.fpsMinMax && _fpsMax > 0)
        {
            float mn = _fpsMin < float.MaxValue ? _fpsMin : 0;
            Row("↕ FPS", $"{mn:F0} – {_fpsMax:F0}");
        }
        if (config.fpsTarget)
        {
            int t = Application.targetFrameRate;
            Row("TARGET", t < 0 ? "VSYNC" : t == 0 ? "MAX" : t.ToString());
        }
        if (config.heapMB)
        {
            float mb = System.GC.GetTotalMemory(false) / 1048576f;
            string col = mb < 200 ? "#44FF88" : mb < 400 ? "#FFD744" : "#FF4444";
            Row("HEAP", $"{mb:F1} MB", col);
        }
        if (config.gcCount)
            Row("GC", $"{System.GC.CollectionCount(0) - _gcBase} coll");
    }

    // ── NETWORK ──────────────────────────────────────────────────────────────
    void WriteNetwork()
    {
        if (!AnyOf(config.netRole, config.localIp, config.clientId,
                   config.ping, config.playersCount, config.netObjCount,
                   config.serverTime, config.transport)) return;
        Sec("NETWORK", "#44AAFF");

        var nm = NetworkManager.Singleton;

        if (config.netRole)
        {
            string role, col;
            if      (nm == null)            { role = "OFFLINE"; col = "#555566"; }
            else if (nm.IsHost)             { role = "HOST";    col = "#FFD700"; }
            else if (nm.IsServer)           { role = "SERVER";  col = "#FF8844"; }
            else if (nm.IsConnectedClient)  { role = "CLIENT";  col = "#44CCFF"; }
            else if (nm.IsListening)        { role = "LISTEN";  col = "#FF88FF"; }
            else                            { role = "IDLE";    col = "#888888"; }
            Row("ROLE", role, col);
        }
        if (config.localIp)
            Row("MY IP", NetworkLobbyManager.Instance?.GetLocalIP() ?? "---");
        if (config.clientId)
        {
            if (nm != null && (nm.IsConnectedClient || nm.IsHost))
                Row("MY CID", nm.LocalClientId.ToString());
            else
                Row("MY CID", "---");
        }
        if (config.ping)
        {
            string p = GetPingStr(out string pcol);
            Row("PING", p, pcol);
        }
        if (config.playersCount && nm != null)
        {
            int cnt = nm.IsServer
                ? nm.ConnectedClientsList.Count
                : NetworkLobbyManager.Instance?.GetPlayers().Count ?? -1;
            Row("ONLINE", cnt >= 0 ? cnt.ToString() : "---");
        }
        if (config.netObjCount && nm?.SpawnManager != null)
            Row("NET OBJ", nm.SpawnManager.SpawnedObjects.Count.ToString());
        if (config.serverTime && nm != null && (nm.IsConnectedClient || nm.IsHost))
            Row("SRV TIME", $"{nm.ServerTime.TimeAsFloat:F2}s");
        if (config.transport && nm != null)
        {
            var t = nm.NetworkConfig?.NetworkTransport as UnityTransport;
            if (t != null)
            {
                // Show the endpoint we're connected to / listening on
                var ep = t.ConnectionData;
                Row("XPORT", $"UTP {ep.Address}:{ep.Port}");
            }
            else
                Row("XPORT", "---");
        }
    }

    string GetPingStr(out string col)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || (!nm.IsConnectedClient && !nm.IsHost)) { col = "#555566"; return "---"; }
        if (nm.IsHost)                           { col = "#44FF88"; return "0 ms (host)"; }
        try
        {
            var t = nm.NetworkConfig.NetworkTransport as UnityTransport;
            if (t != null)
            {
                ulong rtt = t.GetCurrentRtt(NetworkManager.ServerClientId);
                col = rtt < 60 ? "#44FF88" : rtt < 150 ? "#FFD744" : "#FF4444";
                return $"{rtt} ms";
            }
        }
        catch { }
        col = "#888888";
        return "---";
    }

    // ── LOBBY (comprehensive multiplayer diagnostics) ─────────────────────────
    void WriteLobby()
    {
        if (!config.lobbySection) return;

        var nm  = NetworkManager.Singleton;
        var mgr = NetworkLobbyManager.Instance;
        var lan = LanDiscovery.Instance;

        // Only show lobby section when network is active or server list has entries
        bool networkActive = nm != null && (nm.IsListening || nm.IsConnectedClient || nm.IsHost);
        var servers = lan?.GetServers();
        bool hasServers = servers != null && servers.Count > 0;
        if (!networkActive && !hasServers) return;

        Sec("LOBBY DIAG", "#FF66FF");

        // Own IP + port
        string myIp = mgr?.GetLocalIP() ?? "127.0.0.1";
        Row("HOST IP", myIp + ":7777");

        // Transport connection data
        if (nm != null)
        {
            var utp = nm.NetworkConfig?.NetworkTransport as UnityTransport;
            if (utp != null)
            {
                var cd = utp.ConnectionData;
                Row("BIND ADDR", $"{cd.Address}:{cd.Port}");
                Row("SRV ADDR", $"{cd.ServerListenAddress}");
            }
        }

        // Connected clients (only host can see all)
        if (config.connClients && nm != null && nm.IsServer)
        {
            var clients = nm.ConnectedClientsList;
            Row("CONN CLT", clients.Count.ToString(), "#FFD700");
            var utp = nm.NetworkConfig?.NetworkTransport as UnityTransport;
            for (int i = 0; i < clients.Count; i++)
            {
                ulong cid = clients[i].ClientId;
                string pingStr = "?ms";
                if (utp != null)
                {
                    try
                    {
                        ulong rtt = utp.GetCurrentRtt(cid);
                        pingStr = rtt + "ms";
                    }
                    catch { }
                }
                Row($" CL[{i}]", $"id={cid}  {pingStr}",
                    cid == nm.LocalClientId ? "#FFD700" : "#44CCFF");
            }
        }

        // Lobby player list — visible on all clients via NetworkList
        if (config.lobbyPlayers && mgr != null)
        {
            var players = mgr.GetPlayers();
            Row("LOBBY PLR", players.Count.ToString(), "#FF66FF");
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                bool isMe = nm != null && p.clientId == nm.LocalClientId;
                string tag = p.isHost ? "H" : (isMe ? "ME" : "");
                string col = p.isHost ? "#FFD700" : (isMe ? "#44FF88" : "#C8DCFF");
                // colorIndex → color name abbreviation
                string[] colorNames = { "BLU","RED","GRN","ORG","PRP","CYN","PNK","YLW" };
                string colorTag = p.colorIndex < colorNames.Length ? colorNames[p.colorIndex] : p.colorIndex.ToString();
                Row($" PL[{i}]",
                    $"{p.nickname}  [{colorTag}] id={p.clientId} {tag}", col);
            }
        }

        // LAN discovery server browser
        if (config.lanServers && lan != null)
        {
            if (servers != null && servers.Count > 0)
            {
                Row("LAN SVR", servers.Count.ToString(), "#44FFAA");
                foreach (var sv in servers)
                {
                    float age = Time.time - sv.lastSeen;
                    string col = age < 3f ? "#44FF88" : "#FFD744";
                    Row($"  SV", $"{sv.ip}  {sv.playerCount}/{sv.maxPlayers}  {age:F1}s", col);
                }
            }
            else if (nm == null || !nm.IsHost)
                Row("LAN SVR", "searching...", "#888888");
        }

        // ALL NetworkObject hashes — critical for diagnosing NGO hash mismatches
        if (config.allHashes && nm?.SpawnManager != null)
        {
            var spawned = nm.SpawnManager.SpawnedObjects;
            int sceneObjs = 0, dynObjs = 0;
            foreach (var kv in spawned)
            {
                if (kv.Value.IsSceneObject.GetValueOrDefault(false)) sceneObjs++;
                else dynObjs++;
            }
            Row("OBJ HASH", $"scene={sceneObjs} dyn={dynObjs}", "#BBAAFF");

            // List scene object hashes (critical for NGO hash mismatch diagnosis)
            int shown = 0;
            foreach (var kv in spawned)
            {
                var no = kv.Value;
                if (!no.IsSceneObject.GetValueOrDefault(false)) continue;
                string name = no.gameObject.name;
                if (name.Length > 8) name = name.Substring(0, 8);
                Row($"  H[{shown}]",
                    $"{name}={no.NetworkObjectId:X8}", "#BBAAFF");
                shown++;
                if (shown >= 12) { Row("  H[…]", "truncated"); break; } // cap at 12
            }
        }
    }

    // ── GAME ─────────────────────────────────────────────────────────────────
    void WriteGame()
    {
        if (!AnyOf(config.isRacing, config.roundNum, config.gameTimer,
                   config.rank, config.score, config.timeScale)) return;
        Sec("GAME", "#44FF99");

        var rm = RaceManager.Instance;

        if (config.isRacing)
        {
            bool r = rm != null && rm.IsRacing;
            Row("RACING", r ? "YES" : "NO", r ? "#44FF88" : "#FF6644");
        }
        if (config.roundNum)
            Row("ROUND", rm != null ? rm.CurrentRound.ToString() : "---");
        if (config.gameTimer)
        {
            if (rm != null && rm.IsRacing)
            {
                float t = rm.TimeRemaining;
                string col = t > 20 ? "#44FF88" : t > 10 ? "#FFD744" : "#FF4444";
                Row("TIMER", $"{t:F1}s", col);
            }
            else
                Row("TIMER", "---");
        }
        if (config.rank)
            Row("RANK", GetRankStr(rm));
        if (config.score)
            Row("SCORE", ScoreManager.Instance?.Score.ToString() ?? "---");
        if (config.timeScale)
        {
            float ts = Time.timeScale;
            Row("TSCALE", ts.ToString("F2"), ts == 1f ? "#44FF88" : "#FFD744");
        }
    }

    string GetRankStr(RaceManager rm)
    {
        if (rm == null || _rp == null) return "---";
        var list = rm.GetActivePlayers();
        if (list == null || list.Count == 0) return "---";
        float myX = _rp.transform.position.x;
        int rank = 1;
        foreach (var p in list)
            if (p != _rp && p.transform.position.x > myX) rank++;
        return $"#{rank} / {list.Count}";
    }

    // ── PLAYER ───────────────────────────────────────────────────────────────
    void WritePlayer()
    {
        if (!AnyOf(config.playerPos, config.playerVel, config.playerSpeed,
                   config.grounded, config.canControl, config.playerState,
                   config.playerColor)) return;
        if (_ctrl == null && _rb == null && _rp == null) return;
        Sec("PLAYER", "#44FFDD");

        if (config.playerPos && _ctrl != null)
        {
            var p = _ctrl.transform.position;
            Row("POS", $"{p.x:F1}  {p.y:F1}");
        }
        if (_rb != null)
        {
            var v = _rb.linearVelocity;
            if (config.playerVel)
                Row("VEL", $"{v.x:F1}  {v.y:F1}");
            if (config.playerSpeed)
            {
                float spd = v.magnitude;
                string col = spd > 16 ? "#FF6644" : spd > 9 ? "#FFD744" : "#44FF88";
                Row("SPEED", $"{spd:F1} m/s", col);
            }
        }
        if (config.grounded && _ctrl != null)
        {
            bool g = _ctrl.IsGrounded;
            Row("GRND", g ? "YES" : "NO", g ? "#44FF88" : "#FFD744");
        }
        if (config.canControl && _ctrl != null)
            Row("CTRL", _ctrl.canControl ? "YES" : "NO",
                _ctrl.canControl ? "#44FF88" : "#FF4444");
        if (config.playerState && _rp != null)
            Row("STATE", _rp.hasFinished ? "FINISHED" : "ACTIVE",
                _rp.hasFinished ? "#FFD744" : "#44FF88");
        if (config.playerColor && _rp != null)
        {
            var sr = _rp.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                Row("COLOR", $"rgb({(int)(c.r*255)} {(int)(c.g*255)} {(int)(c.b*255)})");
            }
        }
    }

    // ── BUILD & HASHES ───────────────────────────────────────────────────────
    void WriteBuild()
    {
        if (!AnyOf(config.appVer, config.buildGuid, config.buildType,
                   config.unityVer, config.platform, config.netHashes)) return;
        Sec("BUILD", "#CC88FF");

        if (config.appVer)
            Row("VERSION", Application.version.Length > 0 ? Application.version : "1.0");
        if (config.buildGuid)
        {
            string g = Application.buildGUID;
            // buildGUID is empty in editor — use a placeholder
            Row("B.GUID", g.Length >= 8 ? g.Substring(0, 8) + "…" : (g.Length > 0 ? g : "[editor]"));
        }
        if (config.buildType)
            Row("TYPE", Debug.isDebugBuild ? "DEV" : "RELEASE",
                Debug.isDebugBuild ? "#FFD744" : "#44FF88");
        if (config.unityVer)
            Row("UNITY", Application.unityVersion);
        if (config.platform)
            Row("PLAT", Application.platform.ToString());

        // Legacy: first 4 scene NetworkObject IDs (kept for backwards compat)
        if (config.netHashes)
        {
            var nm = NetworkManager.Singleton;
            if (nm?.SpawnManager != null)
            {
                int shown = 0;
                foreach (var kv in nm.SpawnManager.SpawnedObjects)
                {
                    if (shown >= 4) break;
                    var no = kv.Value;
                    if (!no.IsSceneObject.GetValueOrDefault(false)) continue;
                    Row($"H[{shown}]", $"{no.NetworkObjectId:X8}", "#BBAAFF");
                    shown++;
                }
            }
        }
    }

    // ── SYSTEM ───────────────────────────────────────────────────────────────
    void WriteSystem()
    {
        if (!AnyOf(config.device, config.osVer, config.resolution,
                   config.dpi, config.battery, config.uptime, config.sceneName)) return;
        Sec("SYSTEM", "#AA88FF");

        if (config.device)
            Row("DEVICE", SystemInfo.deviceModel);
        if (config.osVer)
            Row("OS", SystemInfo.operatingSystem);
        if (config.resolution)
            Row("RES", $"{Screen.width}×{Screen.height}");
        if (config.dpi)
            Row("DPI", $"{Screen.dpi:F0}");
        if (config.battery)
        {
            float b = SystemInfo.batteryLevel;
            string bStr, bCol;
            if (b < 0) { bStr = "N/A"; bCol = "#555566"; }
            else
            {
                int pct = (int)(b * 100);
                bStr = $"{pct}%";
                var s = SystemInfo.batteryStatus;
                if (s == BatteryStatus.Charging) bStr += " ⚡";
                else if (s == BatteryStatus.Full) bStr += " ✓";
                bCol = pct > 30 ? "#44FF88" : "#FF4444";
            }
            Row("BAT", bStr, bCol);
        }
        if (config.uptime)
        {
            int u = (int)Time.realtimeSinceStartup;
            Row("UPTIME", $"{u / 3600:D2}:{(u / 60) % 60:D2}:{u % 60:D2}");
        }
        if (config.sceneName)
            Row("SCENE", SceneManager.GetActiveScene().name);
    }

    // ── Utility ──────────────────────────────────────────────────────────────
    static bool AnyOf(params bool[] vals)
    {
        foreach (bool v in vals) if (v) return true;
        return false;
    }
}
