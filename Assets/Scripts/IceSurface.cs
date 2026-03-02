using UnityEngine;

/// <summary>
/// Slippery ice surface — player velocity is lerped slowly instead of set
/// directly, simulating low friction.  OnCollisionStay2D sets the flag on
/// PlayerController each frame the player is on top; Exit clears it.
/// </summary>
public class IceSurface : MonoBehaviour
{
    // Trigger-based: ice slab is non-solid so players never snag on its edges.
    // Ice is always horizontal so any player inside the trigger is standing on it.
    void OnTriggerStay2D(Collider2D other)
    {
        other.GetComponent<PlayerController>()?.SetOnIce(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        other.GetComponent<PlayerController>()?.SetOnIce(false);
    }

}
