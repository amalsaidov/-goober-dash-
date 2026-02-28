using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// LAN multiplayer lobby.
/// • Host: press HOST → share your IP → others enter it and press JOIN → HOST clicks START.
/// • Supports up to 7 human players (1 host + 6 clients).
/// • Bots fill remaining slots; can be toggled on/off before starting.
/// • players (NetworkList) keeps per-player nickname + color synced to all clients.
/// </summary>
public class NetworkLobbyManager : NetworkBehaviour
{
    public static NetworkLobbyManager Instance;

    const ushort PORT = 7777;

    // ── Synced state ──────────────────────────────────────────────────────
    readonly NetworkVariable<int>  nvPlayers = new(1,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<bool> nvBots    = new(true,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Synced player list — readable on all clients, writable only by server.
    // Initialized at declaration so NGO's OnDestroy never sees a null list,
    // even when a duplicate instance is destroyed before Awake() sets it.
    readonly NetworkList<LobbyPlayerData> players = new NetworkList<LobbyPlayerData>();

    // Server-side: bot slots available for assignment to clients
    readonly List<GameObject> freeSlots = new();

    // Server-side: clientId → networkObjectId, populated when slot is reserved in OnClientJoined
    readonly Dictionary<ulong, ulong> _pendingSlots = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public override void OnNetworkSpawn()
    {
        players.OnListChanged += OnPlayersChanged;

        // Client: send nickname to server after spawn
        if (!IsServer && IsClient)
            StartCoroutine(RegisterSelfNextFrame());
    }

    public override void OnNetworkDespawn()
    {
        players.OnListChanged -= OnPlayersChanged;
    }

    void OnPlayersChanged(NetworkListEvent<LobbyPlayerData> _)
    {
        LobbyPanelController.Instance?.RefreshPlayerList();
        ApplyAllPlayerColors();
    }

    // Apply lobby colors to all player sprites on this machine.
    // Uses networkObjectId stored in the NetworkList — works on every machine,
    // including late-joining clients who receive the full list on connect.
    void ApplyAllPlayerColors()
    {
        for (int i = 0; i < players.Count; i++)
        {
            ulong noid = players[i].networkObjectId;
            if (noid == 0) continue;
            Color col = LobbyPanelController.PlayerColors[
                players[i].colorIndex % LobbyPanelController.PlayerColors.Length];

            foreach (var ns in Object.FindObjectsByType<NetworkSync>(FindObjectsSortMode.None))
            {
                var no = ns.GetComponent<NetworkObject>();
                if (no != null && no.NetworkObjectId == noid)
                {
                    var sr = ns.GetComponent<SpriteRenderer>();
                    if (sr) sr.color = col;
                    // Also update floating name tag
                    ns.SetNameTag(players[i].nickname.ToString(), col);
                    Debug.Log($"[Lobby] Color {players[i].colorIndex} → obj {noid} (client {players[i].clientId})");
                    break;
                }
            }
        }
    }

    IEnumerator RegisterSelfNextFrame()
    {
        yield return null; // wait for full initialization
        SendNicknameServerRpc(
            NetworkManager.LocalClientId,
            LobbyPanelController.GetLocalNickname(),
            LobbyPanelController.GetLocalColorIndex());
    }

    // Client → Server: register nickname + chosen color
    [Rpc(SendTo.Server, RequireOwnership = false)]
    void SendNicknameServerRpc(ulong clientId, FixedString32Bytes nickname, int colorIndex)
    {
        if (!IsSpawned || NetworkManager.ShutdownInProgress) return;
        // Ignore duplicate registrations
        for (int i = 0; i < players.Count; i++)
            if (players[i].clientId == clientId) return;

        _pendingSlots.TryGetValue(clientId, out ulong noid);
        players.Add(new LobbyPlayerData
        {
            clientId        = clientId,
            nickname        = nickname,
            colorIndex      = colorIndex,   // use client's chosen color from Settings
            isHost          = false,
            networkObjectId = noid
        });
        _pendingSlots.Remove(clientId);
        // OnListChanged fires automatically → RefreshPlayerList() on all clients
    }

    // Called by SceneSetup after bots are created — registers available human slots
    public void RegisterBotSlots(List<GameObject> botObjects)
    {
        freeSlots.Clear();
        freeSlots.AddRange(botObjects);
    }

    // ── Connection ────────────────────────────────────────────────────────

    public void Host()
    {
        var nm = NetworkManager.Singleton;
        if (nm.IsListening || nm.IsConnectedClient)
        {
            Debug.LogWarning("[Lobby] Already connected — call Leave() first.");
            return;
        }

        // ── Re-discover bot slots at runtime ───────────────────────────────
        // freeSlots is NOT serialized, so SceneSetup's RegisterBotSlots call
        // is lost between editor sessions. Always rediscover here.
        freeSlots.Clear();
        _pendingSlots.Clear();
        foreach (var ai in Object.FindObjectsByType<AIPlayer>(FindObjectsSortMode.None))
        {
            if (ai.GetComponent<NetworkObject>() != null)
                freeSlots.Add(ai.gameObject);
        }
        Debug.Log($"[Lobby] Host: found {freeSlots.Count} bot slots.");

        Transport().SetConnectionData("0.0.0.0", PORT);
        // Remove before add to prevent double-registration across sessions
        nm.OnClientConnectedCallback  -= OnClientJoined;
        nm.OnClientDisconnectCallback -= OnClientLeft;
        nm.OnClientConnectedCallback  += OnClientJoined;
        nm.OnClientDisconnectCallback += OnClientLeft;
        nm.StartHost();

        // Start broadcasting so clients can discover this server
        LanDiscovery.Instance?.StartBroadcast(1);

        // Find HOST's own Player NetworkObjectId (the one without AIPlayer)
        ulong hostObjectId = 0;
        foreach (var ns in Object.FindObjectsByType<NetworkSync>(FindObjectsSortMode.None))
        {
            if (ns.GetComponent<AIPlayer>() == null)
            {
                var no = ns.GetComponent<NetworkObject>();
                if (no != null) hostObjectId = no.NetworkObjectId;
                break;
            }
        }

        // Register host as first player — use their chosen color from Settings
        players.Add(new LobbyPlayerData
        {
            clientId        = NetworkManager.Singleton.LocalClientId,
            nickname        = LobbyPanelController.GetLocalNickname(),
            colorIndex      = LobbyPanelController.GetLocalColorIndex(),
            isHost          = true,
            networkObjectId = hostObjectId
        });

        UIManager.Instance?.HideMainMenu();
        UIManager.Instance?.ShowLobbyPanel();
        RefreshUI();
    }

    public void JoinGame(string ip)
    {
        var nm = NetworkManager.Singleton;
        if (nm.IsListening || nm.IsConnectedClient)
        {
            Debug.LogWarning("[Lobby] Already connected — call Leave() first.");
            return;
        }
        if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";
        Transport().SetConnectionData(ip.Trim(), PORT);
        nm.StartClient();

        UIManager.Instance?.HideMainMenu();
        UIManager.Instance?.ShowLobbyPanel();
    }

    public void Leave()
    {
        LanDiscovery.Instance?.StopAll();
        if (IsServer && IsSpawned && !NetworkManager.ShutdownInProgress)
            players.Clear();
        NetworkManager.Singleton.Shutdown();
        Time.timeScale = 1f;
        UIManager.Instance?.HideLobbyPanel();
        UIManager.Instance?.ShowMainMenu();
    }

    // ── Server callbacks ──────────────────────────────────────────────────

    void OnClientJoined(ulong clientId)
    {
        if (!IsServer) return;
        if (!IsSpawned || NetworkManager.ShutdownInProgress) return;

        if (freeSlots.Count > 0)
        {
            var slot = freeSlots[0];
            freeSlots.RemoveAt(0);
            ulong slotId = GetNetworkObjectId(slot);
            _pendingSlots[clientId] = slotId;
            AssignSlotRpc(slotId, clientId);
        }

        nvPlayers.Value = NetworkManager.Singleton.ConnectedClientsList.Count;
        LanDiscovery.Instance?.UpdateBroadcast(nvPlayers.Value);
        SyncUIRpc(nvPlayers.Value, nvBots.Value);
    }

    void OnClientLeft(ulong clientId)
    {
        if (!IsServer) return;
        if (!IsSpawned || NetworkManager.ShutdownInProgress) return;

        // Remove from player list
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].clientId == clientId)
            {
                players.RemoveAt(i);
                break;
            }
        }

        nvPlayers.Value = NetworkManager.Singleton.ConnectedClientsList.Count;
        LanDiscovery.Instance?.UpdateBroadcast(nvPlayers.Value);
        SyncUIRpc(nvPlayers.Value, nvBots.Value);
    }

    // ── Slot assignment ───────────────────────────────────────────────────

    [Rpc(SendTo.Everyone)]
    void AssignSlotRpc(ulong networkObjectId, ulong targetClientId)
    {
        foreach (var ns in Object.FindObjectsByType<NetworkSync>(FindObjectsSortMode.None))
        {
            var no = ns.GetComponent<NetworkObject>();
            if (no == null || no.NetworkObjectId != networkObjectId) continue;

            // Disable AI and mark as human-controlled on ALL machines.
            // This stops the server from broadcasting AI position for this slot,
            // and stops LaunchRpc from deactivating it as a bot.
            var ai = ns.GetComponent<AIPlayer>();
            if (ai) { ai.canMove = false; ai.enabled = false; }

            var rp = ns.GetComponent<RacePlayer>();
            if (rp) rp.isHuman = true;

            // Only the actual target client takes physical control
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                ns.SetAsLocalController();

                var pc = ns.GetComponent<PlayerController>();
                if (pc != null && pc.groundCheck == null)
                {
                    var gc = new GameObject("GroundCheck");
                    gc.transform.SetParent(ns.transform);
                    gc.transform.localPosition = new Vector3(0, -0.55f, 0);
                    pc.groundCheck = gc.transform;
                    pc.groundLayer = LayerMask.GetMask("Ground");
                }
            }
            break;
        }
    }

    // ── Bots toggle ───────────────────────────────────────────────────────

    public void ToggleBots()
    {
        if (IsServer) ApplyBotsToggle();
        else ToggleBotsServerRpc();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    void ToggleBotsServerRpc() => ApplyBotsToggle();

    void ApplyBotsToggle()
    {
        nvBots.Value = !nvBots.Value;
        SyncUIRpc(nvPlayers.Value, nvBots.Value);
    }

    // ── Start game ────────────────────────────────────────────────────────

    public void StartGame()
    {
        if (!IsServer) return;
        LaunchRpc(nvBots.Value);
    }

    [Rpc(SendTo.Everyone)]
    void LaunchRpc(bool withBots)
    {
        UIManager.Instance?.HideLobbyPanel();

        foreach (var ai in Object.FindObjectsByType<AIPlayer>(FindObjectsSortMode.None))
        {
            var rp = ai.GetComponent<RacePlayer>();
            // Never deactivate human-controlled slots — only toggle pure bot slots
            if (rp != null && rp.isHuman) continue;
            ai.gameObject.SetActive(withBots);
        }

        if (NetworkManager.Singleton.IsHost)
            UIManager.Instance?.ShowDifficultyPanel();
    }

    // ── Race start (host → all clients) ──────────────────────────────────

    /// Called by DifficultyManager on the host after difficulty is selected.
    public void StartMultiplayerRace(int diffIndex)
    {
        if (!IsServer) return;
        StartRaceRpc(diffIndex);
    }

    [Rpc(SendTo.Everyone)]
    void StartRaceRpc(int diffIndex)
    {
        var p = DifficultyManager.Presets[diffIndex];
        foreach (var ai in Object.FindObjectsByType<AIPlayer>(FindObjectsSortMode.None))
        {
            if (ai.enabled) ai.ApplyPreset(p);  // skip human-assigned slots
        }
        UIManager.Instance?.HideDifficultyPanel();
        RaceManager.Instance?.BeginRace();
    }

    // ── UI sync ───────────────────────────────────────────────────────────

    [Rpc(SendTo.Everyone)]
    void SyncUIRpc(int count, bool bots)
        => LobbyPanelController.Instance?.Refresh(count, bots, NetworkManager.Singleton.IsHost);

    void RefreshUI()
    {
        int count = IsServer ? NetworkManager.Singleton.ConnectedClientsList.Count : 1;
        LobbyPanelController.Instance?.Refresh(count, nvBots.Value, IsHost);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    public NetworkList<LobbyPlayerData> GetPlayers() => players;

    public string GetLocalIP()
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            s.Connect("8.8.8.8", 65530);
            return ((IPEndPoint)s.LocalEndPoint).Address.ToString();
        }
        catch { return "127.0.0.1"; }
    }

    static ulong GetNetworkObjectId(GameObject go)
    {
        var no = go.GetComponent<NetworkObject>();
        return no != null ? no.NetworkObjectId : 0;
    }

    static UnityTransport Transport()
        => (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
}
