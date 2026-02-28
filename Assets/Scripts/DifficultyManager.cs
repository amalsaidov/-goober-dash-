using UnityEngine;
using Unity.Netcode;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    public enum Diff { Easy, Normal, Hard, Ultra }

    public struct Preset
    {
        // Movement — similar across all difficulties (not the main differentiator)
        public float speedMin, speedMax;
        public float reactMin, reactMax;
        public float dashCDMin, dashCDMax;

        // ── INTELLIGENCE — this is what separates the difficulties ────────
        // Chance to miss/botch a jump (0 = perfect, 0.4 = falls off often)
        public float mistakeChance;
        // Seconds until bot detects it's stuck (0 = never recovers)
        public float stuckRecoveryTime;
        // Ultra only: uses the wall-jump shaft shortcut in Zone 5
        public bool useWallJump;
        // How far ahead to see obstacles (bigger = smarter path reading)
        public float lookahead;

        // Rubber-band multipliers
        public float catchUp1, catchUp2, leadBoost;
        public float jumpMult;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ALL bots move at IDENTICAL speed (7.0 — same as the player).
    //  NO rubber-banding, NO speed multipliers, NO catch-up boosts.
    //  The ONLY difference is HOW WELL they play:
    //
    //  EASY   — 40% chance to botch a jump, never recovers when stuck,
    //            barely sees gaps ahead, reacts slowly, rarely dashes
    //  NORMAL — 8%  chance → occasional fumble, fair fight
    //  HARD   — 2%  chance → rare mistake, reacts fast, reads ahead early
    //  ULTRA  — 0%  chance → PERFECT play: never falls, wall-jumps shaft,
    //                         reads gaps 2× earlier, near-instant reactions
    // ═══════════════════════════════════════════════════════════════════
    public static readonly Preset[] Presets =
    {
        // 0 ── EASY  (same speed as player — just plays poorly)
        new Preset {
            speedMin = 7.0f,  speedMax = 7.0f,   // identical to player
            reactMin = 0.35f, reactMax = 0.60f,  // slow reactions
            dashCDMin = 18f,  dashCDMax = 30f,   // rarely dashes

            mistakeChance     = 0.40f,  // botches 2 in 5 jumps
            stuckRecoveryTime = 0f,     // stays stuck forever
            useWallJump       = false,
            lookahead         = 0.70f,  // barely sees gaps before falling in

            catchUp1 = 1.00f, catchUp2 = 1.00f, leadBoost = 1.00f,
            jumpMult  = 1.00f,
        },
        // 1 ── NORMAL
        new Preset {
            speedMin = 7.0f,  speedMax = 7.0f,
            reactMin = 0.09f, reactMax = 0.18f,
            dashCDMin = 6f,   dashCDMax = 10f,

            mistakeChance     = 0.08f,
            stuckRecoveryTime = 1.5f,
            useWallJump       = false,
            lookahead         = 1.1f,

            catchUp1 = 1.00f, catchUp2 = 1.00f, leadBoost = 1.00f,
            jumpMult  = 1.00f,
        },
        // 2 ── HARD
        new Preset {
            speedMin = 7.0f,  speedMax = 7.0f,
            reactMin = 0.02f, reactMax = 0.06f,
            dashCDMin = 3f,   dashCDMax = 6f,

            mistakeChance     = 0.02f,
            stuckRecoveryTime = 0.7f,
            useWallJump       = true,   // experienced — knows the shaft shortcut
            lookahead         = 1.5f,

            catchUp1 = 1.00f, catchUp2 = 1.00f, leadBoost = 1.00f,
            jumpMult  = 1.00f,
        },
        // 3 ── ULTRA ☠  (same speed — wins through PERFECT play only)
        new Preset {
            speedMin = 7.0f,  speedMax = 7.0f,
            reactMin = 0.00f, reactMax = 0.005f,
            dashCDMin = 2.5f, dashCDMax = 5f,

            mistakeChance     = 0.00f,
            stuckRecoveryTime = 0.30f,
            useWallJump       = true,
            lookahead         = 2.00f,

            catchUp1 = 1.00f, catchUp2 = 1.00f, leadBoost = 1.00f,
            jumpMult  = 1.00f,
        },
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Select(Diff d)
    {
        // In multiplayer the host broadcasts difficulty + race start to all clients via RPC
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsHost &&
            NetworkLobbyManager.Instance != null && NetworkLobbyManager.Instance.IsSpawned)
        {
            NetworkLobbyManager.Instance.StartMultiplayerRace((int)d);
            return;
        }

        // Single-player / offline fallback
        var p = Presets[(int)d];
        foreach (var ai in FindObjectsByType<AIPlayer>(FindObjectsSortMode.None))
            ai.ApplyPreset(p);
        UIManager.Instance?.HideDifficultyPanel();
        RaceManager.Instance?.BeginRace();
    }
}
