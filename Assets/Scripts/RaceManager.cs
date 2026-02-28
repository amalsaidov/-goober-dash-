using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    public float roundDuration    = 60f;
    public int   eliminatePerRound = 2;

    private List<RacePlayer> players  = new List<RacePlayer>();
    private List<RacePlayer> finished = new List<RacePlayer>();
    private bool  racing = false;
    public  bool  IsRacing     => racing;
    public  int   CurrentRound => round;
    public  float TimeRemaining => timer;
    public  IReadOnlyList<RacePlayer> GetActivePlayers() => players;
    private float timer;
    private int   round = 1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        Invoke(nameof(FindPlayers), 0.1f);
    }

    void FindPlayers()
    {
        players.Clear();
        foreach (var p in FindObjectsByType<RacePlayer>(FindObjectsSortMode.None))
            players.Add(p);

#if UNITY_EDITOR
        // ParrelSync clone: skip splash + main menu, go straight to multiplayer lobby.
        // The user only needs to click JOIN — no navigation required.
        if (ParrelSync.ClonesManager.IsClone())
        {
            StartCoroutine(AutoLobbyInClone());
            return;
        }
#endif

        // Show main menu — race starts only after the player clicks PLAY
        UIManager.Instance?.ShowMainMenu();
    }

#if UNITY_EDITOR
    IEnumerator AutoLobbyInClone()
    {
        // Let singletons and LAN discovery initialize
        yield return new WaitForSeconds(0.8f);

        // Dismiss splash — no tap needed in clone
        var splash = Object.FindAnyObjectByType<SplashController>();
        if (splash != null) splash.gameObject.SetActive(false);

        // Open lobby directly — clone never needs to visit main menu
        UIManager.Instance?.HideMainMenu();
        UIManager.Instance?.ShowLobbyPanel();
        Debug.Log("[CLONE] Lobby opened. Scanning for host...");

        // Poll every 0.5 s for up to 12 s, then auto-join the first server found
        float timeout = 12f;
        while (timeout > 0f)
        {
            yield return new WaitForSeconds(0.5f);
            timeout -= 0.5f;

            var servers = LanDiscovery.Instance?.GetServers();
            if (servers == null || servers.Count == 0) continue;

            string ip = servers[0].ip;
            Debug.Log($"[CLONE] Auto-joining host at {ip}");
            LanDiscovery.Instance?.StopListening();
            NetworkLobbyManager.Instance?.JoinGame(ip);
            LobbyPanelController.Instance?.ShowRoomView(false);
            UIManager.Instance?.ShowLobbyRoomView(false);
            yield break;
        }
        Debug.LogWarning("[CLONE] Auto-join timed out — no host found. In main editor: MULTIPLAYER → HOST, then re-enter Play Mode in clone.");
    }
#endif

    // ── Called by MainMenuManager when PLAY is pressed ─────────────────────
    public void OnPlayPressed()
    {
        UIManager.Instance?.ShowDifficultyPanel();
    }

    // ── Called by DifficultyManager after difficulty is selected ───────────
    public void BeginRace()
    {
        StartCoroutine(Countdown());
    }

    IEnumerator Countdown()
    {
        UIManager.Instance?.ShowRoundText(
            (LocalizationManager.Instance?.Get("hud.round") ?? "Round ") + round);
        UIManager.Instance?.UpdatePlayerCount(players.Count);

        foreach (var p in players) p.ResetForRound();

        yield return new WaitForSeconds(0.5f);

        for (int i = 3; i >= 1; i--)
        {
            UIManager.Instance?.ShowCountdown(i.ToString());
            yield return new WaitForSeconds(1f);
        }
        UIManager.Instance?.ShowCountdown("GO!");
        yield return new WaitForSeconds(0.6f);
        UIManager.Instance?.HideCountdown();

        StartRace();
    }

    void StartRace()
    {
        racing = true;
        timer  = roundDuration;
        finished.Clear();
        foreach (var p in players) p.OnRaceStart();
    }

    void Update()
    {
        if (!racing) return;

        timer -= Time.deltaTime;
        UIManager.Instance?.UpdateTimer(timer);

        // Position tracker
        RacePlayer human = players.Find(p => p.isHuman);
        if (human != null)
        {
            int pos = 1;
            foreach (var p in players)
                if (!p.isHuman && p.transform.position.x > human.transform.position.x) pos++;
            UIManager.Instance?.UpdatePosition(pos, players.Count);
        }

        if (timer <= 0) StartCoroutine(EndRound());
    }

    public void PlayerFinished(RacePlayer player)
    {
        if (!racing) return;
        finished.Add(player);

        if (player.isHuman)
            UIManager.Instance?.ShowMessage(
                string.Format(
                    LocalizationManager.Instance?.Get("msg.finished.fmt") ?? "You finished #{0}!",
                    finished.Count),
                Color.yellow);

        int toAdvance = Mathf.Max(1, players.Count - eliminatePerRound);
        if (finished.Count >= toAdvance)
            StartCoroutine(EndRound());
    }

    IEnumerator EndRound()
    {
        if (!racing) yield break;
        racing = false;

        yield return new WaitForSeconds(1f);

        // Eliminate players who didn't finish
        List<RacePlayer> didntFinish = new List<RacePlayer>();
        foreach (var p in players)
            if (!p.hasFinished) didntFinish.Add(p);

        // Also eliminate last finishers if needed
        int toKeep = Mathf.Max(1, players.Count - eliminatePerRound);
        while (finished.Count > toKeep)
        {
            var last = finished[finished.Count - 1];
            finished.RemoveAt(finished.Count - 1);
            didntFinish.Add(last);
        }

        foreach (var p in didntFinish)
        {
            players.Remove(p);
            p.Eliminate();
        }

        UIManager.Instance?.UpdatePlayerCount(players.Count);

        // ── Game over ────────────────────────────────────────────────────────
        if (players.Count <= 1)
        {
            bool playerWon = players.Count == 1 && players[0].isHuman;
            var  loc       = LocalizationManager.Instance;
            string title   = playerWon ? (loc?.Get("msg.youwin")   ?? "YOU WIN!")
                                       : (loc?.Get("msg.gameover") ?? "GAME OVER");
            string sub     = playerWon ? (loc?.Get("msg.wintext")  ?? "You beat everyone!")
                                       : (loc?.Get("msg.losetext") ?? "Better luck next time...");
            Color  col     = playerWon ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.28f, 0.28f);

            yield return new WaitForSeconds(1.5f);
            UIManager.Instance?.ShowEndScreen(title, sub, col);
            yield break;
        }

        // ── Next round ───────────────────────────────────────────────────────
        round++;
        yield return new WaitForSeconds(2f);
        UIManager.Instance?.HideMessage();

        players.Clear();
        foreach (var p in FindObjectsByType<RacePlayer>(FindObjectsSortMode.None))
            players.Add(p);

        StartCoroutine(Countdown());
    }

    // ── Play Again ───────────────────────────────────────────────────────────
    public void PlayAgain()
    {
        UIManager.Instance?.HideEndScreen();
        ReviveAllPlayers();
        round = 1;
        UIManager.Instance?.ShowDifficultyPanel();
    }

    // ── Return to Main Menu ──────────────────────────────────────────────────
    public void ReturnToMainMenu()
    {
        UIManager.Instance?.HideEndScreen();
        ReviveAllPlayers();
        round = 1;
        UIManager.Instance?.ShowMainMenu();
    }

    void ReviveAllPlayers()
    {
        racing = false;
        StopAllCoroutines();
        players.Clear();
        finished.Clear();

        // Find ALL players including those that were disabled (eliminated)
        foreach (var p in Object.FindObjectsByType<RacePlayer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            p.Revive();
            players.Add(p);
        }

        UIManager.Instance?.UpdatePlayerCount(players.Count);
        UIManager.Instance?.HideCountdown();
        UIManager.Instance?.HideMessage();
    }
}
