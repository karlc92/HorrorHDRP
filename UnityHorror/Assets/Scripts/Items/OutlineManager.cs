using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds the current set of renderers that should be outlined.
///
/// The actual outline rendering is performed by a HDRP Custom Pass (see SilhouetteOutlineCustomPass).
/// </summary>
public sealed class OutlineManager : MonoBehaviour
{
    public static OutlineManager Instance { get; private set; }

    [Header("Outline Look")]
    [ColorUsage(false, true)]
    [SerializeField] Color outlineColor = Color.yellow;

    // World-space expansion along vertex normals.
    // Keep this small (e.g. 0.005 - 0.03) depending on your scale.
    [SerializeField, Min(0f)] float outlineThickness = 0.015f;

    Renderer[] targetRenderers = System.Array.Empty<Renderer>();
    Interactable currentTarget;

    readonly List<Renderer> rendererBuffer = new List<Renderer>(32);

    public Color OutlineColor => outlineColor;
    public float OutlineThickness => outlineThickness;

    /// <summary>Returns the current outlined renderers (may be empty).</summary>
    public Renderer[] TargetRenderers => targetRenderers;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetOutlineColor(Color color) => outlineColor = color;
    public void SetOutlineThickness(float thickness) => outlineThickness = Mathf.Max(0f, thickness);

    /// <summary>
    /// Sets the outlined target to the given interactable.
    /// Calling this repeatedly with the same target is cheap.
    /// </summary>
    public void SetTarget(Interactable interactable)
    {
        if (interactable == currentTarget)
            return;

        currentTarget = interactable;

        if (currentTarget == null)
        {
            targetRenderers = System.Array.Empty<Renderer>();
            return;
        }

        // Only outline actual mesh renderers (most reliable for silhouette hull outlines).
        rendererBuffer.Clear();

        // Include inactive children so interactables can outline even if some parts are disabled at runtime.
        // The custom pass will still skip disabled Renderer components.
        currentTarget.GetComponentsInChildren(true, rendererBuffer);

        // Filter to MeshRenderer / SkinnedMeshRenderer.
        for (int i = rendererBuffer.Count - 1; i >= 0; i--)
        {
            Renderer r = rendererBuffer[i];
            if (r == null)
            {
                rendererBuffer.RemoveAt(i);
                continue;
            }

            bool supported = r is MeshRenderer || r is SkinnedMeshRenderer;
            if (!supported)
                rendererBuffer.RemoveAt(i);
        }

        targetRenderers = rendererBuffer.ToArray();
    }

    public void ClearTarget()
    {
        currentTarget = null;
        targetRenderers = System.Array.Empty<Renderer>();
    }
}