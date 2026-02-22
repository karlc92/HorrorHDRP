using UnityEngine;

public class SaveBed : Interactable
{
    public override void Interact()
    {
        Game.SaveGame(Game.State.Slot);
    }
}
