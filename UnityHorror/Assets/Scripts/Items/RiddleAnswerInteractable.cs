using UnityEngine;

[RequireComponent(typeof(RiddleAnswerHook))]
[AddComponentMenu("Horror/Items/Riddle Answer Interactable")]
public class RiddleAnswerInteractable : Interactable
{
    [SerializeField] private RiddleAnswerHook answerHook;

    protected override void Reset()
    {
        base.Reset();
        answerHook = GetComponent<RiddleAnswerHook>();
    }

    public override bool CanInteract()
    {
        if (!base.CanInteract())
            return false;

        answerHook ??= GetComponent<RiddleAnswerHook>();
        return answerHook != null && (TaskManager.Instance == null || TaskManager.Instance.IsHookCurrentlyValid(answerHook));
    }

    public override void Interact()
    {
        if (answerHook == null)
            answerHook = GetComponent<RiddleAnswerHook>();

        answerHook?.SelectAnswer();
    }
}
