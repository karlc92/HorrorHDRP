using UnityEngine;

public class DialogueTriggerItem : Interactable
{
    public string DialogueName;
    public bool UseLocalAudioSource = false;
    public override void Interact()
    {
        if (string.IsNullOrWhiteSpace(DialogueName))
        {
            Console.Print("DialogueName is empty for " + transform.name);
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Console.Print("DialogueManager is null");
        }

        DialogueManager.Instance.PlayDialogueSequence(DialogueName, UseLocalAudioSource ? GetComponent<AudioSource>() : null);

    }
}
