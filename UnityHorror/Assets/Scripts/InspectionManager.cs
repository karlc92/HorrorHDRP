using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public sealed class InspectionManager : MonoBehaviour
{
    public static InspectionManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] RawImage previewRawImage;
    [SerializeField] TMP_Text descriptionText;

    [Header("Rig")]
    [SerializeField] Camera inspectionCamera;
    [SerializeField] Transform inspectPivot;

    [Header("Layer")]
    [SerializeField] int inspectionLayer = 0; // set to your "Inspection" layer index in Inspector

    [Header("Interaction")]
    [SerializeField] float rotateSpeed = 0.18f;
    [SerializeField] float pitchMin = -70f;
    [SerializeField] float pitchMax = 70f;
    [SerializeField] float zoomSpeed = 0.35f;
    [SerializeField] float minDistance = 0.25f;
    [SerializeField] float maxDistance = 3.0f;

    [Header("Fitting")]
    [SerializeField] float fitPadding = 1.15f;

    GameObject currentInstance;
    InspectableItem currentItem;

    float yaw;
    float pitch;

    float cameraDistance;
    Vector3 cameraLocalDir = new Vector3(0, 0, -1); // camera looks at pivot from -Z by default

    readonly List<Renderer> rendererBuffer = new List<Renderer>(64);

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (inspectionCamera != null)
            inspectionCamera.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Open(InspectableItem item)
    {
        if (item == null || item.InspectPrefab == null)
            return;

        Close(); // close any existing inspection

        currentItem = item;

        // UI
        if (panelRoot != null) panelRoot.SetActive(true);
        if (descriptionText != null) descriptionText.text = item.Description ?? "";

        // Spawn
        currentInstance = Instantiate(item.InspectPrefab, inspectPivot);
        SetLayerRecursively(currentInstance, inspectionLayer);

        // Reset pivot rotation
        yaw = 0f;
        pitch = 0f;
        inspectPivot.localRotation = Quaternion.identity;

        // Fit to camera
        FitToCamera(currentInstance, item.InspectScaleMultiplier);

        // Enable camera only while inspecting (perf)
        if (inspectionCamera != null)
            inspectionCamera.gameObject.SetActive(true);

        // Optional: keep cursor locked for consistent mouse delta
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Close()
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);
            currentInstance = null;
        }

        if (inspectionCamera != null)
            inspectionCamera.gameObject.SetActive(false);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (currentItem != null)
        {
            currentItem.NotifyInspectionClosed();
            currentItem = null;
        }
    }

    void Update()
    {
        if (!IsOpen)
            return;

        // Close on Esc
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        // Rotate on LMB drag (works fine with locked cursor)
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            yaw += delta.x * rotateSpeed;
            pitch = Mathf.Clamp(pitch - delta.y * rotateSpeed, pitchMin, pitchMax);

            inspectPivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // Zoom on wheel
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                cameraDistance = Mathf.Clamp(cameraDistance - scroll * zoomSpeed * 0.001f, minDistance, maxDistance);
                UpdateCameraTransform();
            }
        }
    }

    void FitToCamera(GameObject go, float scaleMultiplier)
    {
        if (inspectionCamera == null || inspectPivot == null)
            return;

        // Calculate bounds
        rendererBuffer.Clear();
        go.GetComponentsInChildren(true, rendererBuffer);

        if (rendererBuffer.Count == 0)
        {
            cameraDistance = Mathf.Clamp(1.0f, minDistance, maxDistance);
            UpdateCameraTransform();
            return;
        }

        Bounds b = rendererBuffer[0].bounds;
        for (int i = 1; i < rendererBuffer.Count; i++)
            b.Encapsulate(rendererBuffer[i].bounds);

        // Move object so bounds center sits at pivot origin
        // Convert world bounds center to pivot local space:
        Vector3 centerWS = b.center;
        Vector3 centerLS = inspectPivot.InverseTransformPoint(centerWS);

        go.transform.localPosition -= centerLS;

        // Apply optional scale multiplier
        if (!Mathf.Approximately(scaleMultiplier, 1f))
            go.transform.localScale *= scaleMultiplier;

        // Recompute bounds after moving/scaling (cheap enough for inspection open)
        rendererBuffer.Clear();
        go.GetComponentsInChildren(true, rendererBuffer);

        b = rendererBuffer[0].bounds;
        for (int i = 1; i < rendererBuffer.Count; i++)
            b.Encapsulate(rendererBuffer[i].bounds);

        float radius = b.extents.magnitude * fitPadding;

        // Distance to fit bounding sphere in camera
        float fovRad = inspectionCamera.fieldOfView * Mathf.Deg2Rad;
        float dist = radius / Mathf.Tan(fovRad * 0.5f);

        cameraDistance = Mathf.Clamp(dist, minDistance, maxDistance);
        UpdateCameraTransform();
    }

    void UpdateCameraTransform()
    {
        if (inspectionCamera == null || inspectPivot == null)
            return;

        // Place camera at pivot + direction * distance
        inspectionCamera.transform.position = inspectPivot.position + (inspectPivot.TransformDirection(cameraLocalDir) * cameraDistance);
        inspectionCamera.transform.LookAt(inspectPivot.position, Vector3.up);
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        Transform t = go.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i).gameObject, layer);
    }
}