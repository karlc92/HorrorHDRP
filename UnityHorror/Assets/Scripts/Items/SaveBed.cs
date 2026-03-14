using UnityEngine;

public class SaveBed : Interactable
{
    public override void Interact()
    {
        var gameUI = FindFirstObjectByType<GameUI>();

        if (RunManager.Instance != null)
        {
            bool endedNight = RunManager.Instance.TryEndNight();
            if (gameUI != null)
            {
                gameUI.ShowNotification(endedNight
                    ? "Night completed."
                    : "You cannot sleep yet.");
            }
            return;
        }

        if (gameUI != null)
        {
            gameUI.ShowNotification("Your progress has been saved.");
        }
        Game.SaveGameState();
    }
}
