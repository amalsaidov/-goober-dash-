using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// UDP-broadcast LAN server discovery — Minecraft / CS 1.6 style.
/// Host broadcasts presence on port 7778 every 2 s.
/// Clients listen and populate a server list automatically.
/// </summary>
public class LanDiscovery : MonoBehaviour
{
    public static LanDiscovery Instance;

    const int    DISCOVERY_PORT = 7778;
    const string GAME_TOKEN     = "GOOBERDASH_V7";
    const float  BROADCAST_INTV = 2f;
    const float  SERVER_TIMEOUT = 6f;   // 3 missed broadcasts → remove

    public struct ServerInfo
    {
        public string ip;
        public int    playerCount;
        public int    maxPlayers;
        public float  lastSeen;
    }

    readonly List<ServerInfo>          _servers  = new();
    readonly ConcurrentQueue<Action>   _mainQ    = new();

    UdpClient _listener;
    Thread    _listenThread;
    bool      _listening;

    bool  _broadcasting;
    float _broadcastTimer;
    int   _broadcastPlayers    = 1;
    int   _broadcastMaxPlayers = 8;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Update()
    {
        // Drain callbacks queued from the listen thread
        while (_mainQ.TryDequeue(out var act)) act();

        // Periodic broadcast
        if (_broadcasting)
        {
            _broadcastTimer -= Time.deltaTime;
            if (_broadcastTimer <= 0f)
            {
                _broadcastTimer = BROADCAST_INTV;
                SendBroadcast();
            }
        }

        // Expire stale servers
        bool changed = false;
        for (int i = _servers.Count - 1; i >= 0; i--)
        {
            if (Time.time - _servers[i].lastSeen > SERVER_TIMEOUT)
            { _servers.RemoveAt(i); changed = true; }
        }
        if (changed) NotifyUI();
    }

    void OnDestroy() => StopAll();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Start broadcasting host presence (call from NetworkLobbyManager.Host).</summary>
    public void StartBroadcast(int playerCount, int maxPlayers = 8)
    {
        _broadcastPlayers    = playerCount;
        _broadcastMaxPlayers = maxPlayers;
        _broadcasting        = true;
        _broadcastTimer      = 0f;   // send immediately on first Update
    }

    /// <summary>Update player count in ongoing broadcast.</summary>
    public void UpdateBroadcast(int playerCount)
    {
        _broadcastPlayers = playerCount;
    }

    public void StopBroadcast() => _broadcasting = false;

    /// <summary>Start listening for host broadcasts (call when ConnectView opens).</summary>
    public void StartListening()
    {
        if (_listening) return;
        _servers.Clear();
        NotifyUI();
        _listening    = true;
        _listenThread = new Thread(ListenLoop) { IsBackground = true };
        _listenThread.Start();
    }

    public void StopListening()
    {
        _listening = false;
        try { _listener?.Close(); } catch { }
        _listener = null;
    }

    public void StopAll()
    {
        StopBroadcast();
        StopListening();
    }

    public List<ServerInfo> GetServers() => _servers;

    // ── Broadcast ─────────────────────────────────────────────────────────────

    void SendBroadcast()
    {
        try
        {
            string local = GetLocalIP();
            string msg   = $"{GAME_TOKEN}|{local}|7777|{_broadcastPlayers}|{_broadcastMaxPlayers}";
            byte[] data  = Encoding.UTF8.GetBytes(msg);
            using var client = new UdpClient();
            client.EnableBroadcast = true;
            client.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DISCOVERY_PORT));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LanDiscovery] Broadcast error: " + e.Message);
        }
    }

    // ── Listen thread ─────────────────────────────────────────────────────────

    void ListenLoop()
    {
        try
        {
            // Create socket manually so we can set SO_REUSEADDR BEFORE binding.
            // The UdpClient(endpoint) constructor binds immediately without the option,
            // causing "Address already in use" when two editors share the same machine.
            _listener = new UdpClient();
            _listener.ExclusiveAddressUse = false;
            _listener.Client.SetSocketOption(
                SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Client.Bind(new IPEndPoint(IPAddress.Any, DISCOVERY_PORT));
            _listener.Client.ReceiveTimeout = 1000;

            while (_listening)
            {
                try
                {
                    var    ep   = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _listener.Receive(ref ep);
                    string msg  = Encoding.UTF8.GetString(data);
                    var    parts = msg.Split('|');

                    if (parts.Length >= 5 && parts[0] == GAME_TOKEN)
                    {
                        string ip      = parts[1];
                        int    players = int.Parse(parts[3]);
                        int    max     = int.Parse(parts[4]);
                        _mainQ.Enqueue(() => UpdateServer(ip, players, max));
                    }
                }
                catch (SocketException) { }   // receive timeout — normal
            }
        }
        catch (Exception e)
        {
            if (_listening) Debug.LogWarning("[LanDiscovery] Listen error: " + e.Message);
        }
        finally
        {
            try { _listener?.Close(); } catch { }
            _listener = null;
        }
    }

    // Called on main thread via _mainQ
    void UpdateServer(string ip, int players, int max)
    {
        for (int i = 0; i < _servers.Count; i++)
        {
            if (_servers[i].ip == ip)
            {
                var s = _servers[i];
                s.playerCount = players;
                s.maxPlayers  = max;
                s.lastSeen    = Time.time;
                _servers[i]   = s;
                NotifyUI();
                return;
            }
        }
        _servers.Add(new ServerInfo
        {
            ip          = ip,
            playerCount = players,
            maxPlayers  = max,
            lastSeen    = Time.time
        });
        NotifyUI();
    }

    void NotifyUI()
    {
        LobbyPanelController.Instance?.RefreshServerList();
        UIManager.Instance?.RefreshLobbyServers();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    string GetLocalIP()
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            s.Connect("8.8.8.8", 65530);
            return ((IPEndPoint)s.LocalEndPoint).Address.ToString();
        }
        catch { return "127.0.0.1"; }
    }
}
