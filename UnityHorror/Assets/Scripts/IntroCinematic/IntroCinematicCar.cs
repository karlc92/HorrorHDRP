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

    [Header("Wheels")]
    [SerializeField] private List<GameObject> wheels = new List<GameObject>();
    [SerializeField] private float wheelRotationSpeed = 360f;

    private AudioSource audioSource;

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
        if (startPoint != null)
        {
            transform.position = startPoint.position;
        }

        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (isMoving && !audioSource.isPlaying)
        {
            audioSource.volume = Game.Settings.MasterVolume * 1f;
            audioSource.Play();
        }
        if (!isMoving || startPoint == null || endPoint == null)
            return;

        MoveCar();
        RotateWheels();
    }

    private void MoveCar()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            endPoint.position,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, endPoint.position) <= 0.001f)
        {
            transform.position = startPoint.position;
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