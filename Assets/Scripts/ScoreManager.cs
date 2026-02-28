using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public int Score { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddScore(int amount)
    {
        Score += amount;
        UIManager.Instance?.UpdateScore(Score);
    }

    public void ResetScore()
    {
        Score = 0;
        UIManager.Instance?.UpdateScore(Score);
    }
}
