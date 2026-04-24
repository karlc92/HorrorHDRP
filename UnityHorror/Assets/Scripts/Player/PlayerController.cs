using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerController : MonoBehaviour
{
    const string PreviewTorchItemId = "default.torch";
    const float HeldItemReleaseDuration = 0.22f;
    const int ViewModelLayer = 11;

    [Header("Refs")]
    [SerializeField] Transform headPivot;  // rotates for pitch
    [SerializeField] Transform bobTarget;  // moves for bob (usually the Camera)
    public Transform raycastTarget;
    public Camera deathCamera;

    [Header("Look")]
    [SerializeField, Range(1f, 89f)] float pitchLimit = 85f;

    [Header("Move")]
    [SerializeField, Min(0f)] float moveSpeed = 6f;
    [SerializeField, Min(0f)] float sprintSpeed = 6f;
    [SerializeField, Min(0f)] float jumpHeight = 1f;
    [SerializeField, Min(0f)] float moveAcceleration = 45f;
    [SerializeField, Min(0f)] float moveDeceleration = 45f;

    [Header("Crouch")]
    [SerializeField, Min(0f)] float crouchCameraSharpness = 12f;
    [SerializeField, Range(0f, 1f)] float crouchSpeedModifier = 0.5f;
    [SerializeField, Min(0f)] float DefaultCharacterHeight = 2f;
    [SerializeField, Min(0f)] float CrouchedCharacterHeight = 1f;

    // NEW: headroom check controls
    [Header("Crouch - Headroom Check")]
    [SerializeField] LayerMask standObstructionMask = ~0; // everything by default
    [SerializeField, Min(0f)] float standCheckInset = 0.02f; // small shrink to avoid false positives

    [Header("Camera Bob")]
    [SerializeField] bool enableBob = true;
    [SerializeField, Min(0f)] float bobAmplitude = 0.04f;
    [SerializeField, Min(0f)] float bobFrequency = 1.8f;
    [SerializeField, Min(0f)] float bobReturnSharpness = 12f;
    [SerializeField, Min(0f)] float sprintBobAmpMultiplier = 3f;
    [SerializeField, Min(0f)] float sprintBobFreqMultiplier = 1.5f;

    [Header("Footsteps")]
    [SerializeField] AudioClip defaultFootstep;
    [SerializeField] AudioClip woodFootstep;

    [Header("Interact")]
    [SerializeField] LayerMask interactLayer = 1 << 9;
    [SerializeField] LayerMask playerLayerToIgnore = 1 << 8;
    [SerializeField, Min(0f)] float interactRange = 1.5f;

    [Header("Interact - Outline")]
    [SerializeField] bool enableInteractOutline = true;
    [Tooltip("Optional. If left empty, PlayerController will use OutlineManager.Instance.")]
    [SerializeField] OutlineManager outlineManager;

    [Header("Hands")]
    [SerializeField] GameObject leftHandPrefab;
    [SerializeField] GameObject rightHandPrefab;
    [System.NonSerialized] FirstPersonHandsSettings firstPersonHands = new FirstPersonHandsSettings();

    public bool isInDeathSequence = false;

    AudioSource footstepsAudioSource;
    CharacterController cc;
    float yaw, pitch;
    float yVel;
    bool grounded;
    float bobTime;
    float prevBobCos;
    Vector3 bobBaseLocalPos;
    float planarSpeed01;
    float outlineUpdateTime = 0f;
    Vector3 planarVelocity;
    float airborneMoveSpeed;
    Vector2 currentMoveInput;
    Vector2 currentLookInput;
    bool currentIsSprinting;

    InputAction moveAction;   // Vector2
    InputAction lookAction;   // Vector2
    InputAction sprintAction; // Button
    InputAction crouchAction; // Button
    InputAction interactAction; // Button
    InputAction dropAction; // Button
    InputAction jumpAction; // Button
    InputAction leftHandRaiseAction; // Button
    InputAction rightHandRaiseAction; // Button
    InputAction toggleTorchAction; // Button

    bool isCrouched;

    Vector3 ccCenterBase;
    Vector3 headPivotBaseLocalPos;

    Interactable hoveredInteractable;
    Interactable activeInteractable;
    FirstPersonHandsRig handsRig;
    GameObject activeHeldItemInstance;
    string activeHeldItemId;
    FirstPersonHandSide activeHeldItemSide;
    Transform activeHeldItemAnchor;
    HeldItemDefinition activeHeldItemDefinition;
    bool isPreviewHeldItemActive;
    float heldItemReleaseTimer;
    bool isTorchToggledOn;
    Camera firstPersonCamera;
    CustomPassVolume viewModelPassVolume;
    DrawRenderersCustomPass viewModelOpaquePass;
    DrawRenderersCustomPass viewModelTransparentPass;

    void Awake()
    {
        firstPersonHands = new FirstPersonHandsSettings();
        cc = GetComponent<CharacterController>();
        ccCenterBase = cc.center;

        if (!headPivot)
            headPivot = GetComponentInChildren<Camera>()?.transform; // fallback (not ideal)
        if (!bobTarget && headPivot)
            bobTarget = headPivot; // fallback

        firstPersonCamera = bobTarget != null ? bobTarget.GetComponent<Camera>() : null;
        if (firstPersonCamera == null && bobTarget != null)
            firstPersonCamera = bobTarget.GetComponentInChildren<Camera>(true);

        if (headPivot) headPivotBaseLocalPos = headPivot.localPosition;
        if (bobTarget) bobBaseLocalPos = bobTarget.localPosition;

        if (!footstepsAudioSource) footstepsAudioSource = GetComponent<AudioSource>();

        SetupInput();
        SetupViewModelRendering();
        InitializeFirstPersonHands();

        ApplyCharacterHeight(DefaultCharacterHeight);

        if (headPivot)
        {
            Vector3 target = new Vector3(headPivotBaseLocalPos.x, cc.height, headPivotBaseLocalPos.z);
            headPivot.localPosition = target;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        Game.Settings.InputBindingsChanged += RefreshInputBindings;

        moveAction?.Enable();
        lookAction?.Enable();
        sprintAction?.Enable();
        crouchAction?.Enable();
        interactAction?.Enable();
        dropAction?.Enable();
        jumpAction?.Enable();
        leftHandRaiseAction?.Enable();
        rightHandRaiseAction?.Enable();
        toggleTorchAction?.Enable();
    }

    void OnDisable()
    {
        Game.Settings.InputBindingsChanged -= RefreshInputBindings;

        moveAction?.Disable();
        lookAction?.Disable();
        sprintAction?.Disable();
        crouchAction?.Disable();
        interactAction?.Disable();
        dropAction?.Disable();
        jumpAction?.Disable();
        leftHandRaiseAction?.Disable();
        rightHandRaiseAction?.Disable();
        toggleTorchAction?.Disable();
    }

    void OnDestroy()
    {
        DisposeInput();
        ClearHeldItemVisual();
        CleanupViewModelRendering();
        handsRig?.Dispose();
    }

    void SetupInput()
    {
        DisposeInput();

        PlayerInputSettings settings = Game.Settings.Input;

        // Move: keyboard bindings + gamepad left stick
        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", settings.MoveUp)
            .With("Down", settings.MoveDown)
            .With("Left", settings.MoveLeft)
            .With("Right", settings.MoveRight);
        moveAction.AddBinding(settings.MoveGamepad);

        // Look: mouse/pointer delta + gamepad right stick
        lookAction = new InputAction("Look", InputActionType.Value, expectedControlType: "Vector2");
        lookAction.AddBinding(settings.LookPointer);
        lookAction.AddBinding(settings.LookGamepad);

        // Sprint: keyboard + gamepad stick press
        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding(settings.SprintKeyboard);
        sprintAction.AddBinding(settings.SprintGamepad);

        // Crouch
        crouchAction = new InputAction("Crouch", InputActionType.Button);
        crouchAction.AddBinding(settings.CrouchKeyboard);

        // Interact
        interactAction = new InputAction("Interact", InputActionType.Button);
        interactAction.AddBinding(settings.InteractKeyboard);

        // Drop carry item
        dropAction = new InputAction("Drop", InputActionType.Button);
        dropAction.AddBinding(settings.DropKeyboard);

        // Jump
        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding(settings.JumpKeyboard);

        leftHandRaiseAction = new InputAction("RaiseLeftHand", InputActionType.Button);
        leftHandRaiseAction.AddBinding("<Mouse>/leftButton");

        rightHandRaiseAction = new InputAction("RaiseRightHand", InputActionType.Button);
        rightHandRaiseAction.AddBinding("<Mouse>/rightButton");

        toggleTorchAction = new InputAction("ToggleTorch", InputActionType.Button);
        toggleTorchAction.AddBinding("<Keyboard>/o");

        if (isActiveAndEnabled)
        {
            moveAction.Enable();
            lookAction.Enable();
            sprintAction.Enable();
            crouchAction.Enable();
            interactAction.Enable();
            dropAction.Enable();
            jumpAction.Enable();
            leftHandRaiseAction.Enable();
            rightHandRaiseAction.Enable();
            toggleTorchAction.Enable();
        }
    }

    void RefreshInputBindings()
    {
        SetupInput();
    }

    void DisposeInput()
    {
        moveAction?.Dispose();
        lookAction?.Dispose();
        sprintAction?.Dispose();
        crouchAction?.Dispose();
        interactAction?.Dispose();
        dropAction?.Dispose();
        jumpAction?.Dispose();
        leftHandRaiseAction?.Dispose();
        rightHandRaiseAction?.Dispose();
        toggleTorchAction?.Dispose();

        moveAction = null;
        lookAction = null;
        sprintAction = null;
        crouchAction = null;
        interactAction = null;
        dropAction = null;
        jumpAction = null;
        leftHandRaiseAction = null;
        rightHandRaiseAction = null;
        toggleTorchAction = null;
    }

    void Update()
    {
        if (isInDeathSequence)
        {
            if (!deathCamera.gameObject.activeSelf)
                deathCamera.gameObject.SetActive(true);

            if (bobTarget.gameObject.activeSelf)
                bobTarget.gameObject.SetActive(false);
        }
        else if (deathCamera != null)
        {
            if (deathCamera.gameObject.activeSelf)
                deathCamera.gameObject.SetActive(false);

            if (!bobTarget.gameObject.activeSelf)
                bobTarget.gameObject.SetActive(true);
        }

        if (PreventInput())
        {
            UpdateFirstPersonHands(forceLowered: true);
            CancelActiveInteraction();
            return;
        }

        Look();
        UpdateCrouch();
        MoveAndGravity();
        CameraBob();
        UpdateTorchToggle();
        UpdateHeldItemVisual();
        UpdateFirstPersonHands();
        UpdateInteraction();

    }

    private void FixedUpdate()
    {
        Game.State.PlayerPos = this.transform.position;
        Game.State.PlayerRot = this.transform.rotation;

        if (outlineUpdateTime < Time.time)
        {
            outlineUpdateTime = Time.time + Random.Range(0.03f, 0.07f);
            UpdateInteractOutline();
        }
    }

    bool PreventInput()
    {
        return Console.IsShowing() || isInDeathSequence || (InspectionManager.Instance && InspectionManager.Instance.IsOpen);
    }

    void Look()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();
        currentLookInput = look;
        float sensScale = 0.022f;
        float mx = look.x * sensScale * Game.Settings.MouseSensitivity;
        float my = look.y * sensScale * Game.Settings.MouseSensitivity;

        yaw += mx;
        pitch = Mathf.Clamp(pitch - my, -pitchLimit, pitchLimit);

        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        if (headPivot) headPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void MoveAndGravity()
    {
        Vector2 move = moveAction.ReadValue<Vector2>();
        currentMoveInput = move;
        float x = move.x;
        float z = move.y;

        bool isSprinting = grounded && sprintAction.IsPressed() && !isCrouched;
        currentIsSprinting = isSprinting;

        bool movingBackwards = z < 0;
        float backwardsMult = (movingBackwards ? 0.35f : 1);

        Vector3 planar = (transform.right * x * backwardsMult) + (transform.forward * z * backwardsMult);
        float planarMag = planar.magnitude;
        if (planarMag > 1f) planar /= planarMag;

        planarSpeed01 = Mathf.Clamp01(planarMag);

        if (grounded && yVel < 0f) yVel = -2f;

        float groundedBaseSpeed = (isSprinting && !movingBackwards ? sprintSpeed : moveSpeed);
        if (isCrouched) groundedBaseSpeed *= crouchSpeedModifier;

        if (grounded && !isCrouched && jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            airborneMoveSpeed = groundedBaseSpeed;
            yVel = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            grounded = false;
        }

        yVel += Physics.gravity.y * Time.deltaTime;

        float baseSpeed = grounded ? groundedBaseSpeed : airborneMoveSpeed;

        Vector3 targetPlanarVelocity = planar * baseSpeed;
        float moveRate = targetPlanarVelocity.sqrMagnitude > 0.0001f ? moveAcceleration : moveDeceleration;
        planarVelocity = Vector3.MoveTowards(planarVelocity, targetPlanarVelocity, moveRate * Time.deltaTime);

        Vector3 vel = planarVelocity;
        vel.y = yVel;

        CollisionFlags flags = cc.Move(vel * Time.deltaTime);
        grounded = (flags & CollisionFlags.Below) != 0;

        if (grounded)
            airborneMoveSpeed = groundedBaseSpeed;
    }

    void UpdateCrouch()
    {
        bool wantsCrouch = crouchAction != null && crouchAction.IsPressed();

        // NEW LOGIC:
        // - If holding crouch -> crouch.
        // - If released crouch while crouched -> only stand if there's room.
        if (wantsCrouch)
        {
            isCrouched = true;
        }
        else if (isCrouched)
        {
            // key released, try to stand up
            if (CanStandUp())
                isCrouched = false;
            else
                isCrouched = true; // stay crouched until room exists
        }

        float targetHeight = isCrouched ? CrouchedCharacterHeight : DefaultCharacterHeight;
        ApplyCharacterHeight(targetHeight);

        if (headPivot)
        {
            Vector3 target = new Vector3(headPivotBaseLocalPos.x, cc.height, headPivotBaseLocalPos.z);
            headPivot.localPosition = SmoothTo(headPivot.localPosition, target, crouchCameraSharpness);
        }
    }

    // NEW: checks if the player can safely return to standing height
    bool CanStandUp()
    {
        float targetHeight = DefaultCharacterHeight;

        // Use a slightly smaller radius / inset to reduce false positives from tiny overlaps.
        float radius = Mathf.Max(0.01f, cc.radius - 0.01f);

        // Build the would-be standing capsule using the same bottom-anchoring scheme as ApplyCharacterHeight.
        Vector3 localCenter = new Vector3(ccCenterBase.x, targetHeight * 0.5f, ccCenterBase.z);
        Vector3 worldCenter = transform.TransformPoint(localCenter);

        Vector3 up = transform.up;
        float half = targetHeight * 0.5f;
        float pointOffset = Mathf.Max(0f, half - radius);

        // Inset endpoints a little (and clamp inset so we don't invert the capsule)
        float inset = Mathf.Clamp(standCheckInset, 0f, Mathf.Max(0f, pointOffset - 0.001f));

        Vector3 bottom = worldCenter - up * pointOffset + up * inset;
        Vector3 top = worldCenter + up * pointOffset - up * inset;

        int mask = standObstructionMask.value & ~(1 << gameObject.layer);

        bool blocked = Physics.CheckCapsule(
            bottom,
            top,
            radius,
            mask,
            QueryTriggerInteraction.Ignore
        );

        return !blocked;
    }

    void ApplyCharacterHeight(float height)
    {
        cc.height = height;
        cc.center = new Vector3(ccCenterBase.x, height * 0.5f, ccCenterBase.z);
    }

    void CameraBob()
    {
        if (!bobTarget) return;

        Vector3 basePos = bobBaseLocalPos;

        if (bobTarget == headPivot)
        {
            basePos = new Vector3(bobBaseLocalPos.x, cc.height, bobBaseLocalPos.z);
        }

        if (!enableBob)
        {
            bobTarget.localPosition = SmoothTo(bobTarget.localPosition, basePos, bobReturnSharpness);
            return;
        }

        bool movingOnGround = grounded && planarSpeed01 > 0.01f;
        bool isSprinting = sprintAction.IsPressed() && !isCrouched;
        bool movingBackwards = moveAction.ReadValue<Vector2>().y < 0f;
        float backwardsMult = (movingBackwards ? 0.75f : 1);

        if (movingOnGround)
        {
            bobTime += Time.deltaTime * (bobFrequency * (isSprinting && !movingBackwards ? sprintBobFreqMultiplier : backwardsMult) * 2f * Mathf.PI) * Mathf.Lerp(0.6f, 1f, planarSpeed01);

            float bobSin = Mathf.Sin(bobTime);
            float bobCos = Mathf.Cos(bobTime);

            if (prevBobCos > 0f && bobCos <= 0f && bobSin > 0f)
            {
                PlayFootstep();
            }
            prevBobCos = bobCos;

            float y = bobSin * bobAmplitude * (isSprinting && !movingBackwards ? sprintBobAmpMultiplier : backwardsMult);
            float x = Mathf.Cos(bobTime * 0.5f) * (bobAmplitude * (isSprinting && !movingBackwards ? sprintBobAmpMultiplier : backwardsMult) * 0.5f);

            Vector3 target = basePos + new Vector3(x, y, 0f);
            bobTarget.localPosition = SmoothTo(bobTarget.localPosition, target, bobReturnSharpness);
        }
        else
        {
            bobTime = 0f;
            prevBobCos = 1f;
            bobTarget.localPosition = SmoothTo(bobTarget.localPosition, basePos, bobReturnSharpness);
        }
    }

    void TryInteract()
    {
        // Prefer the last FixedUpdate hover result to avoid doing another raycast on key press.
        // (Fallback raycast remains in place for safety.)
        if (hoveredInteractable != null)
        {
            StartInteraction(hoveredInteractable);
            return;
        }

        // Always cast from the center of the player camera (bobTarget).
        Transform t = bobTarget ? bobTarget : (headPivot ? headPivot : transform);
        Vector3 origin = t.position;
        Vector3 dir = t.forward;

        int mask = interactLayer.value & ~playerLayerToIgnore.value;

        if (Physics.Raycast(origin, dir, out var hit, interactRange, mask, QueryTriggerInteraction.Ignore))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
            if (interactable != null && interactable.IsValidInteractionTarget())
            {
                StartInteraction(interactable);
                return;
            }
        }
    }

    void UpdateInteractOutline()
    {
        // Never do per-frame raycasts in Update; we keep this in FixedUpdate for the requested performance pattern.
        // This uses the same origin/dir/mask/range criteria as TryInteract().

        OutlineManager om = outlineManager != null ? outlineManager : OutlineManager.Instance;

        if (!enableInteractOutline || PreventInput())
        {
            hoveredInteractable = null;
            if (om != null) om.ClearTarget();
            return;
        }

        Transform t = bobTarget ? bobTarget : (headPivot ? headPivot : transform);
        Vector3 origin = t.position;
        Vector3 dir = t.forward;

        int mask = interactLayer.value & ~playerLayerToIgnore.value;

        Interactable next = null;
        if (Physics.Raycast(origin, dir, out var hit, interactRange, mask, QueryTriggerInteraction.Ignore))
        {
            next = hit.collider.GetComponentInParent<Interactable>();
            if (next != null && !next.IsValidInteractionTarget())
                next = null;
        }

        // Only update the outline system when the hovered interactable changes.
        if (activeInteractable != null && next != activeInteractable)
            CancelActiveInteraction();

        if (next == hoveredInteractable)
            return;

        hoveredInteractable = next;

        if (om == null)
            return;

        if (hoveredInteractable != null)
            om.SetTarget(hoveredInteractable);
        else
            om.ClearTarget();
    }

    void PlayFootstep()
    {
        Vector3 origin = transform.position + cc.center + Vector3.down * (cc.height * 0.5f - cc.radius + 0.02f);
        if (Physics.Raycast(origin, Vector3.down, out var hit, 2f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.CompareTag("Wood"))
        {
            footstepsAudioSource.Stop();

            if (footstepsAudioSource.clip != woodFootstep)
                footstepsAudioSource.clip = woodFootstep;

            footstepsAudioSource.pitch = Random.Range(0.95f, 1.05f);
            footstepsAudioSource.volume = Game.Settings.MasterVolume * 0.15f;
            footstepsAudioSource.Play();
            return;
        }

        footstepsAudioSource.Stop();

        if (footstepsAudioSource.clip != defaultFootstep)
            footstepsAudioSource.clip = defaultFootstep;

        footstepsAudioSource.pitch = Random.Range(0.95f, 1.05f);
        footstepsAudioSource.volume = Game.Settings.MasterVolume * 0.15f;
        footstepsAudioSource.Play();
    }

    public void ApplySavedPose(Vector3 position, Quaternion rotation)
    {
        if (!cc) cc = GetComponent<CharacterController>();

        bool ccWasEnabled = cc != null && cc.enabled;
        if (cc != null) cc.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        // Keep internal look state aligned so the next Look() call doesn't snap us back.
        yaw = rotation.eulerAngles.y;
        pitch = 0f;
        if (headPivot) headPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (cc != null)
        {
            cc.enabled = ccWasEnabled;
            if (cc.enabled) cc.Move(Vector3.zero); // flush internal state
        }
    }

    static Vector3 SmoothTo(Vector3 current, Vector3 target, float sharpness)
    {
        float t = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
        return Vector3.Lerp(current, target, t);
    }

    void UpdateInteraction()
    {
        if (interactAction == null)
            return;

        if (activeInteractable != null)
        {
            bool stillHolding = interactAction.IsPressed();
            bool stillFocused = hoveredInteractable == activeInteractable;

            if (!stillHolding || !stillFocused || !activeInteractable.CanInteract())
            {
                CancelActiveInteraction();
            }
            else
            {
                activeInteractable.TickInteraction(Time.deltaTime);
                if (!activeInteractable.IsInteracting)
                    activeInteractable = null;
            }
        }

        if (interactAction.WasPressedThisFrame())
            TryInteract();
    }

    void StartInteraction(Interactable interactable)
    {
        if (interactable == null || !interactable.IsValidInteractionTarget())
            return;

        interactable.BeginInteraction();

        if (interactable.Mode == InteractionMode.Hold && interactable.IsInteracting)
            activeInteractable = interactable;
    }

    void CancelActiveInteraction()
    {
        if (activeInteractable == null)
            return;

        activeInteractable.CancelInteraction();
        activeInteractable = null;
    }

    public void SetHandStance(FirstPersonHandSide side, FirstPersonHandStance stance)
    {
        handsRig?.SetStance(side, stance);
    }

    public Transform GetHandItemAnchor(FirstPersonHandSide side)
    {
        return handsRig != null ? handsRig.GetItemAnchor(side) : null;
    }

    void UpdateHeldItemVisual()
    {
        activeHeldItemDefinition = null;
        isPreviewHeldItemActive = false;

        if (handsRig == null || !handsRig.IsInitialized || InventoryManager.Instance == null)
        {
            ClearHeldItemVisual();
            return;
        }

        bool showPreviewTorch = isTorchToggledOn || (rightHandRaiseAction != null && rightHandRaiseAction.IsPressed());
        string desiredItemId = showPreviewTorch ? PreviewTorchItemId : InventoryManager.Instance.GetActiveItemId();
        HeldItemDefinition desiredDefinition = showPreviewTorch
            ? InventoryManager.Instance.GetDefinition(PreviewTorchItemId)
            : InventoryManager.Instance.GetActiveDefinition();
        if (desiredDefinition == null || desiredDefinition.FirstPersonPrefab == null)
        {
            if (activeHeldItemInstance != null)
            {
                heldItemReleaseTimer -= Time.deltaTime;
                if (heldItemReleaseTimer <= 0f)
                    ClearHeldItemVisual();
            }
            else
            {
                ClearHeldItemVisual();
            }
            return;
        }

        Transform desiredAnchor = GetHandItemAnchor(desiredDefinition.FirstPersonHandSide);
        if (desiredAnchor == null)
        {
            ClearHeldItemVisual();
            return;
        }

        bool needsRespawn =
            activeHeldItemInstance == null ||
            !string.Equals(activeHeldItemId, desiredItemId, System.StringComparison.OrdinalIgnoreCase) ||
            activeHeldItemSide != desiredDefinition.FirstPersonHandSide ||
            activeHeldItemAnchor != desiredAnchor;

        if (needsRespawn)
        {
            ClearHeldItemVisual();

            activeHeldItemInstance = Instantiate(desiredDefinition.FirstPersonPrefab, desiredAnchor, false);
            activeHeldItemId = desiredItemId;
            activeHeldItemSide = desiredDefinition.FirstPersonHandSide;
            activeHeldItemAnchor = desiredAnchor;

            SetLayerRecursive(activeHeldItemInstance.transform, ViewModelLayer);
            ConfigureHeldItemRenderers(activeHeldItemInstance);
        }

        heldItemReleaseTimer = HeldItemReleaseDuration;
        activeHeldItemDefinition = desiredDefinition;
        isPreviewHeldItemActive = showPreviewTorch;
        ApplyHeldItemTransform(activeHeldItemDefinition, activeHeldItemInstance.transform);
    }

    void ApplyHeldItemTransform(HeldItemDefinition definition, Transform itemTransform)
    {
        if (definition == null || itemTransform == null)
            return;

        itemTransform.localPosition = definition.FirstPersonLocalPosition;
        itemTransform.localRotation = Quaternion.Euler(definition.FirstPersonLocalEuler);
        itemTransform.localScale = definition.FirstPersonLocalScale;
    }

    void ConfigureHeldItemRenderers(GameObject instance)
    {
        if (instance == null)
            return;

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                skinnedMeshRenderer.updateWhenOffscreen = true;

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }
    }

    void ClearHeldItemVisual()
    {
        if (activeHeldItemInstance != null)
            Destroy(activeHeldItemInstance);

        activeHeldItemInstance = null;
        activeHeldItemId = null;
        activeHeldItemAnchor = null;
        activeHeldItemDefinition = null;
        isPreviewHeldItemActive = false;
        heldItemReleaseTimer = 0f;
        activeHeldItemSide = default;
    }

    void UpdateTorchToggle()
    {
        if (toggleTorchAction != null && toggleTorchAction.WasPressedThisFrame())
            isTorchToggledOn = !isTorchToggledOn;
    }

    static void SetLayerRecursive(Transform root, int layer)
    {
        if (root == null)
            return;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }

    void InitializeFirstPersonHands()
    {
        if (!firstPersonHands.Enabled)
            return;

        if (handsRig == null)
            handsRig = new FirstPersonHandsRig();

        Transform handParent = bobTarget != null ? bobTarget : (headPivot != null ? headPivot : transform);
        handsRig.Initialize(handParent, firstPersonHands, leftHandPrefab, rightHandPrefab, ViewModelLayer);
    }

    void SetupViewModelRendering()
    {
        int mask = 1 << ViewModelLayer;

        if (firstPersonCamera != null)
            firstPersonCamera.cullingMask &= ~mask;

        if (deathCamera != null)
            deathCamera.cullingMask &= ~mask;

        if (firstPersonCamera == null)
            return;

        GameObject volumeObject = new GameObject("ViewModel Custom Pass");
        volumeObject.hideFlags = HideFlags.HideAndDontSave;
        volumeObject.transform.SetParent(firstPersonCamera.transform, false);

        viewModelPassVolume = volumeObject.AddComponent<CustomPassVolume>();
        viewModelPassVolume.isGlobal = true;
        viewModelPassVolume.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;
        viewModelPassVolume.targetCamera = firstPersonCamera;
        viewModelPassVolume.hideFlags = HideFlags.HideAndDontSave;

        viewModelOpaquePass = viewModelPassVolume.AddPassOfType(typeof(DrawRenderersCustomPass)) as DrawRenderersCustomPass;
        viewModelTransparentPass = viewModelPassVolume.AddPassOfType(typeof(DrawRenderersCustomPass)) as DrawRenderersCustomPass;
        if (viewModelOpaquePass == null || viewModelTransparentPass == null)
            return;

        viewModelOpaquePass.name = "ViewModel Opaque Pass";
        viewModelOpaquePass.renderQueueType = CustomPass.RenderQueueType.AllOpaque;
        viewModelOpaquePass.layerMask = mask;
        viewModelOpaquePass.sortingCriteria = SortingCriteria.CommonOpaque;
        viewModelOpaquePass.clearFlags = ClearFlag.Depth;
        viewModelOpaquePass.overrideDepthState = true;
        viewModelOpaquePass.depthWrite = true;
        viewModelOpaquePass.depthCompareFunction = CompareFunction.LessEqual;
        viewModelOpaquePass.targetColorBuffer = CustomPass.TargetBuffer.Camera;
        viewModelOpaquePass.targetDepthBuffer = CustomPass.TargetBuffer.Custom;

        viewModelTransparentPass.name = "ViewModel Transparent Pass";
        viewModelTransparentPass.renderQueueType = CustomPass.RenderQueueType.AllTransparent;
        viewModelTransparentPass.layerMask = mask;
        viewModelTransparentPass.sortingCriteria = SortingCriteria.CommonTransparent;
        viewModelTransparentPass.clearFlags = ClearFlag.None;
        viewModelTransparentPass.overrideDepthState = true;
        viewModelTransparentPass.depthWrite = false;
        viewModelTransparentPass.depthCompareFunction = CompareFunction.LessEqual;
        viewModelTransparentPass.targetColorBuffer = CustomPass.TargetBuffer.Camera;
        viewModelTransparentPass.targetDepthBuffer = CustomPass.TargetBuffer.Custom;
    }

    void CleanupViewModelRendering()
    {
        if (viewModelPassVolume != null)
            Destroy(viewModelPassVolume.gameObject);

        viewModelPassVolume = null;
        viewModelOpaquePass = null;
        viewModelTransparentPass = null;
    }

    void UpdateFirstPersonHands(bool forceLowered = false)
    {
        if (handsRig == null || !handsRig.IsInitialized)
            return;

        HeldItemDefinition previewTorchDefinition = !forceLowered && (isTorchToggledOn || (rightHandRaiseAction != null && rightHandRaiseAction.IsPressed())) && InventoryManager.Instance != null
            ? InventoryManager.Instance.GetDefinition(PreviewTorchItemId)
            : null;

        bool hasPreviewTorch = previewTorchDefinition != null && previewTorchDefinition.FirstPersonPrefab != null;
        bool hasEquippedLeftItem = activeHeldItemDefinition != null
            && !isPreviewHeldItemActive
            && activeHeldItemDefinition.FirstPersonHandSide == FirstPersonHandSide.Left;
        bool hasEquippedRightItem = activeHeldItemDefinition != null
            && !isPreviewHeldItemActive
            && activeHeldItemDefinition.FirstPersonHandSide == FirstPersonHandSide.Right;

        FirstPersonHandStance leftStance = FirstPersonHandStance.None;
        FirstPersonHandStance rightStance = FirstPersonHandStance.None;

        if (!forceLowered)
        {
            if (hasPreviewTorch)
            {
                if (previewTorchDefinition.FirstPersonHandSide == FirstPersonHandSide.Left)
                    leftStance = previewTorchDefinition.FirstPersonHandStance;
                else
                    rightStance = previewTorchDefinition.FirstPersonHandStance;
            }

            if (hasEquippedLeftItem)
                leftStance = activeHeldItemDefinition.FirstPersonHandStance;

            if (hasEquippedRightItem)
                rightStance = activeHeldItemDefinition.FirstPersonHandStance;
        }

        handsRig.SetStance(FirstPersonHandSide.Left, leftStance);
        handsRig.SetStance(FirstPersonHandSide.Right, rightStance);
        handsRig.SetRaiseInput(FirstPersonHandSide.Left, true);
        handsRig.SetRaiseInput(FirstPersonHandSide.Right, true);
        handsRig.Tick(Time.deltaTime, currentMoveInput, currentLookInput, planarSpeed01, grounded, currentIsSprinting, yVel);
    }

}
