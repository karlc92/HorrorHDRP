using UnityEngine;

public class SaveBed : Interactable
{
    public override void Interact()
    {
        var gameUI = FindFirstObjectByType<GameUI>();
        if (gameUI != null)
        {
            gameUI.ShowNotification("Your progress has been saved.");
        }
        Game.SaveGame(Game.State.Slot);
    }
}
