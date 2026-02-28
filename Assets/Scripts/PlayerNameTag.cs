using UnityEngine;

/// <summary>
/// Floating nickname tag pinned under a player/bot.
/// Lives as a top-level scene object and follows its target in LateUpdate,
/// so it is completely unaffected by the player's squash/stretch scale.
/// Naming convention: "NTag_<targetName>" — Start() resolves the target
/// by name at runtime if the serialized reference was lost.
/// </summary>
public class PlayerNameTag : MonoBehaviour
{
    TextMesh _tm;
    [SerializeField] Transform _target;

    void Awake() => _tm = GetComponent<TextMesh>();

    void Start()
    {
        // Runtime fallback: [SerializeField] ref is null when the scene was
        // saved before [SerializeField] was added.  Resolve by name convention:
        //   "NTag_Bot_1"  →  find "Bot_1"
        //   "NTag_Player" →  find "Player"
        if (_target == null && gameObject.name.StartsWith("NTag_"))
        {
            string targetName = gameObject.name.Substring(5); // strip "NTag_"
            var found = GameObject.Find(targetName);
            if (found != null)
                _target = found.transform;
            else
                Debug.LogWarning($"[PlayerNameTag] Could not find target '{targetName}' for '{gameObject.name}'");
        }
    }

    /// <summary>Called by SceneSetup to link this tag to its owner.</summary>
    public void InitFollow(Transform target) => _target = target;

    /// <returns>True if this tag is already following <paramref name="target"/>.</returns>
    public bool IsFollowing(Transform target) => _target == target;

    void LateUpdate()
    {
        if (_target == null) return;
        transform.position = _target.position + new Vector3(0f, -0.9f, -0.1f);
    }

    public void SetTag(string nickname, Color col)
    {
        if (_tm == null) return;
        _tm.text  = nickname;
        _tm.color = col;
    }
}
