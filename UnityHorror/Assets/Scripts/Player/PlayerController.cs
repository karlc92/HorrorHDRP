using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerController : MonoBehaviour
{
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

    [Header("Crouch")]
    [SerializeField, Min(0f)] float crouchCameraDrop = 0.6f;
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

    InputAction moveAction;   // Vector2
    InputAction lookAction;   // Vector2
    InputAction sprintAction; // Button
    InputAction crouchAction; // Button
    InputAction interactAction; // Button

    bool isCrouched;
    Vector3 crouchOffsetLocal;

    Vector3 ccCenterBase;
    Vector3 headPivotBaseLocalPos;

    Interactable hoveredInteractable;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        ccCenterBase = cc.center;

        if (!headPivot)
            headPivot = GetComponentInChildren<Camera>()?.transform; // fallback (not ideal)
        if (!bobTarget && headPivot)
            bobTarget = headPivot; // fallback

        if (headPivot) headPivotBaseLocalPos = headPivot.localPosition;
        if (bobTarget) bobBaseLocalPos = bobTarget.localPosition;

        if (!footstepsAudioSource) footstepsAudioSource = GetComponent<AudioSource>();

        SetupInput();

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
        moveAction?.Enable();
        lookAction?.Enable();
        sprintAction?.Enable();
        crouchAction?.Enable();
        interactAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        sprintAction?.Disable();
        crouchAction?.Disable();
        interactAction?.Disable();
    }

    void SetupInput()
    {
        // Move: WASD + gamepad left stick
        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        // Look: mouse/pointer delta + gamepad right stick
        lookAction = new InputAction("Look", InputActionType.Value, expectedControlType: "Vector2");
        lookAction.AddBinding("<Pointer>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick");

        // Sprint: left shift (optionally gamepad stick press)
        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");

        // Crouch: C
        crouchAction = new InputAction("Crouch", InputActionType.Button);
        crouchAction.AddBinding("<Keyboard>/c");

        // Interact: E
        interactAction = new InputAction("Interact", InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");
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
        else
        {
            if (deathCamera.gameObject.activeSelf)
                deathCamera.gameObject.SetActive(false);

            if (!bobTarget.gameObject.activeSelf)
                bobTarget.gameObject.SetActive(true);
        }

        if (PreventInput())
            return;

        Look();
        UpdateCrouch();
        MoveAndGravity();
        CameraBob();

        if (interactAction != null && interactAction.WasPressedThisFrame())
            TryInteract();
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
        float x = move.x;
        float z = move.y;

        bool isSprinting = sprintAction.IsPressed() && !isCrouched;

        bool movingBackwards = z < 0;
        float backwardsMult = (movingBackwards ? 0.35f : 1);

        Vector3 planar = (transform.right * x * backwardsMult) + (transform.forward * z * backwardsMult);
        float planarMag = planar.magnitude;
        if (planarMag > 1f) planar /= planarMag;

        planarSpeed01 = Mathf.Clamp01(planarMag);

        if (grounded && yVel < 0f) yVel = -2f;
        yVel += Physics.gravity.y * Time.deltaTime;

        float baseSpeed = (isSprinting && !movingBackwards ? sprintSpeed : moveSpeed);
        if (isCrouched) baseSpeed *= crouchSpeedModifier;

        Vector3 vel = planar * baseSpeed;
        vel.y = yVel;

        CollisionFlags flags = cc.Move(vel * Time.deltaTime);
        grounded = (flags & CollisionFlags.Below) != 0;
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
            hoveredInteractable.Interact();
            return;
        }

        // Always cast from the center of the player camera (bobTarget).
        Transform t = bobTarget ? bobTarget : (headPivot ? headPivot : transform);
        Vector3 origin = t.position;
        Vector3 dir = t.forward;

        Debug.DrawLine(origin, origin + dir * interactRange, Color.yellow);

        int mask = interactLayer.value & ~playerLayerToIgnore.value;

        if (Physics.Raycast(origin, dir, out var hit, interactRange, mask, QueryTriggerInteraction.Ignore))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
            if (interactable != null)
                interactable.Interact();
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
            next = hit.collider.GetComponentInParent<Interactable>();

        // Only update the outline system when the hovered interactable changes.
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

    static Vector3 SmoothTo(Vector3 current, Vector3 target, float sharpness)
    {
        float t = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
        return Vector3.Lerp(current, target, t);
    }
}