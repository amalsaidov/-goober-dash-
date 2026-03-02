using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

[InitializeOnLoad]          // запускается при старте Unity и после компиляции
public class SceneSetup : Editor
{
    const string VERSION = "v7.0 — full UI redesign";

    // ── Автозапуск при нажатии Play ▶ ─────────────────────────────────────
    static SceneSetup()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            if (!SceneNeedsSetup()) return;

            // In a ParrelSync clone, ParrelSync blocks asset saves — never run Setup here.
            // If the scene is still empty in the clone, stop and tell the user.
            if (ParrelSync.ClonesManager.IsClone())
            {
                Debug.LogError("[SceneSetup] CLONE: Scene is empty — run 'Game → Setup 2D Runner Scene' in the MAIN editor first, then use 'Game → Reload Scene From Disk' in this clone.");
                EditorApplication.ExitPlaymode();
                return;
            }

            Setup();
        };

        // After compile / editor start: build + save ONLY if scene is empty.
        // Once objects exist their GlobalObjectIdHash is stable across Play runs
        // and iOS builds — do NOT recreate them or hashes will change and
        // clients will get "NetworkPrefab hash was not found! Hash: 0".
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // ── ParrelSync clone ──────────────────────────────────────────
            // ParrelSync blocks asset saves in the clone, so Setup() can't save
            // the scene — objects would be lost on Play Mode reload from disk.
            // Instead: reload the scene from disk (main editor's Setup saved it).
            if (ParrelSync.ClonesManager.IsClone())
            {
                // Force re-read from disk so we pick up whatever the main editor saved.
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                var scenePath = EditorSceneManager.GetActiveScene().path;
                if (string.IsNullOrEmpty(scenePath))
                    scenePath = "Assets/Scenes/SampleScene.unity";

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                if (SceneNeedsSetup())
                    Debug.LogWarning("[SceneSetup] CLONE: Scene still empty. Run 'Game → Setup 2D Runner Scene' in the MAIN editor first, then 'Game → Reload Scene From Disk' here.");
                else
                    Debug.Log("[SceneSetup] CLONE: Scene loaded from disk ✅");
                return;
            }

            // ── Main editor ───────────────────────────────────────────────
            if (!SceneNeedsSetup()) return; // already set up — keep stable GUIDs
            Setup(); // Setup() auto-saves internally
            Debug.Log("[SceneSetup] Auto-built SampleScene (was empty).");
        };
    }

    // Returns true when the scene has no game objects yet (needs Setup)
    static bool SceneNeedsSetup()
        => Object.FindAnyObjectByType<RaceManager>() == null;

    // ── World Palette — STANDARD (colorful) ────────────────────
    // WorldThemeManager converts to greyscale at runtime for B&W mode.
    static readonly Color SKY_TOP    = new Color(0.10f, 0.35f, 0.75f);
    static readonly Color SKY_MID    = new Color(0.18f, 0.55f, 0.92f);
    static readonly Color MTN_DARK   = new Color(0.08f, 0.22f, 0.50f);
    static readonly Color MTN_MID    = new Color(0.12f, 0.30f, 0.62f);
    static readonly Color CLOUD_COL  = new Color(0.94f, 0.97f, 1.00f);

    // Zone ground colors
    static readonly Color Z1_GND = new Color(0.18f, 0.62f, 0.18f);
    static readonly Color Z2_GND = new Color(0.14f, 0.50f, 0.32f);
    static readonly Color Z3_GND = new Color(0.40f, 0.26f, 0.16f);
    static readonly Color Z4_GND = new Color(0.22f, 0.18f, 0.48f);
    static readonly Color Z5_GND = new Color(0.65f, 0.50f, 0.12f);
    static readonly Color Z6_GND = new Color(0.55f, 0.12f, 0.12f); // crimson — descent zone

    // Platform colors per zone
    static readonly Color PLAT_Z2   = new Color(0.42f, 0.26f, 0.12f);
    static readonly Color PLAT_Z2T  = new Color(0.60f, 0.40f, 0.20f);
    static readonly Color PLAT_Z3   = new Color(0.28f, 0.22f, 0.18f);
    static readonly Color PLAT_Z3T  = new Color(0.44f, 0.36f, 0.28f);
    static readonly Color PLAT_Z4   = new Color(0.20f, 0.28f, 0.62f);
    static readonly Color PLAT_Z4T  = new Color(0.36f, 0.48f, 0.88f);
    static readonly Color PLAT_Z6   = new Color(0.42f, 0.10f, 0.10f);
    static readonly Color PLAT_Z6T  = new Color(0.68f, 0.22f, 0.22f);

    // Interactable colors
    static readonly Color BOUNCE_C  = new Color(1.00f, 0.45f, 0.08f);
    static readonly Color SPEED_C   = new Color(0.08f, 0.92f, 0.38f);
    static readonly Color WARN_C    = new Color(0.90f, 0.20f, 0.10f);
    static readonly Color WALL_C    = new Color(0.45f, 0.15f, 0.65f);

    // ── UI Palette v7 — deep neon dark ────────────────────────────────────
    // Backgrounds
    static readonly Color UI_BG_DEEP  = new Color(0.00f, 0.02f, 0.06f, 0.98f); // near-void
    static readonly Color UI_BG_CARD  = new Color(0.05f, 0.09f, 0.20f, 1.00f); // card surface
    static readonly Color UI_BG_RAISE = new Color(0.08f, 0.14f, 0.30f, 1.00f); // raised / hover
    // Accents
    static readonly Color UI_ACCENT   = new Color(0.20f, 0.58f, 1.00f, 1.00f); // electric blue
    static readonly Color UI_CYAN     = new Color(0.00f, 0.86f, 0.96f, 1.00f); // cyber cyan
    static readonly Color UI_GOLD     = new Color(1.00f, 0.78f, 0.08f, 1.00f); // pure gold
    static readonly Color UI_GREEN    = new Color(0.10f, 0.95f, 0.52f, 1.00f); // neon green
    static readonly Color UI_RED      = new Color(1.00f, 0.22f, 0.34f, 1.00f); // neon red
    static readonly Color UI_ORANGE   = new Color(1.00f, 0.54f, 0.06f, 1.00f); // hot orange
    // Text
    static readonly Color UI_TEXT_PRI = new Color(0.92f, 0.97f, 1.00f, 1.00f); // near-white
    static readonly Color UI_TEXT_SEC = new Color(0.42f, 0.58f, 0.80f, 1.00f); // mid blue-gray
    static readonly Color UI_TEXT_DIM = new Color(0.18f, 0.26f, 0.42f, 1.00f); // dim

    static int groundLayer, playerLayer;

    // ── Clone helper: reload the scene that the main editor saved ────────
    [MenuItem("Game/Reload Scene From Disk")]
    static void ReloadSceneFromDisk()
    {
        // Force Unity to re-read files from disk — bypasses the AssetDatabase cache.
        // This is essential in the ParrelSync clone where the main editor may have
        // saved the scene after the clone opened it.
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var path = EditorSceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(path))
            path = "Assets/Scenes/SampleScene.unity";   // fallback to known location

        Debug.Log($"[SceneSetup] Loading from: {path}");
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Debug.Log(SceneNeedsSetup()
            ? $"[SceneSetup] Still empty after reload ({path}). Run Setup in MAIN editor first."
            : "[SceneSetup] Reloaded from disk ✅");
    }

    [MenuItem("Game/Setup 2D Runner Scene")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[SceneSetup] ❌ Stop Play Mode first, then run Setup!");
            return;
        }

        // ── CRITICAL: open the actual scene file before modifying it ─────────
        // If the active scene is "Untitled" (path=""), SaveScene would try to
        // overwrite SampleScene.unity with a new file → Unity blocks it silently
        // → file never updated → clone gets empty scene → NGO hash mismatch.
        const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";
        if (EditorSceneManager.GetActiveScene().path != SCENE_PATH)
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        _whiteSprite = null;
        ClearScene();
        groundLayer = CreateLayer("Ground");
        playerLayer = CreateLayer("Player");
        Physics2D.IgnoreLayerCollision(playerLayer, playerLayer, false); // players push each other

        SetupCamera();
        BuildSky();
        BuildMountains();
        BuildClouds();
        BuildTrack();      // inside MapContainer_0
        BuildWaypoints();  // for map 0 (inside MapContainer_0)
        BuildTrack1();     // inside MapContainer_1 (active during setup so child Find() works)
        BuildWaypoints1(); // for map 1 (inside MapContainer_1)
        // Disable MapContainer_1 now that all its children are set up
        if (_container1 != null) _container1.SetActive(false);
        SpawnPlayers();
        CreateManagers();
        CreateUI();

        // Scene is now dirty and has the correct path — save in-place.
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        if (!saved)
            Debug.LogError("[SceneSetup] ❌ SAVE FAILED — check console for errors");
        else
            Debug.Log($"✅ GOOBER DASH {VERSION} — Scene saved. A/D | Space×2 | Shift=dash | Wall slide+jump: press into wall while falling");
    }

    // ── Clear ──────────────────────────────────────────────────
    static void ClearScene()
    {
        foreach (var rp in Object.FindObjectsByType<RacePlayer>(FindObjectsSortMode.None))
            DestroyImmediate(rp.gameObject);
        foreach (var cp in Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
            DestroyImmediate(cp.gameObject);

        string[] named = { "Main Camera","Sky_Top","Sky_Mid","Waypoints","Waypoints1",
                           "GameCanvas","EventSystem","RaceManager","ScoreManager",
                           "DifficultyManager","MainMenuManager","GameSettings",
                           "PauseManager","NetworkManager","NetworkLobbyManager",
                           "LocalizationManager","GlobalLight 2D","TouchControls",
                           "DebugOverlay","OnboardingPanel","WorldThemeManager",
                           "UIToolkitRoot","MapManager","SpectatorController" };
        foreach (string n in named) { var o = GameObject.Find(n); if (o) DestroyImmediate(o); }

        // Destroy ALL LanDiscovery instances — GameObject.Find only gets the first,
        // so repeated Setup runs accumulate duplicates without this fix.
        foreach (var ld in Object.FindObjectsByType<LanDiscovery>(FindObjectsSortMode.None))
            if (ld != null) DestroyImmediate(ld.gameObject);

        // Destroy ALL UIDocument objects (UIToolkitRoot duplicates from repeated Setup runs)
        foreach (var doc in Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsSortMode.None))
            if (doc != null) DestroyImmediate(doc.gameObject);

        // Destroy track containers from previous setup (use root objects so inactive ones are found too)
        foreach (var rootGO in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (rootGO == null) continue;
            if (rootGO.name == "MapContainer_0" || rootGO.name == "MapContainer_1")
                DestroyImmediate(rootGO);
        }

        // Destroy floating name tags (top-level objects, not under canvas)
        foreach (var pnt in Object.FindObjectsByType<PlayerNameTag>(FindObjectsSortMode.None))
            if (pnt != null) DestroyImmediate(pnt.gameObject);

        // Destroy kill zone triggers
        foreach (var kz in Object.FindObjectsByType<KillZone>(FindObjectsSortMode.None))
            if (kz != null) DestroyImmediate(kz.gameObject);

        // Destroy new mechanic/deco objects
        foreach (var o in Object.FindObjectsByType<IceSurface>(FindObjectsSortMode.None))
            if (o != null) DestroyImmediate(o.gameObject);
        foreach (var o in Object.FindObjectsByType<WindZone>(FindObjectsSortMode.None))
            if (o != null) DestroyImmediate(o.gameObject);
        foreach (var o in Object.FindObjectsByType<DashBoost>(FindObjectsSortMode.None))
            if (o != null) DestroyImmediate(o.gameObject);
        foreach (var o in Object.FindObjectsByType<DashBar>(FindObjectsSortMode.None))
            if (o != null) DestroyImmediate(o.gameObject);
        foreach (var o in Object.FindObjectsByType<ConveyorBelt>(FindObjectsSortMode.None))
            if (o != null) DestroyImmediate(o.gameObject);
        foreach (var o in Object.FindObjectsByType<CrumblingPlatform>(FindObjectsSortMode.None))
            if (o != null) DestroyImmediate(o.gameObject);
        foreach (var o in Object.FindObjectsByType<LowGravityZone>(FindObjectsSortMode.None))
            if (o != null) DestroyImmediate(o.gameObject);
        foreach (var o in Object.FindObjectsByType<TeleportPad>(FindObjectsSortMode.None))
            if (o != null) DestroyImmediate(o.gameObject);
        foreach (var o in Object.FindObjectsByType<DynamicSpikes>(FindObjectsSortMode.None))
            if (o != null) DestroyImmediate(o.gameObject);

        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (sr == null) continue;
            string n = sr.gameObject.name;
            if (n.StartsWith("Gnd") || n.StartsWith("Plt") || n.StartsWith("Mov") ||
                n.StartsWith("Bnc") || n.StartsWith("Spd") || n.StartsWith("Fin") ||
                n.StartsWith("Cld") || n.StartsWith("Mtn") || n.StartsWith("CP_") ||
                n.StartsWith("Str") || n.StartsWith("Wrn") || n.StartsWith("Star") ||
                n.StartsWith("Wll") || n.StartsWith("Ice") || n.StartsWith("Wnd") ||
                n.StartsWith("Dsh") || n.StartsWith("Spk") || n.StartsWith("Deco"))
                DestroyImmediate(sr.gameObject);
        }
    }

    // ── Camera ─────────────────────────────────────────────────
    static void SetupCamera()
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7.5f;
        cam.backgroundColor = SKY_MID; // WorldThemeManager updates this at runtime
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0, 0, -10);
        var cf = camGO.AddComponent<CameraFollow>();
        cf.offset = new Vector3(0f, 0f, -10f);
        cf.smoothSpeed = 5f;
        cf.baseSize = 7.5f;
    }

    // ── Background ─────────────────────────────────────────────
    static void BuildSky()
    {
        // Cover full map width (x=-9 to x=192) and full height (y=-3 to y=22)
        MakeBg("Sky_Top", SKY_TOP, new Vector3(95, 12, 9), new Vector3(300, 50, 1), -14, 0.04f);
        MakeBg("Sky_Mid", SKY_MID, new Vector3(95,  8, 8), new Vector3(300, 36, 1), -13, 0.10f);
    }

    static void BuildMountains()
    {
        // Far dark mountains — extended to cover full x=-10 to x=195
        float[] fxs = { 0,14,28,42,56,70,84,98,112,128,142,156,170,185 };
        float[] fhs = { 8,10, 7, 9, 8,11, 7, 9,  8, 10,  7,  9,  8, 10 };
        float[] fws = { 9, 7,10, 8, 9, 7,10, 8,  9,  7, 10,  8,  9,  7 };
        for (int i = 0; i < fxs.Length; i++)
            MakeBg("Mtn_far_"+i, MTN_DARK,
                new Vector3(fxs[i], -3+fhs[i]*0.5f, 7),
                new Vector3(fws[i], fhs[i], 1), -12, 0.15f);

        // Near lighter mountains
        float[] nxs = { 5,18,33,48,62,78,93,108,122,136,150,165,180 };
        float[] nhs = { 5, 7, 6, 8, 5, 7, 6,  8,  5,  7,  6,  8,  5 };
        for (int i = 0; i < nxs.Length; i++)
            MakeBg("Mtn_near_"+i, MTN_MID,
                new Vector3(nxs[i], -3+nhs[i]*0.5f, 6),
                new Vector3(6, nhs[i], 1), -11, 0.22f);
    }

    static void BuildClouds()
    {
        // Low clouds (ground level area)
        float[] xs = { -4, 8, 20, 34, 48, 62, 76, 90, 105 };
        float[] ys = {  6, 8,  5,  7,  6,  8,  5,  7,   6 };
        float[] ws = {  7, 5,  8,  6,  7,  5,  8,  6,   5 };
        for (int i = 0; i < xs.Length; i++)
        {
            float x = xs[i], y = ys[i], w = ws[i];
            float p = 0.18f + i * 0.02f;
            Bg($"Cld{i}b", CLOUD_COL, new Vector3(x, y, 5),          new Vector3(w, 1.2f, 1),    -9, p);
            Bg($"Cld{i}L", CLOUD_COL, new Vector3(x-w*.22f,y+.7f,5), new Vector3(w*.38f,.95f,1), -9, p);
            Bg($"Cld{i}R", CLOUD_COL, new Vector3(x+w*.18f,y+.85f,5),new Vector3(w*.32f,.80f,1), -9, p);
        }

        // High clouds — visible in vertical climb (Z4) and sky highway (Z5) sections
        float[] hxs = { 100, 112, 124, 136, 148, 160, 174 };
        float[] hys = {  14,  18,  15,  20,  16,  19,  14 };
        float[] hws = {   5,   4,   6,   5,   4,   5,   6 };
        for (int i = 0; i < hxs.Length; i++)
        {
            float x = hxs[i], y = hys[i], w = hws[i];
            float p = 0.10f + i * 0.015f;
            Bg($"CldH{i}b", CLOUD_COL, new Vector3(x, y, 5),          new Vector3(w, 1.0f, 1),   -9, p);
            Bg($"CldH{i}L", CLOUD_COL, new Vector3(x-w*.22f,y+.6f,5), new Vector3(w*.35f,.8f,1), -9, p);
            Bg($"CldH{i}R", CLOUD_COL, new Vector3(x+w*.18f,y+.75f,5),new Vector3(w*.28f,.7f,1), -9, p);
        }

        // Stars — scattered across the sky section (x=115-165, y=12-21)
        for (int i = 0; i < 18; i++)
        {
            float sx = 115f + i * 3.0f;
            float sy = 12f + (i % 4) * 2.2f;
            Bg($"Star{i}", new Color(1,1,1,0.7f),
                new Vector3(sx, sy, 4.5f), new Vector3(0.18f, 0.18f, 1), -8, 0.08f);
        }
    }

    // Container used by all track-object helpers — set before calling Gnd/Plt/etc.
    static Transform _trackParent;
    // Persistent refs to map containers — set by BuildTrack/BuildTrack1, consumed by CreateManagers
    static GameObject _container0;
    static GameObject _container1;

    // ── TRACK MAP 0 — Forest Run ────────────────────────────────
    // NON-LINEAR PATH:
    //   Z1 right → Z2 up-staircase → Z3 left-bridge → Z4 up-climb →
    //   Z5 right-skyhighway → Z6 down-descent → Z7 right+up → FINISH
    // World span: x = -15 to +96,  y = -5 to +50
    static void BuildTrack()
    {
        var container = new GameObject("MapContainer_0");
        _container0 = container;
        _trackParent = container.transform;

        // ── Platform color palette ────────────────────────────────────────────
        var Pb2 = PLAT_Z2;  var Pt2 = PLAT_Z2T;   // brown staircase
        var Pb3 = PLAT_Z3;  var Pt3 = PLAT_Z3T;   // stone bridge
        var Pb4 = PLAT_Z4;  var Pt4 = PLAT_Z4T;   // purple sky climb
        var Pb6 = PLAT_Z6;  var Pt6 = PLAT_Z6T;   // crimson descent

        // ════════════════════════════════════════════════════════
        // ZONE 1 — Flat Launch (→ RIGHT, y=-3)
        //   Long green floor, 2 speed pads, 1 BIG bounce at the end,
        //   then a gap + small ledge so there's one tricky jump.
        // ════════════════════════════════════════════════════════
        Gnd("Gnd_Z1",  new Vector3(0,    -3, 0), new Vector3(30, 1, 1), Z1_GND);
        Spd("Spd_Z1a", new Vector3(4,   -2.5f, 0), new Vector3(2.5f, 0.22f, 1));
        Spd("Spd_Z1b", new Vector3(10,  -2.5f, 0), new Vector3(2.5f, 0.22f, 1));
        Ice("Ice_Z1",  new Vector3(6.5f, -2.53f, 0), new Vector3(5f, 0.10f, 1)); // slippery mid-run
        // Mega bounce at end of floor — launches player into Z2 staircase
        Bnc("Bnc_Z1",  new Vector3(14,  -2.5f, 0), new Vector3(2.5f, 0.30f, 1));
        // Gap x=16–20, small ledge + stepping stone up to Z2
        Gnd("Gnd_Z1b", new Vector3(21.5f,-3, 0), new Vector3(3.5f, 1, 1), Z1_GND);
        // Stepping stone — bridges Z1 floor (y=-3) to Z2 first platform (y=3.5)
        // Reachable by normal jump from floor; Z2a reachable by normal jump from here
        Plt("Plt_Z12step", new Vector3(22, 0.5f, 0), new Vector3(3f, 0.42f, 1),
            new Color(0.18f, 0.62f, 0.18f), new Color(0.32f, 0.80f, 0.28f));
        // Deco: grass tufts
        Deco("Deco_Z1g0", new Vector3(-8, -2.35f, 0), new Vector3(0.22f, 0.45f, 1), new Color(0.22f,0.82f,0.18f,0.70f), 2);
        Deco("Deco_Z1g1", new Vector3(-3, -2.32f, 0), new Vector3(0.18f, 0.40f, 1), new Color(0.18f,0.75f,0.16f,0.68f), 2);
        Deco("Deco_Z1g2", new Vector3( 8, -2.30f, 0), new Vector3(0.20f, 0.42f, 1), new Color(0.20f,0.78f,0.17f,0.65f), 2);
        CP("CP_1", new Vector3(12, -1, 0));

        // ════════════════════════════════════════════════════════
        // ZONE 2 — Staircase UP (↑, x≈22–42, y: 3.5 → 22)
        //   Six ascending platforms — each step is 3 units up,
        //   4 units right. One moving platform near the top.
        //   A wall at x=44 blocks further right — forces LEFT turn.
        // ════════════════════════════════════════════════════════
        Plt("Plt_Z2a", new Vector3(23,  3.5f, 0), new Vector3(4f, 0.42f, 1), Pb2, Pt2);
        Plt("Plt_Z2b", new Vector3(27,  6.5f, 0), new Vector3(4f, 0.42f, 1), Pb2, Pt2);
        Spk("Spk_Z2b", new Vector3(27,  6.96f,0), new Vector3(2f, 0.18f, 1), 1.0f, 3.5f, 0.3f);
        Plt("Plt_Z2c", new Vector3(31,  9.5f, 0), new Vector3(4f, 0.42f, 1), Pb2, Pt2);
        Spd("Spd_Z2c", new Vector3(31,  9.96f,0), new Vector3(2.5f, 0.22f, 1));
        Plt("Plt_Z2d", new Vector3(35, 12.5f, 0), new Vector3(4f, 0.42f, 1), Pb2, Pt2);
        Ice("Ice_Z2d", new Vector3(35, 12.93f,0), new Vector3(3.8f, 0.10f, 1));
        Plt("Plt_Z2e", new Vector3(38, 15.5f, 0), new Vector3(3.5f, 0.42f, 1), Pb2, Pt2);
        Spk("Spk_Z2e", new Vector3(38, 15.96f,0), new Vector3(1.8f, 0.18f, 1), 1.0f, 3.5f, 0.6f);
        Mov("Mov_Z2f", new Vector3(41, 18.5f, 0), new Vector3(3.5f, 0.40f, 1), Pb2, Pt2, 2.0f, false);
        // Top landing pad — path continues LEFT from here
        Gnd("Gnd_Z2top", new Vector3(36, 21.5f, 0), new Vector3(14, 1, 1), Z2_GND);
        Cnv("Cnv_Z2", new Vector3(40, 22.05f, 0), new Vector3(6f, 0.15f, 1), -5f); // LEFT push — fight the belt!
        // Wall barrier — blocks further right, forces players to turn left
        Wll("Wll_Z2R", new Vector3(44.5f, 12, 0), new Vector3(0.8f, 24, 1));
        CP("CP_2", new Vector3(38, 23, 0));

        // ════════════════════════════════════════════════════════
        // ZONE 3 — Left Bridge (← LEFT, y≈22, x: 43 → -5)
        //   Three platform segments separated by gaps.
        //   Wind zone over first gap. Ice on last segment.
        //   DashBoost mid-bridge. Kill zone below.
        // ════════════════════════════════════════════════════════
        // Gap 1: x≈29 to x≈25 — wind helps
        Wnd("Wnd_Z3a", new Vector3(27, 24.5f, 0), new Vector3(3f, 5.5f, 1));
        Plt("Plt_Z3a",  new Vector3(21, 22, 0), new Vector3(8f, 0.42f, 1), Pb3, Pt3);
        // Gap 2: x≈17 to x≈12
        Plt("Plt_Z3b",  new Vector3(9,  22, 0), new Vector3(8f, 0.42f, 1), Pb3, Pt3);
        Dsh("Dsh_Z3",   new Vector3(9, 23.4f, 0)); // DashBoost mid-bridge
        // Gap 3: x≈5 to x≈0
        Plt("Plt_Z3c",  new Vector3(-4, 22, 0), new Vector3(6f, 0.42f, 1), Pb3, Pt3);
        Ice("Ice_Z3c",  new Vector3(-4, 22.43f, 0), new Vector3(5.5f, 0.10f, 1));
        CP("CP_3", new Vector3(8, 23, 0));

        // ════════════════════════════════════════════════════════
        // ZONE 4 — Second Climb (↑, x: -7 to +7, y: 22 → 47)
        //   Wall-jump surfaces on left and right.
        //   Six stepping platforms zigzag up.
        //   Wind column mid-climb. Ice & spikes on platforms.
        //   DashBoost near top.
        // ════════════════════════════════════════════════════════
        // Walls bottom raised to y=23.5 (above Z3 bridge at y=22) so players can
        // walk on Plt_Z3c (x=-7 to -1) without hitting the wall edge.
        Wll("Wll_Z4L", new Vector3(-6, 37f, 0), new Vector3(0.8f, 27f, 1));
        Wll("Wll_Z4R", new Vector3( 6, 37f, 0), new Vector3(0.8f, 27f, 1));
        Plt("Plt_Z4a", new Vector3(-1.5f, 25.5f,0), new Vector3(4f, 0.42f, 1), Pb4, Pt4);
        Plt("Plt_Z4b", new Vector3( 2,   28.5f, 0), new Vector3(4f, 0.42f, 1), Pb4, Pt4);
        Spk("Spk_Z4b", new Vector3( 2,   28.96f,0), new Vector3(2.2f,0.18f,1), 1.0f,3.5f,0.2f);
        Plt("Plt_Z4c", new Vector3(-1.5f, 31.5f,0), new Vector3(4f, 0.42f, 1), Pb4, Pt4);
        Wnd("Wnd_Z4",  new Vector3( 0,   35.5f, 0), new Vector3(6f, 5.5f, 1)); // uplift column
        Plt("Plt_Z4d", new Vector3( 2,   36f,   0), new Vector3(4f, 0.42f, 1), Pb4, Pt4);
        Ice("Ice_Z4d", new Vector3( 2,   36.43f,0), new Vector3(3.8f,0.10f, 1));
        Plt("Plt_Z4e", new Vector3(-1.5f, 39.5f,0), new Vector3(4f, 0.42f, 1), Pb4, Pt4);
        Dsh("Dsh_Z4",  new Vector3(-1.5f, 41f,  0)); // DashBoost — reward near top
        Lgz("Lgz_Z4",  new Vector3( 0,   42.5f, 0), new Vector3(14f, 9f,   1)); // floaty upper climb
        Plt("Plt_Z4f", new Vector3( 2,   43.5f, 0), new Vector3(4f, 0.42f, 1), Pb4, Pt4);
        Plt("Plt_Z4g", new Vector3(-1.5f, 46.5f,0), new Vector3(4f, 0.42f, 1), Pb4, Pt4);
        CP("CP_4", new Vector3(0, 48, 0));

        // ════════════════════════════════════════════════════════
        // ZONE 5 — Sky Highway (→ RIGHT, y≈46.5, x: 5 → 58)
        //   Entry landing pad, then 5 platform segments going right.
        //   Speed pads, spikes, 1 moving platform, 1 wind zone.
        // ════════════════════════════════════════════════════════
        Gnd("Gnd_Z5entry", new Vector3(6.5f, 46.5f, 0), new Vector3(7f, 0.8f, 1), Z4_GND);
        // Gap → Plt_Z5a
        Plt("Plt_Z5a", new Vector3(15, 46.5f, 0), new Vector3(5f, 0.42f, 1), Pb4, Pt4);
        Spd("Spd_Z5a", new Vector3(15, 46.96f,0), new Vector3(3.5f,0.22f, 1));
        // Gap → Plt_Z5b — CRUMBLING (risky, spikes above)
        Crm("Plt_Z5b", new Vector3(24, 46.5f, 0), new Vector3(5f, 0.42f, 1), Pb4, Pt4);
        Spk("Spk_Z5b", new Vector3(24, 46.96f,0), new Vector3(2.5f,0.18f,1), 1.0f,3.5f,0.4f);
        Dsh("Dsh_Z5",  new Vector3(24, 48.0f, 0)); // DashBoost mid-sky
        // Moving platform gap
        Mov("Mov_Z5c", new Vector3(31, 46.5f, 0), new Vector3(3.5f,0.40f, 1), Pb4, Pt4, 2.2f, false);
        // Gap → Plt_Z5d
        Plt("Plt_Z5d", new Vector3(39, 46.5f, 0), new Vector3(5f, 0.42f, 1), Pb4, Pt4);
        Spd("Spd_Z5d", new Vector3(39, 46.96f,0), new Vector3(3.5f,0.22f, 1));
        Wnd("Wnd_Z5",  new Vector3(42.5f,49f,  0), new Vector3(2.5f,4f,   1)); // rescue wind
        // Gap → Plt_Z5e (vertical moving)
        Mov("Mov_Z5e", new Vector3(47, 46.5f, 0), new Vector3(3.5f,0.40f, 1), Pb4, Pt4, 1.8f, true);
        // Gap → final ground
        Gnd("Gnd_Z5end", new Vector3(55, 46.5f, 0), new Vector3(8f, 0.8f, 1), Z5_GND);
        Spd("Spd_Z5end", new Vector3(54, 46.96f,0), new Vector3(4f, 0.22f, 1));
        Tpt("Tpt_Z5",    new Vector3(58, 46.88f, 0), new Vector3(79f, 11.5f, 0)); // shortcut → Z6 bottom
        CP("CP_5", new Vector3(55, 48, 0));

        // ════════════════════════════════════════════════════════
        // ZONE 6 — Descent (↓, x: 57 → 77, y: 46 → 10)
        //   Four platforms zigzag down — each drops 6 units.
        //   Bounce pad near bottom to redirect player right.
        // ════════════════════════════════════════════════════════
        Crm("Plt_Z6a",  new Vector3(61, 40, 0), new Vector3(4.5f,0.42f, 1), Pb6, Pt6); // crumbles!
        Plt("Plt_Z6b",  new Vector3(65, 34, 0), new Vector3(4.5f,0.42f, 1), Pb6, Pt6);
        Spk("Spk_Z6b", new Vector3(65, 34.46f,0), new Vector3(2f, 0.18f, 1), 1.0f,3.5f,0.0f);
        Plt("Plt_Z6c", new Vector3(70, 28, 0), new Vector3(4.5f,0.42f, 1), Pb6, Pt6);
        Plt("Plt_Z6d", new Vector3(74, 22, 0), new Vector3(4.5f,0.42f, 1), Pb6, Pt6);
        Bnc("Bnc_Z6",  new Vector3(73, 22.32f,0), new Vector3(2.5f,0.30f, 1)); // redirects right
        Gnd("Gnd_Z6",  new Vector3(78, 10, 0), new Vector3(10f, 1, 1), Z6_GND);
        CP("CP_6", new Vector3(77, 11.5f, 0));

        // ════════════════════════════════════════════════════════
        // ZONE 7 — Final Sprint + UP climb (→ RIGHT then ↑, x: 80–96, y: 10–28)
        //   Speed pads on sprint floor.
        //   Three ascending platforms with spikes.
        //   High finish platform.
        // ════════════════════════════════════════════════════════
        Gnd("Gnd_Z7", new Vector3(84, 10, 0), new Vector3(10f, 1, 1), Z5_GND);
        Spd("Spd_Z7a", new Vector3(81, 10.5f, 0), new Vector3(2.5f,0.22f, 1));
        Spd("Spd_Z7b", new Vector3(85, 10.5f, 0), new Vector3(2.5f,0.22f, 1));
        Spk("Spk_Z7",  new Vector3(83, 10.96f,0), new Vector3(2f, 0.18f, 1), 1.0f,3.5f,0.1f);
        // Three rising platforms going UP
        Plt("Plt_Z7a", new Vector3(84, 14, 0), new Vector3(4f, 0.42f, 1), Pb4, Pt4);
        Plt("Plt_Z7b", new Vector3(79, 18, 0), new Vector3(4f, 0.42f, 1), Pb4, Pt4); // slight left — twist
        Plt("Plt_Z7c", new Vector3(84, 22, 0), new Vector3(4f, 0.42f, 1), Pb4, Pt4);
        // Final high platform — FINISH is here
        Gnd("Gnd_Z7top", new Vector3(90, 26, 0), new Vector3(10f, 1, 1), Z4_GND);
        Ice("Ice_Z7top", new Vector3(90, 26.47f,0), new Vector3(9f, 0.10f, 1)); // dramatic icy finish
        Spd("Spd_Z7top", new Vector3(88, 26.5f, 0), new Vector3(3f, 0.22f, 1));

        // ── Finish line (tall trigger at x=96, spans y=-10 to +38) ───────
        var fl = new GameObject("Fin");
        fl.transform.position   = new Vector3(96f, 14, 0);
        fl.transform.localScale = new Vector3(0.5f, 28f, 1f);
        if (_trackParent != null) fl.transform.SetParent(_trackParent, true);
        fl.AddComponent<SpriteRenderer>().sprite = MakeChecker();
        fl.GetComponent<SpriteRenderer>().sortingOrder = 3;
        var flc = fl.AddComponent<BoxCollider2D>();
        flc.isTrigger = true;
        fl.AddComponent<FinishLine>();

        // ── Wide floor after finish — players land here instead of falling ──
        Gnd("Gnd_FinFloor", new Vector3(103f, 26f, 0), new Vector3(18f, 1f, 1), Z4_GND);

        // Kill zones auto-calculated from actual geometry bounds + padding
        BuildBoundaryKillZones(_container0, "KZ_", 18f);
    }

    // ── Waypoints ──────────────────────────────────────────────
    // Mirrors the non-linear path: right→up→left→up→right→down→up→finish
    static void BuildWaypoints()
    {
        var go = new GameObject("Waypoints");
        if (_container0 != null) go.transform.SetParent(_container0.transform, true);
        var wp = go.AddComponent<WaypointPath>();
        wp.points = new Vector3[]
        {
            // Z1 — flat sprint right (y=-3)
            new Vector3(-12,  -2,    0),
            new Vector3( -5,  -2,    0),
            new Vector3(  4,  -2,    0),   // speed pads
            new Vector3( 10,  -2,    0),
            new Vector3( 14,   0,    0),   // at bounce pad
            new Vector3( 21,  -2,    0),   // small ledge after gap

            // Z2 — staircase UP (right + rising)
            new Vector3( 23,   4.0f, 0),   // Plt_Z2a
            new Vector3( 27,   7.0f, 0),   // Plt_Z2b
            new Vector3( 31,  10.0f, 0),   // Plt_Z2c
            new Vector3( 35,  13.0f, 0),   // Plt_Z2d
            new Vector3( 38,  16.0f, 0),   // Plt_Z2e
            new Vector3( 41,  19.0f, 0),   // Mov_Z2f
            new Vector3( 36,  22.0f, 0),   // Gnd_Z2top

            // Z3 — bridge going LEFT (y≈22)
            new Vector3( 21,  22.5f, 0),   // Plt_Z3a
            new Vector3(  9,  22.5f, 0),   // Plt_Z3b
            new Vector3( -4,  22.5f, 0),   // Plt_Z3c (left end)

            // Z4 — zigzag climb UP
            new Vector3(-1.5f, 26.0f, 0),  // Plt_Z4a
            new Vector3( 2,    29.0f, 0),  // Plt_Z4b
            new Vector3(-1.5f, 32.0f, 0),  // Plt_Z4c
            new Vector3( 2,    36.5f, 0),  // Plt_Z4d
            new Vector3(-1.5f, 40.0f, 0),  // Plt_Z4e
            new Vector3( 2,    44.0f, 0),  // Plt_Z4f
            new Vector3(-1.5f, 47.0f, 0),  // Plt_Z4g (top)

            // Z5 — sky highway RIGHT (y≈46.5)
            new Vector3( 6.5f, 47.3f, 0),  // Gnd_Z5entry
            new Vector3( 15,   47.0f, 0),  // Plt_Z5a
            new Vector3( 24,   47.0f, 0),  // Plt_Z5b
            new Vector3( 31,   47.0f, 0),  // Mov_Z5c
            new Vector3( 39,   47.0f, 0),  // Plt_Z5d
            new Vector3( 47,   47.0f, 0),  // Mov_Z5e
            new Vector3( 55,   47.3f, 0),  // Gnd_Z5end

            // Z6 — zigzag descent DOWN
            new Vector3( 61,   40.5f, 0),  // Plt_Z6a
            new Vector3( 65,   34.5f, 0),  // Plt_Z6b
            new Vector3( 70,   28.5f, 0),  // Plt_Z6c
            new Vector3( 74,   22.5f, 0),  // Plt_Z6d
            new Vector3( 78,   10.5f, 0),  // Gnd_Z6

            // Z7 — final sprint right then climb UP
            new Vector3( 81,   10.5f, 0),  // Gnd_Z7 sprint
            new Vector3( 85,   10.5f, 0),
            new Vector3( 84,   14.5f, 0),  // Plt_Z7a
            new Vector3( 79,   18.5f, 0),  // Plt_Z7b (left twist)
            new Vector3( 84,   22.5f, 0),  // Plt_Z7c
            new Vector3( 90,   26.5f, 0),  // Gnd_Z7top
            new Vector3( 96,   26.5f, 0),  // finish line
        };
    }

    // ── TRACK MAP 1 — Volcano Rush ──────────────────────────────
    // Same non-linear layout as Map 0 but shifted +55 Y and volcanic colors.
    static void BuildTrack1()
    {
        const float Y = 55f; // vertical offset from map 0

        // Volcano palette
        Color vZ1  = new Color(0.52f, 0.14f, 0.08f);
        Color vZ2  = new Color(0.48f, 0.18f, 0.06f);
        Color vZ3  = new Color(0.40f, 0.10f, 0.06f);
        Color vZ4  = new Color(0.36f, 0.08f, 0.26f);
        Color vZ5  = new Color(0.55f, 0.20f, 0.05f);
        Color vZ6  = new Color(0.60f, 0.10f, 0.05f);
        Color vPb  = new Color(0.45f, 0.18f, 0.08f);
        Color vPt  = new Color(0.75f, 0.35f, 0.10f);
        Color vPb4 = new Color(0.30f, 0.08f, 0.20f);
        Color vPt4 = new Color(0.55f, 0.22f, 0.45f);
        Color vPb6 = new Color(0.45f, 0.08f, 0.08f);
        Color vPt6 = new Color(0.72f, 0.24f, 0.12f);

        var container = new GameObject("MapContainer_1");
        _container1 = container;
        _trackParent = container.transform;

        // Z1 — Lava Launch (→ RIGHT)
        Gnd("Gnd_V1",  new Vector3(0,    -3+Y, 0), new Vector3(30, 1, 1), vZ1);
        Spd("Spd_V1a", new Vector3(4,   -2.5f+Y,0), new Vector3(2.5f, 0.22f, 1));
        Spd("Spd_V1b", new Vector3(10,  -2.5f+Y,0), new Vector3(2.5f, 0.22f, 1));
        Ice("Ice_V1",  new Vector3(6.5f,-2.53f+Y,0), new Vector3(5f, 0.10f, 1)); // cooled lava crust
        Bnc("Bnc_V1",  new Vector3(14,  -2.5f+Y,0), new Vector3(2.5f, 0.30f, 1));
        Gnd("Gnd_V1b", new Vector3(21.5f,-3+Y,  0), new Vector3(3.5f, 1, 1), vZ1);
        // Stepping stone — same purpose as Map 0 (bridges floor to Z2 staircase entry)
        Plt("Plt_V12step", new Vector3(22, 0.5f+Y, 0), new Vector3(3f, 0.42f, 1), vPb, vPt);
        Deco("Deco_V1e0", new Vector3(-8, -2.38f+Y,0), new Vector3(0.28f,0.28f,1), new Color(0.85f,0.28f,0.05f,0.65f), 2);
        Deco("Deco_V1e1", new Vector3(-3, -2.40f+Y,0), new Vector3(0.22f,0.22f,1), new Color(0.90f,0.40f,0.06f,0.60f), 2);
        Deco("Deco_V1e2", new Vector3( 8, -2.38f+Y,0), new Vector3(0.24f,0.24f,1), new Color(0.80f,0.22f,0.04f,0.68f), 2);
        CP("CP_V1",    new Vector3(12,   0+Y,    0));

        // Z2 — Lava Staircase (↑ UP)
        Plt("Plt_V2a", new Vector3(23,  3.5f+Y, 0), new Vector3(4f, 0.42f, 1), vPb, vPt);
        Plt("Plt_V2b", new Vector3(27,  6.5f+Y, 0), new Vector3(4f, 0.42f, 1), vPb, vPt);
        Spk("Spk_V2b", new Vector3(27,  6.96f+Y,0), new Vector3(2f, 0.18f, 1), 1.0f,3.5f,0.3f);
        Plt("Plt_V2c", new Vector3(31,  9.5f+Y, 0), new Vector3(4f, 0.42f, 1), vPb, vPt);
        Spd("Spd_V2c", new Vector3(31,  9.96f+Y,0), new Vector3(2.5f,0.22f, 1));
        Plt("Plt_V2d", new Vector3(35, 12.5f+Y, 0), new Vector3(4f, 0.42f, 1), vPb, vPt);
        Ice("Ice_V2d", new Vector3(35, 12.93f+Y,0), new Vector3(3.8f,0.10f, 1)); // obsidian crust
        Plt("Plt_V2e", new Vector3(38, 15.5f+Y, 0), new Vector3(3.5f,0.42f, 1), vPb, vPt);
        Spk("Spk_V2e", new Vector3(38, 15.96f+Y,0), new Vector3(1.8f,0.18f, 1), 1.0f,3.5f,0.6f);
        Mov("Mov_V2f", new Vector3(41, 18.5f+Y, 0), new Vector3(3.5f,0.40f, 1), vPb, vPt, 2.0f, false);
        Gnd("Gnd_V2top", new Vector3(36, 21.5f+Y,0), new Vector3(14, 1, 1), vZ2);
        Cnv("Cnv_V2",    new Vector3(40, 22.05f+Y,0), new Vector3(6f, 0.15f, 1), -5f); // LEFT push
        Wll("Wll_V2R", new Vector3(44.5f,12+Y,  0), new Vector3(0.8f,24, 1));
        CP("CP_V2",    new Vector3(38,   23+Y,   0));

        // Z3 — Bridge of Magma (← LEFT)
        Wnd("Wnd_V3a", new Vector3(27, 24.5f+Y, 0), new Vector3(3f, 5.5f, 1)); // steam vent
        Plt("Plt_V3a", new Vector3(21, 22+Y,    0), new Vector3(8f, 0.42f, 1), vPb, vPt);
        Plt("Plt_V3b", new Vector3( 9, 22+Y,    0), new Vector3(8f, 0.42f, 1), vPb, vPt);
        Dsh("Dsh_V3",  new Vector3( 9, 23.4f+Y, 0));
        Plt("Plt_V3c", new Vector3(-4, 22+Y,    0), new Vector3(6f, 0.42f, 1), vPb, vPt);
        Ice("Ice_V3c", new Vector3(-4, 22.43f+Y,0), new Vector3(5.5f,0.10f, 1)); // cooled lava
        CP("CP_V3",    new Vector3( 8, 23+Y,    0));

        // Z4 — Volcanic Climb (↑ UP)
        // Walls bottom raised to y=23.5+Y (above Z3 bridge at y=22+Y).
        Wll("Wll_V4L", new Vector3(-6, 37f+Y,   0), new Vector3(0.8f, 27f, 1));
        Wll("Wll_V4R", new Vector3( 6, 37f+Y,   0), new Vector3(0.8f, 27f, 1));
        Plt("Plt_V4a", new Vector3(-1.5f,25.5f+Y,0), new Vector3(4f, 0.42f, 1), vPb4, vPt4);
        Plt("Plt_V4b", new Vector3( 2,  28.5f+Y, 0), new Vector3(4f, 0.42f, 1), vPb4, vPt4);
        Spk("Spk_V4b", new Vector3( 2,  28.96f+Y,0), new Vector3(2.2f,0.18f,1), 1.0f,3.5f,0.2f);
        Plt("Plt_V4c", new Vector3(-1.5f,31.5f+Y,0), new Vector3(4f, 0.42f, 1), vPb4, vPt4);
        Wnd("Wnd_V4",  new Vector3( 0,  35.5f+Y, 0), new Vector3(6f, 5.5f, 1)); // volcanic updraft
        Plt("Plt_V4d", new Vector3( 2,  36+Y,    0), new Vector3(4f, 0.42f, 1), vPb4, vPt4);
        Ice("Ice_V4d", new Vector3( 2,  36.43f+Y,0), new Vector3(3.8f,0.10f,1));
        Plt("Plt_V4e", new Vector3(-1.5f,39.5f+Y,0), new Vector3(4f, 0.42f, 1), vPb4, vPt4);
        Dsh("Dsh_V4",  new Vector3(-1.5f,41+Y,   0));
        Lgz("Lgz_V4",  new Vector3( 0,  42.5f+Y, 0), new Vector3(14f, 9f,   1)); // low-grav upper climb
        Plt("Plt_V4f", new Vector3( 2,  43.5f+Y, 0), new Vector3(4f, 0.42f, 1), vPb4, vPt4);
        Plt("Plt_V4g", new Vector3(-1.5f,46.5f+Y,0), new Vector3(4f, 0.42f, 1), vPb4, vPt4);
        CP("CP_V4",    new Vector3(0,   48+Y,    0));

        // Z5 — Lava Highway (→ RIGHT, y≈46.5)
        Gnd("Gnd_V5entry", new Vector3(6.5f,46.5f+Y,0), new Vector3(7f, 0.8f, 1), vZ4);
        Plt("Plt_V5a", new Vector3(15, 46.5f+Y, 0), new Vector3(5f, 0.42f, 1), vPb4, vPt4);
        Spd("Spd_V5a", new Vector3(15, 46.96f+Y,0), new Vector3(3.5f,0.22f, 1));
        Crm("Plt_V5b", new Vector3(24, 46.5f+Y, 0), new Vector3(5f, 0.42f, 1), vPb4, vPt4); // crumbles!
        Spk("Spk_V5b", new Vector3(24, 46.96f+Y,0), new Vector3(2.5f,0.18f,1), 1.0f,3.5f,0.4f);
        Dsh("Dsh_V5",  new Vector3(24, 48+Y,    0));
        Mov("Mov_V5c", new Vector3(31, 46.5f+Y, 0), new Vector3(3.5f,0.40f, 1), vPb4, vPt4, 2.2f, false);
        Plt("Plt_V5d", new Vector3(39, 46.5f+Y, 0), new Vector3(5f, 0.42f, 1), vPb4, vPt4);
        Spd("Spd_V5d", new Vector3(39, 46.96f+Y,0), new Vector3(3.5f,0.22f, 1));
        Wnd("Wnd_V5",  new Vector3(42.5f,49+Y,  0), new Vector3(2.5f,4f,   1)); // rescue updraft
        Mov("Mov_V5e", new Vector3(47, 46.5f+Y, 0), new Vector3(3.5f,0.40f, 1), vPb4, vPt4, 1.8f, true);
        Gnd("Gnd_V5end", new Vector3(55, 46.5f+Y,0), new Vector3(8f, 0.8f, 1), vZ5);
        Spd("Spd_V5end", new Vector3(54, 46.96f+Y,0), new Vector3(4f,0.22f, 1));
        Tpt("Tpt_V5",    new Vector3(58, 46.88f+Y, 0), new Vector3(79f, 11.5f+Y, 0)); // shortcut → Z6
        CP("CP_V5",    new Vector3(55, 48+Y,    0));

        // Z6 — Lava Fall (↓ DOWN)
        Crm("Plt_V6a",  new Vector3(61, 40+Y,   0), new Vector3(4.5f,0.42f, 1), vPb6, vPt6); // crumbles!
        Plt("Plt_V6b", new Vector3(65, 34+Y,   0), new Vector3(4.5f,0.42f, 1), vPb6, vPt6);
        Spk("Spk_V6b", new Vector3(65, 34.46f+Y,0), new Vector3(2f, 0.18f, 1), 1.0f,3.5f,0.0f);
        Plt("Plt_V6c", new Vector3(70, 28+Y,   0), new Vector3(4.5f,0.42f, 1), vPb6, vPt6);
        Plt("Plt_V6d", new Vector3(74, 22+Y,   0), new Vector3(4.5f,0.42f, 1), vPb6, vPt6);
        Bnc("Bnc_V6",  new Vector3(73, 22.32f+Y,0), new Vector3(2.5f,0.30f, 1));
        Gnd("Gnd_V6",  new Vector3(78, 10+Y,   0), new Vector3(10f, 1, 1), vZ6);
        CP("CP_V6",    new Vector3(77, 11.5f+Y, 0));

        // Z7 — Crimson Sprint (→ RIGHT + ↑ UP to FINISH)
        Gnd("Gnd_V7",  new Vector3(84, 10+Y,   0), new Vector3(10f, 1, 1), vZ6);
        Spd("Spd_V7a", new Vector3(81, 10.5f+Y,0), new Vector3(2.5f,0.22f, 1));
        Spd("Spd_V7b", new Vector3(85, 10.5f+Y,0), new Vector3(2.5f,0.22f, 1));
        Spk("Spk_V7",  new Vector3(83, 10.96f+Y,0), new Vector3(2f, 0.18f, 1), 1.0f,3.5f,0.1f);
        Plt("Plt_V7a", new Vector3(84, 14+Y,   0), new Vector3(4f, 0.42f, 1), vPb6, vPt6);
        Plt("Plt_V7b", new Vector3(79, 18+Y,   0), new Vector3(4f, 0.42f, 1), vPb6, vPt6);
        Plt("Plt_V7c", new Vector3(84, 22+Y,   0), new Vector3(4f, 0.42f, 1), vPb6, vPt6);
        Gnd("Gnd_V7top", new Vector3(90,26+Y,  0), new Vector3(10f, 1, 1), vZ4);
        Ice("Ice_V7top", new Vector3(90,26.47f+Y,0), new Vector3(9f, 0.10f, 1));
        Spd("Spd_V7top", new Vector3(88,26.5f+Y,0), new Vector3(3f, 0.22f, 1));

        // Finish line
        var fl1 = new GameObject("Fin");
        fl1.transform.position   = new Vector3(96f, 14+Y, 0);
        fl1.transform.localScale = new Vector3(0.5f, 28f, 1f);
        fl1.transform.SetParent(_trackParent, true);
        fl1.AddComponent<SpriteRenderer>().sprite = MakeChecker();
        fl1.GetComponent<SpriteRenderer>().sortingOrder = 3;
        var fl1c = fl1.AddComponent<BoxCollider2D>();
        fl1c.isTrigger = true;
        fl1.AddComponent<FinishLine>();

        // ── Wide floor after finish (Map 1) ───────────────────────────────
        Gnd("Gnd_FinFloor", new Vector3(103f, 26f + Y, 0), new Vector3(18f, 1f, 1), Z4_GND);

        // Kill zones auto-calculated from actual geometry bounds + padding
        BuildBoundaryKillZones(_container1, "KZ_V_", 18f);

        _trackParent = null;
    }

    // ── Waypoints for Map 1 (Volcano Rush) — mirrors Map 0 + Y offset ──
    static void BuildWaypoints1()
    {
        const float Y = 55f;
        var go = new GameObject("Waypoints1");
        if (_container1 != null) go.transform.SetParent(_container1.transform, true);
        var wp = go.AddComponent<WaypointPath>();
        wp.points = new Vector3[]
        {
            new Vector3(-12,  -2+Y,    0), new Vector3( -5,  -2+Y,    0),
            new Vector3(  4,  -2+Y,    0), new Vector3( 10,  -2+Y,    0),
            new Vector3( 14,   0+Y,    0), new Vector3( 21,  -2+Y,    0),
            new Vector3( 23,   4+Y,    0), new Vector3( 27,   7+Y,    0),
            new Vector3( 31,  10+Y,    0), new Vector3( 35,  13+Y,    0),
            new Vector3( 38,  16+Y,    0), new Vector3( 41,  19+Y,    0),
            new Vector3( 36,  22+Y,    0), new Vector3( 21,  22.5f+Y, 0),
            new Vector3(  9,  22.5f+Y, 0), new Vector3( -4,  22.5f+Y, 0),
            new Vector3(-1.5f,26+Y,    0), new Vector3(  2,  29+Y,    0),
            new Vector3(-1.5f,32+Y,    0), new Vector3(  2,  36.5f+Y, 0),
            new Vector3(-1.5f,40+Y,    0), new Vector3(  2,  44+Y,    0),
            new Vector3(-1.5f,47+Y,    0), new Vector3(6.5f, 47.3f+Y, 0),
            new Vector3( 15,  47+Y,    0), new Vector3( 24,  47+Y,    0),
            new Vector3( 31,  47+Y,    0), new Vector3( 39,  47+Y,    0),
            new Vector3( 47,  47+Y,    0), new Vector3( 55,  47.3f+Y, 0),
            new Vector3( 61,  40.5f+Y, 0), new Vector3( 65,  34.5f+Y, 0),
            new Vector3( 70,  28.5f+Y, 0), new Vector3( 74,  22.5f+Y, 0),
            new Vector3( 78,  10.5f+Y, 0), new Vector3( 81,  10.5f+Y, 0),
            new Vector3( 85,  10.5f+Y, 0), new Vector3( 84,  14.5f+Y, 0),
            new Vector3( 79,  18.5f+Y, 0), new Vector3( 84,  22.5f+Y, 0),
            new Vector3( 90,  26.5f+Y, 0), new Vector3( 96,  26.5f+Y, 0),
        };
    }

    // ── Players ────────────────────────────────────────────────
    static void SpawnPlayers()
    {
        Color[] cols = {
            new Color(.95f,.22f,.22f), new Color(.22f,.85f,.22f),
            new Color(1.0f,.60f,.10f), new Color(.72f,.18f,.88f),
            new Color(.15f,.85f,.85f), new Color(1.0f,.35f,.70f),
            new Color(1.0f,.90f,.18f)
        };

        var botSlots = new List<GameObject>();

        for (int i = 0; i < 7; i++)
        {
            var bot = MakeChar("Bot_"+(i+1), new Vector3(-13f+i*1.2f,-1.5f,0), cols[i], false);

            // Floating name tag (top-level, follows bot in world space)
            // Name = "NTag_" + bot.name so runtime Start() can resolve by convention
            var ntagGO = new GameObject("NTag_" + bot.name);
            ntagGO.transform.position = bot.transform.position + new Vector3(0, -0.9f, -0.1f);
            var ntm = ntagGO.AddComponent<TextMesh>();
            ntm.text = "Bot "+(i+1);
            ntm.fontSize = 32;
            ntm.characterSize = 0.10f;
            ntm.anchor = TextAnchor.MiddleCenter;
            ntm.alignment = TextAlignment.Center;
            ntm.color = cols[i];
            var nmr = ntagGO.GetComponent<MeshRenderer>();
            if (nmr) nmr.sortingOrder = 5;
            var npnt = ntagGO.AddComponent<PlayerNameTag>();
            npnt.InitFollow(bot.transform);

            // AI component
            var ai = bot.AddComponent<AIPlayer>();
            ai.groundLayer = LayerMask.GetMask("Ground");
            ai.canMove = false;
            bot.AddComponent<PlayerTrail>();

            // Pre-add PlayerController (disabled) so a joining client can take over this slot
            var bpc = bot.AddComponent<PlayerController>();
            var bgc = new GameObject("GroundCheck");
            bgc.transform.SetParent(bot.transform);
            bgc.transform.localPosition = new Vector3(0, -0.55f, 0);
            bpc.groundCheck = bgc.transform;
            bpc.groundLayer = LayerMask.GetMask("Ground");
            bpc.canControl  = false;
            bpc.enabled     = false;  // only enabled when a human takes this slot

            // Network sync
            bot.AddComponent<NetworkObject>();
            bot.AddComponent<NetworkSync>();

            // Dash cooldown bar above bot
            AttachDashBar("Bot_" + (i + 1), bot.transform, bpc);

            botSlots.Add(bot);
        }

        // Host's own player
        var player = MakeChar("Player", new Vector3(-13.5f,-1.5f,0), new Color(.20f,.55f,1f), true);

        // Floating name tag for host
        var pntagGO = new GameObject("NTag_Player");
        pntagGO.transform.position = player.transform.position + new Vector3(0, -0.9f, -0.1f);
        var ptm = pntagGO.AddComponent<TextMesh>();
        ptm.text = "";  // filled at runtime by NetworkLobbyManager
        ptm.fontSize = 32;
        ptm.characterSize = 0.10f;
        ptm.anchor = TextAnchor.MiddleCenter;
        ptm.alignment = TextAlignment.Center;
        ptm.color = new Color(.20f, .55f, 1f);
        var pmr = pntagGO.GetComponent<MeshRenderer>();
        if (pmr) pmr.sortingOrder = 5;
        var ppnt = pntagGO.AddComponent<PlayerNameTag>();
        ppnt.InitFollow(player.transform);
        var pc = player.AddComponent<PlayerController>();
        var gc = new GameObject("GroundCheck");
        gc.transform.SetParent(player.transform);
        gc.transform.localPosition = new Vector3(0,-0.55f,0);
        pc.groundCheck = gc.transform;
        pc.groundLayer = LayerMask.GetMask("Ground");
        pc.canControl = false;
        player.AddComponent<PlayerTrail>();
        player.AddComponent<NetworkObject>();
        var hostSync = player.AddComponent<NetworkSync>();

        // Dash cooldown bar above host player
        AttachDashBar("Player", player.transform, pc);

        var cf = Camera.main?.GetComponent<CameraFollow>();
        if (cf) cf.target = player.transform;

        // Tell NetworkLobbyManager about the available bot slots
        // (called after NetworkManager is created in CreateManagers)
        // We store them in a static list picked up later
        _pendingBotSlots = botSlots;
        _hostSync = hostSync;
    }

    // Temporary storage so CreateManagers can register slots with NetworkLobbyManager
    static List<GameObject> _pendingBotSlots;
    static NetworkSync      _hostSync;

    static GameObject MakeChar(string name, Vector3 pos, Color color, bool isHuman)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.88f, 1.38f, 1f);
        go.layer = playerLayer;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSprite();
        sr.color  = color;
        sr.sortingOrder = 2;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 3.6f;
        var col = go.AddComponent<BoxCollider2D>();
        // Zero friction — prevents player from sticking to walls instead of sliding
        var mat = new PhysicsMaterial2D("PlayerMat");
        mat.friction   = 0f;
        mat.bounciness = 0f;
        col.sharedMaterial = mat;
        var rp = go.AddComponent<RacePlayer>();
        rp.isHuman = isHuman;
        return go;
    }

    // ── Managers ───────────────────────────────────────────────
    static void CreateManagers()
    {
        new GameObject("LocalizationManager").AddComponent<LocalizationManager>();

        // Map manager — tracks which map container is active
        var mapMgrGO = new GameObject("MapManager");
        var mapMgr = mapMgrGO.AddComponent<MapManager>();
        mapMgr.container0 = _container0;
        mapMgr.container1 = _container1;
        _container0 = null; _container1 = null; // clear after use

        var rm = new GameObject("RaceManager");
        var r = rm.AddComponent<RaceManager>();
        r.roundDuration = 75f;
        r.eliminatePerRound = 2;
        var sm = new GameObject("ScoreManager");
        sm.AddComponent<ScoreManager>();
        var dm = new GameObject("DifficultyManager");
        dm.AddComponent<DifficultyManager>();
        var mm = new GameObject("MainMenuManager");
        mm.AddComponent<MainMenuManager>();
        var gs = new GameObject("GameSettings");
        gs.AddComponent<GameSettings>();
        new GameObject("WorldThemeManager").AddComponent<WorldThemeManager>();
        var pm = new GameObject("PauseManager");
        pm.AddComponent<PauseManager>();

        // ── NetworkManager (Unity NGO) ──────────────────────────────────────
        var nmGO = new GameObject("NetworkManager");
        var nm   = nmGO.AddComponent<NetworkManager>();
        var utp  = nmGO.AddComponent<UnityTransport>();
        nm.NetworkConfig = new Unity.Netcode.NetworkConfig
        {
            NetworkTransport = utp,
            TickRate         = 30,
        };
        // NetworkLobbyManager needs its OWN GameObject + NetworkObject
        // (NetworkBehaviour components must NOT live on the NetworkManager object)
        var discGO = new GameObject("LanDiscovery");
        discGO.AddComponent<LanDiscovery>();

        var lobbyGO = new GameObject("NetworkLobbyManager");
        lobbyGO.AddComponent<NetworkObject>();
        var lobby = lobbyGO.AddComponent<NetworkLobbyManager>();

        // Register bot slots (created by SpawnPlayers before CreateManagers runs)
        if (_pendingBotSlots != null)
        {
            lobby.RegisterBotSlots(_pendingBotSlots);
            _pendingBotSlots = null;
        }
        _hostSync = null;
    }

    // ── UI ─────────────────────────────────────────────────────
    static void CreateUI()
    {
        // EventSystem is required for Button clicks — create it if missing
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // Project uses New Input System — must use InputSystemUIInputModule, not StandaloneInputModule
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        var cgo = new GameObject("GameCanvas");
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = cgo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        cgo.AddComponent<GraphicRaycaster>();

        // MenuAnimator lives on the canvas so it can run coroutines on panels
        cgo.AddComponent<MenuAnimator>();

        var ui = cgo.AddComponent<UIManager>();
        // HUD pills — dark chips floating in corners, accent left stripe
        ui.playerCountText = HudChip(cgo.transform,"Players","Players:8",
            new Vector2(0,1), new Vector2(14,-14), new Vector2(220,46), 28, UI_CYAN);
        ui.roundText       = HudChip(cgo.transform,"Round","Round 1",
            new Vector2(.5f,1), new Vector2(0,-14), new Vector2(240,46), 28, UI_GOLD);
        ui.timerText       = HudChip(cgo.transform,"Timer","75s",
            new Vector2(1,1), new Vector2(-14,-14), new Vector2(120,46), 30, UI_ACCENT);
        ui.positionText    = HudChip(cgo.transform,"Pos","1st",
            new Vector2(1,1), new Vector2(-14,-68), new Vector2(120,40), 26, UI_GREEN);

        // Countdown overlay — full-screen dim + giant number
        var cdp = Pan(cgo.transform,"CDP", new Color(0f,0f,0.04f,0.72f), Vector2.zero, Vector2.one);
        // Glow ring behind number
        var cdGlow = Pan(cdp.transform,"CDGlow", new Color(UI_ACCENT.r*.12f,UI_ACCENT.g*.12f,UI_ACCENT.b*.12f,1f),
                         new Vector2(.5f,.5f), new Vector2(.5f,.5f));
        cdGlow.GetComponent<RectTransform>().sizeDelta = new Vector2(320,320);
        // Outer ring
        var cdRing = Pan(cdp.transform,"CDRing", new Color(UI_ACCENT.r,UI_ACCENT.g,UI_ACCENT.b,0.18f),
                         new Vector2(.5f,.5f), new Vector2(.5f,.5f));
        cdRing.GetComponent<RectTransform>().sizeDelta = new Vector2(360,360);
        ui.countdownPanel = cdp;
        var cdT = T(cdp.transform,"CDT","3", new Vector2(.5f,.5f), Vector2.zero, new Vector2(300,200), 140, TextAnchor.MiddleCenter);
        cdT.color = UI_TEXT_PRI;
        var cdOl = cdT.gameObject.AddComponent<Outline>();
        cdOl.effectColor = new Color(UI_ACCENT.r,UI_ACCENT.g,UI_ACCENT.b,0.6f);
        cdOl.effectDistance = new Vector2(4,-4);
        ui.countdownText = cdT;
        cdp.SetActive(false);

        // Message banner — floating card at center
        var mp = Pan(cgo.transform,"MsgP", UI_BG_CARD, new Vector2(.5f,.5f), new Vector2(.5f,.5f));
        var mpRt = mp.GetComponent<RectTransform>();
        mpRt.sizeDelta = new Vector2(700,100);
        // Accent stripe at top of message
        var msgLine = Pan(mp.transform,"MsgLine", UI_ACCENT, new Vector2(.5f,1f), new Vector2(1f,1f));
        msgLine.GetComponent<RectTransform>().sizeDelta = new Vector2(700,3);
        msgLine.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        // Left glow bar
        var msgBar = Pan(mp.transform,"MsgBar", new Color(UI_ACCENT.r,UI_ACCENT.g,UI_ACCENT.b,0.6f),
                         new Vector2(0,0), new Vector2(0,1));
        var mbRt = msgBar.GetComponent<RectTransform>();
        mbRt.offsetMin = Vector2.zero; mbRt.offsetMax = new Vector2(4,0);
        ui.messagePanel = mp;
        ui.messageText = T(mp.transform,"MsgT","", new Vector2(.5f,.5f), Vector2.zero, new Vector2(660,90), 48, TextAnchor.MiddleCenter);
        ui.messageText.color = UI_TEXT_PRI;
        ui.messageText.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        mp.SetActive(false);

        // ── Difficulty selection panel ──────────────────────────
        ui.difficultyPanel = BuildDifficultyPanel(cgo.transform);
        ui.difficultyPanel.SetActive(false);

        // ── Main menu ───────────────────────────────────────────
        ui.mainMenuPanel = BuildMainMenuPanel(cgo.transform);
        ui.mainMenuPanel.SetActive(false); // shown by RaceManager.FindPlayers()

        // ── Settings panel ──────────────────────────────────────
        ui.settingsPanel = BuildSettingsPanel(cgo.transform);
        ui.settingsPanel.SetActive(false);

        // ── End screen ──────────────────────────────────────────
        var (endPanel, endTitle, endSub) = BuildEndPanel(cgo.transform);
        ui.endPanel     = endPanel;
        ui.endTitleText = endTitle;
        ui.endSubText   = endSub;
        endPanel.SetActive(false);

        // ── Pause menu ──────────────────────────────────────────
        ui.pausePanel = BuildPausePanel(cgo.transform);
        ui.pausePanel.SetActive(false);

        // ── Lobby panel ─────────────────────────────────────────
        ui.lobbyPanel = BuildLobbyPanel(cgo.transform);
        ui.lobbyPanel.SetActive(false);

        // ── Spectator overlay ────────────────────────────────────
        {
            var specMgrGO = new GameObject("SpectatorController");
            var sc2 = specMgrGO.AddComponent<SpectatorController>();

            // Bottom-center floating bar — appears when local player finishes
            var specPan = Pan(cgo.transform, "SpectatorPanel",
                new Color(0f, 0f, 0.04f, 0.78f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            var spRt = specPan.GetComponent<RectTransform>();
            spRt.sizeDelta        = new Vector2(560, 70);
            spRt.anchoredPosition = new Vector2(0, 30);

            Button MakeSpecBtn(string bname, string icon, float anchorX, float posX)
            {
                var bgo  = new GameObject(bname);
                bgo.transform.SetParent(specPan.transform, false);
                var bImg = bgo.AddComponent<Image>();
                bImg.color = UI_ACCENT;
                var bBtn = bgo.AddComponent<Button>();
                bBtn.targetGraphic = bImg;
                var bRt = bgo.GetComponent<RectTransform>();
                bRt.anchorMin = bRt.anchorMax = bRt.pivot = new Vector2(anchorX, 0.5f);
                bRt.anchoredPosition = new Vector2(posX, 0);
                bRt.sizeDelta = new Vector2(70, 60);
                T(bgo.transform, "L", icon, new Vector2(0.5f, 0.5f), Vector2.zero,
                  new Vector2(70, 60), 38, TextAnchor.MiddleCenter).color = Color.white;
                return bBtn;
            }

            var prevBtn2 = MakeSpecBtn("SP_Prev", "◀", 0f,  38f);
            prevBtn2.onClick.AddListener(() => sc2.OnPrevPressed());

            var specLabel = T(specPan.transform, "SP_Label", "👁 WATCHING: ...",
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 60), 28, TextAnchor.MiddleCenter);
            specLabel.color = UI_TEXT_PRI;

            var nextBtn2 = MakeSpecBtn("SP_Next", "▶", 1f, -38f);
            nextBtn2.onClick.AddListener(() => sc2.OnNextPressed());

            sc2.prevBtn    = prevBtn2;
            sc2.nextBtn    = nextBtn2;
            sc2.watchLabel = specLabel;
            sc2.panel      = specPan;
            specPan.SetActive(false);
        }

        // ── Wire localization keys to all static UI texts ────────
        PostLocalize(cgo.transform);

        // ── UI Toolkit layer (parallel to legacy UGUI) ────────────
        BuildUIToolkit();

        // ── Touch controls (on-screen buttons for iOS/iPad) ──────
        var tcGO = new GameObject("TouchControls");
        tcGO.AddComponent<TouchControlsOverlay>();

        // ── Debug overlay — top-left corner HUD (` to toggle) ────
        var dbgGO = new GameObject("DebugOverlay");
        dbgGO.AddComponent<DebugOverlay>();
    }

    static void BuildUIToolkit()
    {
        const string psPath   = "Assets/UI/PanelSettings/GamePanelSettings.asset";
        const string uiRootName = "UIToolkitRoot";

        // ── PanelSettings asset (create once, reuse) ──────────────
        var ps = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>(psPath);
        if (ps == null)
        {
            ps = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
            ps.scaleMode            = UnityEngine.UIElements.PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution  = new Vector2Int(1920, 1080);
            ps.screenMatchMode      = UnityEngine.UIElements.PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match                = 0.5f;
            ps.sortingOrder         = 10;
            AssetDatabase.CreateAsset(ps, psPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneSetup] Created PanelSettings at " + psPath);
        }

        // ── VisualTreeAsset (GameRoot.uxml) ───────────────────────
        const string uxmlPath = "Assets/UI/UXML/GameRoot.uxml";
        var vta = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>(uxmlPath);
        if (vta == null)
        {
            Debug.LogWarning("[SceneSetup] GameRoot.uxml not found at " + uxmlPath +
                             " — UI Toolkit layer skipped.");
            return;
        }

        // ── UIDocument GameObject ─────────────────────────────────
        var go = new GameObject(uiRootName);
        var doc = go.AddComponent<UnityEngine.UIElements.UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = vta;

        go.AddComponent<UIToolkitManager>();

        // ── Permanently remove legacy GameCanvas — UIToolkitManager is the UI layer ──
        // Using DestroyImmediate (not SetActive) so the old panels are truly gone from the scene.
        var legacyCanvas = GameObject.Find("GameCanvas");
        if (legacyCanvas != null)
        {
            DestroyImmediate(legacyCanvas);
            Debug.Log("[SceneSetup] Legacy GameCanvas destroyed — UIToolkitManager is now the sole UI.");
        }

        Debug.Log("[SceneSetup] UIToolkitRoot created with GameRoot.uxml");
    }

    // ── Attach LocalizedText to all static UI strings ──────────
    static void PostLocalize(Transform canvasRoot)
    {
        // Map: gameObject name → localization key
        var nameMap = new System.Collections.Generic.Dictionary<string, string>
        {
            // Main menu
            { "MM_Sub",              "menu.subtitle"    },
            { "MM_Play_Lbl",         "menu.play"        },
            { "MM_Multi_Lbl",        "menu.multiplayer" },
            { "MM_Settings_Lbl",     "menu.settings"    },
            // Settings
            { "ST_Title",            "settings.title"   },
            { "ST_Sub",              "settings.subtitle"},
            { "ST_Back_Lbl",         "settings.back"    },
            { "SR_Quality",          "settings.quality" },
            { "SR_Round Time",       "settings.roundtime"},
            { "SR_Bots Out / Round", "settings.elim"    },
            { "SR_Camera Shake",     "settings.shake"   },
            { "SR_Player Trails",    "settings.trails"  },
            { "SR_Debug Overlay",    "settings.debug"   },
            { "SR_Language",         "settings.language"},
            // Section headers (named "SH_<text>")
            { "SH_D I S P L A Y",         "settings.display"   },
            { "SH_G A M E P L A Y",        "settings.gameplay"  },
            { "SH_E X P E R I E N C E",   "settings.experience"},
            // Difficulty
            { "DiffSub",             "diff.select"      },
            // End screen
            { "EP_Again_Lbl",        "end.playagain"    },
            { "EP_Menu_Lbl",         "end.mainmenu"     },
            // Pause
            { "PAU_Title",           "pause.title"      },
            { "PAU_Resume_Lbl",      "pause.resume"     },
            { "PAU_Restart_Lbl",     "pause.restart"    },
            { "PAU_Menu_Lbl",        "pause.mainmenu"   },
            // Lobby
            { "LP_Title",            "lobby.title"      },
            { "LP_Sub",              "lobby.subtitle"   },
            { "LP_ConnHdr",          "lobby.connecthdr" },
            { "LP_Host_Lbl",         "lobby.host"       },
            { "LP_IpLbl",            "lobby.iplabel"    },
            { "LP_Join_Lbl",         "lobby.join"       },
            { "LP_ConnBack_Lbl",     "lobby.back"       },
            { "LP_RoomHdr",          "lobby.roomhdr"    },
            { "LP_ToggleBots_Lbl",   "lobby.togglebots" },
            { "LP_Start_Lbl",        "lobby.start"      },
            { "LP_RoomBack_Lbl",     "lobby.leave"      },
        };

        // Difficulty button descriptions, keyed by "parentName/childName"
        var parentChildMap = new System.Collections.Generic.Dictionary<string, string>
        {
            { "DiffBtn_EASY/D1",   "diff.easy.d1"   },
            { "DiffBtn_EASY/D2",   "diff.easy.d2"   },
            { "DiffBtn_NORMAL/D1", "diff.normal.d1" },
            { "DiffBtn_NORMAL/D2", "diff.normal.d2" },
            { "DiffBtn_HARD/D1",   "diff.hard.d1"   },
            { "DiffBtn_HARD/D2",   "diff.hard.d2"   },
            { "DiffBtn_ULTRA/D1",  "diff.ultra.d1"  },
            { "DiffBtn_ULTRA/D2",  "diff.ultra.d2"  },
        };

        foreach (var t in canvasRoot.GetComponentsInChildren<Text>(true))
        {
            string n = t.gameObject.name;

            if (nameMap.TryGetValue(n, out var key))
            {
                var lt = t.gameObject.AddComponent<LocalizedText>();
                lt.key = key;
                continue;
            }

            var parent = t.transform.parent;
            if (parent != null)
            {
                string pc = parent.gameObject.name + "/" + n;
                if (parentChildMap.TryGetValue(pc, out var pcKey))
                {
                    var lt = t.gameObject.AddComponent<LocalizedText>();
                    lt.key = pcKey;
                }
            }
        }
    }

    static GameObject BuildDifficultyPanel(Transform canvasRoot)
    {
        var panel = Pan(canvasRoot, "DiffPanel", UI_BG_DEEP, Vector2.zero, Vector2.one);

        // Top glow strip
        UIHLine(panel.transform, new Vector2(0.5f,1f), new Vector2(0,-1),
                new Vector2(1920,3), new Color(UI_GOLD.r,UI_GOLD.g,UI_GOLD.b,0.55f));

        // Title
        var title = T(panel.transform,"DiffTitle","GOOBER DASH",
                      new Vector2(0.5f,1f), new Vector2(0,-58), new Vector2(900,100), 90, TextAnchor.UpperCenter);
        title.color = UI_GOLD;
        var olD = title.gameObject.AddComponent<Outline>();
        olD.effectColor = new Color(0.6f,0.25f,0f,0.50f); olD.effectDistance = new Vector2(3,-3);

        // Subtitle
        var sub = T(panel.transform,"DiffSub","SELECT DIFFICULTY",
                    new Vector2(0.5f,1f), new Vector2(0,-164), new Vector2(700,46), 34, TextAnchor.UpperCenter);
        sub.color = UI_TEXT_SEC; sub.fontStyle = FontStyle.Normal;

        // 4 cards in 2×2 grid — dark surface with accent header
        var defs = new (int idx, string name, Color accent, Vector2 pos, string d1, string d2, string icon)[]
        {
            (0,"EASY",   UI_GREEN,  new Vector2(-230, 110), "Bots play like beginners",  "Falls off edges, gets stuck",         "●"),
            (1,"NORMAL", UI_ACCENT, new Vector2( 230, 110), "Balanced competition",      "Occasional fumbles — fair fight",     "◆"),
            (2,"HARD",   UI_ORANGE, new Vector2(-230,-110), "Sharp & experienced play",  "Rare mistakes, reads gaps early",     "▲"),
            (3,"ULTRA",  UI_RED,    new Vector2( 230,-110), "PERFECT professional play", "Never fails, uses every shortcut ☠", "★"),
        };

        foreach (var d in defs)
        {
            // Card outer glow
            var glow = new GameObject("DiffGlow_"+d.name);
            glow.transform.SetParent(panel.transform,false);
            glow.AddComponent<UnityEngine.UI.Image>().color = new Color(d.accent.r,d.accent.g,d.accent.b,0.18f);
            var grt = glow.GetComponent<RectTransform>();
            grt.anchorMin = grt.anchorMax = grt.pivot = new Vector2(0.5f,0.5f);
            grt.anchoredPosition = d.pos; grt.sizeDelta = new Vector2(408,204);

            // Card surface
            var btnGO = new GameObject("DiffBtn_"+d.name);
            btnGO.transform.SetParent(panel.transform,false);
            var img = btnGO.AddComponent<UnityEngine.UI.Image>();
            img.color = UI_BG_CARD;
            var btn = btnGO.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            var cols = btn.colors;
            cols.normalColor      = UI_BG_CARD;
            cols.highlightedColor = new Color(d.accent.r*0.22f, d.accent.g*0.22f, d.accent.b*0.22f, 1f);
            cols.pressedColor     = new Color(UI_BG_CARD.r*0.8f, UI_BG_CARD.g*0.8f, UI_BG_CARD.b*0.8f);
            cols.fadeDuration     = 0.08f;
            btn.colors = cols;
            var brt = btnGO.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f,0.5f);
            brt.anchoredPosition = d.pos; brt.sizeDelta = new Vector2(400,196);

            // Accent header stripe (full width, top)
            var hdr = new GameObject("Hdr"); hdr.transform.SetParent(btnGO.transform,false);
            hdr.AddComponent<UnityEngine.UI.Image>().color = new Color(d.accent.r,d.accent.g,d.accent.b,0.28f);
            var hrt = hdr.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0,1); hrt.anchorMax = new Vector2(1,1);
            hrt.offsetMin = new Vector2(0,-58); hrt.offsetMax = Vector2.zero;

            // Top accent line
            var line = new GameObject("Line"); line.transform.SetParent(btnGO.transform,false);
            line.AddComponent<UnityEngine.UI.Image>().color = d.accent;
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0,1); lrt.anchorMax = new Vector2(1,1);
            lrt.offsetMin = new Vector2(0,-3); lrt.offsetMax = Vector2.zero;

            // Icon + title side by side in header
            T(btnGO.transform,"Icon",d.icon,
              new Vector2(0f,1f), new Vector2(22,-30), new Vector2(38,44), 32, TextAnchor.MiddleLeft)
              .color = d.accent;
            var tn = T(btnGO.transform,"BtnTitle",d.name,
                       new Vector2(0f,1f), new Vector2(54,-12), new Vector2(300,56), 46, TextAnchor.UpperLeft);
            tn.fontStyle = FontStyle.Bold; tn.color = UI_TEXT_PRI;

            // Divider
            var div = new GameObject("Div"); div.transform.SetParent(btnGO.transform,false);
            var divImg = div.AddComponent<UnityEngine.UI.Image>();
            divImg.color = new Color(1,1,1,0.07f);
            var drt = div.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.04f,0.5f); drt.anchorMax = new Vector2(0.96f,0.5f);
            drt.offsetMin = new Vector2(0,12); drt.offsetMax = new Vector2(0,13);

            // Description lines
            var d1T = T(btnGO.transform,"D1",d.d1,
                        new Vector2(0.5f,0f), new Vector2(0,64), new Vector2(368,38), 26, TextAnchor.LowerCenter);
            d1T.color = UI_TEXT_PRI; d1T.fontStyle = FontStyle.Normal;

            var d2T = T(btnGO.transform,"D2",d.d2,
                        new Vector2(0.5f,0f), new Vector2(0,26), new Vector2(368,34), 22, TextAnchor.LowerCenter);
            d2T.color = UI_TEXT_SEC; d2T.fontStyle = FontStyle.Normal;

            var db = btnGO.AddComponent<DifficultyButton>();
            db.diffIndex = d.idx;
        }

        return panel;
    }

    // ── Main Menu Panel ────────────────────────────────────────
    static GameObject BuildMainMenuPanel(Transform root)
    {
        var panel = Pan(root, "MainMenuPanel", UI_BG_DEEP, Vector2.zero, Vector2.one);

        // Vignette — radial darkness from edges
        var vig = Pan(panel.transform, "MM_Vig",
                      new Color(0f, 0f, 0.04f, 0.55f), Vector2.zero, Vector2.one);

        // Top edge glow
        var topGlow = Pan(panel.transform, "MM_TopGlow",
                          new Color(UI_ACCENT.r, UI_ACCENT.g, UI_ACCENT.b, 0.07f),
                          new Vector2(0,1), new Vector2(1,1));
        var tgRt = topGlow.GetComponent<RectTransform>();
        tgRt.offsetMin = new Vector2(0,-90); tgRt.offsetMax = Vector2.zero;
        // Top accent stripe
        UIHLine(panel.transform, new Vector2(0.5f,1f), new Vector2(0,-1),
                new Vector2(1920,3), new Color(UI_ACCENT.r, UI_ACCENT.g, UI_ACCENT.b, 0.65f));

        // ── Title area ────────────────────────────────────────────────
        // Outer title glow (wide, very soft gold)
        var glowOuter = Pan(panel.transform,"MM_GlowO",
                            new Color(UI_GOLD.r*0.06f, UI_GOLD.g*0.04f, 0f, 0.9f),
                            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
        SetAP(glowOuter.GetComponent<RectTransform>(), new Vector2(0,195), new Vector2(1200,200));

        // Inner title glow (tighter, more orange)
        var glowInner = Pan(panel.transform,"MM_GlowI",
                            new Color(UI_GOLD.r*0.14f, UI_GOLD.g*0.08f, 0f, 1f),
                            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
        SetAP(glowInner.GetComponent<RectTransform>(), new Vector2(0,195), new Vector2(620,120));

        // Title text
        var titleT = T(panel.transform,"MM_Title","GOOBER DASH",
                       new Vector2(0.5f,1f), new Vector2(0,-60), new Vector2(1200,148), 120, TextAnchor.UpperCenter);
        titleT.color = UI_GOLD;
        var olT = titleT.gameObject.AddComponent<Outline>();
        olT.effectColor    = new Color(0.6f, 0.25f, 0f, 0.50f);
        olT.effectDistance = new Vector2(4,-4);

        // Gold dividers flanking subtitle
        UIHLine(panel.transform, new Vector2(0.5f,1f), new Vector2(-220,-218),
                new Vector2(160,1), new Color(UI_GOLD.r,UI_GOLD.g,UI_GOLD.b,0.35f));
        UIHLine(panel.transform, new Vector2(0.5f,1f), new Vector2(220,-218),
                new Vector2(160,1), new Color(UI_GOLD.r,UI_GOLD.g,UI_GOLD.b,0.35f));

        // Subtitle
        var subT = T(panel.transform,"MM_Sub","RACE  \u00b7  JUMP  \u00b7  WIN",
                     new Vector2(0.5f,1f), new Vector2(0,-226), new Vector2(700,38), 26, TextAnchor.UpperCenter);
        subT.color = UI_TEXT_SEC;
        subT.fontStyle = FontStyle.Normal;

        // ── Buttons (wider, more space between) ───────────────────────
        MakeAccentBtn(panel.transform,"MM_Play","\u25b6  P L A Y",
                      MenuButton.Action.Play, UI_GREEN,
                      new Vector2(0, 130), new Vector2(540,90));

        MakeAccentBtn(panel.transform,"MM_Multi","\u25c6  M U L T I P L A Y E R",
                      MenuButton.Action.Multiplayer, UI_CYAN,
                      new Vector2(0, 20), new Vector2(540,90));

        MakeAccentBtn(panel.transform,"MM_Settings","\u2699  S E T T I N G S",
                      MenuButton.Action.Settings, UI_ACCENT,
                      new Vector2(0,-90), new Vector2(540,90));

        // Bottom accent line
        UIHLine(panel.transform, new Vector2(0.5f,0f), new Vector2(0,1),
                new Vector2(1920,1), new Color(UI_ACCENT.r,UI_ACCENT.g,UI_ACCENT.b,0.12f));

        // Version watermark
        var ver = T(panel.transform,"MM_Ver", VERSION,
                    new Vector2(1f,0f), new Vector2(-24,18), new Vector2(420,24), 16, TextAnchor.LowerRight);
        ver.color = UI_TEXT_DIM;
        ver.fontStyle = FontStyle.Normal;

        return panel;
    }

    // ── Settings Panel ─────────────────────────────────────────
    static GameObject BuildSettingsPanel(Transform root)
    {
        var panel = Pan(root, "SettingsPanel", UI_BG_DEEP, Vector2.zero, Vector2.one);

        // Top accent line
        UIHLine(panel.transform, new Vector2(0.5f, 1f), new Vector2(0, -1),
                new Vector2(1920, 2), new Color(UI_ACCENT.r, UI_ACCENT.g, UI_ACCENT.b, 0.55f));

        // Title
        var titleT = T(panel.transform, "ST_Title", "SETTINGS",
                       new Vector2(0.5f, 1f), new Vector2(0, -72), new Vector2(700, 90),
                       74, TextAnchor.UpperCenter);
        titleT.color = UI_GOLD;
        var olS = titleT.gameObject.AddComponent<Outline>();
        olS.effectColor = new Color(0.75f, 0.35f, 0f, 0.40f);
        olS.effectDistance = new Vector2(2, -2);

        // Subtitle
        var st2 = T(panel.transform, "ST_Sub", "Customize your experience",
                    new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(600, 38),
                    24, TextAnchor.UpperCenter);
        st2.color = UI_TEXT_SEC;

        // ── DISPLAY card ── Quality + Theme
        var dispCard = MakeCard(panel.transform, "ST_Disp", new Vector2(0, 152), new Vector2(880, 148));
        CardTopLine(dispCard.transform);
        MakeSectionHeader(dispCard.transform, "D I S P L A Y", 44f);
        MakeSettingRow(dispCard.transform, "Quality",
                       new[] { "LOW", "MED", "HIGH", "ULTRA" },
                       SettingButton.SettingId.Quality, 8f, 88f, 44f);
        MakeSettingRow(dispCard.transform, "Theme",
                       new[] { "STANDARD", "B & W" },
                       SettingButton.SettingId.Theme, -36f, 140f, 44f);

        // ── GAMEPLAY card ── Round time + Elim
        var gameCard = MakeCard(panel.transform, "ST_Game", new Vector2(0, -4), new Vector2(880, 148));
        CardTopLine(gameCard.transform);
        MakeSectionHeader(gameCard.transform, "G A M E P L A Y", 44f);
        MakeSettingRow(gameCard.transform, "Round Time",
                       new[] { "45 s", "60 s", "75 s" },
                       SettingButton.SettingId.RoundTime, 8f, 116f, 44f);
        MakeSettingRow(gameCard.transform, "Bots Out / Round",
                       new[] { "1", "2", "3" },
                       SettingButton.SettingId.ElimPerRound, -36f, 116f, 44f);

        // ── EXPERIENCE card ── Shake + Trails + Language + Debug + Your Color
        var expCard = MakeCard(panel.transform, "ST_Exp", new Vector2(0, -196), new Vector2(880, 330));
        CardTopLine(expCard.transform);
        MakeSectionHeader(expCard.transform, "E X P E R I E N C E", 118f);
        MakeSettingRow(expCard.transform, "Camera Shake",
                       new[] { "ON", "OFF" },
                       SettingButton.SettingId.CameraShake, 82f, 154f, 44f);
        MakeSettingRow(expCard.transform, "Player Trails",
                       new[] { "ON", "OFF" },
                       SettingButton.SettingId.PlayerTrails, 38f, 154f, 44f);
        MakeSettingRow(expCard.transform, "Debug Overlay",
                       new[] { "ON", "OFF" },
                       SettingButton.SettingId.DebugOverlay, -6f, 154f, 44f);
        MakeSettingRow(expCard.transform, "Language",
                       new[] { "EN", "RU" },
                       SettingButton.SettingId.Language, -50f, 154f, 44f);

        // ── Color picker ── (added inside EXPERIENCE card) ────────────────────
        // Thin divider above color section
        var cpDiv = new GameObject("CP_Div"); cpDiv.transform.SetParent(expCard.transform, false);
        cpDiv.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);
        var cpDivRt = cpDiv.GetComponent<RectTransform>();
        cpDivRt.anchorMin = new Vector2(0.04f, 0.5f); cpDivRt.anchorMax = new Vector2(0.96f, 0.5f);
        cpDivRt.offsetMin = new Vector2(0, -100); cpDivRt.offsetMax = new Vector2(0, -99);

        // "Your Color" label
        var ycLbl = T(expCard.transform, "SR_YourColor", "Your Color",
                      new Vector2(0.5f, 0.5f), new Vector2(-100f, -114f), new Vector2(200f, 36f),
                      21, TextAnchor.MiddleRight);
        ycLbl.color = UI_TEXT_SEC;

        // 10 color swatches in a horizontal row (right of the label)
        Color[] swatchColors = {
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
        float swStart = 42f;  // x-start for first swatch, right of label
        for (int i = 0; i < swatchColors.Length; i++)
        {
            float sx = swStart + i * 50f;
            var sw = new GameObject("ColorSwatch_" + i);
            sw.transform.SetParent(expCard.transform, false);
            var swImg = sw.AddComponent<UnityEngine.UI.Image>();
            swImg.color = swatchColors[i];
            var swBtn = sw.AddComponent<Button>();
            swBtn.targetGraphic = swImg;
            var swBc = swBtn.colors;
            swBc.normalColor      = swatchColors[i];
            swBc.highlightedColor = new Color(
                Mathf.Min(swatchColors[i].r + 0.15f, 1f),
                Mathf.Min(swatchColors[i].g + 0.15f, 1f),
                Mathf.Min(swatchColors[i].b + 0.15f, 1f));
            swBc.pressedColor  = new Color(swatchColors[i].r * 0.70f,
                                           swatchColors[i].g * 0.70f,
                                           swatchColors[i].b * 0.70f);
            swBc.fadeDuration  = 0.06f;
            swBtn.colors = swBc;
            var swRt = sw.GetComponent<RectTransform>();
            swRt.anchorMin = swRt.anchorMax = swRt.pivot = new Vector2(0.5f, 0.5f);
            swRt.anchoredPosition = new Vector2(sx, -114f);
            swRt.sizeDelta = new Vector2(40f, 40f);
            var swOl = sw.AddComponent<Outline>();
            swOl.effectColor    = new Color(1f, 1f, 1f, 0.12f);
            swOl.effectDistance = new Vector2(1, -1);
            var cswb = sw.AddComponent<ColorSwatchButton>();
            cswb.colorIndex = i;
        }

        // ── CONTROLS card ── read-only reference
        var ctrlCard = MakeCard(panel.transform, "ST_Ctrl", new Vector2(0, -408), new Vector2(880, 72));
        CardTopLine(ctrlCard.transform);
        var ctrlData = new (string k, string d)[]
        {
            ("A  /  D",        "Move"),
            ("Space",          "Jump \u00d72"),
            ("Space + Wall",   "Wall Jump"),
            ("Shift",          "Dash"),
        };
        for (int i = 0; i < ctrlData.Length; i++)
        {
            float x = -312f + i * 208f;
            var kt = T(ctrlCard.transform, "CK" + i, ctrlData[i].k,
                       new Vector2(0.5f, 0.5f), new Vector2(x, 12f), new Vector2(190, 28),
                       19, TextAnchor.MiddleCenter);
            kt.color = new Color(1f, 0.88f, 0.28f);
            T(ctrlCard.transform, "CD" + i, ctrlData[i].d,
              new Vector2(0.5f, 0.5f), new Vector2(x, -14f), new Vector2(190, 24),
              16, TextAnchor.MiddleCenter).color = UI_TEXT_SEC;
        }

        // ← BACK
        MakeAccentBtn(panel.transform, "ST_Back", "\u2190   B A C K",
                      MenuButton.Action.SettingsBack, UI_RED,
                      new Vector2(0, -520), new Vector2(340, 76));

        return panel;
    }

    // ── End Screen Panel ───────────────────────────────────────
    static (GameObject panel, Text title, Text sub) BuildEndPanel(Transform root)
    {
        var panel = Pan(root, "EndPanel", UI_BG_DEEP, Vector2.zero, Vector2.one);

        // Top accent line
        UIHLine(panel.transform, new Vector2(0.5f, 1f), new Vector2(0, -1),
                new Vector2(1920, 2), new Color(UI_ACCENT.r, UI_ACCENT.g, UI_ACCENT.b, 0.55f));

        // Glow behind title
        var glow = Pan(panel.transform, "EP_Glow",
                       new Color(0.02f, 0.04f, 0.14f, 1f),
                       new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        SetAP(glow.GetComponent<RectTransform>(), new Vector2(0, 90), new Vector2(960, 320));

        // Stars
        for (int i = 0; i < 5; i++)
        {
            var s = T(panel.transform, "EP_Star" + i, "\u2605",
                      new Vector2(0.5f, 0.5f), new Vector2(-160f + i * 80f, 225f),
                      new Vector2(56, 56), 44, TextAnchor.MiddleCenter);
            s.color = new Color(1f, 0.82f, 0.1f, 0.5f);
        }

        // Result title (text + color set at runtime by UIManager)
        var titleT = T(panel.transform, "EP_Title", "GAME OVER",
                       new Vector2(0.5f, 0.5f), new Vector2(0, 122), new Vector2(1000, 148),
                       104, TextAnchor.MiddleCenter);
        titleT.fontStyle = FontStyle.Bold;
        var olE = titleT.gameObject.AddComponent<Outline>();
        olE.effectColor = new Color(0, 0, 0, 0.55f);
        olE.effectDistance = new Vector2(3, -3);

        // Thin divider
        UIHLine(panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 42),
                new Vector2(420, 1), new Color(1, 1, 1, 0.10f));

        // Sub message (set at runtime)
        var subT = T(panel.transform, "EP_Sub", "",
                     new Vector2(0.5f, 0.5f), new Vector2(0, 16), new Vector2(800, 60),
                     38, TextAnchor.MiddleCenter);
        subT.color = UI_TEXT_SEC;

        // Separator above buttons
        UIHLine(panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -74),
                new Vector2(540, 1), new Color(1, 1, 1, 0.08f));

        // ▶ PLAY AGAIN
        MakeAccentBtn(panel.transform, "EP_Again", "\u25b6   PLAY AGAIN",
                      MenuButton.Action.PlayAgain, UI_GREEN,
                      new Vector2(-185, -136), new Vector2(344, 86));

        // ⌂ MAIN MENU
        MakeAccentBtn(panel.transform, "EP_Menu", "\u2302   MAIN MENU",
                      MenuButton.Action.GoToMainMenu, UI_ACCENT,
                      new Vector2(185, -136), new Vector2(344, 86));

        return (panel, titleT, subT);
    }

    // ── Pause Menu Panel ───────────────────────────────────────
    static GameObject BuildPausePanel(Transform root)
    {
        // Full-screen dim with a subtle blue tint (more immersive than pure black)
        var panel = Pan(root, "PausePanel",
                        new Color(0f, 0.01f, 0.06f, 0.88f), Vector2.zero, Vector2.one);

        // Centered card
        var card = MakeCard(panel.transform, "PAU_Card",
                            new Vector2(0, 0), new Vector2(500, 420));
        CardTopLine(card.transform);

        // Pause icon ring
        var ring = Pan(card.transform,"PAU_Ring",
                       new Color(UI_GOLD.r*0.15f,UI_GOLD.g*0.10f,0f,1f),
                       new Vector2(0.5f,1f), new Vector2(0.5f,1f));
        var rrt = ring.GetComponent<RectTransform>();
        rrt.anchoredPosition = new Vector2(0,-30); rrt.sizeDelta = new Vector2(70,70);

        // Pause symbol "⏸"
        var pauseIcon = T(ring.transform,"PAU_Icon","\u23f8",
                          new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(60,60), 38, TextAnchor.MiddleCenter);
        pauseIcon.color = UI_GOLD;

        // Title
        var titleT = T(card.transform,"PAU_Title","PAUSED",
                       new Vector2(0.5f,1f), new Vector2(0,-106), new Vector2(420,72), 58, TextAnchor.UpperCenter);
        titleT.color = UI_GOLD;
        var ol = titleT.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0.6f,0.25f,0f,0.45f); ol.effectDistance = new Vector2(2,-2);

        // Thin divider
        UIHLine(card.transform, new Vector2(0.5f,1f), new Vector2(0,-182),
                new Vector2(380,1), new Color(1,1,1,0.07f));

        MakeAccentBtn(card.transform,"PAU_Resume","\u25b6  R E S U M E",
                      MenuButton.Action.Resume, UI_GREEN,
                      new Vector2(0, 78), new Vector2(430,76));

        MakeAccentBtn(card.transform,"PAU_Restart","\u21ba  R E S T A R T",
                      MenuButton.Action.RestartFromPause, UI_ACCENT,
                      new Vector2(0,-14), new Vector2(430,76));

        MakeAccentBtn(card.transform,"PAU_Menu","\u2302  M A I N   M E N U",
                      MenuButton.Action.MainMenuFromPause, UI_RED,
                      new Vector2(0,-106), new Vector2(430,76));

        return panel;
    }

    // ── Lobby Panel ────────────────────────────────────────────
    static GameObject BuildLobbyPanel(Transform canvasRoot)
    {
        var panel = Pan(canvasRoot, "LobbyPanel", UI_BG_DEEP, Vector2.zero, Vector2.one);

        // Top accent line
        UIHLine(panel.transform, new Vector2(0.5f, 1f), new Vector2(0, -1),
                new Vector2(1920, 2), new Color(UI_ACCENT.r, UI_ACCENT.g, UI_ACCENT.b, 0.55f));

        // Title
        var titleT = T(panel.transform, "LP_Title", "MULTIPLAYER",
                       new Vector2(0.5f, 1f), new Vector2(0, -72), new Vector2(700, 90),
                       74, TextAnchor.UpperCenter);
        titleT.color = UI_GOLD;
        var olLP = titleT.gameObject.AddComponent<Outline>();
        olLP.effectColor    = new Color(0.75f, 0.35f, 0f, 0.40f);
        olLP.effectDistance = new Vector2(2, -2);

        // Subtitle
        var sub = T(panel.transform, "LP_Sub", "LAN  \u00b7  LOCAL NETWORK",
                    new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(600, 38),
                    24, TextAnchor.UpperCenter);
        sub.color = UI_TEXT_SEC;

        // ── CONNECT VIEW ──────────────────────────────────────────────────────
        var connectView = new GameObject("LP_ConnectView");
        connectView.transform.SetParent(panel.transform, false);
        var cvRt = connectView.AddComponent<RectTransform>();
        cvRt.anchorMin = Vector2.zero; cvRt.anchorMax = Vector2.one;
        cvRt.offsetMin = cvRt.offsetMax = Vector2.zero;

        // Connect card — taller to fit server browser
        var connCard = MakeCard(connectView.transform, "LP_ConnCard", new Vector2(0, 0), new Vector2(640, 660));
        CardTopLine(connCard.transform);

        // Connect card title
        var connHdr = T(connCard.transform, "LP_ConnHdr", "M U L T I P L A Y E R",
                        new Vector2(0.5f, 0.5f), new Vector2(0, 298), new Vector2(580, 34),
                        22, TextAnchor.MiddleCenter);
        connHdr.color = new Color(0.55f, 0.70f, 0.95f);

        // ── Nickname ─────────────────────────────────────────────────────────
        var nickLbl = T(connCard.transform, "LP_NickLbl", "Y O U R   N A M E",
                        new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(580, 24),
                        14, TextAnchor.MiddleCenter);
        nickLbl.color = UI_TEXT_DIM;

        var nickInput = MakeInputField(connCard.transform, "LP_NickInput", "Player123",
                                       new Vector2(0, 218), new Vector2(480, 46));

        // ⌂ HOST GAME
        MakeAccentBtn(connCard.transform, "LP_Host", "\u2302   H O S T   G A M E",
                      MenuButton.Action.LobbyHost, UI_GREEN,
                      new Vector2(0, 154), new Vector2(540, 72));

        // ── LAN SERVERS section ───────────────────────────────────────────────
        UIHLine(connCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 106),
                new Vector2(560, 1), new Color(1, 1, 1, 0.06f));

        var lanHdr = T(connCard.transform, "LP_LanHdr", "L A N   S E R V E R S",
                       new Vector2(0.5f, 0.5f), new Vector2(0, 87), new Vector2(560, 22),
                       13, TextAnchor.MiddleCenter);
        lanHdr.color = UI_TEXT_DIM;

        // Server list container (dark background, holds dynamic rows)
        var svrListGO = new GameObject("LP_ServerList");
        svrListGO.transform.SetParent(connCard.transform, false);
        var slRt = svrListGO.AddComponent<RectTransform>();
        slRt.anchorMin = slRt.anchorMax = slRt.pivot = new Vector2(0.5f, 0.5f);
        slRt.sizeDelta        = new Vector2(580, 214);
        slRt.anchoredPosition = new Vector2(0, -24);
        svrListGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.22f);

        // "Searching..." shown when no servers discovered
        var searchTxt = T(connCard.transform, "LP_Searching",
                          "\u25cf  \u25cf  \u25cf   Searching for local games\u2026",
                          new Vector2(0.5f, 0.5f), new Vector2(0, -24), new Vector2(560, 214),
                          18, TextAnchor.MiddleCenter);
        searchTxt.color = UI_TEXT_DIM;

        // ── Direct-IP fallback ────────────────────────────────────────────────
        UIHLine(connCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -140),
                new Vector2(560, 1), new Color(1, 1, 1, 0.06f));

        var ipHdr = T(connCard.transform, "LP_IpHdr", "D I R E C T   I P",
                      new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(560, 20),
                      12, TextAnchor.MiddleCenter);
        ipHdr.color = UI_TEXT_DIM;

        // IP InputField (compact, sits left of JOIN)
        var ipInput = MakeInputField(connCard.transform, "LP_IpInput", "192.168.1.x",
                                     new Vector2(-80, -194), new Vector2(300, 42));

        // ▶ JOIN (inline with IP input)
        MakeAccentBtn(connCard.transform, "LP_Join", "\u25b6  J O I N",
                      MenuButton.Action.LobbyJoin, UI_ACCENT,
                      new Vector2(196, -194), new Vector2(148, 42));

        // ← BACK (below card)
        MakeAccentBtn(connectView.transform, "LP_ConnBack", "\u2190   B A C K",
                      MenuButton.Action.LobbyBack, UI_RED,
                      new Vector2(0, -440), new Vector2(340, 72));

        // ── ROOM VIEW ─────────────────────────────────────────────────────────
        var roomView = new GameObject("LP_RoomView");
        roomView.transform.SetParent(panel.transform, false);
        var rvRt = roomView.AddComponent<RectTransform>();
        rvRt.anchorMin = Vector2.zero; rvRt.anchorMax = Vector2.one;
        rvRt.offsetMin = rvRt.offsetMax = Vector2.zero;

        // Room card (taller to fit player list)
        var roomCard = MakeCard(roomView.transform, "LP_RoomCard", new Vector2(0, 10), new Vector2(640, 620));
        CardTopLine(roomCard.transform);

        // Room header
        var roomHdr = T(roomCard.transform, "LP_RoomHdr", "W A I T I N G   F O R   P L A Y E R S",
                        new Vector2(0.5f, 0.5f), new Vector2(0, 278), new Vector2(580, 34),
                        20, TextAnchor.MiddleCenter);
        roomHdr.color = new Color(0.55f, 0.70f, 0.95f);

        // IP display
        var ipDisplayText = T(roomCard.transform, "LP_IpDisplay", "Your IP:  ...",
                              new Vector2(0.5f, 0.5f), new Vector2(0, 228), new Vector2(560, 42),
                              26, TextAnchor.MiddleCenter);
        ipDisplayText.color = UI_TEXT_SEC;

        UIHLine(roomCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 196),
                new Vector2(480, 1), new Color(1, 1, 1, 0.06f));

        // Player count — compact label above list
        var playerCountText = T(roomCard.transform, "LP_PlayerCount", "Players:  1 / 8",
                                new Vector2(0.5f, 0.5f), new Vector2(0, 168), new Vector2(560, 32),
                                20, TextAnchor.MiddleCenter);
        playerCountText.color = UI_TEXT_SEC;

        // ── Player list container ─────────────────────────────────────────
        var plGO = new GameObject("LP_PlayerList");
        plGO.transform.SetParent(roomCard.transform, false);
        var plRt = plGO.AddComponent<RectTransform>();
        plRt.anchorMin = plRt.anchorMax = plRt.pivot = new Vector2(0.5f, 0.5f);
        plRt.sizeDelta        = new Vector2(580, 250);
        plRt.anchoredPosition = new Vector2(0, 38);
        plGO.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.15f);

        UIHLine(roomCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -88),
                new Vector2(480, 1), new Color(1, 1, 1, 0.06f));

        // Bots status
        var botsStatusText = T(roomCard.transform, "LP_BotsStatus", "Bots: ON",
                               new Vector2(0.5f, 0.5f), new Vector2(0, -116), new Vector2(480, 40),
                               26, TextAnchor.MiddleCenter);
        botsStatusText.color = UI_GREEN;

        UIHLine(roomCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -148),
                new Vector2(480, 1), new Color(1, 1, 1, 0.06f));

        // ⇄ TOGGLE BOTS (host only — hidden for clients at runtime)
        MakeAccentBtn(roomCard.transform, "LP_ToggleBots", "\u21c4   T O G G L E   B O T S",
                      MenuButton.Action.LobbyToggleBots, UI_GOLD,
                      new Vector2(0, -198), new Vector2(500, 66));
        var toggleBotsButton = roomCard.transform.Find("LP_ToggleBots")?.GetComponent<Button>();

        // CLIENT ONLY — replaces bots controls; hidden for host at runtime
        var clientWaitT = T(roomCard.transform, "LP_ClientWait", "Waiting for host to start\u2026",
                            new Vector2(0.5f, 0.5f), new Vector2(0, -220), new Vector2(500, 66),
                            22, TextAnchor.MiddleCenter);
        clientWaitT.color = UI_TEXT_SEC;

        // ▶ START GAME (host only)
        MakeAccentBtn(roomCard.transform, "LP_Start", "\u25b6   S T A R T   G A M E",
                      MenuButton.Action.LobbyStart, UI_GREEN,
                      new Vector2(0, -278), new Vector2(500, 78));
        var startButton = roomCard.transform.Find("LP_Start")?.GetComponent<Button>();

        // ← LEAVE (below card)
        MakeAccentBtn(roomView.transform, "LP_RoomBack", "\u2190   L E A V E",
                      MenuButton.Action.LobbyBack, UI_RED,
                      new Vector2(0, -440), new Vector2(340, 72));

        // ── Wire LobbyPanelController ─────────────────────────────────────────
        var lpc                 = panel.AddComponent<LobbyPanelController>();
        lpc.connectView         = connectView;
        lpc.roomView            = roomView;
        lpc.ipInput             = ipInput;
        lpc.nicknameInput       = nickInput;
        lpc.ipDisplayText       = ipDisplayText;
        lpc.playerCountText     = playerCountText;
        lpc.botsStatusText      = botsStatusText;
        lpc.toggleBotsButton    = toggleBotsButton;
        lpc.clientWaitText      = clientWaitT;
        lpc.startButton         = startButton;
        lpc.playerListContainer = plGO.transform;
        lpc.serverListContainer = svrListGO.transform;
        lpc.serverSearchText    = searchTxt;

        return panel;
    }

    // ── Onboarding / splash screen ─────────────────────────────
    // Shown once on first launch (or always until tapped). SplashController
    // hides it and shows MainMenu on first touch / tap.
    static void BuildOnboardingPanel(Transform canvasRoot)
    {
        var panel = Pan(canvasRoot, "OnboardingPanel", UI_BG_DEEP, Vector2.zero, Vector2.one);

        // ── Layer 1: deep space gradient illusion ──────────────────────────────
        // Bottom-fade: darker at the very bottom (ground)
        var groundFade = Pan(panel.transform,"OB_GroundFade",
                             new Color(0f, 0.01f, 0.03f, 0.80f),
                             new Vector2(0,0), new Vector2(1,0));
        var gfRt = groundFade.GetComponent<RectTransform>();
        gfRt.offsetMin = Vector2.zero; gfRt.offsetMax = new Vector2(0, 260);

        // Mid horizon glow (cyan)
        var horizGlow = Pan(panel.transform,"OB_HorizGlow",
                            new Color(UI_CYAN.r*0.04f, UI_CYAN.g*0.06f, UI_CYAN.b*0.08f, 1f),
                            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
        var hgRt = horizGlow.GetComponent<RectTransform>();
        hgRt.anchoredPosition = new Vector2(0,-120); hgRt.sizeDelta = new Vector2(2200,320);

        // ── Layer 2: grid lines (simulated race track perspective) ─────────────
        // Horizontal grid lines (5 lines perspective)
        float[] hLineYs = {-240f, -310f, -360f, -398f, -424f};
        float[] hLineWs = {1920f, 1200f, 700f, 380f, 160f};
        for (int i = 0; i < hLineYs.Length; i++)
        {
            var hLine = Pan(panel.transform, "OB_HLine"+i,
                            new Color(UI_CYAN.r, UI_CYAN.g, UI_CYAN.b, 0.06f + i*0.018f),
                            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
            var hlRt = hLine.GetComponent<RectTransform>();
            hlRt.anchoredPosition = new Vector2(0, hLineYs[i]);
            hlRt.sizeDelta = new Vector2(hLineWs[i], 1.5f);
        }
        // Vertical grid lines (vanishing point)
        float[] vLineXs = {-360f,-220f,-90f,0f,90f,220f,360f};
        float[] vLineT  = { 0.05f, 0.07f, 0.09f, 0.12f, 0.09f, 0.07f, 0.05f};
        for (int i = 0; i < vLineXs.Length; i++)
        {
            var vLine = Pan(panel.transform,"OB_VLine"+i,
                            new Color(UI_CYAN.r, UI_CYAN.g, UI_CYAN.b, vLineT[i]),
                            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
            var vlRt = vLine.GetComponent<RectTransform>();
            // Taper from wide at bottom to narrow at vanish point
            vlRt.anchoredPosition = new Vector2(vLineXs[i], -330f);
            vlRt.sizeDelta = new Vector2(1.5f, 260f);
        }

        // ── Layer 3: decorative "speed lines" coming from center ───────────────
        float[] angles  = { -30f, -18f, -8f, 0f, 8f, 18f, 30f };
        float[] lengths = {  340f, 280f, 200f, 160f, 200f, 280f, 340f };
        for (int i = 0; i < angles.Length; i++)
        {
            var sl = Pan(panel.transform, "OB_Spd"+i,
                         new Color(UI_ACCENT.r, UI_ACCENT.g, UI_ACCENT.b, 0.08f),
                         new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
            var slRt = sl.GetComponent<RectTransform>();
            slRt.anchoredPosition = new Vector2(0, 80f);
            slRt.sizeDelta = new Vector2(2f, lengths[i]);
            slRt.localRotation = UnityEngine.Quaternion.Euler(0, 0, angles[i]);
        }

        // ── Layer 4: accent glow rings (decorative) ────────────────────────────
        // Large outer ring
        var ringOuter = Pan(panel.transform,"OB_RingO",
                            new Color(UI_ACCENT.r, UI_ACCENT.g, UI_ACCENT.b, 0.04f),
                            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
        var roRt = ringOuter.GetComponent<RectTransform>();
        roRt.anchoredPosition = new Vector2(0, 100f); roRt.sizeDelta = new Vector2(900, 900);

        // Inner ring (tighter)
        var ringInner = Pan(panel.transform,"OB_RingI",
                            new Color(UI_CYAN.r, UI_CYAN.g, UI_CYAN.b, 0.06f),
                            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
        var riRt = ringInner.GetComponent<RectTransform>();
        riRt.anchoredPosition = new Vector2(0, 100f); riRt.sizeDelta = new Vector2(520, 520);

        // ── Layer 5: top accent stripe ──────────────────────────────────────────
        UIHLine(panel.transform, new Vector2(0.5f,1f), new Vector2(0,-1),
                new Vector2(1920,3), new Color(UI_ACCENT.r, UI_ACCENT.g, UI_ACCENT.b, 0.70f));

        // ── Layer 6: Title block ────────────────────────────────────────────────
        // Outer gold glow behind title
        var glowOuter = Pan(panel.transform,"OB_TitleGlowO",
                            new Color(UI_GOLD.r*0.08f, UI_GOLD.g*0.05f, 0f, 1f),
                            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
        SetAP(glowOuter.GetComponent<RectTransform>(), new Vector2(0,170), new Vector2(1400,200));

        // Inner gold glow
        var glowInner = Pan(panel.transform,"OB_TitleGlowI",
                            new Color(UI_GOLD.r*0.18f, UI_GOLD.g*0.10f, 0f, 1f),
                            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
        SetAP(glowInner.GetComponent<RectTransform>(), new Vector2(0,170), new Vector2(640,130));

        // Game title — large and bold
        var title = T(panel.transform,"OB_Title","GOOBER DASH",
                      new Vector2(0.5f,0.5f), new Vector2(0,170), new Vector2(1200,148),
                      120, TextAnchor.MiddleCenter);
        title.color = UI_GOLD;
        var olTitle = title.gameObject.AddComponent<Outline>();
        olTitle.effectColor = new Color(0.7f,0.28f,0f,0.55f);
        olTitle.effectDistance = new Vector2(4,-4);

        // Gold dividers flanking tagline
        UIHLine(panel.transform, new Vector2(0.5f,0.5f), new Vector2(-235,68),
                new Vector2(180,1), new Color(UI_GOLD.r,UI_GOLD.g,UI_GOLD.b,0.38f));
        UIHLine(panel.transform, new Vector2(0.5f,0.5f), new Vector2(235,68),
                new Vector2(180,1), new Color(UI_GOLD.r,UI_GOLD.g,UI_GOLD.b,0.38f));

        // Tagline
        var tag = T(panel.transform,"OB_Tag","RACE  ·  JUMP  ·  WIN",
                    new Vector2(0.5f,0.5f), new Vector2(0,64),
                    new Vector2(560,36), 24, TextAnchor.MiddleCenter);
        tag.color = UI_TEXT_SEC;
        tag.fontStyle = FontStyle.Normal;

        // ── Layer 7: Feature chips (3 game feature pills) ──────────────────────
        var featureData = new (string icon, string label, Color accent)[]
        {
            ("⚡", "FAST-PACED", UI_ORANGE),
            ("◆", "MULTIPLAYER", UI_CYAN),
            ("★", "RANKED BOTS",  UI_GREEN),
        };
        for (int i = 0; i < featureData.Length; i++)
        {
            float cx = -280f + i * 280f;
            var chip = Pan(panel.transform, "OB_Chip"+i,
                           new Color(0.03f, 0.06f, 0.14f, 0.78f),
                           new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
            var crt = chip.GetComponent<RectTransform>();
            crt.anchoredPosition = new Vector2(cx, -22f); crt.sizeDelta = new Vector2(236,60);

            // Left accent bar
            var cBar = Pan(chip.transform,"CBar",
                           new Color(featureData[i].accent.r, featureData[i].accent.g,
                                     featureData[i].accent.b, 1f),
                           new Vector2(0,0), new Vector2(0,1));
            cBar.GetComponent<RectTransform>().offsetMax = new Vector2(4,0);

            // Icon
            var ico = T(chip.transform,"Icon",featureData[i].icon,
                        new Vector2(0f,0.5f), new Vector2(16,0), new Vector2(36,44),
                        24, TextAnchor.MiddleLeft);
            ico.color = featureData[i].accent;

            // Label
            var lbl = T(chip.transform,"Lbl",featureData[i].label,
                        new Vector2(0f,0.5f), new Vector2(50,0), new Vector2(174,44),
                        19, TextAnchor.MiddleLeft);
            lbl.color = UI_TEXT_PRI; lbl.fontStyle = FontStyle.Bold;
        }

        // ── Layer 8: "TAP TO START" pulse prompt ────────────────────────────────
        // Subtle glow behind prompt
        var promptGlow = Pan(panel.transform,"OB_PromptGlow",
                             new Color(UI_ACCENT.r*0.06f, UI_ACCENT.g*0.06f, UI_ACCENT.b*0.10f, 1f),
                             new Vector2(0.5f,0f), new Vector2(0.5f,0f));
        var pgRt = promptGlow.GetComponent<RectTransform>();
        pgRt.anchoredPosition = new Vector2(0,80f); pgRt.sizeDelta = new Vector2(700,80);

        var tapT = T(panel.transform,"OB_Tap","TAP  TO  START",
                     new Vector2(0.5f,0f), new Vector2(0,62),
                     new Vector2(520,44), 30, TextAnchor.MiddleCenter);
        tapT.color = new Color(UI_TEXT_SEC.r, UI_TEXT_SEC.g, UI_TEXT_SEC.b, 0.75f);
        tapT.fontStyle = FontStyle.Normal;

        // Bottom accent line
        UIHLine(panel.transform, new Vector2(0.5f,0f), new Vector2(0,1),
                new Vector2(1920,1), new Color(UI_ACCENT.r, UI_ACCENT.g, UI_ACCENT.b, 0.12f));

        // Version watermark
        var ver = T(panel.transform,"OB_Ver", VERSION,
                    new Vector2(1f,0f), new Vector2(-20,18),
                    new Vector2(400,24), 15, TextAnchor.LowerRight);
        ver.color = UI_TEXT_DIM;
        ver.fontStyle = FontStyle.Normal;

        // ── SplashController drives the "tap to continue" → show MainMenu ──────
        panel.AddComponent<SplashController>();
    }

    // ── InputField ─────────────────────────────────────────────
    static InputField MakeInputField(Transform parent, string name, string placeholder,
                                      Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.10f, 0.20f, 1f);
        var ol = go.AddComponent<Outline>();
        ol.effectColor    = new Color(UI_ACCENT.r, UI_ACCENT.g, UI_ACCENT.b, 0.40f);
        ol.effectDistance = new Vector2(1, -1);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        // Placeholder
        var phGO = new GameObject("Placeholder"); phGO.transform.SetParent(go.transform, false);
        var phT  = phGO.AddComponent<Text>();
        phT.text = placeholder;
        phT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        phT.fontSize = 22; phT.alignment = TextAnchor.MiddleCenter;
        phT.color = new Color(0.40f, 0.50f, 0.65f, 0.75f); phT.fontStyle = FontStyle.Italic;
        var phRt = phGO.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(8, 0); phRt.offsetMax = new Vector2(-8, 0);

        // Text
        var tGO = new GameObject("Text"); tGO.transform.SetParent(go.transform, false);
        var tT  = tGO.AddComponent<Text>();
        tT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tT.fontSize = 22; tT.alignment = TextAnchor.MiddleCenter; tT.color = Color.white;
        var tRt = tGO.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(8, 0); tRt.offsetMax = new Vector2(-8, 0);

        var input = go.AddComponent<InputField>();
        input.textComponent = tT;
        input.placeholder   = phT;

        return input;
    }

    // ── Accent button: glow-bar design ──────────────────────────
    // ── Goober Dash–style button: full accent fill, centered bold text,
    //    bright top highlight + dark bottom edge for chunky 3-D depth.
    static void MakeAccentBtn(Transform parent, string name, string label,
                               MenuButton.Action action, Color accent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        // Shadow / drop-offset layer (slightly larger, darker, behind)
        var shadow = new GameObject(name + "_Shadow"); shadow.transform.SetParent(go.transform, false);
        shadow.AddComponent<Image>().color = new Color(accent.r * 0.28f, accent.g * 0.28f, accent.b * 0.28f, 0.80f);
        var shRt = shadow.GetComponent<RectTransform>();
        shRt.anchorMin = Vector2.zero; shRt.anchorMax = Vector2.one;
        shRt.offsetMin = new Vector2(0, -6); shRt.offsetMax = new Vector2(0, -6);

        // Bottom-edge depth band (darker stripe at the bottom, like a pressed pill)
        var bot = new GameObject(name + "_BotEdge"); bot.transform.SetParent(go.transform, false);
        bot.AddComponent<Image>().color = new Color(accent.r * 0.45f, accent.g * 0.45f, accent.b * 0.45f, 1f);
        var botRt = bot.GetComponent<RectTransform>();
        botRt.anchorMin = new Vector2(0,0); botRt.anchorMax = new Vector2(1,0);
        botRt.pivot = new Vector2(0.5f,0);
        botRt.sizeDelta = new Vector2(0, 8);
        botRt.anchoredPosition = Vector2.zero;

        // Main fill — full accent color
        var img = go.AddComponent<Image>();
        img.color = accent;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var bc = btn.colors;
        bc.normalColor      = accent;
        bc.highlightedColor = new Color(Mathf.Min(accent.r + 0.12f,1f), Mathf.Min(accent.g + 0.12f,1f), Mathf.Min(accent.b + 0.12f,1f));
        bc.pressedColor     = new Color(accent.r * 0.70f, accent.g * 0.70f, accent.b * 0.70f);
        bc.colorMultiplier  = 1f;
        bc.fadeDuration     = 0.06f;
        btn.colors = bc;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        // Top highlight — bright thin strip for sheen
        var hi = new GameObject(name + "_Hi"); hi.transform.SetParent(go.transform, false);
        hi.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);
        var hiRt = hi.GetComponent<RectTransform>();
        hiRt.anchorMin = new Vector2(0,1); hiRt.anchorMax = new Vector2(1,1);
        hiRt.pivot = new Vector2(0.5f,1);
        hiRt.sizeDelta = new Vector2(-16, size.y * 0.38f);
        hiRt.anchoredPosition = new Vector2(0, -4);

        // Centered bold label — white with strong shadow
        var lblGO = new GameObject(name + "_Lbl"); lblGO.transform.SetParent(go.transform, false);
        var lbl = lblGO.AddComponent<Text>();
        lbl.text = label.TrimStart('\u25b6','\u25c6','\u2699','\u2190','\u2302','\u21ba','\u21c4',' ');
        lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lbl.fontSize  = Mathf.Clamp(Mathf.RoundToInt(size.y * 0.46f), 24, 52);
        lbl.fontStyle = FontStyle.Bold;
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.color = Color.white;
        var sh = lblGO.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.60f);
        sh.effectDistance = new Vector2(0, -3);
        var lRt = lbl.GetComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = new Vector2(8, 6); lRt.offsetMax = new Vector2(-8, -2);

        go.AddComponent<MenuButton>().action = action;
        go.AddComponent<ButtonHover>();
    }

    // ── Card container — surface + glow border ─────────────────
    static GameObject MakeCard(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        // Outer glow border (1px inset on each side = 2px wider/taller than card)
        var outer = new GameObject(name + "_Outer"); outer.transform.SetParent(parent, false);
        outer.AddComponent<Image>().color = new Color(UI_ACCENT.r, UI_ACCENT.g, UI_ACCENT.b, 0.22f);
        var ort = outer.GetComponent<RectTransform>();
        ort.anchorMin = ort.anchorMax = ort.pivot = new Vector2(0.5f, 0.5f);
        ort.anchoredPosition = pos; ort.sizeDelta = new Vector2(size.x + 2, size.y + 2);

        // Card surface
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = UI_BG_CARD;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        // Inner top highlight (light edge)
        var hi = new GameObject("Hi"); hi.transform.SetParent(go.transform, false);
        hi.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
        var hrt = hi.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0,1); hrt.anchorMax = new Vector2(1,1);
        hrt.offsetMin = new Vector2(1,-1); hrt.offsetMax = new Vector2(-1,0);

        return go;
    }

    // ── Accent stripe at top of card + subtle glow beneath ─────
    static void CardTopLine(Transform card)
    {
        // Thin bright stripe
        var go = new GameObject("CardLine"); go.transform.SetParent(card, false);
        go.AddComponent<Image>().color = new Color(UI_ACCENT.r, UI_ACCENT.g, UI_ACCENT.b, 0.70f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(1,1);
        rt.offsetMin = new Vector2(0,-2); rt.offsetMax = Vector2.zero;

        // Glow beneath stripe (wider, softer)
        var glow = new GameObject("CardGlow"); glow.transform.SetParent(card, false);
        glow.AddComponent<Image>().color = new Color(UI_ACCENT.r, UI_ACCENT.g, UI_ACCENT.b, 0.10f);
        var grt = glow.GetComponent<RectTransform>();
        grt.anchorMin = new Vector2(0,1); grt.anchorMax = new Vector2(1,1);
        grt.offsetMin = new Vector2(0,-18); grt.offsetMax = Vector2.zero;
    }

    // ── Section header inside card ─────────────────────────────
    static void MakeSectionHeader(Transform card, string text, float y)
    {
        var bar = new GameObject("SH_Bar"); bar.transform.SetParent(card, false);
        bar.AddComponent<Image>().color = UI_ACCENT;
        var bRt = bar.GetComponent<RectTransform>();
        bRt.anchorMin = bRt.anchorMax = bRt.pivot = new Vector2(0.5f, 0.5f);
        bRt.anchoredPosition = new Vector2(-388, y); bRt.sizeDelta = new Vector2(3, 18);

        var t = T(card, "SH_" + text, text,
                  new Vector2(0.5f, 0.5f), new Vector2(-330, y), new Vector2(260, 24),
                  16, TextAnchor.MiddleLeft);
        t.color = new Color(0.55f, 0.70f, 0.95f); t.fontStyle = FontStyle.Bold;
    }

    // ── Setting row: label + segmented option buttons ──────────
    static void MakeSettingRow(Transform card, string label, string[] opts,
                                SettingButton.SettingId type, float y, float btnW, float btnH)
    {
        var lbl = T(card, "SR_" + label, label,
                    new Vector2(0.5f, 0.5f), new Vector2(-130, y), new Vector2(240, btnH),
                    21, TextAnchor.MiddleRight);
        lbl.color = UI_TEXT_SEC;

        const float gap = 6f;
        float totalW = opts.Length * btnW + (opts.Length - 1) * gap;
        float groupCX = 210f;
        for (int i = 0; i < opts.Length; i++)
        {
            float x = groupCX - totalW * 0.5f + i * (btnW + gap) + btnW * 0.5f;
            var go = new GameObject($"SB_{label}_{i}");
            go.transform.SetParent(card, false);
            var imgC = go.AddComponent<Image>();
            var btnC = go.AddComponent<Button>();
            btnC.targetGraphic = imgC;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(btnW, btnH);
            var sb = go.AddComponent<SettingButton>();
            sb.setting = type; sb.valueIndex = i;
            var lT = T(go.transform, "L", opts[i],
                       new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(btnW, btnH),
                       20, TextAnchor.MiddleCenter);
            lT.fontStyle = FontStyle.Bold;
            imgC.color = new Color(0.05f, 0.08f, 0.16f);
            lT.color   = new Color(0.38f, 0.52f, 0.72f);
        }
    }

    // ── HUD pill chip: dark bg + accent left stripe + label ────
    // Returns the Text so UIManager can update it at runtime.
    static Text HudChip(Transform parent, string name, string text,
                        Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, Color accent)
    {
        // Outer glow (slightly wider/taller, very soft)
        var glowGO = new GameObject(name + "_HGlow"); glowGO.transform.SetParent(parent, false);
        glowGO.AddComponent<Image>().color = new Color(accent.r, accent.g, accent.b, 0.14f);
        var grt = glowGO.GetComponent<RectTransform>();
        grt.anchorMin = grt.anchorMax = grt.pivot = anchor;
        grt.anchoredPosition = pos; grt.sizeDelta = new Vector2(size.x + 6, size.y + 6);

        // Dark pill background
        var bg = new GameObject(name + "_HBg"); bg.transform.SetParent(parent, false);
        bg.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.12f, 0.82f);
        var brt = bg.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = brt.pivot = anchor;
        brt.anchoredPosition = pos; brt.sizeDelta = size;

        // Accent left stripe (4px)
        var bar = new GameObject(name + "_HBar"); bar.transform.SetParent(bg.transform, false);
        bar.AddComponent<Image>().color = accent;
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0,0); barRt.anchorMax = new Vector2(0,1);
        barRt.offsetMin = Vector2.zero; barRt.offsetMax = new Vector2(4,0);

        // Text label
        var lgo = new GameObject(name); lgo.transform.SetParent(bg.transform, false);
        var lbl = lgo.AddComponent<Text>();
        lbl.text = text; lbl.fontSize = fontSize; lbl.color = UI_TEXT_PRI;
        lbl.fontStyle = FontStyle.Bold; lbl.alignment = TextAnchor.MiddleLeft;
        lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var sh = lgo.AddComponent<Shadow>();
        sh.effectColor = new Color(0,0,0,0.8f); sh.effectDistance = new Vector2(1,-1);
        var lrt = lgo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(12,0); lrt.offsetMax = Vector2.zero;
        return lbl;
    }

    // ── Horizontal line helper ──────────────────────────────────
    static void UIHLine(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color col)
    {
        var go = new GameObject("HLine"); go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    // ── RectTransform anchor+size helper ───────────────────────
    static void SetAP(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    // ── Track helpers ──────────────────────────────────────────
    static void Gnd(string n, Vector3 p, Vector3 s, Color c)
    {
        var go = GO(n, p, s); go.layer = groundLayer;
        Sr(go, c, 0); go.AddComponent<BoxCollider2D>();
        // Top light strip — child of body so it moves with it
        var top = GO(n+"_t", new Vector3(p.x, p.y+s.y*.42f, p.z-.05f), new Vector3(s.x, s.y*.18f,1));
        top.transform.SetParent(go.transform, worldPositionStays: true);
        Sr(top, Lighter(c,.12f), 1);
    }

    static void Plt(string n, Vector3 p, Vector3 s, Color body, Color top)
    {
        var go = GO(n, p, s); go.layer = groundLayer;
        Sr(go, body, 0); go.AddComponent<BoxCollider2D>();
        var t = GO(n+"_t", new Vector3(p.x, p.y+s.y*.42f, p.z-.05f), new Vector3(s.x, s.y*.24f,1));
        // Parent top strip to body so MovingPlatform carries it along
        t.transform.SetParent(go.transform, worldPositionStays: true);
        Sr(t, top, 1);
    }

    static void Mov(string n, Vector3 p, Vector3 s, Color body, Color top, float spd, bool vert)
    {
        Plt(n, p, s, body, top);
        var go = GameObject.Find(n);
        go.layer = groundLayer;
        var mp = go.AddComponent<MovingPlatform>();
        mp.speed = spd; mp.distance = vert ? 2.2f : 3.2f; mp.vertical = vert;
    }

    static void Bnc(string n, Vector3 p, Vector3 s)
    {
        var go = GO(n, p, s); go.layer = groundLayer;
        Sr(go, BOUNCE_C, 2);
        go.AddComponent<BoxCollider2D>().isTrigger = true; // trigger — no physical step
        go.AddComponent<BouncePad>();
        // White stripe on bounce pad
        var st = GO(n+"_st", new Vector3(p.x, p.y, p.z-.02f), new Vector3(s.x*.9f, s.y*.3f,1));
        Sr(st, new Color(1,1,1,.6f), 3);
    }

    static void Spd(string n, Vector3 p, Vector3 s)
    {
        var go = GO(n, p, s);
        Sr(go, SPEED_C, 2);
        var c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true;
        go.AddComponent<SpeedPad>();
    }

    static void Wrn(string n, Vector3 p, Vector3 s)
    {
        // Red/yellow warning stripes before void
        for (int i = 0; i < 4; i++)
        {
            var st = GO(n+"_"+i, new Vector3(p.x+i*.15f, p.y, p.z), new Vector3(s.x, s.y,1));
            Sr(st, i%2==0 ? WARN_C : new Color(1f,.85f,.0f), 2);
        }
    }

    static void CP(string n, Vector3 p)
    {
        // Tall trigger strip
        var go = GO(n, p, new Vector3(0.6f, 8f, 1));
        Sr(go, new Color(1f, 1f, 0.2f, 0.10f), -1);
        var c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true;
        go.AddComponent<Checkpoint>();

        // Visible flag post so the player actually sees the checkpoint
        var post = new GameObject(n + "_post");
        post.transform.SetParent(go.transform, false);
        post.transform.localPosition = new Vector3(0, 0, -0.05f);
        post.transform.localScale    = new Vector3(0.12f / 0.6f, 1f, 1f); // thin pole, full height
        var postSr = post.AddComponent<SpriteRenderer>();
        postSr.sprite = WhiteSprite();
        postSr.color  = new Color(1f, 0.92f, 0.12f, 0.55f); // gold pole
        postSr.sortingOrder = 2;

        // Flag diamond at top
        var flag = new GameObject(n + "_flag");
        flag.transform.SetParent(go.transform, false);
        flag.transform.localPosition = new Vector3(0.3f / 0.6f, 0.44f, -0.06f); // top-right of post
        flag.transform.localScale    = new Vector3(0.50f / 0.6f, 0.20f, 1f);
        var flagSr = flag.AddComponent<SpriteRenderer>();
        flagSr.sprite = WhiteSprite();
        flagSr.color  = new Color(1f, 0.92f, 0.12f, 0.80f); // bright gold flag
        flagSr.sortingOrder = 3;
    }

    // Purple wall-jump surface with horizontal grip stripes
    static void Wll(string n, Vector3 p, Vector3 s)
    {
        var go = GO(n, p, s); go.layer = groundLayer;
        Sr(go, WALL_C, 1); go.AddComponent<BoxCollider2D>();
        // Horizontal grip lines — visual cue: "wall jump here"
        for (int i = 0; i < 5; i++)
        {
            float yOff = -s.y * 0.35f + i * (s.y * 0.175f);
            var stripe = GO(n + "_g" + i,
                new Vector3(p.x, p.y + yOff, p.z - 0.05f),
                new Vector3(s.x * 1.4f, s.y * 0.04f, 1));
            Sr(stripe, new Color(0.80f, 0.55f, 1f, 0.65f), 2);
        }
    }

    // Kill zone — invisible trigger that calls RacePlayer.Respawn()
    static void Kz(string n, Vector3 p, Vector3 s)
    {
        var go = GO(n, p, s);
        var c = go.AddComponent<BoxCollider2D>();
        c.isTrigger = true;
        go.AddComponent<KillZone>();
    }

    /// <summary>
    /// Scans every SpriteRenderer inside <paramref name="container"/>, computes the
    /// combined world-space bounding box of all geometry, then places four invisible
    /// kill-zone triggers offset by <paramref name="padding"/> outside that box.
    ///
    /// This means the kill-zone "frame" always matches the actual map size — no
    /// manual numbers needed.  Changing the map auto-updates the boundaries.
    /// </summary>
    static void BuildBoundaryKillZones(GameObject container, string prefix, float padding = 18f)
    {
        if (container == null) return;

        // ── 1. Compute world-space bounds of all geometry renderers ──────────
        // IMPORTANT: sr.bounds is UNRELIABLE in Edit Mode (returns 0 until rendered).
        // Instead, compute bounds from world position + sprite pixel size / PPU × lossyScale.
        // This mirrors exactly what Unity would compute at runtime.
        Bounds total = new Bounds();
        bool   any   = false;
        foreach (var sr in container.GetComponentsInChildren<SpriteRenderer>())
        {
            if (sr.sprite == null) continue;
            Vector3 pos   = sr.transform.position;
            Vector3 sc    = sr.transform.lossyScale; // world-space scale (inherits all parents)
            // Sprite size in world units: sprite.rect / pixelsPerUnit, scaled by lossyScale
            float w = (sr.sprite.rect.width  / sr.sprite.pixelsPerUnit) * Mathf.Abs(sc.x);
            float h = (sr.sprite.rect.height / sr.sprite.pixelsPerUnit) * Mathf.Abs(sc.y);
            var   b = new Bounds(pos, new Vector3(w, h, 0f));
            if (any) total.Encapsulate(b);
            else     { total = b; any = true; }
        }
        if (!any) { UnityEngine.Debug.LogWarning($"BuildBoundaryKillZones: no renderers in {container.name}"); return; }

        // ── 2. Expand by padding ─────────────────────────────────────────────
        const float T = 8f;   // kill-zone strip thickness

        float cx  = total.center.x;
        float cy  = total.center.y;

        float yBot = total.min.y - padding;        // bottom edge
        float yTop = total.max.y + padding;        // top edge
        float xL   = total.min.x - padding;        // left edge
        float xR   = total.max.x + padding;        // right edge

        // Horizontal strips span the full width + T overlap so corners are covered
        float fullW = (xR - xL) + T * 2f;
        // Vertical strips span the full height (corners already covered above)
        float fullH = yTop - yBot;

        // ── 3. Spawn four strips as children of the container ────────────────
        var saved    = _trackParent;
        _trackParent = container.transform;      // Kz() parents to _trackParent

        Kz(prefix + "Bottom", new Vector3(cx,  yBot, 0), new Vector3(fullW, T,     1));
        Kz(prefix + "Top",    new Vector3(cx,  yTop, 0), new Vector3(fullW, T,     1));
        Kz(prefix + "Left",   new Vector3(xL,  cy,   0), new Vector3(T,     fullH, 1));
        Kz(prefix + "Right",  new Vector3(xR,  cy,   0), new Vector3(T,     fullH, 1));

        _trackParent = saved;

        UnityEngine.Debug.Log(
            $"[KillZone] {prefix} bounds: x=[{total.min.x:F1}, {total.max.x:F1}] " +
            $"y=[{total.min.y:F1}, {total.max.y:F1}]  padding={padding}");
    }

    // ── New mechanic object helpers ─────────────────────────────

    // Ice surface — thin slab on top of a platform; player slides on it
    static void Ice(string n, Vector3 p, Vector3 s)
    {
        var go = GO(n, p, s); go.layer = groundLayer;
        Sr(go, new Color(0.72f, 0.92f, 1.00f, 0.55f), 3); // icy blue-white
        go.AddComponent<BoxCollider2D>().isTrigger = true; // trigger — no physical step
        go.AddComponent<IceSurface>();
    }

    // Wind / tornado column — upward lift trigger zone, teal semi-transparent
    static void Wnd(string n, Vector3 p, Vector3 s)
    {
        var go = GO(n, p, s);
        Sr(go, new Color(0.45f, 1.00f, 0.85f, 0.28f), 1); // soft teal
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true;
        go.AddComponent<WindZone>();
        // Flow indicator strips — children so they move with the zone
        for (int i = 0; i < 3; i++)
        {
            float yo = (-s.y * 0.3f + i * (s.y * 0.3f)) / s.y; // local offset
            var line = new GameObject(n + "_l" + i);
            line.transform.SetParent(go.transform, false);
            line.transform.localPosition = new Vector3(0, yo, -0.05f / s.y);
            line.transform.localScale    = new Vector3(0.55f, 0.06f / s.y, 1f);
            var lsr = line.AddComponent<SpriteRenderer>();
            lsr.sprite = WhiteSprite();
            lsr.color  = new Color(0.5f, 1f, 0.9f, 0.35f);
            lsr.sortingOrder = 2;
        }
    }

    // Dash-boost orb — next dash travels 1.7× further; bobs up/down, purple glow
    static void Dsh(string n, Vector3 p)
    {
        var go = GO(n, p, new Vector3(0.52f, 0.52f, 1f));
        Sr(go, new Color(0.72f, 0.18f, 1.00f, 0.90f), 4); // purple
        // Inner bright core — child so it moves with the orb (immune to scale)
        var core = new GameObject(n + "_c");
        core.transform.SetParent(go.transform, false);
        core.transform.localPosition = new Vector3(0, 0, -0.05f);
        core.transform.localScale    = new Vector3(0.55f, 0.55f, 1f);
        var coreSr = core.AddComponent<SpriteRenderer>();
        coreSr.sprite = WhiteSprite();
        coreSr.color  = new Color(0.88f, 0.55f, 1.00f, 0.75f);
        coreSr.sortingOrder = 5;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true;
        go.AddComponent<DashBoost>();
    }

    // Dynamic spikes — appear/disappear cycle; respawns player on touch when active
    static void Spk(string n, Vector3 p, Vector3 s,
                    float onDur = 1.0f, float offDur = 3.5f, float delay = 0f)
    {
        var go = GO(n, p, s);
        var baseSr = go.AddComponent<SpriteRenderer>();
        baseSr.sprite = WhiteSprite();
        baseSr.color  = new Color(1.00f, 0.18f, 0.18f, 0.92f); // neon red
        baseSr.sortingOrder = 4;
        // Spike tip triangles — children so DynamicSpikes can hide/show them together
        // (We only animate the base collider; tips are purely visual siblings under the parent)
        int tipsCount = Mathf.Max(1, Mathf.RoundToInt(s.x / 0.42f));
        for (int i = 0; i < tipsCount; i++)
        {
            float localXOff = s.x > 0.6f
                ? (-0.5f + (i + 0.5f) / tipsCount) // normalized -0.5 to +0.5
                : 0f;
            var tip = new GameObject(n + "_t" + i);
            tip.transform.SetParent(go.transform, false);
            tip.transform.localPosition = new Vector3(localXOff, 0.38f, -0.04f / s.y);
            tip.transform.localScale    = new Vector3(0.22f / s.x, 0.32f / s.y, 1f);
            var tsr = tip.AddComponent<SpriteRenderer>();
            tsr.sprite = WhiteSprite();
            tsr.color  = new Color(1f, 0.45f, 0.12f, 0.88f); // orange tips
            tsr.sortingOrder = 5;
        }
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true;
        var ds = go.AddComponent<DynamicSpikes>();
        ds.onDuration   = onDur;
        ds.offDuration  = offDur;
        ds.initialDelay = delay;
    }

    // Decorative block — purely visual, no collider, no logic
    // Used to add environmental flavour to each zone (trees, rocks, grass, etc.)
    static void Deco(string n, Vector3 p, Vector3 s, Color c, int order = 1)
    {
        var go = GO(n, p, s);
        Sr(go, c, order);
    }

    // ── New interactive objects ────────────────────────────────

    /// Conveyor belt: trigger strip that pushes players horizontally.
    /// speed > 0 = right, speed < 0 = left.
    static void Cnv(string n, Vector3 p, Vector3 s, float speed)
    {
        var go = GO(n, p, s);
        // Colour: green for right-push, orange for left-push
        var baseCol = speed > 0
            ? new Color(0.20f, 0.85f, 0.35f, 0.88f)
            : new Color(0.95f, 0.45f, 0.10f, 0.88f);
        Sr(go, baseCol, 2);
        // Arrow stripes (children, purely visual)
        int arrows = Mathf.Max(1, Mathf.RoundToInt(s.x / 0.55f));
        for (int i = 0; i < arrows; i++)
        {
            float lx = s.x > 0.6f ? (-0.5f + (i + 0.5f) / arrows) : 0f;
            var ar = new GameObject(n + "_a" + i);
            ar.transform.SetParent(go.transform, false);
            ar.transform.localPosition = new Vector3(lx, 0f, -0.04f);
            ar.transform.localScale    = new Vector3(0.18f / s.x, 0.55f / s.y, 1f);
            Sr(ar, new Color(1f, 1f, 1f, 0.55f), 3);
        }
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true;
        go.AddComponent<ConveyorBelt>().speed = speed;
    }

    /// Crumbling platform: solid platform that shakes and falls when stood on.
    static void Crm(string n, Vector3 p, Vector3 s, Color body, Color top)
    {
        var go = GO(n, p, s); go.layer = groundLayer;
        Sr(go, body, 0);
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = false;
        go.AddComponent<CrumblingPlatform>();
        // Top stripe (same visual as regular platform)
        var t = GO(n + "_t", new Vector3(p.x, p.y + s.y * 0.42f, p.z - 0.05f),
                             new Vector3(s.x, s.y * 0.24f, 1));
        t.transform.SetParent(go.transform, worldPositionStays: true);
        Sr(t, top, 1);
        // Crack marks (visual, horizontal thin lines)
        for (int i = 0; i < 3; i++)
        {
            float lx = -0.3f + i * 0.3f;
            var cr = new GameObject(n + "_cr" + i);
            cr.transform.SetParent(go.transform, false);
            cr.transform.localPosition = new Vector3(lx, 0.1f, -0.06f);
            cr.transform.localScale    = new Vector3(0.12f / s.x, 0.04f / s.y, 1f);
            Sr(cr, new Color(0f, 0f, 0f, 0.35f), 3);
        }
    }

    /// Low-gravity zone: semi-transparent trigger that reduces gravity.
    static void Lgz(string n, Vector3 p, Vector3 s, float gravScale = 0.35f)
    {
        var go = GO(n, p, s);
        Sr(go, new Color(0.45f, 0.80f, 1.00f, 0.10f), -1); // very subtle tint
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true;
        go.AddComponent<LowGravityZone>().gravityScale = gravScale;
        // Border glow lines (4 edges)
        Color border = new Color(0.45f, 0.80f, 1.00f, 0.40f);
        float t = 0.05f; // thickness in local units
        foreach (var (lp, ls) in new[]{
            (new Vector3(0,  0.5f - t * 0.5f, -0.02f), new Vector3(1f, t, 1f)), // top
            (new Vector3(0, -0.5f + t * 0.5f, -0.02f), new Vector3(1f, t, 1f)), // bot
            (new Vector3(-0.5f + t * 0.5f, 0,  -0.02f), new Vector3(t, 1f, 1f)), // left
            (new Vector3( 0.5f - t * 0.5f, 0,  -0.02f), new Vector3(t, 1f, 1f)), // right
        })
        {
            var edge = new GameObject(n + "_e");
            edge.transform.SetParent(go.transform, false);
            edge.transform.localPosition = lp;
            edge.transform.localScale    = ls;
            Sr(edge, border, 0);
        }
    }

    /// Teleport pad: trigger circle; teleports to exitPoint.
    static void Tpt(string n, Vector3 p, Vector3 exitPos)
    {
        float r = 0.7f; // radius in world units
        var go = GO(n, p, new Vector3(r * 2f, r * 0.35f, 1f));
        Sr(go, new Color(0.4f, 0.9f, 1.0f, 0.85f), 4);
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true;
        var tp = go.AddComponent<TeleportPad>();
        tp.exitPoint = exitPos;
        // Outer ring visual
        var ring = new GameObject(n + "_ring");
        ring.transform.SetParent(go.transform, false);
        ring.transform.localPosition = new Vector3(0, 0, -0.03f);
        ring.transform.localScale    = new Vector3(1.15f, 3.5f, 1f);
        Sr(ring, new Color(0.65f, 0.35f, 1.00f, 0.55f), 3);
    }

    // ── DashBar factory ────────────────────────────────────────
    /// <summary>
    /// Creates a top-level "DBar_<id>" object (immune to player scale) with a
    /// dark background strip and a cyan fill strip.  DashBar.LateUpdate handles
    /// positioning and fill width each frame.
    /// </summary>
    static void AttachDashBar(string id, Transform target, PlayerController pc)
    {
        const float W = 0.72f;
        const float H = 0.055f;

        var barGO = new GameObject("DBar_" + id);
        barGO.transform.position = target.position;

        // Background
        var bg = new GameObject("DB_Bg");
        bg.transform.SetParent(barGO.transform, false);
        bg.transform.localScale = new Vector3(W, H, 1f);
        var bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = WhiteSprite();
        bgSr.color  = new Color(0.04f, 0.06f, 0.14f, 0.78f);
        bgSr.sortingOrder = 6;

        // Fill strip (left-aligned by DashBar LateUpdate)
        var fill = new GameObject("DB_Fill");
        fill.transform.SetParent(barGO.transform, false);
        fill.transform.localScale = new Vector3(W, H * 0.65f, 1f);
        var fillSr = fill.AddComponent<SpriteRenderer>();
        fillSr.sprite = WhiteSprite();
        fillSr.color  = new Color(0.10f, 0.88f, 1.00f, 0.30f); // faded cyan = ready
        fillSr.sortingOrder = 7;

        var db = barGO.AddComponent<DashBar>();
        db.Init(target, pc, fill.transform, fillSr);
    }

    // ── Generic helpers ────────────────────────────────────────
    static GameObject GO(string n, Vector3 p, Vector3 s)
    {
        var go = new GameObject(n);
        go.transform.position = p;
        go.transform.localScale = s;
        // Parent to active track container so map switching can enable/disable the whole set
        if (_trackParent != null) go.transform.SetParent(_trackParent, true);
        return go;
    }

    static void Sr(GameObject go, Color c, int order)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSprite();
        sr.color  = c;
        sr.sortingOrder = order;
    }

    static void MakeBg(string n, Color c, Vector3 p, Vector3 s, int order, float parallax)
        => Bg(n, c, p, s, order, parallax);

    static void Bg(string n, Color c, Vector3 p, Vector3 s, int order, float par)
    {
        var go = GO(n, p, s);
        Sr(go, c, order);
        var pb = go.AddComponent<ParallaxBackground>();
        pb.parallaxFactor = par;
    }

    static Color Lighter(Color c, float amt)
        => new Color(Mathf.Min(c.r+amt,1), Mathf.Min(c.g+amt,1), Mathf.Min(c.b+amt*.5f,1));

    static Text T(Transform parent, string n, string content, Vector2 anchor,
        Vector2 pos, Vector2 size, int fs, TextAnchor align)
    {
        var go = new GameObject(n); go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content; t.fontSize = fs; t.color = Color.white;
        t.fontStyle = FontStyle.Bold; t.alignment = align;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0,0,0,.9f); sh.effectDistance = new Vector2(2,-2);
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return t;
    }

    static GameObject Pan(Transform parent, string n, Color c, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(n); go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = c;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    static Sprite S(Color c)
    {
        var tex = new Texture2D(4,4);
        var px = new Color[16]; for (int i=0;i<16;i++) px[i]=c;
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(.5f,.5f), 4);
    }

    // Shared white sprite — color is stored in SpriteRenderer.color so
    // WorldThemeManager can convert it to grayscale for B&W mode.
    static Sprite _whiteSprite;
    static Sprite WhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;
        var tex = new Texture2D(4,4);
        var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        _whiteSprite = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(.5f,.5f), 4);
        return _whiteSprite;
    }

    static Sprite MakeChecker()
    {
        var tex = new Texture2D(8,8);
        var px = new Color[64];
        for (int y=0;y<8;y++) for (int x=0;x<8;x++)
            px[y*8+x] = ((x+y)%2==0) ? Color.white : Color.black;
        tex.SetPixels(px); tex.filterMode = FilterMode.Point; tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,8,8), new Vector2(.5f,.5f), 8);
    }

    static int CreateLayer(string name)
    {
        int e = LayerMask.NameToLayer(name);
        if (e != -1) return e;
        var tm = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("layers");
        for (int i=8;i<layers.arraySize;i++)
        {
            var sp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            { sp.stringValue = name; tm.ApplyModifiedProperties(); return i; }
        }
        return 0;
    }
}

// ── Build-time scene populator ───────────────────────────────────────────────
// IProcessSceneWithReport fires for each scene DURING build packaging —
// modifications here are guaranteed to end up in the final binary.
class SceneBuildPopulator : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null) return;
        if (scene.name != "SampleScene") return;

        // NEVER call Setup() here.
        // Setup() creates new GameObjects at build-time with temporary instance IDs.
        // NGO's GlobalObjectIdHash is derived from each object's Unity GlobalObjectId:
        //   • Saved scene objects  → fileID-based hash   (stable across Editor + build)
        //   • Build-time new objs  → instance-ID-based hash (different every build)
        // Calling Setup() here would give the iPad different hashes than the Editor
        // → "NetworkPrefab hash was not found!" on every client connection.
        //
        // Correct workflow: Game → Setup 2D Runner Scene (auto-saves) → Build And Run.
        if (Object.FindAnyObjectByType<RaceManager>() == null)
            Debug.LogWarning("[SceneSetup] ⚠️  Building with an empty scene! Run Game → Setup 2D Runner Scene first, then Build And Run.");
        else
            Debug.Log($"[SceneSetup] Scene '{scene.name}' already set up — using saved objects (stable hashes).");
    }
}
