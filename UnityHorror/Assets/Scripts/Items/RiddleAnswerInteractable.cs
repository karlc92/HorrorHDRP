using UnityEngine;

[RequireComponent(typeof(RiddleAnswerHook))]
[AddComponentMenu("Horror/Items/Riddle Answer Interactable")]
public class RiddleAnswerInteractable : Interactable
{
    [SerializeField] private RiddleAnswerHook answerHook;

    private void Reset()
    {
        answerHook = GetComponent<RiddleAnswerHook>();
    }

    public override void Interact()
    {
        if (answerHook == null)
            answerHook = GetComponent<RiddleAnswerHook>();

        answerHook?.SelectAnswer();
    }
}
