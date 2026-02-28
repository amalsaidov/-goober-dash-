using UnityEngine;

/// <summary>
/// Ghost-trail effect using a fixed pool of pre-allocated GameObjects.
/// Replaces the old create/Destroy loop that generated ~200 GOs/sec and
/// flooded the GC.  Pool size of 10 comfortably covers the longest fade.
/// </summary>
public class PlayerTrail : MonoBehaviour
{
    public float interval = 0.04f;   // seconds between ghosts
    public float fadeTime = 0.25f;   // seconds to fade out

    private SpriteRenderer _sr;
    private float _timer;

    // ── Pool ─────────────────────────────────────────────────────────────────
    private const int POOL_SIZE = 10;

    struct Ghost
    {
        public GameObject  go;
        public SpriteRenderer sr;
        public float       life;   // time remaining (0 = inactive)
        public float       alpha;  // starting alpha
    }

    Ghost[] _pool;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _sr   = GetComponent<SpriteRenderer>();
        _pool = new Ghost[POOL_SIZE];

        for (int i = 0; i < POOL_SIZE; i++)
        {
            var go = new GameObject("TrailGhost");
            go.hideFlags = HideFlags.HideInHierarchy;
            go.SetActive(false);
            var gsr = go.AddComponent<SpriteRenderer>();
            _pool[i] = new Ghost { go = go, sr = gsr, life = 0f };
        }
    }

    void OnDestroy()
    {
        if (_pool == null) return;
        for (int i = 0; i < POOL_SIZE; i++)
            if (_pool[i].go != null) Destroy(_pool[i].go);
    }

    void Update()
    {
        if (_pool == null || _sr == null) return; // not yet initialized

        // Spawn a new ghost on interval
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _timer = interval;
            SpawnGhost();
        }

        // Tick every active ghost — no coroutines, no allocations
        float dt = Time.deltaTime;
        for (int i = 0; i < POOL_SIZE; i++)
        {
            if (_pool[i].life <= 0f) continue;

            _pool[i].life -= dt;

            if (_pool[i].life <= 0f)
            {
                _pool[i].go.SetActive(false);
                _pool[i].life = 0f;
            }
            else
            {
                float t = _pool[i].life / fadeTime;   // 1→0 as it ages
                Color c = _pool[i].sr.color;
                c.a = _pool[i].alpha * t;
                _pool[i].sr.color = c;
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void SpawnGhost()
    {
        // Find a free slot
        int slot = -1;
        for (int i = 0; i < POOL_SIZE; i++)
        {
            if (_pool[i].life <= 0f) { slot = i; break; }
        }
        if (slot < 0) return; // pool exhausted — skip this frame

        _pool[slot].go.transform.position   = transform.position;
        _pool[slot].go.transform.localScale = transform.localScale;

        _pool[slot].sr.sprite       = _sr.sprite;
        _pool[slot].sr.sortingOrder = _sr.sortingOrder - 1;

        Color c = _sr.color;
        float a = c.a * 0.35f;
        _pool[slot].sr.color = new Color(c.r, c.g, c.b, a);
        _pool[slot].alpha    = a;
        _pool[slot].life     = fadeTime;

        _pool[slot].go.SetActive(true);
    }
}
