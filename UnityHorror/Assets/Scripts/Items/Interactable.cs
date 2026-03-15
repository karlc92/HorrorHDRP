using UnityEngine;

public enum InteractionMode
{
    Press,
    Hold,
}

public class Interactable : MonoBehaviour
{
    private const int DefaultInteractLayer = 9;

    [Header("Interaction")]
    [SerializeField] private InteractionMode interactionMode = InteractionMode.Press;
    [SerializeField, Min(0.01f)] private float holdDurationSeconds = 1f;
    [SerializeField] private bool resetHoldProgressOnCancel = true;

    public InteractionMode Mode => interactionMode;
    public float HoldDurationSeconds => holdDurationSeconds;
    public bool ResetHoldProgressOnCancel => resetHoldProgressOnCancel;
    public bool IsInteracting { get; private set; }
    public float CurrentProgress01 { get; private set; }

    protected virtual void Awake()
    {
        EnsureInteractLayer();
    }

    protected virtual void Reset()
    {
        EnsureInteractLayer();
    }

    protected virtual void OnValidate()
    {
        EnsureInteractLayer();
    }

    public void ConfigureInteraction(
        InteractionMode mode,
        float holdDurationSeconds,
        bool resetHoldProgressOnCancel = true)
    {
        interactionMode = mode;
        this.holdDurationSeconds = Mathf.Max(0.01f, holdDurationSeconds);
        this.resetHoldProgressOnCancel = resetHoldProgressOnCancel;
    }

    public virtual bool CanInteract()
    {
        return isActiveAndEnabled;
    }

    public virtual bool IsValidInteractionTarget()
    {
        return CanInteract();
    }

    public void BeginInteraction()
    {
        if (!CanInteract())
            return;

        if (interactionMode == InteractionMode.Press)
        {
            CompleteInteraction();
            return;
        }

        if (IsInteracting)
            return;

        IsInteracting = true;
        OnInteractionStarted();
    }

    public void TickInteraction(float deltaTime)
    {
        if (!IsInteracting || interactionMode != InteractionMode.Hold)
            return;

        float duration = Mathf.Max(0.01f, holdDurationSeconds);
        CurrentProgress01 = Mathf.Clamp01(CurrentProgress01 + (deltaTime / duration));
        OnInteractionProgressChanged(CurrentProgress01);

        if (CurrentProgress01 >= 1f)
            CompleteInteraction();
    }

    public void CancelInteraction()
    {
        if (!IsInteracting && CurrentProgress01 <= 0f)
            return;

        IsInteracting = false;

        if (resetHoldProgressOnCancel)
            SetProgress(0f);

        OnInteractionCanceled();
    }

    protected void SetProgress(float progress01)
    {
        CurrentProgress01 = Mathf.Clamp01(progress01);
        OnInteractionProgressChanged(CurrentProgress01);
    }

    protected virtual void OnInteractionStarted()
    {
    }

    protected virtual void OnInteractionCanceled()
    {
    }

    protected virtual void OnInteractionProgressChanged(float progress01)
    {
    }

    protected virtual void OnInteractionCompleted()
    {
    }

    private void CompleteInteraction()
    {
        IsInteracting = false;
        SetProgress(1f);
        Interact();
        OnInteractionCompleted();

        if (interactionMode == InteractionMode.Hold)
            SetProgress(0f);
    }

    public virtual void Interact()
    {

    }

    private void EnsureInteractLayer()
    {
        gameObject.layer = DefaultInteractLayer;
    }
}
