using UnityEngine;

public class GameMonobehaviour : MonoBehaviour
{
    private void Start()
    {
        if (Game.Started)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
        Game.Start();
    }

    private void Awake() => Game.Awake();

    private void Update() => Game.Update();

    private void FixedUpdate() => Game.FixedUpdate();
}
