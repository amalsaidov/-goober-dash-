using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages EN / RU localization. Add LocalizedText to any UI Text
/// component to auto-update when the language changes.
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;
    public static event System.Action OnLanguageChanged;

    public enum Lang { English = 0, Russian = 1 }
    public Lang Current { get; private set; } = Lang.English;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        Current = (Lang)PlayerPrefs.GetInt("Language", 0);
    }

    public void SetLanguage(Lang lang)
    {
        if (Current == lang) return;
        Current = lang;
        PlayerPrefs.SetInt("Language", (int)lang);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }

    public string Get(string key)
    {
        if (_t.TryGetValue(key, out var arr))
            return Current == Lang.Russian ? arr[1] : arr[0];
        return key; // fallback: show key
    }

    // Static version used before Instance exists (e.g. SceneSetup)
    public static string Default(string key)
    {
        if (_t.TryGetValue(key, out var arr)) return arr[0];
        return key;
    }

    // ── String table  [English, Russian] ──────────────────────────────────
    static readonly Dictionary<string, string[]> _t = new()
    {
        // ── Main Menu ─────────────────────────────────────────────────────
        { "menu.subtitle",       new[] { "RACE  \u00b7  JUMP  \u00b7  WIN",
                                          "\u0413\u041e\u041d\u041a\u0410  \u00b7  \u041f\u0420\u042b\u0416\u041a\u0418  \u00b7  \u041f\u041e\u0411\u0415\u0414\u0410" }},
        { "menu.play",           new[] { "\u25b6   P L A Y",
                                          "\u25b6   \u0418 \u0413 \u0420 \u0410 \u0422 \u042c" }},
        { "menu.multiplayer",    new[] { "\u25c6   M U L T I P L A Y E R",
                                          "\u25c6   \u041c\u0423\u041b\u042c\u0422\u0418\u041f\u041b\u0415\u0415\u0420" }},
        { "menu.settings",       new[] { "\u2699   S E T T I N G S",
                                          "\u2699   \u041d\u0410\u0421\u0422\u0420\u041e\u0419\u041a\u0418" }},

        // ── Settings ──────────────────────────────────────────────────────
        { "settings.title",      new[] { "SETTINGS",
                                          "\u041d\u0410\u0421\u0422\u0420\u041e\u0419\u041a\u0418" }},
        { "settings.subtitle",   new[] { "Customize your experience",
                                          "\u041d\u0430\u0441\u0442\u0440\u043e\u0439\u0442\u0435 \u0438\u0433\u0440\u0443 \u043f\u043e\u0434 \u0441\u0435\u0431\u044f" }},
        { "settings.display",    new[] { "D I S P L A Y",
                                          "\u042d \u041a \u0420 \u0410 \u041d" }},
        { "settings.gameplay",   new[] { "G A M E P L A Y",
                                          "\u0413 \u0415 \u0419 \u041c \u041f \u041b \u0415 \u0419" }},
        { "settings.experience", new[] { "E X P E R I E N C E",
                                          "\u041e \u041f \u042b \u0422" }},
        { "settings.controls",   new[] { "C O N T R O L S",
                                          "\u0423 \u041f \u0420 \u0410 \u0412 \u041b \u0415 \u041d \u0418 \u0415" }},
        { "settings.language",   new[] { "Language",     "\u042f\u0437\u044b\u043a" }},
        { "settings.quality",    new[] { "Quality",      "\u041a\u0430\u0447\u0435\u0441\u0442\u0432\u043e" }},
        { "settings.roundtime",  new[] { "Round Time",   "\u0412\u0440\u0435\u043c\u044f \u0440\u0430\u0443\u043d\u0434\u0430" }},
        { "settings.elim",       new[] { "Bots Out / Round",
                                          "\u0412\u044b\u0431. \u0431\u043e\u0442\u043e\u0432 / \u0440\u0430\u0443\u043d\u0434" }},
        { "settings.shake",      new[] { "Camera Shake", "\u0422\u0440\u044f\u0441\u043a\u0430 \u043a\u0430\u043c\u0435\u0440\u044b" }},
        { "settings.trails",     new[] { "Player Trails","\u0421\u043b\u0435\u0434\u044b \u0438\u0433\u0440\u043e\u043a\u0430" }},
        { "settings.debug",      new[] { "Debug Overlay","\u0414\u0435\u0431\u0430\u0433 \u0425\u0423\u0414" }},
        { "settings.back",       new[] { "\u2190   B A C K",
                                          "\u2190   \u041d \u0410 \u0417 \u0410 \u0414" }},

        // ── Difficulty ────────────────────────────────────────────────────
        { "diff.select",         new[] { "SELECT DIFFICULTY",
                                          "\u0412\u042b\u0411\u0415\u0420\u0418\u0422\u0415 \u0421\u041b\u041e\u0416\u041d\u041e\u0421\u0422\u042c" }},
        { "diff.easy.d1",        new[] { "Bots play like beginners",
                                          "\u0411\u043e\u0442\u044b \u0438\u0433\u0440\u0430\u044e\u0442 \u043a\u0430\u043a \u043d\u043e\u0432\u0438\u0447\u043a\u0438" }},
        { "diff.easy.d2",        new[] { "Falls off edges, gets stuck",
                                          "\u041f\u0430\u0434\u0430\u044e\u0442 \u0441 \u043f\u043b\u0430\u0442\u0444\u043e\u0440\u043c, \u0437\u0430\u0441\u0442\u0440\u0435\u0432\u0430\u044e\u0442" }},
        { "diff.normal.d1",      new[] { "Balanced competition",
                                          "\u0421\u0431\u0430\u043b\u0430\u043d\u0441\u0438\u0440\u043e\u0432\u0430\u043d\u043d\u0430\u044f \u0438\u0433\u0440\u0430" }},
        { "diff.normal.d2",      new[] { "Occasional fumbles \u2014 fair fight",
                                          "\u0420\u0435\u0434\u043a\u0438\u0435 \u043e\u0448\u0438\u0431\u043a\u0438 \u2014 \u0447\u0435\u0441\u0442\u043d\u0430\u044f \u0431\u043e\u0440\u044c\u0431\u0430" }},
        { "diff.hard.d1",        new[] { "Sharp & experienced play",
                                          "\u041e\u043f\u044b\u0442\u043d\u0430\u044f \u0438 \u0430\u0433\u0440\u0435\u0441\u0441\u0438\u0432\u043d\u0430\u044f \u0438\u0433\u0440\u0430" }},
        { "diff.hard.d2",        new[] { "Rare mistakes, reads gaps early",
                                          "\u0420\u0435\u0434\u043a\u0438\u0435 \u043e\u0448\u0438\u0431\u043a\u0438, \u0447\u0438\u0442\u0430\u044e\u0442 \u0442\u0440\u0430\u0441\u0441\u0443" }},
        { "diff.ultra.d1",       new[] { "PERFECT professional play",
                                          "\u0418\u0414\u0415\u0410\u041b\u042c\u041d\u0410\u042f \u0438\u0433\u0440\u0430" }},
        { "diff.ultra.d2",       new[] { "Never fails, uses every shortcut \u2620",
                                          "\u041d\u0438\u043a\u043e\u0433\u0434\u0430 \u043d\u0435 \u043e\u0448\u0438\u0431\u0430\u0435\u0442\u0441\u044f \u2620" }},

        // ── End Screen ────────────────────────────────────────────────────
        { "end.playagain",       new[] { "\u25b6   PLAY AGAIN",
                                          "\u25b6   \u0415\u0449\u0451 \u0440\u0430\u0437" }},
        { "end.mainmenu",        new[] { "\u2302   MAIN MENU",
                                          "\u2302   \u0413\u041b\u0410\u0412\u041d\u041e\u0415 \u041c\u0415\u041d\u042e" }},

        // ── Pause ─────────────────────────────────────────────────────────
        { "pause.title",         new[] { "PAUSED",         "\u041f\u0410\u0423\u0417\u0410" }},
        { "pause.resume",        new[] { "\u25b6   R E S U M E",
                                          "\u25b6   \u041f\u0420\u041e\u0414\u041e\u041b\u0416\u0418\u0422\u042c" }},
        { "pause.restart",       new[] { "\u21ba   R E S T A R T",
                                          "\u21ba   \u0417\u0410\u041d\u041e\u0412\u041e" }},
        { "pause.mainmenu",      new[] { "\u2302   M A I N   M E N U",
                                          "\u2302   \u0413\u041b\u0410\u0412\u041d\u041e\u0415 \u041c\u0415\u041d\u042e" }},

        // ── Lobby ─────────────────────────────────────────────────────────
        { "lobby.title",         new[] { "MULTIPLAYER",
                                          "\u041c\u0423\u041b\u042c\u0422\u0418\u041f\u041b\u0415\u0415\u0420" }},
        { "lobby.subtitle",      new[] { "LAN  \u00b7  LOCAL NETWORK",
                                          "\u041b\u0410\u041d  \u00b7  \u041b\u041e\u041a\u0410\u041b\u042c\u041d\u0410\u042f \u0421\u0415\u0422\u042c" }},
        { "lobby.connecthdr",    new[] { "J O I N   O R   H O S T",
                                          "\u041f\u041e\u0414\u041a\u041b\u042e\u0427\u0418\u0422\u042c\u0421\u042f" }},
        { "lobby.host",          new[] { "\u2302   H O S T   G A M E",
                                          "\u2302   \u0421\u041e\u0417\u0414\u0410\u0422\u042c \u0418\u0413\u0420\u0423" }},
        { "lobby.iplabel",       new[] { "Host IP:",       "IP \u0445\u043e\u0441\u0442\u0430:" }},
        { "lobby.join",          new[] { "\u25b6   J O I N   G A M E",
                                          "\u25b6   \u041f\u041e\u0414\u041a\u041b\u042e\u0427\u0418\u0422\u042c\u0421\u042f" }},
        { "lobby.back",          new[] { "\u2190   B A C K",
                                          "\u2190   \u041d\u0410\u0417\u0410\u0414" }},
        { "lobby.roomhdr",       new[] { "W A I T I N G   F O R   P L A Y E R S",
                                          "\u041e\u0416\u0418\u0414\u0410\u041d\u0418\u0415 \u0418\u0413\u0420\u041e\u041a\u041e\u0412" }},
        { "lobby.togglebots",    new[] { "\u21c4   T O G G L E   B O T S",
                                          "\u21c4   \u0411\u041e\u0422\u042b   \u0412\u041a\u041b/\u0412\u042b\u041a\u041b" }},
        { "lobby.start",         new[] { "\u25b6   S T A R T   G A M E",
                                          "\u25b6   \u041d\u0410\u0427\u0410\u0422\u042c \u0418\u0413\u0420\u0423" }},
        { "lobby.leave",         new[] { "\u2190   L E A V E",
                                          "\u2190   \u0412\u042b\u0419\u0422\u0418" }},
        { "lobby.yourip",        new[] { "Your IP:  ",
                                          "\u0412\u0430\u0448 IP:  " }},
        { "lobby.connected",     new[] { "Connected to host",
                                          "\u041f\u043e\u0434\u043a\u043b\u044e\u0447\u0435\u043d\u043e \u043a \u0445\u043e\u0441\u0442\u0443" }},
        { "lobby.bots.on",       new[] { "Bots: ON",       "\u0411\u043e\u0442\u044b: \u0412\u041a\u041b" }},
        { "lobby.bots.off",      new[] { "Bots: OFF",      "\u0411\u043e\u0442\u044b: \u0412\u042b\u041a\u041b" }},
        { "lobby.players.fmt",   new[] { "Players: {0} / 8",
                                          "\u0418\u0433\u0440\u043e\u043a\u0438: {0} / 8" }},

        // ── HUD ───────────────────────────────────────────────────────────
        { "hud.round",           new[] { "Round ",         "\u0420\u0430\u0443\u043d\u0434 " }},
        { "hud.players.fmt",     new[] { "Players: {0}",   "\u0418\u0433\u0440\u043e\u043a\u0438: {0}" }},

        // ── Messages ──────────────────────────────────────────────────────
        { "msg.youreout",        new[] { "YOU'RE OUT!",
                                          "\u0422\u042b \u0412\u042b\u0411\u042b\u041b!" }},
        { "msg.finished.fmt",    new[] { "You finished #{0}!",
                                          "\u0422\u044b \u0444\u0438\u043d\u0438\u0448\u0438\u0440\u043e\u0432\u0430\u043b #{0}!" }},
        { "msg.youwin",          new[] { "YOU WIN!",
                                          "\u0422\u042b \u041f\u041e\u0411\u0415\u0414\u0418\u041b!" }},
        { "msg.gameover",        new[] { "GAME OVER",
                                          "\u0418\u0413\u0420\u0410 \u041e\u041a\u041e\u041d\u0427\u0415\u041d\u0410" }},
        { "msg.wintext",         new[] { "You beat everyone!",
                                          "\u0422\u044b \u043f\u043e\u0431\u0435\u0434\u0438\u043b \u0432\u0441\u0435\u0445!" }},
        { "msg.losetext",        new[] { "Better luck next time...",
                                          "\u0412 \u0441\u043b\u0435\u0434\u0443\u044e\u0449\u0438\u0439 \u0440\u0430\u0437 \u043f\u043e\u0432\u0435\u0437\u0451\u0442..." }},
    };
}
