using UnityEngine;

public class SaveBed : Interactable
{
    public override void Interact()
    {
        var gameUI = FindFirstObjectByType<GameUI>();

        StoryGameManager.Instance?.CaptureCheckpointAtPlayer("save-bed");

        if (gameUI != null)
            gameUI.ShowNotification("Your progress has been saved.");

        Game.SaveGameState();
    }
}
