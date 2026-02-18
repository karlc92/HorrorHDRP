using System.Transactions;
using UnityEngine.SceneManagement;

public static class Game
{
    public static GameSettings Settings = new GameSettings();
    public static GameState State = new GameState();
    public static bool Started = false;

    public static void Awake()
    {

    }

    public static void Start()
    {
        Started = true;
    }

    public static void Update()
    {

    }

    public static void FixedUpdate()
    {

    }

    public static void StartNewGame()
    {
        State = new GameState();

        SceneManager.LoadScene("GameScene");
    }
    
}
