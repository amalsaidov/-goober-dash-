using System.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Attached to every player and bot.
/// • If this machine is the local controller (SetAsLocalController() called),
///   we run physics locally and broadcast position to everyone else.
/// • If this machine is the server and the object has an AIPlayer,
///   the server broadcasts the bot's position.
/// • Everyone else smoothly interpolates to the received position.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class NetworkSync : NetworkBehaviour
{
    Rigidbody2D rb;
    bool isLocalController;
    public bool IsLocalController => isLocalController;
    Vector2 remotePos, remoteVel;
    bool remoteReady;
    const float INTERP = 14f;

    // Color retry — the lobby NetworkList may not be populated yet when
    // SetAsLocalController() fires, so we keep retrying until it works.
    bool _colorApplied = false;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    public override void OnNetworkSpawn()
    {
        // Host's own "Player" object (no AIPlayer) auto-inits as local controller
        if (IsHost && GetComponent<AIPlayer>() == null)
            SetAsLocalController();
    }

    /// <summary>Call on the machine that will physically control this character.</summary>
    public void SetAsLocalController()
    {
        isLocalController = true;

        // Enable human-player components, disable AI
        var ai = GetComponent<AIPlayer>();
        if (ai) { ai.canMove = false; ai.enabled = false; }

        var pc = GetComponent<PlayerController>();
        if (pc) pc.enabled = true;

        var rp = GetComponent<RacePlayer>();
        if (rp) rp.isHuman = true;

        if (CameraFollow.Instance != null)
            CameraFollow.Instance.target = transform;

        // Start color retry — the NetworkList may not have our entry yet
        // (client registered via SendNicknameServerRpc which fires one frame after spawn).
        _colorApplied = false;
        StartCoroutine(ColorRetry());
    }

    // Keep trying to apply lobby color every 0.4 s for up to 10 s.
    // Stops as soon as the local player's entry appears in the NetworkList.
    IEnumerator ColorRetry()
    {
        float timeout = 10f;
        while (!_colorApplied && timeout > 0f)
        {
            TryApplyLobbyColor();
            if (_colorApplied) yield break;
            yield return new WaitForSeconds(0.4f);
            timeout -= 0.4f;
        }
        if (!_colorApplied)
            Debug.LogWarning("[NetworkSync] Could not apply lobby color — player not found in NetworkList after 10 s.");
    }

    void TryApplyLobbyColor()
    {
        var mgr = NetworkLobbyManager.Instance;
        if (mgr == null) return;
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        ulong localId = nm.LocalClientId;
        var list = mgr.GetPlayers();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].clientId != localId) continue;
            var sr = GetComponent<SpriteRenderer>();
            if (sr)
                sr.color = LobbyPanelController.PlayerColors[list[i].colorIndex % LobbyPanelController.PlayerColors.Length];
            _colorApplied = true;
            Debug.Log($"[NetworkSync] Color applied: index={list[i].colorIndex}  clientId={localId}");
            return;
        }
    }

    // ── Sync loop ─────────────────────────────────────────────────────────

    void FixedUpdate()
    {
        if (!IsSpawned) return;

        bool iAmServer      = IsServer;
        var  botAI          = GetComponent<AIPlayer>();
        bool hasBotAI       = botAI != null && botAI.enabled;  // false once human took over
        bool sendFromServer = iAmServer && hasBotAI && !isLocalController;

        if (isLocalController || sendFromServer)
        {
            if (iAmServer)
                BroadcastPosClientRpc(rb.position, rb.linearVelocity);
            else
                SendPosServerRpc(rb.position, rb.linearVelocity);
        }
        else if (remoteReady)
        {
            rb.position       = Vector2.Lerp(rb.position,       remotePos, Time.fixedDeltaTime * INTERP);
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, remoteVel, Time.fixedDeltaTime * INTERP * 0.4f);
        }
    }

    /// <summary>Set nickname and color on the floating name tag above this player.</summary>
    public void SetNameTag(string nickname, Color col)
    {
        foreach (var pnt in Object.FindObjectsByType<PlayerNameTag>(FindObjectsSortMode.None))
        {
            if (pnt.IsFollowing(transform))
            {
                pnt.SetTag(nickname, col);
                return;
            }
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    void SendPosServerRpc(Vector2 pos, Vector2 vel)
        => BroadcastPosClientRpc(pos, vel);

    [Rpc(SendTo.Everyone)]
    void BroadcastPosClientRpc(Vector2 pos, Vector2 vel)
    {
        if (isLocalController) return;
        remotePos   = pos;
        remoteVel   = vel;
        remoteReady = true;
    }
}
