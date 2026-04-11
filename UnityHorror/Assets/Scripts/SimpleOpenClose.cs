using UnityEngine;

public class SimpleOpenClose : Interactable
{
    private const string OpenStateName = "Open";
    private const string CloseStateName = "Close";

    private Animator myAnimator;
    private Animator additionalAnimator;
    private SimpleOpenClose additionalOpenClose;

    public bool objectOpen;
    public bool objectOpenAdditional;
    public GameObject animateAdditional;

    private bool hasAdditional;

    protected override void Awake()
    {
        base.Awake();
        CacheReferences();
    }

    void Start()
    {
        CacheReferences();
        ApplyInitialState();
    }

    public override void Interact()
    {
        CacheReferences();

        if (myAnimator == null)
            return;

        float normalizedTime = myAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        if (normalizedTime < 1f)
            return;

        if (!hasAdditional)
        {
            PlayState(myAnimator, !objectOpen);
            objectOpen = !objectOpen;
            return;
        }

        bool nextOpenState = !objectOpen;

        PlayState(myAnimator, nextOpenState);
        objectOpen = nextOpenState;

        if (additionalOpenClose != null)
            additionalOpenClose.objectOpenAdditional = nextOpenState;

        if (additionalAnimator != null)
            PlayState(additionalAnimator, nextOpenState);

        objectOpenAdditional = nextOpenState;

        if (additionalOpenClose != null)
            additionalOpenClose.objectOpen = nextOpenState;
    }

    void CacheReferences()
    {
        if (myAnimator == null)
            myAnimator = GetComponent<Animator>() ?? GetComponentInParent<Animator>();

        hasAdditional = false;
        additionalAnimator = null;
        additionalOpenClose = null;

        if (animateAdditional == null)
            return;

        additionalOpenClose = animateAdditional.GetComponent<SimpleOpenClose>();
        if (additionalOpenClose == null)
            return;

        additionalAnimator = animateAdditional.GetComponent<Animator>();
        hasAdditional = additionalAnimator != null;

        if (hasAdditional)
            objectOpenAdditional = additionalOpenClose.objectOpen;
    }

    void ApplyInitialState()
    {
        if (myAnimator != null && objectOpen)
            myAnimator.Play(OpenStateName, 0, 1f);

        if (additionalAnimator != null && objectOpenAdditional)
            additionalAnimator.Play(OpenStateName, 0, 1f);
    }

    static void PlayState(Animator animator, bool open)
    {
        if (animator == null)
            return;

        animator.Play(open ? OpenStateName : CloseStateName, 0, 0f);
    }
}
