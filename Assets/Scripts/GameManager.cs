using UnityEngine;

// Minimal GameManager singleton for state and simple spawning
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int score = 0;
    public Transform[] enemySpawnPoints;
    public GameObject enemyPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // basic spawn to get things moving — expand later
        if (enemyPrefab != null && enemySpawnPoints != null && enemySpawnPoints.Length > 0)
        {
            foreach (var sp in enemySpawnPoints)
            {
                Instantiate(enemyPrefab, sp.position, sp.rotation);
            }
        }
    }

    public void AddScore(int points)
    {
        score += points;
    }

    public void OnPlayerDestroyed()
    {
        // handle game over state (display UI, restart, etc.)
        Debug.Log("Player destroyed - Game Over");
    }
}
