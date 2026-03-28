using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum FirstPersonHandSide
{
    Left,
    Right
}

public enum FirstPersonHandStance
{
    Open,
    LanternTop,
    TorchSide
}

[Serializable]
public sealed class FirstPersonHandPoseDefinition
{
    public Vector3 LocalPosition;
    public Vector3 LocalEuler;
    public Vector3 UpperArmEuler;
    public Vector3 LowerArm1Euler;
    public Vector3 LowerArm2Euler;
    public Vector3 LowerArm3Euler;
    public Vector3 HandEuler;
    public float ThumbCurl;
    public float ThumbSplay;
    public float ThumbRoll;
    public float IndexCurl;
    public float MiddleCurl;
    public float RingCurl;
    public float PinkyCurl;
    public float FingerSplay;
    public Vector3 ItemAnchorLocalPosition = new Vector3(0f, -0.01f, 0.02f);
    public Vector3 ItemAnchorLocalEuler;
}

[Serializable]
public sealed class FirstPersonSingleHandSettings
{
    public Vector3 HiddenLocalPosition;
    public Vector3 HiddenLocalEuler;
    public float BobPhaseOffset;
    public FirstPersonHandPoseDefinition Open = new FirstPersonHandPoseDefinition();
    public FirstPersonHandPoseDefinition LanternTop = new FirstPersonHandPoseDefinition();
    public FirstPersonHandPoseDefinition TorchSide = new FirstPersonHandPoseDefinition();

    public static FirstPersonSingleHandSettings CreateDefaultLeft()
    {
        return new FirstPersonSingleHandSettings
        {
            HiddenLocalPosition = new Vector3(-0.28f, -0.56f, 0.18f),
            HiddenLocalEuler = new Vector3(38f, 10f, -26f),
            BobPhaseOffset = 0f,
            Open = new FirstPersonHandPoseDefinition
            {
                LocalPosition = Vector3.zero,
                LocalEuler = Vector3.zero,
                UpperArmEuler = Vector3.zero,
                LowerArm1Euler = Vector3.zero,
                LowerArm2Euler = Vector3.zero,
                LowerArm3Euler = Vector3.zero,
                HandEuler = Vector3.zero,
                ThumbCurl = 0f,
                ThumbSplay = 0f,
                ThumbRoll = 0f,
                IndexCurl = 0f,
                MiddleCurl = 0f,
                RingCurl = 0f,
                PinkyCurl = 0f,
                FingerSplay = 0f,
                ItemAnchorLocalPosition = new Vector3(0f, -0.012f, 0.03f),
                ItemAnchorLocalEuler = new Vector3(-8f, 0f, -90f),
            },
            LanternTop = new FirstPersonHandPoseDefinition
            {
                LocalPosition = Vector3.zero,
                LocalEuler = new Vector3(-2f, 0f, -6f),
                UpperArmEuler = new Vector3(-3f, 0f, -4f),
                LowerArm1Euler = new Vector3(8f, 0f, -4f),
                LowerArm2Euler = new Vector3(7f, 0f, -3f),
                LowerArm3Euler = new Vector3(5f, 0f, -2f),
                HandEuler = new Vector3(14f, -8f, -8f),
                ThumbCurl = 0.36f,
                ThumbSplay = 0.1f,
                ThumbRoll = -10f,
                IndexCurl = 0.42f,
                MiddleCurl = 0.5f,
                RingCurl = 0.54f,
                PinkyCurl = 0.48f,
                FingerSplay = 0.04f,
                ItemAnchorLocalPosition = new Vector3(-0.002f, -0.018f, 0.04f),
                ItemAnchorLocalEuler = new Vector3(-70f, 0f, 180f),
            },
            TorchSide = new FirstPersonHandPoseDefinition
            {
                LocalPosition = Vector3.zero,
                LocalEuler = new Vector3(-1f, -3f, -8f),
                UpperArmEuler = new Vector3(-2f, 0f, -3f),
                LowerArm1Euler = new Vector3(6f, 0f, -3f),
                LowerArm2Euler = new Vector3(5f, 0f, -2f),
                LowerArm3Euler = new Vector3(4f, 0f, -1f),
                HandEuler = new Vector3(9f, -12f, -10f),
                ThumbCurl = 0.3f,
                ThumbSplay = 0.16f,
                ThumbRoll = -6f,
                IndexCurl = 0.36f,
                MiddleCurl = 0.42f,
                RingCurl = 0.46f,
                PinkyCurl = 0.5f,
                FingerSplay = 0.02f,
                ItemAnchorLocalPosition = new Vector3(0.01f, -0.012f, 0.042f),
                ItemAnchorLocalEuler = new Vector3(0f, 90f, 90f),
            },
        };
    }

    public static FirstPersonSingleHandSettings CreateDefaultRight()
    {
        return new FirstPersonSingleHandSettings
        {
            HiddenLocalPosition = new Vector3(0.28f, -0.56f, 0.18f),
            HiddenLocalEuler = new Vector3(38f, -10f, 26f),
            BobPhaseOffset = 0f,
            Open = new FirstPersonHandPoseDefinition
            {
                LocalPosition = Vector3.zero,
                LocalEuler = Vector3.zero,
                UpperArmEuler = Vector3.zero,
                LowerArm1Euler = Vector3.zero,
                LowerArm2Euler = Vector3.zero,
                LowerArm3Euler = Vector3.zero,
                HandEuler = Vector3.zero,
                ThumbCurl = 0f,
                ThumbSplay = 0f,
                ThumbRoll = 0f,
                IndexCurl = 0f,
                MiddleCurl = 0f,
                RingCurl = 0f,
                PinkyCurl = 0f,
                FingerSplay = 0f,
                ItemAnchorLocalPosition = new Vector3(0f, -0.012f, 0.03f),
                ItemAnchorLocalEuler = new Vector3(-8f, 0f, 90f),
            },
            LanternTop = new FirstPersonHandPoseDefinition
            {
                LocalPosition = Vector3.zero,
                LocalEuler = new Vector3(-2f, 0f, 6f),
                UpperArmEuler = new Vector3(-3f, 0f, 4f),
                LowerArm1Euler = new Vector3(8f, 0f, 4f),
                LowerArm2Euler = new Vector3(7f, 0f, 3f),
                LowerArm3Euler = new Vector3(5f, 0f, 2f),
                HandEuler = new Vector3(14f, 8f, 8f),
                ThumbCurl = 0.36f,
                ThumbSplay = -0.1f,
                ThumbRoll = 10f,
                IndexCurl = 0.42f,
                MiddleCurl = 0.5f,
                RingCurl = 0.54f,
                PinkyCurl = 0.48f,
                FingerSplay = -0.04f,
                ItemAnchorLocalPosition = new Vector3(0.002f, -0.018f, 0.04f),
                ItemAnchorLocalEuler = new Vector3(-70f, 0f, 0f),
            },
            TorchSide = new FirstPersonHandPoseDefinition
            {
                LocalPosition = Vector3.zero,
                LocalEuler = new Vector3(-1f, 3f, 8f),
                UpperArmEuler = new Vector3(-2f, 0f, 3f),
                LowerArm1Euler = new Vector3(6f, 0f, 3f),
                LowerArm2Euler = new Vector3(5f, 0f, 2f),
                LowerArm3Euler = new Vector3(4f, 0f, 1f),
                HandEuler = new Vector3(9f, 12f, 10f),
                ThumbCurl = 0.3f,
                ThumbSplay = -0.16f,
                ThumbRoll = 6f,
                IndexCurl = 0.36f,
                MiddleCurl = 0.42f,
                RingCurl = 0.46f,
                PinkyCurl = 0.5f,
                FingerSplay = -0.02f,
                ItemAnchorLocalPosition = new Vector3(-0.01f, -0.012f, 0.042f),
                ItemAnchorLocalEuler = new Vector3(0f, -90f, -90f),
            },
        };
    }
}

[Serializable]
public sealed class FirstPersonHandsSettings
{
    public bool Enabled = true;
    public FirstPersonHandStance LeftRaisedStance = FirstPersonHandStance.LanternTop;
    public FirstPersonHandStance RightRaisedStance = FirstPersonHandStance.TorchSide;
    public float RaiseSharpness = 14f;
    public float SwaySharpness = 14f;
    public float IdleBobAmplitude = 0.0035f;
    public float WalkBobAmplitude = 0.012f;
    public float SprintBobAmplitude = 0.02f;
    public float IdleBobFrequency = 0.55f;
    public float WalkBobFrequency = 0.65f;
    public float SprintBobFrequency = 1.3f;
    public float MovePositionSway = 0.024f;
    public float MoveRotationSway = 7f;
    public float LookPositionSway = 0.0012f;
    public float LookRotationSway = 0.06f;
    public float MaxLookRotationSway = 7f;
    public float MaxLookPositionSway = 0.03f;
    public FirstPersonSingleHandSettings Left = FirstPersonSingleHandSettings.CreateDefaultLeft();
    public FirstPersonSingleHandSettings Right = FirstPersonSingleHandSettings.CreateDefaultRight();
}

public sealed class FirstPersonHandsRig
{
    private const float FingerBaseCurlAngle = 65f;
    private const float FingerMidCurlAngle = 80f;
    private const float FingerTipCurlAngle = 55f;

    private sealed class RuntimeHand
    {
        public FirstPersonHandSide Side;
        public FirstPersonSingleHandSettings Settings;
        public Transform Anchor;
        public Transform Root;
        public Transform UpperArm;
        public Transform LowerArm1;
        public Transform LowerArm2;
        public Transform LowerArm3;
        public Transform Hand;
        public Transform Thumb1;
        public Transform Thumb2;
        public Transform Thumb3;
        public Transform Index1;
        public Transform Index2;
        public Transform Index3;
        public Transform Middle1;
        public Transform Middle2;
        public Transform Middle3;
        public Transform Ring1;
        public Transform Ring2;
        public Transform Ring3;
        public Transform Pinky1;
        public Transform Pinky2;
        public Transform Pinky3;
        public Transform Pinky4;
        public Transform ItemAnchor;
        public Renderer[] Renderers;
        public readonly Dictionary<Transform, Quaternion> BindRotations = new Dictionary<Transform, Quaternion>();
        public float RaiseCurrent;
        public float RaiseTarget;
        public Vector3 SwayPositionCurrent;
        public Vector3 SwayRotationCurrent;
        public FirstPersonHandStance CurrentStance;
    }

    private readonly List<RuntimeHand> hands = new List<RuntimeHand>();

    private FirstPersonHandsSettings settings;
    private Transform root;
    private float bobTime;
    private bool initialized;

    public bool IsInitialized => initialized;

    public void Initialize(Transform handParent, FirstPersonHandsSettings handSettings, GameObject leftPrefab, GameObject rightPrefab, int layer)
    {
        Dispose();

        if (handParent == null || handSettings == null || !handSettings.Enabled)
            return;

        settings = handSettings;

        root = new GameObject("FirstPersonHandsRoot").transform;
        root.SetParent(handParent, false);

        CreateHand(FirstPersonHandSide.Left, leftPrefab, settings.Left, layer);
        CreateHand(FirstPersonHandSide.Right, rightPrefab, settings.Right, layer);

        initialized = hands.Count > 0;
    }

    public void Dispose()
    {
        if (root != null)
            UnityEngine.Object.Destroy(root.gameObject);

        hands.Clear();
        root = null;
        settings = null;
        bobTime = 0f;
        initialized = false;
    }

    public void SetRaiseInput(FirstPersonHandSide side, bool isRaised)
    {
        RuntimeHand hand = GetHand(side);
        if (hand == null)
            return;

        hand.RaiseTarget = isRaised ? 1f : 0f;
    }

    public void SetStance(FirstPersonHandSide side, FirstPersonHandStance stance)
    {
        RuntimeHand hand = GetHand(side);
        if (hand == null)
            return;

        hand.CurrentStance = stance;
    }

    public Transform GetItemAnchor(FirstPersonHandSide side)
    {
        RuntimeHand hand = GetHand(side);
        return hand != null ? hand.ItemAnchor : null;
    }

    public void Tick(float deltaTime, Vector2 moveInput, Vector2 lookInput, float planarSpeed01, bool grounded, bool isSprinting)
    {
        if (!initialized || settings == null)
            return;

        float bobFrequency = GetBobFrequency(planarSpeed01, grounded, isSprinting);
        bobTime += deltaTime * bobFrequency * 2f * Mathf.PI;

        for (int i = 0; i < hands.Count; i++)
        {
            RuntimeHand hand = hands[i];
            hand.RaiseCurrent = Damp(hand.RaiseCurrent, hand.RaiseTarget, settings.RaiseSharpness, deltaTime);

            FirstPersonHandPoseDefinition pose = GetPoseDefinition(hand);
            UpdateSway(hand, moveInput, lookInput, planarSpeed01, grounded, isSprinting, deltaTime);
            ApplyAnchor(hand, pose);
            ApplyPose(hand, pose);
            ApplyRendererState(hand);
        }
    }

    private void CreateHand(FirstPersonHandSide side, GameObject prefab, FirstPersonSingleHandSettings singleHandSettings, int layer)
    {
        if (prefab == null || root == null || singleHandSettings == null)
            return;

        Transform anchor = new GameObject(side + "HandAnchor").transform;
        anchor.SetParent(root, false);
        anchor.localPosition = new Vector3(0f, -0.18f, -0.25f);
        anchor.localRotation = Quaternion.identity;

        GameObject instance = UnityEngine.Object.Instantiate(prefab, anchor);
        instance.name = side + "Hand";
        SetLayerRecursive(instance.transform, layer);

        var visualizeJoints = instance.GetComponentsInChildren<VisualizeJoint>(true);
        for (int i = 0; i < visualizeJoints.Length; i++)
            UnityEngine.Object.Destroy(visualizeJoints[i]);

        RuntimeHand hand = new RuntimeHand
        {
            Side = side,
            Settings = singleHandSettings,
            Anchor = anchor,
            Root = instance.transform,
            CurrentStance = side == FirstPersonHandSide.Left ? settings.LeftRaisedStance : settings.RightRaisedStance,
            Renderers = instance.GetComponentsInChildren<Renderer>(true),
        };

        foreach (Renderer renderer in hand.Renderers)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                skinnedMeshRenderer.updateWhenOffscreen = true;

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        string suffix = side == FirstPersonHandSide.Left ? "_l" : "_r";
        hand.UpperArm = Find(instance.transform, "upperarm" + suffix);
        hand.LowerArm1 = Find(instance.transform, "lowerarm1" + suffix);
        hand.LowerArm2 = Find(instance.transform, "lowerarm2" + suffix);
        hand.LowerArm3 = Find(instance.transform, "lowerarm3" + suffix);
        hand.Hand = Find(instance.transform, "hand" + suffix) ?? Find(instance.transform, "hand_" + (side == FirstPersonHandSide.Left ? "left" : "right"));
        hand.Thumb1 = Find(instance.transform, "thumb1" + suffix);
        hand.Thumb2 = Find(instance.transform, "thumb2" + suffix);
        hand.Thumb3 = Find(instance.transform, "thumb3" + suffix);
        hand.Index1 = Find(instance.transform, "index1" + suffix);
        hand.Index2 = Find(instance.transform, "index2" + suffix);
        hand.Index3 = Find(instance.transform, "index3" + suffix);
        hand.Middle1 = Find(instance.transform, "middle1" + suffix);
        hand.Middle2 = Find(instance.transform, "middle2" + suffix);
        hand.Middle3 = Find(instance.transform, "middle3" + suffix);
        hand.Ring1 = Find(instance.transform, "ring1" + suffix);
        hand.Ring2 = Find(instance.transform, "ring2" + suffix);
        hand.Ring3 = Find(instance.transform, "ring3" + suffix);
        hand.Pinky1 = Find(instance.transform, "pinky1" + suffix);
        hand.Pinky2 = Find(instance.transform, "pinky2" + suffix);
        hand.Pinky3 = Find(instance.transform, "pinky3" + suffix);
        hand.Pinky4 = Find(instance.transform, "pinky4" + suffix);

        if (hand.Hand != null)
        {
            hand.ItemAnchor = new GameObject(side + "HandItemAnchor").transform;
            hand.ItemAnchor.SetParent(hand.Hand, false);
        }

        CacheBindRotation(hand, hand.UpperArm);
        CacheBindRotation(hand, hand.LowerArm1);
        CacheBindRotation(hand, hand.LowerArm2);
        CacheBindRotation(hand, hand.LowerArm3);
        CacheBindRotation(hand, hand.Hand);
        CacheBindRotation(hand, hand.Thumb1);
        CacheBindRotation(hand, hand.Thumb2);
        CacheBindRotation(hand, hand.Thumb3);
        CacheBindRotation(hand, hand.Index1);
        CacheBindRotation(hand, hand.Index2);
        CacheBindRotation(hand, hand.Index3);
        CacheBindRotation(hand, hand.Middle1);
        CacheBindRotation(hand, hand.Middle2);
        CacheBindRotation(hand, hand.Middle3);
        CacheBindRotation(hand, hand.Ring1);
        CacheBindRotation(hand, hand.Ring2);
        CacheBindRotation(hand, hand.Ring3);
        CacheBindRotation(hand, hand.Pinky1);
        CacheBindRotation(hand, hand.Pinky2);
        CacheBindRotation(hand, hand.Pinky3);
        CacheBindRotation(hand, hand.Pinky4);

        hands.Add(hand);
        ApplyRendererState(hand);
    }

    private void UpdateSway(RuntimeHand hand, Vector2 moveInput, Vector2 lookInput, float planarSpeed01, bool grounded, bool isSprinting, float deltaTime)
    {
        float strafe = moveInput.x;
        float forward = moveInput.y;
        bool hasMoveInput = Mathf.Abs(forward) > 0.01f || Mathf.Abs(strafe) > 0.01f;
        int bobState = GetBobState(planarSpeed01, grounded, isSprinting);
        float speedWeight = grounded ? Mathf.Lerp(0.25f, 1f, planarSpeed01) : Mathf.Lerp(0.1f, 0.55f, planarSpeed01);
        float sideSign = hand.Side == FirstPersonHandSide.Left ? -1f : 1f;

        float phase = bobTime + hand.Settings.BobPhaseOffset;
        float bobAmplitude = GetBobAmplitude(planarSpeed01, grounded, hasMoveInput, speedWeight, bobState);
        Vector3 bobOffset = Vector3.zero;
        if (grounded || planarSpeed01 > 0.01f)
        {
            float strideWave = Mathf.Sin(phase);
            float arcLift = 1f - (strideWave * strideWave);

            bobOffset = new Vector3(
                strideWave * bobAmplitude * 0.75f,
                (arcLift - 0.5f) * bobAmplitude * 1.15f,
                -Mathf.Abs(strideWave) * bobAmplitude * 0.18f);
            bobOffset *= grounded ? 1f : 0.6f;
        }

        float lookX = Mathf.Clamp(lookInput.x * settings.LookPositionSway, -settings.MaxLookPositionSway, settings.MaxLookPositionSway);
        float lookY = Mathf.Clamp(lookInput.y * settings.LookPositionSway, -settings.MaxLookPositionSway, settings.MaxLookPositionSway);

        Vector3 targetPosition = bobOffset + new Vector3(
            strafe * settings.MovePositionSway * 0.55f * sideSign,
            Mathf.Abs(forward) * settings.MovePositionSway * -0.18f,
            Mathf.Abs(forward) * settings.MovePositionSway * 0.35f);
        targetPosition += new Vector3(
            -lookX * 0.65f,
            -lookY * 0.45f,
            Mathf.Abs(lookX) * 0.25f);

        float lookYaw = Mathf.Clamp(lookInput.x * settings.LookRotationSway, -settings.MaxLookRotationSway, settings.MaxLookRotationSway);
        float lookPitch = Mathf.Clamp(lookInput.y * settings.LookRotationSway, -settings.MaxLookRotationSway, settings.MaxLookRotationSway);
        Vector3 targetEuler = new Vector3(
            -lookPitch * 0.8f,
            -lookYaw * 0.5f,
            sideSign * (lookYaw * 0.85f + strafe * settings.MoveRotationSway + Mathf.Sin(phase) * settings.MoveRotationSway * 0.12f * speedWeight));
        targetEuler += new Vector3(
            forward * settings.MoveRotationSway * -0.35f,
            strafe * settings.MoveRotationSway * 0.4f,
            0f);

        hand.SwayPositionCurrent = Damp(hand.SwayPositionCurrent, targetPosition, settings.SwaySharpness, deltaTime);
        hand.SwayRotationCurrent = Damp(hand.SwayRotationCurrent, targetEuler, settings.SwaySharpness, deltaTime);
    }

    private float GetBobFrequency(float planarSpeed01, bool grounded, bool isSprinting)
    {
        switch (GetBobState(planarSpeed01, grounded, isSprinting))
        {
            case 2:
                return settings.SprintBobFrequency;
            case 1:
                return settings.WalkBobFrequency;
            default:
                return settings.IdleBobFrequency;
        }
    }

    private float GetBobAmplitude(float planarSpeed01, bool grounded, bool hasMoveInput, float speedWeight, int bobState)
    {
        if (bobState <= 0 || !hasMoveInput)
            return settings.IdleBobAmplitude;

        if (bobState == 2)
            return settings.SprintBobAmplitude;

        return Mathf.Lerp(settings.IdleBobAmplitude, settings.WalkBobAmplitude, Mathf.Clamp01((speedWeight - 0.25f) / 0.75f));
    }

    private int GetBobState(float planarSpeed01, bool grounded, bool isSprinting)
    {
        if (!grounded || planarSpeed01 <= 0.01f)
            return 0;

        return isSprinting ? 2 : 1;
    }

    private void ApplyAnchor(RuntimeHand hand, FirstPersonHandPoseDefinition pose)
    {
        hand.Anchor.localPosition = new Vector3(0f, -0.18f, -0.25f);
        hand.Anchor.localRotation = Quaternion.identity;

        Vector3 localPosition = Vector3.Lerp(hand.Settings.HiddenLocalPosition, pose.LocalPosition, hand.RaiseCurrent);
        localPosition += hand.SwayPositionCurrent * hand.RaiseCurrent;

        Quaternion hiddenRotation = Quaternion.Euler(hand.Settings.HiddenLocalEuler);
        Quaternion poseRotation = Quaternion.Euler(pose.LocalEuler + hand.SwayRotationCurrent);
        Quaternion localRotation = Quaternion.Slerp(hiddenRotation, poseRotation, hand.RaiseCurrent);

        if (hand.Root != null)
        {
            hand.Root.localPosition = localPosition;
            hand.Root.localRotation = localRotation;
        }

        if (hand.ItemAnchor != null)
        {
            hand.ItemAnchor.localPosition = pose.ItemAnchorLocalPosition;
            hand.ItemAnchor.localRotation = Quaternion.Euler(pose.ItemAnchorLocalEuler);
        }
    }

    private void ApplyPose(RuntimeHand hand, FirstPersonHandPoseDefinition pose)
    {
        ApplyBone(hand, hand.UpperArm, pose.UpperArmEuler);
        ApplyBone(hand, hand.LowerArm1, pose.LowerArm1Euler);
        ApplyBone(hand, hand.LowerArm2, pose.LowerArm2Euler);
        ApplyBone(hand, hand.LowerArm3, pose.LowerArm3Euler);
        ApplyBone(hand, hand.Hand, pose.HandEuler);

        float sideSign = hand.Side == FirstPersonHandSide.Left ? 1f : -1f;

        ApplyThumb(hand, pose, sideSign);
        ApplyFinger(hand, hand.Index1, hand.Index2, hand.Index3, pose.IndexCurl, pose.FingerSplay * 1.0f * sideSign);
        ApplyFinger(hand, hand.Middle1, hand.Middle2, hand.Middle3, pose.MiddleCurl, pose.FingerSplay * 0.25f * sideSign);
        ApplyFinger(hand, hand.Ring1, hand.Ring2, hand.Ring3, pose.RingCurl, pose.FingerSplay * -0.2f * sideSign);
        ApplyFinger(hand, hand.Pinky1, hand.Pinky2, hand.Pinky3, pose.PinkyCurl, pose.FingerSplay * -0.75f * sideSign);
        if (hand.Pinky4 != null)
            hand.Pinky4.localRotation = hand.BindRotations[hand.Pinky4] * Quaternion.AngleAxis(Mathf.Lerp(0f, FingerTipCurlAngle * 0.75f, pose.PinkyCurl * hand.RaiseCurrent), Vector3.forward);
    }

    private void ApplyThumb(RuntimeHand hand, FirstPersonHandPoseDefinition pose, float sideSign)
    {
        float poseWeight = hand.RaiseCurrent;
        if (hand.Thumb1 == null || hand.Thumb2 == null || hand.Thumb3 == null)
            return;

        hand.Thumb1.localRotation = hand.BindRotations[hand.Thumb1] * Quaternion.Euler(
            pose.ThumbRoll * poseWeight,
            pose.ThumbSplay * 20f * poseWeight,
            pose.ThumbCurl * 34f * poseWeight);
        hand.Thumb2.localRotation = hand.BindRotations[hand.Thumb2] * Quaternion.Euler(
            0f,
            pose.ThumbSplay * 12f * poseWeight * sideSign,
            pose.ThumbCurl * 36f * poseWeight);
        hand.Thumb3.localRotation = hand.BindRotations[hand.Thumb3] * Quaternion.Euler(
            0f,
            pose.ThumbSplay * 8f * poseWeight * sideSign,
            pose.ThumbCurl * 28f * poseWeight);
    }

    private void ApplyFinger(RuntimeHand hand, Transform first, Transform second, Transform third, float curl, float splay)
    {
        if (first == null || second == null || third == null)
            return;

        float poseWeight = hand.RaiseCurrent;
        first.localRotation = hand.BindRotations[first] * Quaternion.Euler(0f, splay * 16f * poseWeight, curl * FingerBaseCurlAngle * poseWeight);
        second.localRotation = hand.BindRotations[second] * Quaternion.AngleAxis(curl * FingerMidCurlAngle * poseWeight, Vector3.forward);
        third.localRotation = hand.BindRotations[third] * Quaternion.AngleAxis(curl * FingerTipCurlAngle * poseWeight, Vector3.forward);
    }

    private void ApplyBone(RuntimeHand hand, Transform bone, Vector3 euler)
    {
        if (bone == null || !hand.BindRotations.TryGetValue(bone, out Quaternion bindRotation))
            return;

        bone.localRotation = Quaternion.Slerp(bindRotation, bindRotation * Quaternion.Euler(euler), hand.RaiseCurrent);
    }

    private void ApplyRendererState(RuntimeHand hand)
    {
        bool isVisible = hand.RaiseCurrent > 0.01f;
        if (hand.Renderers == null)
            return;

        for (int i = 0; i < hand.Renderers.Length; i++)
            hand.Renderers[i].enabled = isVisible;
    }

    private FirstPersonHandPoseDefinition GetPoseDefinition(RuntimeHand hand)
    {
        switch (hand.CurrentStance)
        {
            case FirstPersonHandStance.LanternTop:
                return hand.Settings.LanternTop;
            case FirstPersonHandStance.TorchSide:
                return hand.Settings.TorchSide;
            default:
                return hand.Settings.Open;
        }
    }

    private RuntimeHand GetHand(FirstPersonHandSide side)
    {
        for (int i = 0; i < hands.Count; i++)
        {
            if (hands[i].Side == side)
                return hands[i];
        }

        return null;
    }

    private static Transform Find(Transform rootTransform, string name)
    {
        if (rootTransform == null || string.IsNullOrWhiteSpace(name))
            return null;

        foreach (Transform child in rootTransform.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase))
                return child;
        }

        return null;
    }

    private static void CacheBindRotation(RuntimeHand hand, Transform bone)
    {
        if (bone == null || hand.BindRotations.ContainsKey(bone))
            return;

        hand.BindRotations.Add(bone, bone.localRotation);
    }

    private static void SetLayerRecursive(Transform transformRoot, int layer)
    {
        if (transformRoot == null)
            return;

        foreach (Transform child in transformRoot.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }

    private static float Damp(float current, float target, float sharpness, float deltaTime)
    {
        return Mathf.Lerp(current, target, 1f - Mathf.Exp(-sharpness * deltaTime));
    }

    private static Vector3 Damp(Vector3 current, Vector3 target, float sharpness, float deltaTime)
    {
        return Vector3.Lerp(current, target, 1f - Mathf.Exp(-sharpness * deltaTime));
    }
}
