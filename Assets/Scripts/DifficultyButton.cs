using UnityEngine;
using UnityEngine.UI;

// Attached to each difficulty button in the UI.
// Set diffIndex in the Inspector (or via SceneSetup):
//   0 = Easy   1 = Normal   2 = Hard   3 = Ultra
[RequireComponent(typeof(Button))]
public class DifficultyButton : MonoBehaviour
{
    public int diffIndex;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        DifficultyManager.Instance?.Select((DifficultyManager.Diff)diffIndex);
    }
}
