using System.Collections.Generic;
using UnityEngine;

public class IntroCinematicCar : MonoBehaviour
{
    [Header("Path Points")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool isMoving = true;

    [Header("Suspension Bob")]
    [SerializeField] private bool useSuspensionBob = true;
    [SerializeField] private Transform bobTarget;
    [SerializeField, Min(0f)] private float bobAmplitude = 0.05f;
    [SerializeField, Min(0.01f)] private float bobFrequency = 1.2f;
    [SerializeField, Range(0f, 1f)] private float secondaryBobInfluence = 0.35f;
    [SerializeField, Min(0.01f)] private float secondaryBobFrequency = 2.8f;
    [SerializeField, Min(0.001f)] private float bobSmoothing = 0.08f;
    [SerializeField] private float bobNoiseOffset = 17.31f;

    [Header("Wheels")]
    [SerializeField] private List<GameObject> wheels = new List<GameObject>();
    [SerializeField] private float wheelRotationSpeed = 360f;

    private AudioSource audioSource;

    private Vector3 basePosition;
    private float distanceTravelled;
    private float currentBobOffset;
    private float bobVelocity;
    private Vector3 bobTargetBaseLocalPosition;

    public bool IsMoving
    {
        get => isMoving;
        set => isMoving = value;
    }

    public void StartCar()
    {
        isMoving = true;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (bobTarget == null)
        {
            bobTarget = transform;
        }

        bobTargetBaseLocalPosition = bobTarget.localPosition;

        if (startPoint != null)
        {
            basePosition = startPoint.position;
        }
        else
        {
            basePosition = transform.position;
        }

        currentBobOffset = EvaluateBobOffset(distanceTravelled);
        ApplyPositions();
    }

    private void Update()
    {
        UpdateAudio();

        if (!isMoving || startPoint == null || endPoint == null)
            return;

        MoveCar();
        RotateWheels();
    }

    private void UpdateAudio()
    {
        if (audioSource == null)
            return;

        audioSource.volume = Game.Settings.MasterVolume * 1f;

        if (isMoving)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    private void MoveCar()
    {
        Vector3 previousBasePosition = basePosition;

        basePosition = Vector3.MoveTowards(
            basePosition,
            endPoint.position,
            moveSpeed * Time.deltaTime
        );

        distanceTravelled += Vector3.Distance(previousBasePosition, basePosition);

        if (Vector3.Distance(basePosition, endPoint.position) <= 0.001f)
        {
            ResetToStart();
            return;
        }

        UpdateSuspension();
        ApplyPositions();
    }

    private void ResetToStart()
    {
        basePosition = startPoint.position;
        distanceTravelled = 0f;
        bobVelocity = 0f;
        currentBobOffset = EvaluateBobOffset(distanceTravelled);
        ApplyPositions();
    }

    private void UpdateSuspension()
    {
        float targetBobOffset = EvaluateBobOffset(distanceTravelled);
        currentBobOffset = Mathf.SmoothDamp(
            currentBobOffset,
            targetBobOffset,
            ref bobVelocity,
            bobSmoothing
        );
    }

    private float EvaluateBobOffset(float travelledDistance)
    {
        if (!useSuspensionBob || bobAmplitude <= 0f)
            return 0f;

        float primaryNoise = Mathf.PerlinNoise(
            bobNoiseOffset + travelledDistance * bobFrequency,
            0f
        ) * 2f - 1f;

        float secondaryNoise = Mathf.PerlinNoise(
            0f,
            bobNoiseOffset + 53.7f + travelledDistance * secondaryBobFrequency
        ) * 2f - 1f;

        float combinedNoise = primaryNoise + (secondaryNoise * secondaryBobInfluence);
        float normalization = 1f + secondaryBobInfluence;

        return (combinedNoise / normalization) * bobAmplitude;
    }

    private void ApplyPositions()
    {
        transform.position = basePosition;

        if (bobTarget != null)
        {
            bobTarget.localPosition = bobTargetBaseLocalPosition + (Vector3.up * currentBobOffset);
        }
    }

    private void RotateWheels()
    {
        float rotationAmount = wheelRotationSpeed * Time.deltaTime;

        foreach (GameObject wheel in wheels)
        {
            if (wheel == null)
                continue;

            wheel.transform.Rotate(rotationAmount, 0f, 0f, Space.Self);
        }
    }
}