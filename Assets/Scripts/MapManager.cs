using System.Collections;
using UnityEngine;

/// <summary>
/// Manages which map track is active.
/// Switching maps disables one container, enables the other, teleports all players
/// to the correct spawn Y, and refreshes AI waypoints.
/// </summary>
public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    /// <summary>0 = Forest (default),  1 = Volcano</summary>
    public static int SelectedMap { get; private set; }

    // Track containers — assigned in SceneSetup.CreateManagers()
    public GameObject container0; // MapContainer_0 (Forest)
    public GameObject container1; // MapContainer_1 (Volcano)

    // Spawn Y per map (X is kept as-is from scene setup)
    const float SPAWN_Y_0 = -1.5f;
    const float SPAWN_Y_1 = -1.5f + Y_OFFSET;
    public const float Y_OFFSET = 55f; // how far map 1 sits above map 0 in world space

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        SelectedMap = PlayerPrefs.GetInt("GS_mapIndex", 0);
    }

    void Start()
    {
        // Activate the saved map (no teleport — players are already at the right Y at scene start)
        ApplyContainers(SelectedMap);
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Call from Map Selection UI to switch the active map.</summary>
    public void SelectMap(int idx)
    {
        SelectedMap = idx;
        PlayerPrefs.SetInt("GS_mapIndex", idx);
        ApplyContainers(idx);
        StartCoroutine(TeleportAndRefresh(idx));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    void ApplyContainers(int idx)
    {
        if (container0 != null) container0.SetActive(idx == 0);
        if (container1 != null) container1.SetActive(idx == 1);
    }

    IEnumerator TeleportAndRefresh(int idx)
    {
        // Wait one frame so the newly-enabled container's WaypointPath.Awake() runs
        yield return null;

        float newY = idx == 0 ? SPAWN_Y_0 : SPAWN_Y_1;

        // Move every player/bot to the correct spawn Y for this map
        foreach (var rp in Object.FindObjectsByType<RacePlayer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var pos = new Vector3(rp.transform.position.x, newY, 0f);
            rp.transform.position = pos;
            rp.SetSpawnPoint(pos);

            // Also update PlayerController's respawn point
            var pc = rp.GetComponent<PlayerController>();
            if (pc != null) pc.UpdateSpawnPoint(pos);

            var rb = rp.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // Push new waypoints to every AI
        var pts = WaypointPath.Instance?.points;
        if (pts != null)
        {
            foreach (var ai in Object.FindObjectsByType<AIPlayer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                ai.SetWaypoints(pts);
        }
    }
}
